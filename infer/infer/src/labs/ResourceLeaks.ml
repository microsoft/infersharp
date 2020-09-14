(*
 * Copyright (c) 2017-present, Facebook, Inc.
 * Portions Copyright (c) Microsoft Corporation.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
module F = Format
module L = Logging
module Hashtbl = Caml.Hashtbl

let leak_list = ref []

let type_map = ref (Hashtbl.create 100)

(* Boilerplate to write/read our summaries alongside the summaries of other analyzers *)
module Payload = SummaryPayload.Make (struct
  type t = ResourceLeakDomain.summary

  let update_payloads resources_payload (payloads : Payloads.t) =
    {payloads with lab_resource_leaks= Some resources_payload}


  let of_payloads {Payloads.lab_resource_leaks} = lab_resource_leaks
end)

module TransferFunctions (CFG : ProcCfg.S) = struct
  module CFG = CFG
  module Domain = ResourceLeakDomain

  type extras = unit

  let is_closeable_typename tenv typename =
    let is_closable_interface typename _ =
      match Typ.Name.name typename with
      | "java.io.AutoCloseable" | "java.io.Closeable" ->
          true
      | "System.IDisposable" ->
          true
      | _ ->
          false
    in
    PatternMatch.supertype_exists tenv is_closable_interface typename


  let is_closeable_procname tenv procname =
    match procname with
    | Typ.Procname.Java java_procname ->
        is_closeable_typename tenv
          (Typ.Name.Java.from_string (Typ.Procname.Java.get_class_name java_procname))
    | Typ.Procname.CSharp csharp_procname ->
        is_closeable_typename tenv
          (Typ.Name.CSharp.from_string (Typ.Procname.CSharp.get_class_name csharp_procname))
    | _ ->
        false


  let acquires_resource tenv procname =
    (* We assume all constructors of a subclass of Closeable acquire a resource *)
    Typ.Procname.is_constructor procname && is_closeable_procname tenv procname


  let releases_resource tenv procname =
    (* We assume the close method of a Closeable releases all of its resources *)
    match procname with
    | Typ.Procname.CSharp csharp_procname ->
        (String.equal "Close" (Typ.Procname.get_method procname) ||  String.equal "Dispose" (Typ.Procname.get_method procname)) && is_closeable_procname tenv procname
    | _ ->
        String.equal "close" (Typ.Procname.get_method procname) && is_closeable_procname tenv procname

  (** Take an abstract state and instruction, produce a new abstract state *)
  let exec_instr (astate : ResourceLeakDomain.t) {ProcData.pdesc; tenv} _ (instr : HilInstr.t) =
    let assign_type_map = type_map := ResourceLeakDomain.get_type_map in
    assign_type_map ;
    let is_not_enumerable = 
      let contains s1 s2 =
        let re = Str.regexp_string s2
        in
        try ignore (Str.search_forward re s1 0); false
        with Not_found -> true
      in 
      contains (Typ.Procname.to_string (Procdesc.get_proc_name pdesc)) "IEnumerable" && contains (Typ.Procname.to_string (Procdesc.get_proc_name pdesc)) "Enumerator"
    in
    match instr with
    | Call (_return, Direct callee_procname, HilExp.AccessExpression allocated :: _, _, _loc)
      when acquires_resource tenv callee_procname && is_not_enumerable -> (
        let get_class_name =
          match callee_procname with
          | Typ.Procname.Java java_procname ->
            Typ.Procname.Java.get_class_name java_procname
          | Typ.Procname.CSharp csharp_procname ->
            Typ.Procname.CSharp.get_class_name csharp_procname
        in
        ResourceLeakDomain.acquire_resource
          (HilExp.AccessExpression.to_access_path allocated)
          (get_class_name)
          astate )
    | Call (_, Direct callee_procname, [actual], _, _loc)
      when releases_resource tenv callee_procname -> (
        match actual with
        | HilExp.AccessExpression access_expr ->
            ResourceLeakDomain.release_resource
              (HilExp.AccessExpression.to_access_path access_expr)
              astate 
        | _ ->
            astate )
    | Call (return, Direct callee_procname, actuals, _, _loc) -> (
      match Payload.read pdesc callee_procname with
      | Some summary ->
          (* interprocedural analysis produced a summary: use it *)
          ResourceLeakDomain.Summary.apply ~summary ~return ~actuals astate
      | None ->
          (* No summary for [callee_procname]; it's native code or missing for some reason *)
          astate )
    | Assign (access_expr, AccessExpression rhs_access_expr, _loc) -> 
        ResourceLeakDomain.assign
          (HilExp.AccessExpression.to_access_path access_expr)
          (HilExp.AccessExpression.to_access_path rhs_access_expr)
          astate 
    | Assign (lhs_access_path, rhs_exp, _loc) -> (
        match rhs_exp with
        | HilExp.AccessExpression access_expr -> 
            ResourceLeakDomain.assign
            (HilExp.AccessExpression.to_access_path lhs_access_path)
            (HilExp.AccessExpression.to_access_path access_expr)
            astate 
        | _ ->
          astate )
    | Assume (assume_exp, _, _, _loc) -> (
        (* a conditional assume([assume_exp]). blocks if [assume_exp] evaluates to false *)
        let rec extract_null_compare_expr expr = 
          match expr with
          | HilExp.Cast (_, e) ->
              extract_null_compare_expr e
          | HilExp.BinaryOperator (Binop.Eq, HilExp.AccessExpression access_expr, exp)
          | HilExp.BinaryOperator (Binop.Eq, exp, HilExp.AccessExpression access_expr)
          | HilExp.UnaryOperator
            ( Unop.LNot, ( HilExp.BinaryOperator (Binop.Ne, HilExp.AccessExpression access_expr, exp) ), _ )
          | HilExp.UnaryOperator
            ( Unop.LNot, ( HilExp.BinaryOperator (Binop.Ne, exp, HilExp.AccessExpression access_expr) ), _ ) -> 
              Option.some_if (HilExp.is_null_literal exp)
                (HilExp.AccessExpression.to_access_path access_expr)
          | _ ->
              None
        in
        match extract_null_compare_expr assume_exp with
        | Some ap ->
            ResourceLeakDomain.release_resource
              ap
              astate 
        | _ ->
            astate )
    | Call (_, Indirect _, _, _, _) ->
        (* This should never happen in Java. Fail if it does. *)
        L.(die InternalError) "Unexpected indirect call %a" HilInstr.pp instr
    | ExitScope _ ->
        astate


  let pp_session_name _node fmt = F.pp_print_string fmt "resource leaks"
end

(** 5(a) Type of CFG to analyze--Exceptional to follow exceptional control-flow edges, Normal to
   ignore them *)
module CFG = ProcCfg.Normal

(* Create an intraprocedural abstract interpreter from the transfer functions we defined *)
module Analyzer = LowerHil.MakeAbstractInterpreter (TransferFunctions (CFG))

(** Report an error when we have acquired more resources than we have released *)
let report_if_leak post summary formal_map (proc_data : unit ProcData.t) =
  if ResourceLeakDomain.has_leak formal_map post then
    let last_loc = Procdesc.Node.get_loc (Procdesc.get_exit_node proc_data.pdesc) in
    let message = 
      let concat_types = 
        Hashtbl.iter (fun x y -> 
          if ResourceLeakDomain.check_count x post then
            leak_list := ResourceLeakDomain.LeakList.append_one !leak_list y) !type_map
      in
      concat_types ;
      let concat_leak_list = 
        String.concat ~sep:", " !leak_list
      in
      F.asprintf "Leaked %a resource(s) at type(s) %s" ResourceLeakDomain.pp post concat_leak_list
    in
    ResourceLeakDomain.reset_type_map ;
    ResourceLeakDomain.Summary.reset_interface_type_map ;
    leak_list := [] ;
    Reporting.log_error summary ~loc:last_loc IssueType.resource_leak message
  else
    ResourceLeakDomain.reset_type_map ;
    ResourceLeakDomain.Summary.reset_interface_type_map


(* Callback for invoking the checker from the outside--registered in RegisterCheckers *)
let checker {Callbacks.summary; proc_desc; tenv} : Summary.t =
  let proc_data = ProcData.make proc_desc tenv () in
  match Analyzer.compute_post proc_data ~initial:ResourceLeakDomain.initial with
  | Some post ->
      let formal_map = FormalMap.make proc_desc in
      let procname = Procdesc.get_proc_name proc_data.pdesc in 
      if String.equal ".ctor" (Typ.Procname.get_method procname) then ()
      else
        report_if_leak post summary formal_map proc_data ;
        Payload.update_summary (ResourceLeakDomain.Summary.make formal_map post) summary
  | None ->
      L.(debug Analysis Medium)
        "Analyzer failed to compute post for %a" Typ.Procname.pp
        (Procdesc.get_proc_name proc_data.pdesc) ;
      summary
