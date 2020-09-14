(*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)
open! IStd
module L = Logging

(* Sources: Java Virtual Machine Specification 
   - Chapter 5. Loading, Linking and Initializing
   - Chapter 6. The Java Virtual Machine Instruction Set
*)

(* TODO 
  - catch / throw with exception classes 
*)

module Payload = SummaryPayload.Make (struct
  type t = ClassLoadsDomain.summary

  let update_payloads post (payloads : Payloads.t) = {payloads with class_loads= Some post}

  let of_payloads (payloads : Payloads.t) = payloads.class_loads
end)

let do_call pdesc callee loc init =
  Payload.read pdesc callee |> Option.fold ~init ~f:(ClassLoadsDomain.integrate_summary callee loc)


(** fully load a class given the typename *)
let rec load_class proc_desc tenv loc astate class_name =
  (* don't bother if class is already loaded *)
  if ClassLoadsDomain.mem_typename class_name astate then astate
  else
    (* load the class itself *)
    let astate1 = ClassLoadsDomain.add_typename loc astate class_name in
    (* load classes referenced by the class initializer *)
    let astate2 =
      let class_initializer = Typ.Procname.(Java (Java.get_class_initializer class_name)) in
      (* NB may recurse if we are in class init but the shortcircuiting above makes it a no-op *)
      do_call proc_desc class_initializer loc astate1
    in
    (* finally, recursively load all superclasses *)
    Tenv.lookup tenv class_name
    |> Option.value_map ~default:[] ~f:(fun tstruct -> tstruct.Typ.Struct.supers)
    |> List.fold ~init:astate2 ~f:(load_class proc_desc tenv loc)


let load_type proc_desc tenv loc (typ : Typ.t) astate =
  match typ with
  | {desc= Tstruct name} | {desc= Tptr ({desc= Tstruct name}, _)} ->
      load_class proc_desc tenv loc astate name
  | _ ->
      astate


let rec load_array proc_desc tenv loc (typ : Typ.t) astate =
  match typ with
  | {desc= Tarray {elt}} ->
      load_array proc_desc tenv loc elt astate
  | _ ->
      load_type proc_desc tenv loc typ astate


let rec add_loads_of_exp proc_desc tenv loc (exp : Exp.t) astate =
  match exp with
  | Const (Cclass class_ident) ->
      (* [X.class] expressions *)
      let class_str = Ident.name_to_string class_ident |> Mangled.from_string in
      let class_name = Typ.JavaClass class_str in
      load_class proc_desc tenv loc astate class_name
  | Sizeof {typ= {desc= Tarray {elt}}} ->
      (* anewarray / multinewarray *)
      load_array proc_desc tenv loc elt astate
  | Cast (_, e) | UnOp (_, e, _) | Exn e ->
      (* NB Cast is only used for primitive types *)
      add_loads_of_exp proc_desc tenv loc e astate
  | BinOp (_, e1, e2) ->
      add_loads_of_exp proc_desc tenv loc e1 astate |> add_loads_of_exp proc_desc tenv loc e2
  | Lfield (e, _, typ') ->
      (* getfield / getstatic / putfield / putstatic *)
      load_type proc_desc tenv loc typ' astate |> add_loads_of_exp proc_desc tenv loc e
  | Var _ | Const _ | Closure _ | Sizeof _ | Lindex _ | Lvar _ ->
      astate


let exec_call pdesc tenv callee args loc astate =
  match args with
  | [_; (Exp.Sizeof {typ}, _)] when Typ.Procname.equal callee BuiltinDecl.__instanceof ->
      (* this matches downcasts/instanceof and exception handlers *)
      load_type pdesc tenv loc typ astate
  | _ ->
      (* invokeinterface / invokespecial / invokestatic / invokevirtual / new *)
      List.fold args ~init:astate ~f:(fun acc (exp, _) -> add_loads_of_exp pdesc tenv loc exp acc)
      |> do_call pdesc callee loc


let exec_instr pdesc tenv astate _ (instr : Sil.instr) =
  match instr with
  | Call (_, Const (Cfun callee), args, loc, _) ->
      exec_call pdesc tenv callee args loc astate
  | Load (_, exp, _, loc) | Prune (exp, loc, _, _) ->
      (* NB the java frontend seems to always translate complex guards into a sequence of 
         instructions plus a prune on logical vars only.  So the below is only for completeness. *)
      add_loads_of_exp pdesc tenv loc exp astate
  | Store (e1, _, e2, loc) ->
      add_loads_of_exp pdesc tenv loc e1 astate |> add_loads_of_exp pdesc tenv loc e2
  | _ ->
      astate


let report_loads proc_desc summary astate =
  let report_load ({ClassLoadsDomain.Event.loc; elem} as event) =
    if String.is_prefix ~prefix:"java." elem then ()
    else
      let ltr = ClassLoadsDomain.Event.make_loc_trace event in
      let msg = Format.asprintf "Class %s loaded" elem in
      Reporting.log_warning summary ~loc ~ltr IssueType.class_load msg
  in
  let pname = Procdesc.get_proc_name proc_desc in
  Typ.Procname.get_class_name pname
  |> Option.iter ~f:(fun clazz ->
         let method_strname = Typ.Procname.get_method pname in
         let fullname = clazz ^ "." ^ method_strname in
         if String.Set.mem Config.class_loads_roots fullname then
           ClassLoadsDomain.iter report_load astate )


let analyze_procedure {Callbacks.proc_desc; tenv; summary} =
  let proc_name = Procdesc.get_proc_name proc_desc in
  L.debug Analysis Verbose "CL: ANALYZING %a@." Typ.Procname.pp proc_name ;
  let loc = Procdesc.get_loc proc_desc in
  (* load the method's class *)
  let init =
    Typ.Procname.get_class_type_name proc_name
    |> Option.fold ~init:ClassLoadsDomain.bottom ~f:(load_class proc_desc tenv loc)
  in
  let post = Procdesc.fold_instrs proc_desc ~init ~f:(exec_instr proc_desc tenv) in
  report_loads proc_desc summary post ;
  let result = Payload.update_summary post summary in
  L.debug Analysis Verbose "CL: FINISHED ANALYZING %a@." Typ.Procname.pp proc_name ;
  result
