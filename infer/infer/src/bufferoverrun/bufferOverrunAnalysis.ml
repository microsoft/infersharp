(*
 * Copyright (c) 2016-present, Programming Research Laboratory (ROPAS)
 *                             Seoul National University, Korea
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
open AbsLoc
open! AbstractDomain.Types
module BoUtils = BufferOverrunUtils
module Dom = BufferOverrunDomain
module L = Logging
module Models = BufferOverrunModels
module Sem = BufferOverrunSemantics

module Payload = SummaryPayload.Make (struct
  type t = BufferOverrunAnalysisSummary.t

  let update_payloads astate (payloads : Payloads.t) =
    {payloads with buffer_overrun_analysis= Some astate}


  let of_payloads (payloads : Payloads.t) = payloads.buffer_overrun_analysis
end)

type summary_and_formals = BufferOverrunAnalysisSummary.t * (Pvar.t * Typ.t) list

type get_proc_summary_and_formals = Typ.Procname.t -> summary_and_formals option

type extras = {get_proc_summary_and_formals: get_proc_summary_and_formals; oenv: Dom.OndemandEnv.t}

module CFG = ProcCfg.NormalOneInstrPerNode

module Init = struct
  let initial_state {ProcData.pdesc; tenv; extras= {oenv}} start_node =
    let try_decl_local =
      let pname = Procdesc.get_proc_name pdesc in
      let model_env =
        let node_hash = CFG.Node.hash start_node in
        let location = CFG.Node.loc start_node in
        let integer_type_widths = oenv.Dom.OndemandEnv.integer_type_widths in
        BoUtils.ModelEnv.mk_model_env pname ~node_hash location tenv integer_type_widths
      in
      fun (mem, inst_num) {ProcAttributes.name; typ} ->
        let loc = Loc.of_pvar (Pvar.mk name pname) in
        BoUtils.Exec.decl_local model_env (mem, inst_num) (loc, typ)
    in
    let mem = Dom.Mem.init oenv in
    let mem, _ = List.fold ~f:try_decl_local ~init:(mem, 1) (Procdesc.get_locals pdesc) in
    mem
end

module TransferFunctions = struct
  module CFG = CFG
  module Domain = Dom.Mem

  type nonrec extras = extras

  let instantiate_mem_reachable (ret_id, _) callee_formals callee_pname ~callee_exit_mem
      ({Dom.eval_locpath} as eval_sym_trace) mem location =
    let formal_locs =
      List.fold callee_formals ~init:PowLoc.bot ~f:(fun acc (formal, _) ->
          PowLoc.add (Loc.of_pvar formal) acc )
    in
    let copy_reachable_locs_from locs mem =
      let copy loc acc =
        Option.value_map (Dom.Mem.find_opt loc callee_exit_mem) ~default:acc ~f:(fun v ->
            let locs = PowLoc.subst_loc loc eval_locpath in
            let v = Dom.Val.subst v eval_sym_trace location in
            PowLoc.fold (fun loc acc -> Dom.Mem.add_heap loc v acc) locs acc )
      in
      let reachable_locs = Dom.Mem.get_reachable_locs_from callee_formals locs callee_exit_mem in
      PowLoc.fold copy (PowLoc.diff reachable_locs formal_locs) mem
    in
    let instantiate_ret_alias mem =
      let subst_loc l =
        Option.find_map (Loc.get_path l) ~f:(fun partial ->
            try
              let locs = eval_locpath partial in
              match PowLoc.is_singleton_or_more locs with
              | IContainer.Singleton loc ->
                  Some loc
              | _ ->
                  None
            with Caml.Not_found -> None )
      in
      let ret_alias =
        Option.find_map (Dom.Mem.find_ret_alias callee_exit_mem) ~f:(fun alias_target ->
            Dom.AliasTarget.loc_map alias_target ~f:subst_loc )
      in
      Option.value_map ret_alias ~default:mem ~f:(fun l -> Dom.Mem.load_alias ret_id l mem)
    in
    let ret_var = Loc.of_var (Var.of_id ret_id) in
    let ret_val =
      match Procdesc.load callee_pname with
      | Some callee_pdesc when Procdesc.has_added_return_param callee_pdesc ->
          Dom.Val.of_loc (Loc.of_pvar (Pvar.get_ret_param_pvar callee_pname))
      | _ ->
          Dom.Mem.find (Loc.of_pvar (Pvar.get_ret_pvar callee_pname)) callee_exit_mem
    in
    Dom.Mem.add_stack ret_var (Dom.Val.subst ret_val eval_sym_trace location) mem
    |> instantiate_ret_alias
    |> copy_reachable_locs_from (PowLoc.join formal_locs (Dom.Val.get_all_locs ret_val))


  let forget_ret_relation ret callee_pname mem =
    let ret_loc = Loc.of_pvar (Pvar.get_ret_pvar callee_pname) in
    let ret_var = Loc.of_var (Var.of_id (fst ret)) in
    Dom.Mem.forget_locs (PowLoc.add ret_loc (PowLoc.singleton ret_var)) mem


  let is_external pname =
    match pname with
    | Typ.Procname.Java java_pname ->
        Typ.Procname.Java.is_external java_pname
    | _ ->
        false


  let instantiate_mem :
         Tenv.t
      -> Typ.IntegerWidths.t
      -> Ident.t * Typ.t
      -> (Pvar.t * Typ.t) list
      -> Typ.Procname.t
      -> (Exp.t * Typ.t) list
      -> Dom.Mem.t
      -> BufferOverrunAnalysisSummary.t
      -> Location.t
      -> Dom.Mem.t =
   fun tenv integer_type_widths ret callee_formals callee_pname params caller_mem callee_exit_mem
       location ->
    let rel_subst_map =
      Sem.get_subst_map tenv integer_type_widths callee_formals params caller_mem callee_exit_mem
    in
    let eval_sym_trace =
      Sem.mk_eval_sym_trace integer_type_widths callee_formals params caller_mem
        ~mode:Sem.EvalNormal
    in
    let caller_mem =
      instantiate_mem_reachable ret callee_formals callee_pname ~callee_exit_mem eval_sym_trace
        caller_mem location
      |> forget_ret_relation ret callee_pname
    in
    Dom.Mem.instantiate_relation rel_subst_map ~caller:caller_mem ~callee:callee_exit_mem


  let exec_instr : Dom.Mem.t -> extras ProcData.t -> CFG.Node.t -> Sil.instr -> Dom.Mem.t =
   fun mem {pdesc; tenv; extras= {get_proc_summary_and_formals; oenv= {integer_type_widths}}} node
       instr ->
    match instr with
    | Load (id, _, _, _) when Ident.is_none id ->
        mem
    | Load (id, Exp.Lvar pvar, _, location) when Pvar.is_compile_constant pvar || Pvar.is_ice pvar
      -> (
      match Pvar.get_initializer_pname pvar with
      | Some callee_pname -> (
        match get_proc_summary_and_formals callee_pname with
        | Some (callee_mem, _) ->
            let v = Dom.Mem.find (Loc.of_pvar pvar) callee_mem in
            Dom.Mem.add_stack (Loc.of_id id) v mem
        | None ->
            L.d_printfln_escaped "/!\\ Unknown initializer of global constant %a" (Pvar.pp Pp.text)
              pvar ;
            Dom.Mem.add_unknown_from id ~callee_pname ~location mem )
      | None ->
          L.d_printfln_escaped "/!\\ Failed to get initializer name of global constant %a"
            (Pvar.pp Pp.text) pvar ;
          Dom.Mem.add_unknown id ~location mem )
    | Load (id, exp, typ, _) ->
        BoUtils.Exec.load_locs id typ (Sem.eval_locs exp mem) mem
    | Store (exp1, _, Const (Const.Cstr s), location) ->
        let locs = Sem.eval_locs exp1 mem in
        let model_env =
          let pname = Procdesc.get_proc_name pdesc in
          let node_hash = CFG.Node.hash node in
          BoUtils.ModelEnv.mk_model_env pname ~node_hash location tenv integer_type_widths
        in
        let do_alloc = not (Sem.is_stack_exp exp1 mem) in
        BoUtils.Exec.decl_string model_env ~do_alloc locs s mem
    | Store (exp1, typ, exp2, location) ->
        let locs = Sem.eval_locs exp1 mem in
        let v =
          Sem.eval integer_type_widths exp2 mem |> Dom.Val.add_assign_trace_elem location locs
        in
        let mem =
          let sym_exps =
            Dom.Relation.SymExp.of_exps
              ~get_int_sym_f:(Sem.get_sym_f integer_type_widths mem)
              ~get_offset_sym_f:(Sem.get_offset_sym_f integer_type_widths mem)
              ~get_size_sym_f:(Sem.get_size_sym_f integer_type_widths mem)
              exp2
          in
          Dom.Mem.store_relation locs sym_exps mem
        in
        let mem = Dom.Mem.update_mem locs v mem in
        let mem =
          if Language.curr_language_is Clang && Typ.is_char typ then
            BoUtils.Exec.set_c_strlen ~tgt:(Sem.eval integer_type_widths exp1 mem) ~src:v mem
          else mem
        in
        let mem =
          if not v.represents_multiple_values then
            match PowLoc.is_singleton_or_more locs with
            | IContainer.Singleton loc_v -> (
                let pname = Procdesc.get_proc_name pdesc in
                match Typ.Procname.get_method pname with
                | "__inferbo_empty" when Loc.is_return loc_v -> (
                  match Procdesc.get_pvar_formals pdesc with
                  | [(formal, _)] ->
                      let formal_v = Dom.Mem.find (Loc.of_pvar formal) mem in
                      Dom.Mem.store_empty_alias formal_v loc_v mem
                  | _ ->
                      assert false )
                | _ ->
                    Dom.Mem.store_simple_alias loc_v exp2 mem )
            | _ ->
                mem
          else mem
        in
        let mem = Dom.Mem.update_latest_prune ~updated_locs:locs exp1 exp2 mem in
        mem
    | Prune (exp, _, _, _) ->
        Sem.Prune.prune integer_type_widths exp mem
    | Call (((id, ret_typ) as ret), Const (Cfun callee_pname), params, location, _) -> (
        let mem = Dom.Mem.add_stack_loc (Loc.of_id id) mem in
        match Models.Call.dispatch tenv callee_pname params with
        | Some {Models.exec} ->
            let model_env =
              let node_hash = CFG.Node.hash node in
              BoUtils.ModelEnv.mk_model_env callee_pname ~node_hash location tenv
                integer_type_widths
            in
            exec model_env ~ret mem
        | None -> (
          match get_proc_summary_and_formals callee_pname with
          | Some (callee_exit_mem, callee_formals) ->
              instantiate_mem tenv integer_type_widths ret callee_formals callee_pname params mem
                callee_exit_mem location
          | None ->
              (* This may happen for procedures with a biabduction model too. *)
              L.d_printfln_escaped "/!\\ Unknown call to %a" Typ.Procname.pp callee_pname ;
              if is_external callee_pname then (
                L.(debug BufferOverrun Verbose)
                  "/!\\ External call to unknown  %a \n\n" Typ.Procname.pp callee_pname ;
                let callsite = CallSite.make callee_pname location in
                let path = Symb.SymbolPath.of_callsite ~ret_typ callsite in
                let loc = Loc.of_allocsite (Allocsite.make_symbol path) in
                let v = Dom.Mem.find loc mem in
                Dom.Mem.add_stack (Loc.of_id id) v mem )
              else Dom.Mem.add_unknown_from id ~callee_pname ~location mem ) )
    | Call ((id, _), fun_exp, _, location, _) ->
        let mem = Dom.Mem.add_stack_loc (Loc.of_id id) mem in
        L.d_printfln_escaped "/!\\ Call to non-const function %a" Exp.pp fun_exp ;
        Dom.Mem.add_unknown id ~location mem
    | ExitScope (dead_vars, _) ->
        Dom.Mem.remove_temps (List.filter_map dead_vars ~f:Var.get_ident) mem
    | Abstract _ | Nullify _ ->
        mem


  let pp_session_name node fmt = F.fprintf fmt "bufferoverrun %a" CFG.Node.pp_id (CFG.Node.id node)
end

module Analyzer = AbstractInterpreter.MakeWTO (TransferFunctions)

type invariant_map = Analyzer.invariant_map

type local_decls = PowLoc.t

type memory_summary = BufferOverrunAnalysisSummary.t

let extract_pre = Analyzer.extract_pre

let extract_post = Analyzer.extract_post

let extract_state = Analyzer.extract_state

let get_local_decls : Procdesc.t -> local_decls =
 fun proc_desc ->
  let proc_name = Procdesc.get_proc_name proc_desc in
  let accum_local_decls acc {ProcAttributes.name} =
    let pvar = Pvar.mk name proc_name in
    let loc = Loc.of_pvar pvar in
    PowLoc.add loc acc
  in
  Procdesc.get_locals proc_desc |> List.fold ~init:PowLoc.empty ~f:accum_local_decls


let compute_invariant_map :
    Procdesc.t -> Tenv.t -> Typ.IntegerWidths.t -> get_proc_summary_and_formals -> invariant_map =
 fun pdesc tenv integer_type_widths get_proc_summary_and_formals ->
  Preanal.do_preanalysis pdesc tenv ;
  let cfg = CFG.from_pdesc pdesc in
  let pdata =
    let oenv = Dom.OndemandEnv.mk pdesc tenv integer_type_widths in
    ProcData.make pdesc tenv {get_proc_summary_and_formals; oenv}
  in
  let initial = Init.initial_state pdata (CFG.start_node cfg) in
  Analyzer.exec_pdesc ~do_narrowing:true ~initial pdata


let cached_compute_invariant_map =
  (* Use a weak Hashtbl to prevent memory leaks (GC unnecessarily keeps invariant maps around) *)
  let module WeakInvMapHashTbl = Caml.Weak.Make (struct
    type t = Typ.Procname.t * invariant_map option

    let equal (pname1, _) (pname2, _) = Typ.Procname.equal pname1 pname2

    let hash (pname, _) = Hashtbl.hash pname
  end) in
  let inv_map_cache = WeakInvMapHashTbl.create 100 in
  fun pdesc tenv integer_type_widths ->
    let pname = Procdesc.get_proc_name pdesc in
    match WeakInvMapHashTbl.find_opt inv_map_cache (pname, None) with
    | Some (_, Some inv_map) ->
        inv_map
    | Some (_, None) ->
        (* this should never happen *)
        assert false
    | None ->
        let get_proc_summary_and_formals callee_pname =
          Ondemand.analyze_proc_name ~caller_pdesc:pdesc callee_pname
          |> Option.bind ~f:(fun summary ->
                 Payload.of_summary summary
                 |> Option.map ~f:(fun payload ->
                        (payload, Summary.get_proc_desc summary |> Procdesc.get_pvar_formals) ) )
        in
        let inv_map =
          compute_invariant_map pdesc tenv integer_type_widths get_proc_summary_and_formals
        in
        WeakInvMapHashTbl.add inv_map_cache (pname, Some inv_map) ;
        inv_map


let compute_summary : local_decls -> CFG.t -> invariant_map -> memory_summary =
 fun locals cfg inv_map ->
  let exit_node_id = CFG.exit_node cfg |> CFG.Node.id in
  match extract_post exit_node_id inv_map with
  | Some exit_mem ->
      exit_mem |> Dom.Mem.forget_locs locals |> Dom.Mem.unset_oenv
  | None ->
      Bottom


let do_analysis : Callbacks.proc_callback_args -> Summary.t =
 fun {proc_desc; tenv; integer_type_widths; summary} ->
  let inv_map = cached_compute_invariant_map proc_desc tenv integer_type_widths in
  let locals = get_local_decls proc_desc in
  let cfg = CFG.from_pdesc proc_desc in
  let payload = compute_summary locals cfg inv_map in
  Payload.update_summary payload summary
