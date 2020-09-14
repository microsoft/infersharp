(*
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
open AbsLoc
open! AbstractDomain.Types
module L = Logging
module BoUtils = BufferOverrunUtils
module Dom = BufferOverrunDomain
module PO = BufferOverrunProofObligations
module Sem = BufferOverrunSemantics
module Relation = BufferOverrunDomainRelation
module Trace = BufferOverrunTrace
open BoUtils.ModelEnv

type exec_fun = model_env -> ret:Ident.t * Typ.t -> Dom.Mem.t -> Dom.Mem.t

type check_fun = model_env -> Dom.Mem.t -> PO.ConditionSet.checked_t -> PO.ConditionSet.checked_t

type model = {exec: exec_fun; check: check_fun}

let no_check _model_env _mem cond_set = cond_set

let no_model =
  let exec {pname; location} ~ret:(id, _) mem =
    L.d_printfln_escaped "No model for %a" Typ.Procname.pp pname ;
    Dom.Mem.add_unknown_from id ~callee_pname:pname ~location mem
  in
  {exec; check= no_check}


(* It returns a tuple of:
     - type of array element
     - stride of the type
     - array size
     - flexible array size *)
let get_malloc_info : Exp.t -> Typ.t * Int.t option * Exp.t * Exp.t option = function
  | Exp.BinOp (Binop.Mult _, Exp.Sizeof {typ; nbytes}, length)
  | Exp.BinOp (Binop.Mult _, length, Exp.Sizeof {typ; nbytes}) ->
      (typ, nbytes, length, None)
  (* In Java all arrays are dynamically allocated *)
  | Exp.Sizeof {typ; nbytes; dynamic_length= Some arr_length} when Language.curr_language_is Java
    ->
      (typ, nbytes, arr_length, Some arr_length)
  | Exp.Sizeof {typ; nbytes; dynamic_length} ->
      (typ, nbytes, Exp.one, dynamic_length)
  | x ->
      (Typ.mk (Typ.Tint Typ.IChar), Some 1, x, None)


let check_alloc_size ~can_be_zero size_exp {location; integer_type_widths} mem cond_set =
  let _, _, length0, _ = get_malloc_info size_exp in
  let v_length = Sem.eval integer_type_widths length0 mem in
  match Dom.Val.get_itv v_length with
  | Bottom ->
      cond_set
  | NonBottom length ->
      let traces = Dom.Val.get_traces v_length in
      let latest_prune = Dom.Mem.get_latest_prune mem in
      PO.ConditionSet.add_alloc_size location ~can_be_zero ~length traces latest_prune cond_set


let fgets str_exp num_exp =
  let exec {integer_type_widths} ~ret:(id, _) mem =
    let str_v = Sem.eval integer_type_widths str_exp mem in
    let num_v = Sem.eval integer_type_widths num_exp mem in
    let traces = Trace.Set.join (Dom.Val.get_traces str_v) (Dom.Val.get_traces num_v) in
    let update_strlen1 allocsite arrinfo acc =
      let strlen =
        let offset = ArrayBlk.ArrInfo.offsetof arrinfo in
        let num = Dom.Val.get_itv num_v in
        Itv.plus offset (Itv.set_lb_zero (Itv.decr num))
      in
      Dom.Mem.set_first_idx_of_null (Loc.of_allocsite allocsite) (Dom.Val.of_itv ~traces strlen)
        acc
    in
    mem
    |> Dom.Mem.update_mem (Sem.eval_locs str_exp mem) Dom.Val.Itv.zero_255
    |> ArrayBlk.fold update_strlen1 (Dom.Val.get_array_blk str_v)
    |> Dom.Mem.add_stack (Loc.of_id id) {str_v with itv= Itv.zero}
    |> Dom.Mem.fgets_alias id (Dom.Val.get_all_locs str_v)
  and check {location; integer_type_widths} mem cond_set =
    BoUtils.Check.lindex_byte integer_type_widths ~array_exp:str_exp ~byte_index_exp:num_exp
      ~last_included:true mem location cond_set
  in
  {exec; check}


let malloc ~can_be_zero size_exp =
  let exec ({pname; node_hash; location; tenv; integer_type_widths} as model_env) ~ret:(id, _) mem
      =
    let size_exp = Prop.exp_normalize_noabs tenv Sil.sub_empty size_exp in
    let typ, stride, length0, dyn_length = get_malloc_info size_exp in
    let length = Sem.eval integer_type_widths length0 mem in
    let traces = Trace.(Set.add_elem location ArrayDeclaration) (Dom.Val.get_traces length) in
    let path =
      match Dom.Mem.find_simple_alias id mem with Some (l, None) -> Loc.get_path l | _ -> None
    in
    let offset, size = (Itv.zero, Dom.Val.get_itv length) in
    let allocsite =
      let represents_multiple_values = not (Itv.is_one size) in
      Allocsite.make pname ~node_hash ~inst_num:0 ~dimension:1 ~path ~represents_multiple_values
    in
    let size_exp_opt =
      let size_exp = Option.value dyn_length ~default:length0 in
      Relation.SymExp.of_exp ~get_sym_f:(Sem.get_sym_f integer_type_widths mem) size_exp
    in
    let v = Dom.Val.of_c_array_alloc allocsite ~stride ~offset ~size ~traces in
    mem
    |> Dom.Mem.add_stack (Loc.of_id id) v
    |> Dom.Mem.init_array_relation allocsite ~offset_opt:(Some offset) ~size ~size_exp_opt
    |> BoUtils.Exec.init_c_array_fields model_env path typ (Dom.Val.get_array_locs v) ?dyn_length
  and check = check_alloc_size ~can_be_zero size_exp in
  {exec; check}


let calloc size_exp stride_exp =
  let byte_size_exp = Exp.BinOp (Binop.Mult (Some Typ.size_t), size_exp, stride_exp) in
  malloc byte_size_exp


let memcpy dest_exp src_exp size_exp =
  let exec _ ~ret:_ mem =
    let dest_loc = Sem.eval_locs dest_exp mem in
    let v = Dom.Mem.find_set (Sem.eval_locs src_exp mem) mem in
    Dom.Mem.update_mem dest_loc v mem
  and check {location; integer_type_widths} mem cond_set =
    BoUtils.Check.lindex_byte integer_type_widths ~array_exp:dest_exp ~byte_index_exp:size_exp
      ~last_included:true mem location cond_set
    |> BoUtils.Check.lindex_byte integer_type_widths ~array_exp:src_exp ~byte_index_exp:size_exp
         ~last_included:true mem location
  in
  {exec; check}


let memset arr_exp size_exp =
  let exec _ ~ret:_ mem = mem
  and check {location; integer_type_widths} mem cond_set =
    BoUtils.Check.lindex_byte integer_type_widths ~array_exp:arr_exp ~byte_index_exp:size_exp
      ~last_included:true mem location cond_set
  in
  {exec; check}


let strlen arr_exp =
  let exec _ ~ret:(id, _) mem =
    let v = Dom.Mem.get_c_strlen (Sem.eval_locs arr_exp mem) mem in
    Dom.Mem.add_stack (Loc.of_id id) v mem
  in
  {exec; check= no_check}


let strcpy dest_exp src_exp =
  let exec {integer_type_widths} ~ret:(id, _) mem =
    let src_loc = Sem.eval_locs src_exp mem in
    let dest_loc = Sem.eval_locs dest_exp mem in
    mem
    |> Dom.Mem.update_mem dest_loc (Dom.Mem.find_set src_loc mem)
    |> Dom.Mem.update_mem (PowLoc.of_c_strlen dest_loc) (Dom.Mem.get_c_strlen src_loc mem)
    |> Dom.Mem.add_stack (Loc.of_id id) (Sem.eval integer_type_widths dest_exp mem)
  and check {integer_type_widths; location} mem cond_set =
    let access_last_char =
      let idx = Dom.Mem.get_c_strlen (Sem.eval_locs src_exp mem) mem in
      let relation = Dom.Mem.get_relation mem in
      let latest_prune = Dom.Mem.get_latest_prune mem in
      fun arr cond_set ->
        BoUtils.Check.array_access ~arr ~idx ~idx_sym_exp:None ~relation ~is_plus:true
          ~last_included:false ~latest_prune location cond_set
    in
    cond_set
    |> access_last_char (Sem.eval integer_type_widths dest_exp mem)
    |> access_last_char (Sem.eval integer_type_widths src_exp mem)
  in
  {exec; check}


let strncpy dest_exp src_exp size_exp =
  let {exec= memcpy_exec; check= memcpy_check} = memcpy dest_exp src_exp size_exp in
  let exec model_env ~ret mem =
    let dest_strlen_loc = PowLoc.of_c_strlen (Sem.eval_locs dest_exp mem) in
    let strlen = Dom.Mem.find_set (PowLoc.of_c_strlen (Sem.eval_locs src_exp mem)) mem in
    mem |> memcpy_exec model_env ~ret |> Dom.Mem.update_mem dest_strlen_loc strlen
  in
  {exec; check= memcpy_check}


let strcat dest_exp src_exp =
  let exec {integer_type_widths} ~ret:(id, _) mem =
    let src_loc = Sem.eval_locs src_exp mem in
    let dest_loc = Sem.eval_locs dest_exp mem in
    let new_contents =
      let src_contents = Dom.Mem.find_set src_loc mem in
      let dest_contents = Dom.Mem.find_set dest_loc mem in
      Dom.Val.join dest_contents src_contents
    in
    let src_strlen = Dom.Mem.get_c_strlen src_loc mem in
    let new_strlen =
      let dest_strlen = Dom.Mem.get_c_strlen dest_loc mem in
      Dom.Val.plus_a dest_strlen src_strlen
    in
    mem
    |> Dom.Mem.update_mem dest_loc new_contents
    |> Dom.Mem.update_mem (PowLoc.of_c_strlen dest_loc) new_strlen
    |> Dom.Mem.add_stack (Loc.of_id id) (Sem.eval integer_type_widths dest_exp mem)
  and check {integer_type_widths; location} mem cond_set =
    let access_last_char arr idx cond_set =
      let relation = Dom.Mem.get_relation mem in
      let latest_prune = Dom.Mem.get_latest_prune mem in
      BoUtils.Check.array_access ~arr ~idx ~idx_sym_exp:None ~relation ~is_plus:true
        ~last_included:false ~latest_prune location cond_set
    in
    let src_strlen =
      let str_loc = Sem.eval_locs src_exp mem in
      Dom.Mem.get_c_strlen str_loc mem
    in
    let new_strlen =
      let dest_strlen =
        let dest_loc = Sem.eval_locs dest_exp mem in
        Dom.Mem.get_c_strlen dest_loc mem
      in
      Dom.Val.plus_a dest_strlen src_strlen
    in
    cond_set
    |> access_last_char (Sem.eval integer_type_widths dest_exp mem) new_strlen
    |> access_last_char (Sem.eval integer_type_widths src_exp mem) src_strlen
  in
  {exec; check}


let realloc src_exp size_exp =
  let exec ({location; tenv; integer_type_widths} as model_env) ~ret:(id, _) mem =
    let size_exp = Prop.exp_normalize_noabs tenv Sil.sub_empty size_exp in
    let typ, _, length0, dyn_length = get_malloc_info size_exp in
    let length = Sem.eval integer_type_widths length0 mem in
    let v =
      Sem.eval integer_type_widths src_exp mem |> Dom.Val.set_array_length location ~length
    in
    let mem = Dom.Mem.add_stack (Loc.of_id id) v mem in
    Option.value_map dyn_length ~default:mem ~f:(fun dyn_length ->
        let dyn_length = Dom.Val.get_itv (Sem.eval integer_type_widths dyn_length mem) in
        BoUtils.Exec.set_dyn_length model_env typ (Dom.Val.get_array_locs v) dyn_length mem )
  and check = check_alloc_size ~can_be_zero:false size_exp in
  {exec; check}


let placement_new size_exp (src_exp1, t1) src_arg2_opt =
  match (t1.Typ.desc, src_arg2_opt) with
  | Tint _, None | Tint _, Some (_, {Typ.desc= Tint _}) ->
      malloc ~can_be_zero:true (Exp.BinOp (Binop.PlusA (Some Typ.size_t), size_exp, src_exp1))
  | Tstruct (CppClass (name, _)), None
    when [%compare.equal: string list] (QualifiedCppName.to_list name) ["std"; "nothrow_t"] ->
      malloc ~can_be_zero:true size_exp
  | _, _ ->
      let exec {integer_type_widths} ~ret:(id, _) mem =
        let src_exp =
          if Typ.is_pointer_to_void t1 then src_exp1
          else
            match src_arg2_opt with
            | Some (src_exp2, t2) when Typ.is_pointer_to_void t2 ->
                src_exp2
            | _ ->
                (* TODO: Raise an exception when given unexpected arguments.  Before that, we need
                   to fix the frontend to parse user defined `new` correctly. *)
                L.d_error "Unexpected types of arguments for __placement_new" ;
                src_exp1
        in
        let v = Sem.eval integer_type_widths src_exp mem in
        Dom.Mem.add_stack (Loc.of_id id) v mem
      in
      {exec; check= no_check}


let strndup src_exp length_exp =
  let exec ({pname; node_hash; location; integer_type_widths} as model_env) ~ret:((id, _) as ret)
      mem =
    let v =
      let src_strlen = Dom.Mem.get_c_strlen (Sem.eval_locs src_exp mem) mem in
      let length = Sem.eval integer_type_widths length_exp mem in
      let size = Itv.incr (Itv.min_sem (Dom.Val.get_itv src_strlen) (Dom.Val.get_itv length)) in
      let allocsite =
        let represents_multiple_values = not (Itv.is_one size) in
        Allocsite.make pname ~node_hash ~inst_num:0 ~dimension:1 ~path:None
          ~represents_multiple_values
      in
      let traces =
        Trace.Set.join (Dom.Val.get_traces src_strlen) (Dom.Val.get_traces length)
        |> Trace.Set.add_elem location (Trace.through ~risky_fun:(Some Trace.strndup))
        |> Trace.Set.add_elem location ArrayDeclaration
      in
      Dom.Val.of_c_array_alloc allocsite
        ~stride:(Some (integer_type_widths.char_width / 8))
        ~offset:Itv.zero ~size ~traces
    in
    mem
    |> Dom.Mem.add_stack (Loc.of_id id) v
    |> (strncpy (Exp.Var id) src_exp length_exp).exec model_env ~ret
  in
  {exec; check= no_check}


let inferbo_min e1 e2 =
  let exec {integer_type_widths} ~ret:(id, _) mem =
    let i1 = Sem.eval integer_type_widths e1 mem |> Dom.Val.get_itv in
    let i2 = Sem.eval integer_type_widths e2 mem |> Dom.Val.get_itv in
    let v = Itv.min_sem i1 i2 |> Dom.Val.of_itv in
    mem |> Dom.Mem.add_stack (Loc.of_id id) v
  in
  {exec; check= no_check}


let inferbo_set_size e1 e2 =
  let exec {integer_type_widths; location} ~ret:_ mem =
    let locs = Sem.eval integer_type_widths e1 mem |> Dom.Val.get_pow_loc in
    let length = Sem.eval integer_type_widths e2 mem in
    Dom.Mem.transform_mem ~f:(Dom.Val.set_array_length location ~length) locs mem
  and check = check_alloc_size ~can_be_zero:true e2 in
  {exec; check}


let variable_initialization (e, typ) =
  let exec model_env ~ret:_ mem =
    match e with
    | Exp.Lvar x when Pvar.is_global x ->
        let mem, _ = BoUtils.Exec.decl_local model_env (mem, 1) (Loc.of_pvar x, typ) in
        mem
    | _ ->
        mem
  in
  {exec; check= no_check}


let model_by_value value id mem = Dom.Mem.add_stack (Loc.of_id id) value mem

let cast exp =
  let exec {integer_type_widths} ~ret:(ret_id, _) mem =
    let itv = Sem.eval integer_type_widths exp mem in
    model_by_value itv ret_id mem
  in
  {exec; check= no_check}


let by_value =
  let exec ~value _ ~ret:(ret_id, _) mem = model_by_value value ret_id mem in
  fun value -> {exec= exec ~value; check= no_check}


let by_risky_value_from lib_fun =
  let exec ~value {location} ~ret:(ret_id, _) mem =
    let traces =
      Trace.(Set.add_elem location (through ~risky_fun:(Some lib_fun))) (Dom.Val.get_traces value)
    in
    model_by_value {value with traces} ret_id mem
  in
  fun value -> {exec= exec ~value; check= no_check}


let bottom =
  let exec _model_env ~ret:_ _mem = Bottom in
  {exec; check= no_check}


let infer_print e =
  let exec {location; integer_type_widths} ~ret:_ mem =
    L.(debug BufferOverrun Medium)
      "@[<v>=== Infer Print === at %a@,%a@]%!" Location.pp location Dom.Val.pp
      (Sem.eval integer_type_widths e mem) ;
    mem
  in
  {exec; check= no_check}


let eval_array_locs_length arr_locs mem =
  if PowLoc.is_empty arr_locs then Dom.Val.Itv.top
  else
    let arr = Dom.Mem.find_set arr_locs mem in
    let traces = Dom.Val.get_traces arr in
    let length = arr |> Dom.Val.get_array_blk |> ArrayBlk.sizeof in
    Dom.Val.of_itv ~traces length


(* Java only *)
let get_array_length array_exp =
  let exec _ ~ret:(ret_id, _) mem =
    let result = eval_array_locs_length (Sem.eval_locs array_exp mem) mem in
    model_by_value result ret_id mem
  in
  {exec; check= no_check}


(* Clang only *)
let set_array_length array length_exp =
  let exec {pname; node_hash; location; integer_type_widths} ~ret:_ mem =
    match array with
    | Exp.Lvar array_pvar, {Typ.desc= Typ.Tarray {stride}} ->
        let length = Sem.eval integer_type_widths length_exp mem in
        let stride = Option.map ~f:IntLit.to_int_exn stride in
        let path = Some (Symb.SymbolPath.of_pvar array_pvar) in
        let traces = Trace.(Set.add_elem location ArrayDeclaration) (Dom.Val.get_traces length) in
        let size = Dom.Val.get_itv length in
        let allocsite =
          let represents_multiple_values = not (Itv.is_one size) in
          Allocsite.make pname ~node_hash ~inst_num:0 ~dimension:1 ~path
            ~represents_multiple_values
        in
        let v = Dom.Val.of_c_array_alloc allocsite ~stride ~offset:Itv.zero ~size ~traces in
        Dom.Mem.add_stack (Loc.of_pvar array_pvar) v mem
    | _ ->
        L.(die InternalError) "Unexpected type of first argument for __set_array_length() "
  and check = check_alloc_size ~can_be_zero:false length_exp in
  {exec; check}


let snprintf = by_risky_value_from Trace.snprintf Dom.Val.Itv.nat

let vsnprintf = by_risky_value_from Trace.vsnprintf Dom.Val.Itv.nat

module Split = struct
  let std_vector ~adds_at_least_one (vector_exp, vector_typ) location mem =
    let increment = if adds_at_least_one then Dom.Val.Itv.pos else Dom.Val.Itv.nat in
    let vector_type_name = Option.value_exn (vector_typ |> Typ.strip_ptr |> Typ.name) in
    let size_field = Typ.Fieldname.Clang.from_class_name vector_type_name "infer_size" in
    let vector_size_locs = Sem.eval_locs vector_exp mem |> PowLoc.append_field ~fn:size_field in
    let f_trace _ traces = Trace.(Set.add_elem location (through ~risky_fun:None)) traces in
    Dom.Mem.transform_mem ~f:(Dom.Val.plus_a ~f_trace increment) vector_size_locs mem
end

module Boost = struct
  module Split = struct
    let std_vector vector_arg =
      let exec {location} ~ret:_ mem =
        Split.std_vector ~adds_at_least_one:true vector_arg location mem
      in
      {exec; check= no_check}
  end
end

module Folly = struct
  module Split = struct
    let std_vector vector_arg ignore_empty_opt =
      let exec {location; integer_type_widths} ~ret:_ mem =
        let adds_at_least_one =
          match ignore_empty_opt with
          | Some ignore_empty_exp ->
              Sem.eval integer_type_widths ignore_empty_exp mem |> Dom.Val.get_itv |> Itv.is_false
          | None ->
              (* default: ignore_empty is false *)
              true
        in
        Split.std_vector ~adds_at_least_one vector_arg location mem
      in
      {exec; check= no_check}
  end
end

module StdArray = struct
  let constructor _size =
    let exec _model_env ~ret:_ mem = mem (* initialize? *) in
    {exec; check= no_check}


  let at _size (array_exp, _) (index_exp, _) =
    (* TODO? use size *)
    let exec {integer_type_widths} ~ret:(id, _) mem =
      L.d_printfln_escaped "Using model std::array<_, %Ld>::at" _size ;
      Dom.Mem.add_stack (Loc.of_id id)
        (Sem.eval_lindex integer_type_widths array_exp index_exp mem)
        mem
    and check {location; integer_type_widths} mem cond_set =
      BoUtils.Check.lindex integer_type_widths ~array_exp ~index_exp ~last_included:false mem
        location cond_set
    in
    {exec; check}


  let begin_ _size (array_exp, _) =
    let exec {location; integer_type_widths} ~ret:(id, _) mem =
      let v =
        Sem.eval integer_type_widths array_exp mem |> Dom.Val.set_array_offset location Itv.zero
      in
      Dom.Mem.add_stack (Loc.of_id id) v mem
    in
    {exec; check= no_check}


  let end_ size (array_exp, _) =
    let exec {location; integer_type_widths} ~ret:(id, _) mem =
      let v =
        let offset = Itv.of_int_lit (IntLit.of_int64 size) in
        Sem.eval integer_type_widths array_exp mem |> Dom.Val.set_array_offset location offset
      in
      Dom.Mem.add_stack (Loc.of_id id) v mem
    in
    {exec; check= no_check}


  let back size (array_exp, _) =
    let exec {location; integer_type_widths} ~ret:(id, _) mem =
      let v =
        let offset = Itv.of_int_lit (IntLit.of_int64 Int64.(size - one)) in
        Sem.eval integer_type_widths array_exp mem |> Dom.Val.set_array_offset location offset
      in
      Dom.Mem.add_stack (Loc.of_id id) v mem
    in
    {exec; check= no_check}
end

module StdBasicString = struct
  (* The (4) constructor in https://en.cppreference.com/w/cpp/string/basic_string/basic_string *)
  let constructor_from_char_ptr tgt src len =
    let {exec= malloc_exec; check= malloc_check} = malloc ~can_be_zero:true len in
    let exec model_env ~ret:((ret_id, _) as ret) mem =
      let mem = malloc_exec model_env ~ret mem in
      let v = Dom.Mem.find (Loc.of_id ret_id) mem in
      let mem = Dom.Mem.update_mem (Sem.eval_locs tgt mem) v mem in
      let contents =
        let src_locs = Sem.eval_locs src mem in
        Dom.Mem.find_set src_locs mem
      in
      Dom.Mem.update_mem (Dom.Val.get_all_locs v) contents mem
    in
    let check ({location; integer_type_widths} as model_env) mem cond_set =
      let cond_set = malloc_check model_env mem cond_set in
      BoUtils.Check.lindex integer_type_widths ~array_exp:src ~index_exp:len ~last_included:true
        mem location cond_set
    in
    {exec; check}


  (* The (5) constructor in https://en.cppreference.com/w/cpp/string/basic_string/basic_string *)
  let constructor_from_char_ptr_without_len tgt src =
    let exec ({integer_type_widths} as model_env) ~ret:_ mem =
      match src with
      | Exp.Const (Const.Cstr s) ->
          let locs = Sem.eval_locs tgt mem in
          BoUtils.Exec.decl_string model_env ~do_alloc:true locs s mem
      | _ ->
          let tgt_locs = Sem.eval_locs tgt mem in
          let v = Sem.eval integer_type_widths src mem in
          Dom.Mem.update_mem tgt_locs v mem
    in
    {exec; check= no_check}


  (* The (7) constructor in https://en.cppreference.com/w/cpp/string/basic_string/basic_string *)
  let copy_constructor tgt src =
    let exec _ ~ret:_ mem =
      let tgt_locs = Sem.eval_locs tgt mem in
      let v = Dom.Mem.find_set (Sem.eval_locs src mem) mem in
      Dom.Mem.update_mem tgt_locs v mem
    in
    {exec; check= no_check}


  let empty e =
    let exec {integer_type_widths} ~ret:(ret_id, _) mem =
      let v = Sem.eval integer_type_widths e mem in
      let traces = Dom.Val.get_traces v in
      let size = ArrayBlk.sizeof (Dom.Val.get_array_blk v) in
      let empty = Dom.Val.of_itv ~traces (Itv.of_bool (Itv.le_sem size Itv.zero)) in
      let mem = Dom.Mem.add_stack (Loc.of_id ret_id) empty mem in
      match e with
      | Exp.Var id -> (
        match Dom.Mem.find_simple_alias id mem with
        | Some (l, None) ->
            Dom.Mem.load_empty_alias ret_id l mem
        | _ ->
            mem )
      | _ ->
          mem
    in
    {exec; check= no_check}


  let length e =
    let exec {integer_type_widths} ~ret:(ret_id, _) mem =
      let v = Sem.eval_arr integer_type_widths e mem in
      let length = Dom.Val.of_itv (ArrayBlk.sizeof (Dom.Val.get_array_blk v)) in
      Dom.Mem.add_stack (Loc.of_id ret_id) length mem
    in
    {exec; check= no_check}
end

(* Java's Collections are represented like arrays. But we don't care about the elements.
- when they are constructed, we set the size to 0
- each time we add an element, we increase the length of the array
- each time we delete an element, we decrease the length of the array *)
module Collection = struct
  let new_collection _ =
    let exec {pname; node_hash; location} ~ret:(id, _) mem =
      let represents_multiple_values = true in
      let traces = Trace.(Set.singleton location ArrayDeclaration) in
      let coll_allocsite =
        Allocsite.make pname ~node_hash ~inst_num:0 ~dimension:1 ~path:None
          ~represents_multiple_values
      in
      let internal_array =
        let allocsite =
          Allocsite.make pname ~node_hash ~inst_num:1 ~dimension:1 ~path:None
            ~represents_multiple_values
        in
        Dom.Val.of_java_array_alloc allocsite ~length:Itv.zero ~traces
      in
      let coll_loc = Loc.of_allocsite coll_allocsite in
      let internal_array_loc =
        Loc.append_field coll_loc ~fn:BufferOverrunField.java_collection_internal_array
      in
      mem
      |> Dom.Mem.add_heap internal_array_loc internal_array
      |> Dom.Mem.add_stack (Loc.of_id id)
           (coll_loc |> PowLoc.singleton |> Dom.Val.of_pow_loc ~traces)
    in
    {exec; check= no_check}


  let eval_collection_internal_array_locs coll_exp mem =
    Sem.eval_locs coll_exp mem
    |> PowLoc.append_field ~fn:BufferOverrunField.java_collection_internal_array


  let get_collection_internal_array_locs coll_id mem =
    let coll = Dom.Mem.find (Loc.of_id coll_id) mem in
    Dom.Val.get_pow_loc coll
    |> PowLoc.append_field ~fn:BufferOverrunField.java_collection_internal_array


  let eval_collection_length coll_exp mem =
    let arr_locs = eval_collection_internal_array_locs coll_exp mem in
    eval_array_locs_length arr_locs mem


  let change_size_by ~size_f coll_id {location} ~ret:_ mem =
    Dom.Mem.transform_mem
      ~f:(Dom.Val.transform_array_length location ~f:size_f)
      (get_collection_internal_array_locs coll_id mem)
      mem


  let add coll_id = {exec= change_size_by ~size_f:Itv.incr coll_id; check= no_check}

  let size coll_exp =
    let exec _ ~ret:(ret_id, _) mem =
      let result = eval_collection_length coll_exp mem in
      model_by_value result ret_id mem
    in
    {exec; check= no_check}


  let iterator coll_exp =
    let exec {integer_type_widths} ~ret:(ret_id, _) mem =
      let itr = Sem.eval integer_type_widths coll_exp mem in
      model_by_value itr ret_id mem
    in
    {exec; check= no_check}


  let init lhs_id rhs_exp =
    let exec {integer_type_widths} ~ret:_ mem =
      let itr = Sem.eval integer_type_widths rhs_exp mem in
      model_by_value itr lhs_id mem
    and check = check_alloc_size ~can_be_zero:true rhs_exp in
    {exec; check}


  let hasNext iterator =
    let exec _ ~ret:(ret_id, _) mem =
      (* Set the size of the iterator to be [0, size-1], so that range
         will be size of the collection. *)
      let collection_size = eval_collection_length iterator mem |> Dom.Val.get_iterator_itv in
      model_by_value collection_size ret_id mem
    in
    {exec; check= no_check}


  let addAll coll_id coll_to_add =
    let exec model_env ~ret mem =
      let to_add_length = eval_collection_length coll_to_add mem |> Dom.Val.get_itv in
      change_size_by ~size_f:(Itv.plus to_add_length) coll_id model_env ~ret mem
    in
    {exec; check= no_check}


  let check_index ~last_included coll_id index_exp {location; integer_type_widths} mem cond_set =
    let arr =
      let arr_locs = get_collection_internal_array_locs coll_id mem in
      Dom.Mem.find_set arr_locs mem
    in
    let idx = Sem.eval integer_type_widths index_exp mem in
    let idx_sym_exp =
      Relation.SymExp.of_exp ~get_sym_f:(Sem.get_sym_f integer_type_widths mem) index_exp
    in
    let relation = Dom.Mem.get_relation mem in
    let latest_prune = Dom.Mem.get_latest_prune mem in
    BoUtils.Check.array_access ~arr ~idx ~idx_sym_exp ~relation ~is_plus:true ~last_included
      ~latest_prune location cond_set


  let add_at_index (coll_id : Ident.t) index_exp =
    { exec= change_size_by ~size_f:Itv.incr coll_id
    ; check= check_index ~last_included:true coll_id index_exp }


  let remove_at_index coll_id index_exp =
    { exec= change_size_by ~size_f:Itv.decr coll_id
    ; check= check_index ~last_included:false coll_id index_exp }


  let addAll_at_index coll_id index_exp coll_to_add =
    let exec model_env ~ret mem =
      let to_add_length = eval_collection_length coll_to_add mem |> Dom.Val.get_itv in
      change_size_by ~size_f:(Itv.plus to_add_length) coll_id model_env ~ret mem
    in
    {exec; check= check_index ~last_included:true coll_id index_exp}


  let get_or_set_at_index coll_id index_exp =
    let exec _model_env ~ret:_ mem = mem in
    {exec; check= check_index ~last_included:false coll_id index_exp}
end

module Call = struct
  let dispatch : (Tenv.t, model) ProcnameDispatcher.Call.dispatcher =
    let open ProcnameDispatcher.Call in
    let mk_std_array () = -"std" &:: "array" < any_typ &+ capt_int in
    let std_array0 = mk_std_array () in
    let std_array1 = mk_std_array () in
    let std_array2 = mk_std_array () in
    let char_ptr = Typ.mk (Typ.Tptr (Typ.mk (Typ.Tint Typ.IChar), Pk_pointer)) in
    make_dispatcher
      [ -"__inferbo_min" <>$ capt_exp $+ capt_exp $!--> inferbo_min
      ; -"__inferbo_set_size" <>$ capt_exp $+ capt_exp $!--> inferbo_set_size
      ; -"__variable_initialization" <>$ capt_arg $!--> variable_initialization
      ; -"__exit" <>--> bottom
      ; -"exit" <>--> bottom
      ; -"__cast" <>$ capt_exp $+...$--> cast
      ; -"fgetc" <>--> by_value Dom.Val.Itv.m1_255
      ; -"fgets" <>$ capt_exp $+ capt_exp $+...$--> fgets
      ; -"infer_print" <>$ capt_exp $!--> infer_print
      ; -"malloc" <>$ capt_exp $+...$--> malloc ~can_be_zero:false
      ; -"calloc" <>$ capt_exp $+ capt_exp $!--> calloc ~can_be_zero:false
      ; -"__new"
        <>$ capt_exp_of_typ (+PatternMatch.implements_collection)
        $+...$--> Collection.new_collection
      ; -"__new" <>$ capt_exp $+...$--> malloc ~can_be_zero:true
      ; -"__new_array" <>$ capt_exp $+...$--> malloc ~can_be_zero:true
      ; -"__placement_new" <>$ capt_exp $+ capt_arg $+? capt_arg $!--> placement_new
      ; -"realloc" <>$ capt_exp $+ capt_exp $+...$--> realloc
      ; -"__get_array_length" <>$ capt_exp $!--> get_array_length
      ; -"__set_array_length" <>$ capt_arg $+ capt_exp $!--> set_array_length
      ; +PatternMatch.implements_lang "String" &:: "length" <>$ capt_exp $!--> strlen
      ; -"strlen" <>$ capt_exp $!--> strlen
      ; -"memcpy" <>$ capt_exp $+ capt_exp $+ capt_exp $+...$--> memcpy
      ; -"memmove" <>$ capt_exp $+ capt_exp $+ capt_exp $+...$--> memcpy
      ; -"memset" <>$ capt_exp $+ any_arg $+ capt_exp $!--> memset
      ; -"strcat" <>$ capt_exp $+ capt_exp $+...$--> strcat
      ; +PatternMatch.implements_lang "String"
        &:: "concat" <>$ capt_exp $+ capt_exp $+...$--> strcat
      ; -"strcpy" <>$ capt_exp $+ capt_exp $+...$--> strcpy
      ; -"strncpy" <>$ capt_exp $+ capt_exp $+ capt_exp $+...$--> strncpy
      ; -"snprintf" <>--> snprintf
      ; -"vsnprintf" <>--> vsnprintf
      ; -"strndup" <>$ capt_exp $+ capt_exp $+...$--> strndup
      ; -"boost" &:: "split"
        $ capt_arg_of_typ (-"std" &:: "vector")
        $+ any_arg $+ any_arg $+? any_arg $--> Boost.Split.std_vector
      ; -"folly" &:: "split" $ any_arg $+ any_arg
        $+ capt_arg_of_typ (-"std" &:: "vector")
        $+? capt_exp $--> Folly.Split.std_vector
      ; std_array0 >:: "array" &--> StdArray.constructor
      ; std_array1 >:: "begin" $ capt_arg $!--> StdArray.begin_
      ; std_array1 >:: "cbegin" $ capt_arg $!--> StdArray.begin_
      ; std_array1 >:: "end" $ capt_arg $!--> StdArray.end_
      ; std_array1 >:: "cend" $ capt_arg $!--> StdArray.end_
      ; std_array1 >:: "front" $ capt_arg $!--> StdArray.begin_
      ; std_array1 >:: "back" $ capt_arg $!--> StdArray.back
      ; std_array2 >:: "at" $ capt_arg $+ capt_arg $!--> StdArray.at
      ; std_array2 >:: "operator[]" $ capt_arg $+ capt_arg $!--> StdArray.at
      ; -"std" &:: "array" &::.*--> no_model
      ; -"std" &:: "basic_string" &:: "basic_string" $ capt_exp
        $+ capt_exp_of_typ (-"std" &:: "basic_string")
        $--> StdBasicString.copy_constructor
      ; -"std" &:: "basic_string" &:: "basic_string" $ capt_exp $+ capt_exp_of_prim_typ char_ptr
        $--> StdBasicString.constructor_from_char_ptr_without_len
      ; -"std" &:: "basic_string" &:: "basic_string" $ capt_exp $+ capt_exp_of_prim_typ char_ptr
        $+ capt_exp_of_prim_typ (Typ.mk (Typ.Tint Typ.size_t))
        $--> StdBasicString.constructor_from_char_ptr
      ; -"std" &:: "basic_string" &:: "empty" $ capt_exp $--> StdBasicString.empty
      ; -"std" &:: "basic_string" &:: "length" $ capt_exp $--> StdBasicString.length
      ; -"std" &:: "basic_string" &:: "size" $ capt_exp $--> StdBasicString.length
      ; -"std" &:: "basic_string" &:: "compare" &--> by_value Dom.Val.Itv.top
      ; +PatternMatch.implements_lang "String"
        &:: "equals"
        $ any_arg_of_typ (+PatternMatch.implements_lang "String")
        $+ any_arg_of_typ (+PatternMatch.implements_lang "String")
        $--> by_value Dom.Val.Itv.unknown_bool
      ; +PatternMatch.implements_lang "String"
        &:: "startsWith"
        $ any_arg_of_typ (+PatternMatch.implements_lang "String")
        $+ any_arg_of_typ (+PatternMatch.implements_lang "String")
        $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "operator=="
        $ any_arg_of_typ (-"std" &:: "basic_string")
        $+ any_arg_of_typ (-"std" &:: "basic_string")
        $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "operator=="
        $ any_arg_of_typ (-"std" &:: "basic_string")
        $+ any_arg_of_prim_typ char_ptr $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "operator==" $ any_arg_of_prim_typ char_ptr
        $+ any_arg_of_typ (-"std" &:: "basic_string")
        $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "operator!="
        $ any_arg_of_typ (-"std" &:: "basic_string")
        $+ any_arg_of_typ (-"std" &:: "basic_string")
        $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "operator!="
        $ any_arg_of_typ (-"std" &:: "basic_string")
        $+ any_arg_of_prim_typ char_ptr $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "operator!=" $ any_arg_of_prim_typ char_ptr
        $+ any_arg_of_typ (-"std" &:: "basic_string")
        $--> by_value Dom.Val.Itv.unknown_bool
      ; -"std" &:: "basic_string" &::.*--> no_model
      ; +PatternMatch.implements_collection
        &:: "<init>" <>$ capt_var_exn $+ capt_exp $--> Collection.init
      ; +PatternMatch.implements_collection
        &:: "get" <>$ capt_var_exn $+ capt_exp $--> Collection.get_or_set_at_index
      ; +PatternMatch.implements_collection
        &:: "set" <>$ capt_var_exn $+ capt_exp $+ any_arg $--> Collection.get_or_set_at_index
      ; +PatternMatch.implements_collection
        &:: "remove" <>$ capt_var_exn $+ capt_exp $--> Collection.remove_at_index
      ; +PatternMatch.implements_collection
        &:: "add" <>$ capt_var_exn $+ any_arg $--> Collection.add
      ; +PatternMatch.implements_collection
        &:: "add" <>$ capt_var_exn $+ capt_exp $+ any_arg $!--> Collection.add_at_index
      ; +PatternMatch.implements_lang "Iterable"
        &:: "iterator" <>$ capt_exp $!--> Collection.iterator
      ; +PatternMatch.implements_iterator &:: "hasNext" <>$ capt_exp $!--> Collection.hasNext
      ; +PatternMatch.implements_collection
        &:: "addAll" <>$ capt_var_exn $+ capt_exp $--> Collection.addAll
      ; +PatternMatch.implements_collection
        &:: "addAll" <>$ capt_var_exn $+ capt_exp $+ capt_exp $!--> Collection.addAll_at_index
      ; +PatternMatch.implements_collection &:: "size" <>$ capt_exp $!--> Collection.size ]
end
