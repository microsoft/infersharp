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
module F = Format
module L = Logging
module OndemandEnv = BufferOverrunOndemandEnv
module Relation = BufferOverrunDomainRelation
module SPath = Symb.SymbolPath
module Trace = BufferOverrunTrace
module TraceSet = Trace.Set
module LoopHeadLoc = Location

type eval_sym_trace =
  { eval_sym: Bounds.Bound.eval_sym
  ; trace_of_sym: Symb.Symbol.t -> Trace.Set.t
  ; eval_locpath: PowLoc.eval_locpath }

module ItvThresholds = AbstractDomain.FiniteSet (struct
  include Z

  let pp = pp_print
end)

module Val = struct
  type t =
    { itv: Itv.t
    ; itv_thresholds: ItvThresholds.t
    ; sym: Relation.Sym.t
    ; powloc: PowLoc.t
    ; arrayblk: ArrayBlk.t
    ; offset_sym: Relation.Sym.t
    ; size_sym: Relation.Sym.t
    ; traces: TraceSet.t
    ; represents_multiple_values: bool }

  let bot : t =
    { itv= Itv.bot
    ; itv_thresholds= ItvThresholds.empty
    ; sym= Relation.Sym.bot
    ; powloc= PowLoc.bot
    ; arrayblk= ArrayBlk.bot
    ; offset_sym= Relation.Sym.bot
    ; size_sym= Relation.Sym.bot
    ; traces= TraceSet.bottom
    ; represents_multiple_values= false }


  let pp fmt x =
    let itv_thresholds_pp fmt itv_thresholds =
      if Config.bo_debug >= 3 && not (ItvThresholds.is_empty itv_thresholds) then
        F.fprintf fmt " (thresholds:%a)" ItvThresholds.pp itv_thresholds
    in
    let relation_sym_pp fmt sym =
      if Option.is_some Config.bo_relational_domain then F.fprintf fmt ", %a" Relation.Sym.pp sym
    in
    let trace_pp fmt traces =
      if Config.bo_debug >= 1 then F.fprintf fmt ", %a" TraceSet.pp traces
    in
    let represents_multiple_values_str = if x.represents_multiple_values then "M" else "" in
    F.fprintf fmt "%s(%a%a%a, %a, %a%a%a%a)" represents_multiple_values_str Itv.pp x.itv
      itv_thresholds_pp x.itv_thresholds relation_sym_pp x.sym PowLoc.pp x.powloc ArrayBlk.pp
      x.arrayblk relation_sym_pp x.offset_sym relation_sym_pp x.size_sym trace_pp x.traces


  let sets_represents_multiple_values : represents_multiple_values:bool -> t -> t =
   fun ~represents_multiple_values x -> {x with represents_multiple_values}


  let unknown_from : callee_pname:_ -> location:_ -> t =
   fun ~callee_pname ~location ->
    let traces = Trace.(Set.singleton_final location (UnknownFrom callee_pname)) in
    { itv= Itv.top
    ; itv_thresholds= ItvThresholds.empty
    ; sym= Relation.Sym.top
    ; powloc= PowLoc.unknown
    ; arrayblk= ArrayBlk.unknown
    ; offset_sym= Relation.Sym.top
    ; size_sym= Relation.Sym.top
    ; traces
    ; represents_multiple_values= false }


  let ( <= ) ~lhs ~rhs =
    if phys_equal lhs rhs then true
    else
      Itv.( <= ) ~lhs:lhs.itv ~rhs:rhs.itv
      && ItvThresholds.( <= ) ~lhs:lhs.itv_thresholds ~rhs:rhs.itv_thresholds
      && Relation.Sym.( <= ) ~lhs:lhs.sym ~rhs:rhs.sym
      && PowLoc.( <= ) ~lhs:lhs.powloc ~rhs:rhs.powloc
      && ArrayBlk.( <= ) ~lhs:lhs.arrayblk ~rhs:rhs.arrayblk
      && Relation.Sym.( <= ) ~lhs:lhs.offset_sym ~rhs:rhs.offset_sym
      && Relation.Sym.( <= ) ~lhs:lhs.size_sym ~rhs:rhs.size_sym
      && Bool.( <= ) lhs.represents_multiple_values rhs.represents_multiple_values


  let widen ~prev ~next ~num_iters =
    if phys_equal prev next then prev
    else
      let itv_thresholds = ItvThresholds.join prev.itv_thresholds next.itv_thresholds in
      { itv=
          Itv.widen_thresholds
            ~thresholds:(ItvThresholds.elements itv_thresholds)
            ~prev:prev.itv ~next:next.itv ~num_iters
      ; itv_thresholds
      ; sym= Relation.Sym.widen ~prev:prev.sym ~next:next.sym ~num_iters
      ; powloc= PowLoc.widen ~prev:prev.powloc ~next:next.powloc ~num_iters
      ; arrayblk= ArrayBlk.widen ~prev:prev.arrayblk ~next:next.arrayblk ~num_iters
      ; offset_sym= Relation.Sym.widen ~prev:prev.offset_sym ~next:next.offset_sym ~num_iters
      ; size_sym= Relation.Sym.widen ~prev:prev.size_sym ~next:next.size_sym ~num_iters
      ; traces= TraceSet.join prev.traces next.traces
      ; represents_multiple_values=
          prev.represents_multiple_values || next.represents_multiple_values }


  let join : t -> t -> t =
   fun x y ->
    if phys_equal x y then x
    else
      { itv= Itv.join x.itv y.itv
      ; itv_thresholds= ItvThresholds.join x.itv_thresholds y.itv_thresholds
      ; sym= Relation.Sym.join x.sym y.sym
      ; powloc= PowLoc.join x.powloc y.powloc
      ; arrayblk= ArrayBlk.join x.arrayblk y.arrayblk
      ; offset_sym= Relation.Sym.join x.offset_sym y.offset_sym
      ; size_sym= Relation.Sym.join x.size_sym y.size_sym
      ; traces= TraceSet.join x.traces y.traces
      ; represents_multiple_values= x.represents_multiple_values || y.represents_multiple_values }


  let get_itv : t -> Itv.t = fun x -> x.itv

  let get_sym : t -> Relation.Sym.t = fun x -> x.sym

  let get_sym_var : t -> Relation.Var.t option = fun x -> Relation.Sym.get_var x.sym

  let get_pow_loc : t -> PowLoc.t = fun x -> x.powloc

  let get_array_blk : t -> ArrayBlk.t = fun x -> x.arrayblk

  let get_array_locs : t -> PowLoc.t = fun x -> ArrayBlk.get_pow_loc x.arrayblk

  let get_all_locs : t -> PowLoc.t = fun x -> PowLoc.join x.powloc (get_array_locs x)

  let get_offset_sym : t -> Relation.Sym.t = fun x -> x.offset_sym

  let get_size_sym : t -> Relation.Sym.t = fun x -> x.size_sym

  let get_traces : t -> TraceSet.t = fun x -> x.traces

  let of_itv ?(traces = TraceSet.bottom) itv = {bot with itv; traces}

  let of_int n = of_itv (Itv.of_int n)

  let of_big_int n = of_itv (Itv.of_big_int n)

  let of_int_lit n = of_itv (Itv.of_int_lit n)

  let of_loc ?(traces = TraceSet.bottom) x = {bot with powloc= PowLoc.singleton x; traces}

  let of_pow_loc ~traces powloc = {bot with powloc; traces}

  let of_c_array_alloc :
      Allocsite.t -> stride:int option -> offset:Itv.t -> size:Itv.t -> traces:TraceSet.t -> t =
   fun allocsite ~stride ~offset ~size ~traces ->
    let stride = Option.value_map stride ~default:Itv.nat ~f:Itv.of_int in
    { bot with
      arrayblk= ArrayBlk.make_c allocsite ~offset ~size ~stride
    ; offset_sym= Relation.Sym.of_allocsite_offset allocsite
    ; size_sym= Relation.Sym.of_allocsite_size allocsite
    ; traces }


  let of_java_array_alloc : Allocsite.t -> length:Itv.t -> traces:TraceSet.t -> t =
   fun allocsite ~length ~traces ->
    { bot with
      arrayblk= ArrayBlk.make_java allocsite ~length
    ; size_sym= Relation.Sym.of_allocsite_size allocsite
    ; traces }


  let of_literal_string : Typ.IntegerWidths.t -> string -> t =
   fun integer_type_widths s ->
    let allocsite = Allocsite.literal_string s in
    let stride = Some (integer_type_widths.char_width / 8) in
    let offset = Itv.zero in
    let size = Itv.of_int (String.length s + 1) in
    of_c_array_alloc allocsite ~stride ~offset ~size ~traces:TraceSet.bottom


  let deref_of_literal_string s =
    let max_char = String.fold s ~init:0 ~f:(fun acc c -> max acc (Char.to_int c)) in
    of_itv (Itv.set_lb_zero (Itv.of_int max_char))


  let modify_itv : Itv.t -> t -> t = fun i x -> {x with itv= i}

  let unknown_bit : t -> t = fun x -> {x with itv= Itv.top; sym= Relation.Sym.top}

  let neg : t -> t = fun x -> {x with itv= Itv.neg x.itv; sym= Relation.Sym.top}

  let lnot : t -> t = fun x -> {x with itv= Itv.lnot x.itv |> Itv.of_bool; sym= Relation.Sym.top}

  let lift_itv : (Itv.t -> Itv.t -> Itv.t) -> ?f_trace:_ -> t -> t -> t =
   fun f ?f_trace x y ->
    let itv = f x.itv y.itv in
    let itv_thresholds = ItvThresholds.join x.itv_thresholds y.itv_thresholds in
    let traces =
      match f_trace with
      | Some f_trace ->
          f_trace x.traces y.traces
      | None -> (
        match (Itv.eq itv x.itv, Itv.eq itv y.itv) with
        | true, false ->
            x.traces
        | false, true ->
            y.traces
        | true, true | false, false ->
            TraceSet.join x.traces y.traces )
    in
    {bot with itv; itv_thresholds; traces}


  let lift_cmp_itv : (Itv.t -> Itv.t -> Boolean.t) -> Boolean.EqualOrder.t -> t -> t -> t =
   fun cmp_itv cmp_loc x y ->
    let b =
      match
        ( x.itv
        , PowLoc.is_bot x.powloc
        , ArrayBlk.is_bot x.arrayblk
        , y.itv
        , PowLoc.is_bot y.powloc
        , ArrayBlk.is_bot y.arrayblk )
      with
      | NonBottom _, true, true, NonBottom _, true, true ->
          cmp_itv x.itv y.itv
      | Bottom, false, true, Bottom, false, true ->
          PowLoc.lift_cmp cmp_loc x.powloc y.powloc
      | Bottom, true, false, Bottom, true, false ->
          ArrayBlk.lift_cmp_itv cmp_itv cmp_loc x.arrayblk y.arrayblk
      | _ ->
          Boolean.Top
    in
    let itv = Itv.of_bool b in
    {bot with itv; traces= TraceSet.join x.traces y.traces}


  let plus_a = lift_itv Itv.plus

  let minus_a = lift_itv Itv.minus

  let get_iterator_itv : t -> t =
   fun i ->
    let itv = Itv.get_iterator_itv i.itv in
    of_itv itv ~traces:i.traces


  let mult = lift_itv Itv.mult

  let div = lift_itv Itv.div

  let mod_sem = lift_itv Itv.mod_sem

  let shiftlt = lift_itv Itv.shiftlt

  let shiftrt = lift_itv Itv.shiftrt

  let band_sem = lift_itv Itv.band_sem

  let lt_sem : t -> t -> t = lift_cmp_itv Itv.lt_sem Boolean.EqualOrder.strict_cmp

  let gt_sem : t -> t -> t = lift_cmp_itv Itv.gt_sem Boolean.EqualOrder.strict_cmp

  let le_sem : t -> t -> t = lift_cmp_itv Itv.le_sem Boolean.EqualOrder.loose_cmp

  let ge_sem : t -> t -> t = lift_cmp_itv Itv.ge_sem Boolean.EqualOrder.loose_cmp

  let eq_sem : t -> t -> t = lift_cmp_itv Itv.eq_sem Boolean.EqualOrder.eq

  let ne_sem : t -> t -> t = lift_cmp_itv Itv.ne_sem Boolean.EqualOrder.ne

  let land_sem : t -> t -> t = lift_cmp_itv Itv.land_sem Boolean.EqualOrder.top

  let lor_sem : t -> t -> t = lift_cmp_itv Itv.lor_sem Boolean.EqualOrder.top

  (* TODO: get rid of those cases *)
  let warn_against_pruning_multiple_values : t -> t =
   fun x ->
    if x.represents_multiple_values && Config.write_html then
      L.d_printfln_escaped ~color:Pp.Red "Pruned %a that represents multiple values" pp x ;
    x


  let lift_prune1 : (Itv.t -> Itv.t) -> t -> t =
   fun f x -> warn_against_pruning_multiple_values {x with itv= f x.itv}


  let lift_prune_length1 : (Itv.t -> Itv.t) -> t -> t =
   fun f x ->
    warn_against_pruning_multiple_values {x with arrayblk= ArrayBlk.transform_length ~f x.arrayblk}


  let lift_prune2 :
      (Itv.t -> Itv.t -> Itv.t) -> (ArrayBlk.t -> ArrayBlk.t -> ArrayBlk.t) -> t -> t -> t =
   fun f g x y ->
    let itv = f x.itv y.itv in
    let itv_thresholds =
      Option.value_map (Itv.is_const y.itv) ~default:x.itv_thresholds ~f:(fun z ->
          x.itv_thresholds
          |> ItvThresholds.add Z.(z - one)
          |> ItvThresholds.add z
          |> ItvThresholds.add Z.(z + one) )
    in
    let arrayblk = g x.arrayblk y.arrayblk in
    if
      phys_equal itv x.itv
      && phys_equal itv_thresholds x.itv_thresholds
      && phys_equal arrayblk x.arrayblk
    then (* x hasn't changed, don't join traces *)
      x
    else
      warn_against_pruning_multiple_values
        {x with itv; itv_thresholds; arrayblk; traces= TraceSet.join x.traces y.traces}


  let prune_eq_zero : t -> t = lift_prune1 Itv.prune_eq_zero

  let prune_ne_zero : t -> t = lift_prune1 Itv.prune_ne_zero

  let prune_ge_one : t -> t = lift_prune1 Itv.prune_ge_one

  let prune_length_eq_zero : t -> t = lift_prune_length1 Itv.prune_eq_zero

  let prune_length_ge_one : t -> t = lift_prune_length1 Itv.prune_ge_one

  let prune_comp : Binop.t -> t -> t -> t =
   fun c -> lift_prune2 (Itv.prune_comp c) (ArrayBlk.prune_comp c)


  let is_null : t -> bool =
   fun x -> Itv.is_false x.itv && PowLoc.is_bot x.powloc && ArrayBlk.is_bot x.arrayblk


  let prune_eq : t -> t -> t =
   fun x y ->
    if is_null y then {x with itv= Itv.zero; powloc= PowLoc.bot; arrayblk= ArrayBlk.bot}
    else lift_prune2 Itv.prune_eq ArrayBlk.prune_eq x y


  let prune_ne : t -> t -> t = lift_prune2 Itv.prune_ne ArrayBlk.prune_ne

  let is_pointer_to_non_array x = (not (PowLoc.is_bot x.powloc)) && ArrayBlk.is_bot x.arrayblk

  (* In the pointer arithmetics, it returns top, if we cannot
     precisely follow the physical memory model, e.g., (&x + 1). *)
  let lift_pi : (ArrayBlk.t -> Itv.t -> ArrayBlk.t) -> t -> t -> t =
   fun f x y ->
    let traces = TraceSet.join x.traces y.traces in
    if is_pointer_to_non_array x then {bot with itv= Itv.top; traces}
    else {bot with arrayblk= f x.arrayblk y.itv; traces}


  let plus_pi : t -> t -> t = fun x y -> lift_pi ArrayBlk.plus_offset x y

  let minus_pi : t -> t -> t = fun x y -> lift_pi ArrayBlk.minus_offset x y

  let minus_pp : t -> t -> t =
   fun x y ->
    let itv =
      if is_pointer_to_non_array x && is_pointer_to_non_array y then Itv.top
      else ArrayBlk.diff x.arrayblk y.arrayblk
    in
    {bot with itv; traces= TraceSet.join x.traces y.traces}


  let get_symbols : t -> Itv.SymbolSet.t =
   fun x -> Itv.SymbolSet.union (Itv.get_symbols x.itv) (ArrayBlk.get_symbols x.arrayblk)


  let normalize : t -> t =
   fun x -> {x with itv= Itv.normalize x.itv; arrayblk= ArrayBlk.normalize x.arrayblk}


  let subst : t -> eval_sym_trace -> Location.t -> t =
   fun x {eval_sym; trace_of_sym; eval_locpath} location ->
    let symbols = get_symbols x in
    let traces_caller =
      Itv.SymbolSet.fold
        (fun symbol traces -> TraceSet.join (trace_of_sym symbol) traces)
        symbols TraceSet.bottom
    in
    let traces = TraceSet.call location ~traces_caller ~traces_callee:x.traces in
    let powloc = PowLoc.subst x.powloc eval_locpath in
    let powloc_from_arrayblk, arrayblk = ArrayBlk.subst x.arrayblk eval_sym eval_locpath in
    { x with
      itv= Itv.subst x.itv eval_sym
    ; powloc= PowLoc.join powloc powloc_from_arrayblk
    ; arrayblk
    ; traces }
    (* normalize bottom *)
    |> normalize


  let add_assign_trace_elem location locs x =
    let traces = Trace.(Set.add_elem location (Assign locs)) x.traces in
    {x with traces}


  let set_array_length : Location.t -> length:t -> t -> t =
   fun location ~length v ->
    { v with
      arrayblk= ArrayBlk.set_length length.itv v.arrayblk
    ; traces= Trace.(Set.add_elem location ArrayDeclaration) length.traces }


  let transform_array_length : Location.t -> f:(Itv.t -> Itv.t) -> t -> t =
   fun location ~f v ->
    { v with
      arrayblk= ArrayBlk.transform_length ~f v.arrayblk
    ; traces= Trace.(Set.add_elem location (through ~risky_fun:None)) v.traces }


  let set_array_offset : Location.t -> Itv.t -> t -> t =
   fun location offset v ->
    { v with
      arrayblk= ArrayBlk.set_offset offset v.arrayblk
    ; traces= Trace.(Set.add_elem location (through ~risky_fun:None)) v.traces }


  let set_array_stride : Z.t -> t -> t =
   fun new_stride v ->
    PhysEqual.optim1 v ~res:{v with arrayblk= ArrayBlk.set_stride new_stride v.arrayblk}


  let unknown_locs = of_pow_loc PowLoc.unknown ~traces:TraceSet.bottom

  let is_mone x = Itv.is_mone (get_itv x)

  let of_path tenv ~may_last_field integer_type_widths location typ path =
    let traces_of_loc l =
      let trace = if Loc.is_global l then Trace.Global l else Trace.Parameter l in
      TraceSet.singleton location trace
    in
    let is_java = Language.curr_language_is Java in
    L.d_printfln_escaped "Val.of_path %a : %a%s%s" SPath.pp_partial path (Typ.pp Pp.text) typ
      (if may_last_field then ", may_last_field" else "")
      (if is_java then ", is_java" else "") ;
    match typ.Typ.desc with
    | Tint _ | Tfloat _ | Tvoid | Tfun _ | TVar _ ->
        let l = Loc.of_path path in
        let traces = traces_of_loc l in
        let unsigned = Typ.is_unsigned_int typ in
        of_itv ~traces (Itv.of_normal_path ~unsigned path)
    | Tptr (elt, _) ->
        if is_java || SPath.is_this path then
          let deref_kind =
            if is_java then SPath.Deref_JavaPointer else SPath.Deref_COneValuePointer
          in
          let deref_path = SPath.deref ~deref_kind path in
          let l = Loc.of_path deref_path in
          let traces = traces_of_loc l in
          {bot with powloc= PowLoc.singleton l; traces}
        else
          let deref_kind = SPath.Deref_CPointer in
          let deref_path = SPath.deref ~deref_kind path in
          let l = Loc.of_path deref_path in
          let traces = traces_of_loc l in
          let arrayblk =
            let allocsite = Allocsite.make_symbol deref_path in
            let stride =
              match elt.Typ.desc with
              | Typ.Tint ikind ->
                  Itv.of_int (Typ.width_of_ikind integer_type_widths ikind)
              | _ ->
                  Itv.nat
            in
            let offset = Itv.of_offset_path path in
            let size = Itv.of_length_path path in
            ArrayBlk.make_c allocsite ~stride ~offset ~size
          in
          {bot with arrayblk; traces}
    | Tstruct typename -> (
      match BufferOverrunTypModels.dispatch tenv typename with
      | Some (CArray {deref_kind; length}) ->
          let deref_path = SPath.deref ~deref_kind path in
          let l = Loc.of_path deref_path in
          let traces = traces_of_loc l in
          let allocsite = Allocsite.make_symbol deref_path in
          let offset = Itv.zero in
          let size = Itv.of_int_lit length in
          of_c_array_alloc allocsite ~stride:None ~offset ~size ~traces
      | Some JavaCollection ->
          let deref_path = SPath.deref ~deref_kind:Deref_ArrayIndex path in
          let l = Loc.of_path deref_path in
          let traces = traces_of_loc l in
          let allocsite = Allocsite.make_symbol deref_path in
          let length = Itv.of_length_path path in
          of_java_array_alloc allocsite ~length ~traces
      | None ->
          let l = Loc.of_path path in
          let traces = traces_of_loc l in
          of_loc ~traces l )
    | Tarray {length; stride} ->
        let deref_path = SPath.deref ~deref_kind:Deref_ArrayIndex path in
        let l = Loc.of_path deref_path in
        let traces = traces_of_loc l in
        let allocsite = Allocsite.make_symbol deref_path in
        let size =
          match length with
          | None (* IncompleteArrayType, no-size flexible array *) ->
              Itv.of_length_path path
          | Some length
            when may_last_field && (IntLit.iszero length || IntLit.isone length)
                 (* 0/1-sized flexible array *) ->
              Itv.of_length_path path
          | Some length ->
              Itv.of_big_int (IntLit.to_big_int length)
        in
        if is_java then of_java_array_alloc allocsite ~length:size ~traces
        else
          let stride = Option.map stride ~f:(fun n -> IntLit.to_int_exn n) in
          let offset = Itv.zero in
          of_c_array_alloc allocsite ~stride ~offset ~size ~traces


  let on_demand : default:t -> ?typ:Typ.t -> OndemandEnv.t -> Loc.t -> t =
   fun ~default ?typ {tenv; typ_of_param_path; may_last_field; entry_location; integer_type_widths}
       l ->
    let do_on_demand path typ =
      let may_last_field = may_last_field path in
      let path = OndemandEnv.canonical_path typ_of_param_path path in
      of_path tenv ~may_last_field integer_type_widths entry_location typ path
    in
    match Loc.is_literal_string l with
    | Some s ->
        deref_of_literal_string s
    | None -> (
      match Loc.is_literal_string_strlen l with
      | Some s ->
          of_itv (Itv.of_int (String.length s))
      | None -> (
        match Loc.get_path l with
        | None ->
            L.d_printfln_escaped "Val.on_demand for %a -> no path" Loc.pp l ;
            default
        | Some path -> (
          match typ_of_param_path path with
          | None -> (
            match typ with
            | Some typ when Loc.is_global l ->
                L.d_printfln_escaped "Val.on_demand for %a -> global" Loc.pp l ;
                do_on_demand path typ
            | _ ->
                L.d_printfln_escaped "Val.on_demand for %a -> no type" Loc.pp l ;
                default )
          | Some typ ->
              L.d_printfln_escaped "Val.on_demand for %a" Loc.pp l ;
              do_on_demand path typ ) ) )


  module Itv = struct
    let zero_255 = of_itv Itv.zero_255

    let m1_255 = of_itv Itv.m1_255

    let nat = of_itv Itv.nat

    let pos = of_itv Itv.pos

    let top = of_itv Itv.top

    let unknown_bool = of_itv Itv.unknown_bool

    let zero = of_itv Itv.zero
  end
end

module StackLocs = struct
  include AbstractDomain.FiniteSet (Loc)

  let bot = empty
end

module MemPure = struct
  include AbstractDomain.Map (Loc) (Val)

  let bot = empty

  let range :
      filter_loc:(Loc.t -> LoopHeadLoc.t option) -> t -> Polynomials.NonNegativePolynomial.t =
   fun ~filter_loc mem ->
    fold
      (fun loc v acc ->
        match filter_loc loc with
        | Some loop_head_loc ->
            v |> Val.get_itv |> Itv.range loop_head_loc |> Itv.ItvRange.to_top_lifted_polynomial
            |> Polynomials.NonNegativePolynomial.mult acc
        | None ->
            acc )
      mem Polynomials.NonNegativePolynomial.one


  let join oenv astate1 astate2 =
    if phys_equal astate1 astate2 then astate1
    else
      merge
        (fun l v1_opt v2_opt ->
          match (v1_opt, v2_opt) with
          | Some v1, Some v2 ->
              Some (Val.join v1 v2)
          | Some v1, None | None, Some v1 ->
              let v2 = Val.on_demand ~default:Val.bot oenv l in
              Some (Val.join v1 v2)
          | None, None ->
              None )
        astate1 astate2


  let widen oenv ~prev ~next ~num_iters =
    if phys_equal prev next then prev
    else
      merge
        (fun l v1_opt v2_opt ->
          match (v1_opt, v2_opt) with
          | Some v1, Some v2 ->
              Some (Val.widen ~prev:v1 ~next:v2 ~num_iters)
          | Some v1, None ->
              let v2 = Val.on_demand ~default:Val.bot oenv l in
              Some (Val.widen ~prev:v1 ~next:v2 ~num_iters)
          | None, Some v2 ->
              let v1 = Val.on_demand ~default:Val.bot oenv l in
              Some (Val.widen ~prev:v1 ~next:v2 ~num_iters)
          | None, None ->
              None )
        prev next
end

module AliasTarget = struct
  type t =
    | Simple of Loc.t
    | SimplePlusA of Loc.t * IntLit.t
    | Empty of Loc.t
    | Fgets of Loc.t
    | Nullity of Loc.t
  [@@deriving compare]

  let equal = [%compare.equal: t]

  let pp fmt = function
    | Simple l ->
        Loc.pp fmt l
    | SimplePlusA (l, i) ->
        if IntLit.isnegative i then F.fprintf fmt "%a-%a" Loc.pp l IntLit.pp (IntLit.neg i)
        else F.fprintf fmt "%a+%a" Loc.pp l IntLit.pp i
    | Empty l ->
        F.fprintf fmt "empty(%a)" Loc.pp l
    | Fgets l ->
        F.fprintf fmt "fgets(%a)" Loc.pp l
    | Nullity l ->
        F.fprintf fmt "nullity(%a)" Loc.pp l


  let fgets l = Fgets l

  let nullity l = Nullity l

  let use l = function
    | Simple l' | SimplePlusA (l', _) | Empty l' | Fgets l' | Nullity l' ->
        Loc.equal l l'


  let loc_map x ~f =
    match x with
    | Simple l ->
        Option.map (f l) ~f:(fun l -> Simple l)
    | SimplePlusA (l, i) ->
        Option.map (f l) ~f:(fun l -> SimplePlusA (l, i))
    | Empty l ->
        Option.map (f l) ~f:(fun l -> Empty l)
    | Fgets l ->
        Option.map (f l) ~f:(fun l -> Fgets l)
    | Nullity l ->
        Option.map (f l) ~f:(fun l -> Nullity l)


  let ( <= ) ~lhs ~rhs = equal lhs rhs

  let join x y =
    assert (equal x y) ;
    x


  let widen ~prev ~next ~num_iters:_ = join prev next

  let is_unknown = function
    | Simple l | SimplePlusA (l, _) | Empty l | Fgets l | Nullity l ->
        Loc.is_unknown l
end

(* Relations between values of logical variables(registers) and program variables

   "AliasTarget.Simple relation": Since Sil distinguishes logical and program variables, we need a
   relation for pruning values of program variables.  For example, a C statement "if(x){...}" is
   translated to "%r=load(x); if(%r){...}" in Sil.  At the load statement, we record the alias
   between the values of %r and x, then we can prune not only the value of %r, but also that of x
   inside the if branch.

   "AliasTarget.Empty relation": For pruning vector.length with vector::empty() results, we adopt a
   specific relation between %r and v, where %r=v.empty().  So, if %r!=0, v's array length
   ([v.length]) is pruned by v.length=0.  On the other hand, if %r==0, v's array length is pruned by
   v.length>=1.

   "AliasTarget.Nullity relation": For pruning vector.length with vector::empty() results, we adopt
   a specific relation between %r and i, where %r=v.empty() and i=v.length.  So, if %r!=0, i is
   pruned by i=0.  On the other hand, if %r==0, i is pruned by i>=1.  This is similar to the
   AliasTarget.Empty relation, but is used when there is a program variable [i] for the length of
   the array. *)
module AliasMap = struct
  include AbstractDomain.Map (Ident) (AliasTarget)

  let pp : F.formatter -> t -> unit =
   fun fmt x ->
    if not (is_empty x) then
      let pp_sep fmt () = F.fprintf fmt ", @," in
      let pp1 fmt (k, v) = F.fprintf fmt "%a=%a" Ident.pp k AliasTarget.pp v in
      F.pp_print_list ~pp_sep pp1 fmt (bindings x)


  let load : Ident.t -> AliasTarget.t -> t -> t =
   fun id a x -> if not (AliasTarget.is_unknown a) then add id a x else x


  let store : Loc.t -> t -> t = fun l m -> filter (fun _ y -> not (AliasTarget.use l y)) m

  let find : Ident.t -> t -> AliasTarget.t option = find_opt
end

module AliasRet = struct
  include AbstractDomain.Flat (AliasTarget)

  let pp : F.formatter -> t -> unit = fun fmt x -> F.pp_print_string fmt "ret=" ; pp fmt x
end

module Alias = struct
  type t = {map: AliasMap.t; ret: AliasRet.t}

  let ( <= ) ~lhs ~rhs =
    if phys_equal lhs rhs then true
    else AliasMap.( <= ) ~lhs:lhs.map ~rhs:rhs.map && AliasRet.( <= ) ~lhs:lhs.ret ~rhs:rhs.ret


  let join x y =
    if phys_equal x y then x else {map= AliasMap.join x.map y.map; ret= AliasRet.join x.ret y.ret}


  let widen ~prev ~next ~num_iters =
    if phys_equal prev next then prev
    else
      { map= AliasMap.widen ~prev:prev.map ~next:next.map ~num_iters
      ; ret= AliasRet.widen ~prev:prev.ret ~next:next.ret ~num_iters }


  let pp fmt x =
    F.fprintf fmt "@[<hov 2>{ %a%s%a }@]" AliasMap.pp x.map
      (if AliasMap.is_empty x.map then "" else ", ")
      AliasRet.pp x.ret


  let bot : t = {map= AliasMap.empty; ret= AliasRet.bottom}

  let lift_map : (AliasMap.t -> AliasMap.t) -> t -> t = fun f a -> {a with map= f a.map}

  let bind_map : (AliasMap.t -> 'a) -> t -> 'a = fun f a -> f a.map

  let find : Ident.t -> t -> AliasTarget.t option = fun x -> bind_map (AliasMap.find x)

  let find_ret : t -> AliasTarget.t option = fun x -> AliasRet.get x.ret

  let load : Ident.t -> AliasTarget.t -> t -> t = fun id loc -> lift_map (AliasMap.load id loc)

  let store_simple : Loc.t -> Exp.t -> t -> t =
   fun loc e a ->
    let a = lift_map (AliasMap.store loc) a in
    match e with
    | Exp.Var l when Loc.is_return loc ->
        let update_ret retl = {a with ret= AliasRet.v retl} in
        Option.value_map (find l a) ~default:a ~f:update_ret
    | Exp.BinOp (Binop.PlusA _, Exp.Var id, Exp.Const (Const.Cint i))
    | Exp.BinOp (Binop.PlusA _, Exp.Const (Const.Cint i), Exp.Var id) ->
        lift_map (AliasMap.load id (AliasTarget.SimplePlusA (loc, IntLit.neg i))) a
    | Exp.BinOp (Binop.MinusA _, Exp.Var id, Exp.Const (Const.Cint i)) ->
        lift_map (AliasMap.load id (AliasTarget.SimplePlusA (loc, i))) a
    | _ ->
        a


  let store_empty : Val.t -> Loc.t -> t -> t =
   fun formal loc a ->
    let a = lift_map (AliasMap.store loc) a in
    let locs = Val.get_all_locs formal in
    match PowLoc.is_singleton_or_more locs with
    | IContainer.Singleton loc ->
        {a with ret= AliasRet.v (AliasTarget.nullity loc)}
    | _ ->
        a


  let fgets : Ident.t -> PowLoc.t -> t -> t =
   fun id locs a ->
    let a = PowLoc.fold (fun loc acc -> lift_map (AliasMap.store loc) acc) locs a in
    match PowLoc.is_singleton_or_more locs with
    | IContainer.Singleton loc ->
        load id (AliasTarget.fgets loc) a
    | _ ->
        a


  let remove_temp : Ident.t -> t -> t = fun temp -> lift_map (AliasMap.remove temp)
end

module CoreVal = struct
  type t = Val.t

  let compare x y =
    let r = Itv.compare (Val.get_itv x) (Val.get_itv y) in
    if r <> 0 then r
    else
      let r = PowLoc.compare (Val.get_pow_loc x) (Val.get_pow_loc y) in
      if r <> 0 then r else ArrayBlk.compare (Val.get_array_blk x) (Val.get_array_blk y)


  let pp fmt x =
    F.fprintf fmt "(%a, %a, %a)" Itv.pp (Val.get_itv x) PowLoc.pp (Val.get_pow_loc x) ArrayBlk.pp
      (Val.get_array_blk x)


  let is_symbolic v =
    let itv = Val.get_itv v in
    if Itv.is_bottom itv then ArrayBlk.is_symbolic (Val.get_array_blk v) else Itv.is_symbolic itv


  let is_empty v =
    Itv.is_bottom (Val.get_itv v)
    && PowLoc.is_empty (Val.get_pow_loc v)
    && ArrayBlk.is_empty (Val.get_array_blk v)
end

module PruningExp = struct
  type t = Unknown | Binop of {bop: Binop.t; lhs: CoreVal.t; rhs: CoreVal.t} [@@deriving compare]

  let ( <= ) ~lhs ~rhs =
    match (lhs, rhs) with
    | _, Unknown ->
        true
    | Unknown, _ ->
        false
    | Binop {bop= bop1; lhs= lhs1; rhs= rhs1}, Binop {bop= bop2; lhs= lhs2; rhs= rhs2} ->
        Binop.equal bop1 bop2 && Val.( <= ) ~lhs:lhs1 ~rhs:lhs2 && Val.( <= ) ~lhs:rhs1 ~rhs:rhs2


  let join x y =
    match (x, y) with
    | Binop {bop= bop1; lhs= lhs1; rhs= rhs1}, Binop {bop= bop2; lhs= lhs2; rhs= rhs2}
      when Binop.equal bop1 bop2 ->
        Binop {bop= bop1; lhs= Val.join lhs1 lhs2; rhs= Val.join rhs1 rhs2}
    | _, _ ->
        Unknown


  let widen ~prev ~next ~num_iters =
    match (prev, next) with
    | Binop {bop= bop1; lhs= lhs1; rhs= rhs1}, Binop {bop= bop2; lhs= lhs2; rhs= rhs2}
      when Binop.equal bop1 bop2 ->
        Binop
          { bop= bop1
          ; lhs= Val.widen ~prev:lhs1 ~next:lhs2 ~num_iters
          ; rhs= Val.widen ~prev:rhs1 ~next:rhs2 ~num_iters }
    | _, _ ->
        Unknown


  let pp fmt x =
    match x with
    | Unknown ->
        F.pp_print_string fmt "Unknown"
    | Binop {bop; lhs; rhs} ->
        F.fprintf fmt "(%a %s %a)" CoreVal.pp lhs (Binop.str Pp.text bop) CoreVal.pp rhs


  let make bop ~lhs ~rhs = Binop {bop; lhs; rhs}

  let is_unknown = function Unknown -> true | Binop _ -> false

  let is_symbolic = function
    | Unknown ->
        false
    | Binop {lhs; rhs} ->
        CoreVal.is_symbolic lhs || CoreVal.is_symbolic rhs


  let is_empty =
    let le_false v = Itv.( <= ) ~lhs:(Val.get_itv v) ~rhs:Itv.zero in
    function
    | Unknown ->
        false
    | Binop {bop= Lt; lhs; rhs} ->
        le_false (Val.lt_sem lhs rhs)
    | Binop {bop= Gt; lhs; rhs} ->
        le_false (Val.gt_sem lhs rhs)
    | Binop {bop= Le; lhs; rhs} ->
        le_false (Val.le_sem lhs rhs)
    | Binop {bop= Ge; lhs; rhs} ->
        le_false (Val.ge_sem lhs rhs)
    | Binop {bop= Eq; lhs; rhs} ->
        le_false (Val.eq_sem lhs rhs)
    | Binop {bop= Ne; lhs; rhs} ->
        le_false (Val.ne_sem lhs rhs)
    | Binop _ ->
        assert false


  let subst x eval_sym_trace location =
    match x with
    | Unknown ->
        Unknown
    | Binop {bop; lhs; rhs} ->
        Binop
          { bop
          ; lhs= Val.subst lhs eval_sym_trace location
          ; rhs= Val.subst rhs eval_sym_trace location }
end

module PrunedVal = struct
  type t = {v: CoreVal.t; pruning_exp: PruningExp.t} [@@deriving compare]

  let ( <= ) ~lhs ~rhs =
    Val.( <= ) ~lhs:lhs.v ~rhs:rhs.v && PruningExp.( <= ) ~lhs:lhs.pruning_exp ~rhs:rhs.pruning_exp


  let join x y = {v= Val.join x.v y.v; pruning_exp= PruningExp.join x.pruning_exp y.pruning_exp}

  let widen ~prev ~next ~num_iters =
    { v= Val.widen ~prev:prev.v ~next:next.v ~num_iters
    ; pruning_exp= PruningExp.widen ~prev:prev.pruning_exp ~next:next.pruning_exp ~num_iters }


  let pp fmt x =
    CoreVal.pp fmt x.v ;
    if not (PruningExp.is_unknown x.pruning_exp) then
      F.fprintf fmt " by %a" PruningExp.pp x.pruning_exp


  let make v pruning_exp = {v; pruning_exp}

  let get_val x = x.v

  let subst {v; pruning_exp} eval_sym_trace location =
    { v= Val.subst v eval_sym_trace location
    ; pruning_exp= PruningExp.subst pruning_exp eval_sym_trace location }


  let is_symbolic {v; pruning_exp} = CoreVal.is_symbolic v || PruningExp.is_symbolic pruning_exp

  let is_empty {v; pruning_exp} = CoreVal.is_empty v || PruningExp.is_empty pruning_exp
end

(* [PrunePairs] is a map from abstract locations to abstract values that represents pruned results
   in the latest pruning.  It uses [InvertedMap] because more pruning means smaller abstract
   states. *)
module PrunePairs = struct
  include AbstractDomain.InvertedMap (Loc) (PrunedVal)

  let forget locs x = filter (fun l _ -> not (PowLoc.mem l locs)) x

  let is_reachable x = not (exists (fun _ v -> PrunedVal.is_empty v) x)
end

module LatestPrune = struct
  (* Latest p: The pruned pairs 'p' has pruning information (which
     abstract locations are updated by which abstract values) in the
     latest pruning.

     TrueBranch (x, p): After a pruning, the variable 'x' is assigned
     by 1.  There is no other memory updates after the latest pruning.

     FalseBranch (x, p): After a pruning, the variable 'x' is assigned
     by 0.  There is no other memory updates after the latest pruning.

     V (x, ptrue, pfalse): After two non-sequential prunings ('ptrue'
     and 'pfalse'), the variable 'x' is assigned by 1 and 0,
     respectively.  There is no other memory updates after the latest
     prunings.

     Top: No information about the latest pruning. *)
  type t =
    | Latest of PrunePairs.t
    | TrueBranch of Pvar.t * PrunePairs.t
    | FalseBranch of Pvar.t * PrunePairs.t
    | V of Pvar.t * PrunePairs.t * PrunePairs.t
    | Top

  let pvar_pp = Pvar.pp Pp.text

  let pp fmt = function
    | Top ->
        ()
    | Latest p ->
        F.fprintf fmt "LatestPrune: latest %a" PrunePairs.pp p
    | TrueBranch (v, p) ->
        F.fprintf fmt "LatestPrune: true(%a) %a" pvar_pp v PrunePairs.pp p
    | FalseBranch (v, p) ->
        F.fprintf fmt "LatestPrune: false(%a) %a" pvar_pp v PrunePairs.pp p
    | V (v, p1, p2) ->
        F.fprintf fmt "LatestPrune: v(%a) %a / %a" pvar_pp v PrunePairs.pp p1 PrunePairs.pp p2


  let ( <= ) ~lhs ~rhs =
    if phys_equal lhs rhs then true
    else
      match (lhs, rhs) with
      | _, Top ->
          true
      | Top, _ ->
          false
      | Latest p1, Latest p2 ->
          PrunePairs.( <= ) ~lhs:p1 ~rhs:p2
      | TrueBranch (x1, p1), TrueBranch (x2, p2)
      | FalseBranch (x1, p1), FalseBranch (x2, p2)
      | TrueBranch (x1, p1), V (x2, p2, _)
      | FalseBranch (x1, p1), V (x2, _, p2) ->
          Pvar.equal x1 x2 && PrunePairs.( <= ) ~lhs:p1 ~rhs:p2
      | V (x1, ptrue1, pfalse1), V (x2, ptrue2, pfalse2) ->
          Pvar.equal x1 x2
          && PrunePairs.( <= ) ~lhs:ptrue1 ~rhs:ptrue2
          && PrunePairs.( <= ) ~lhs:pfalse1 ~rhs:pfalse2
      | _, _ ->
          false


  let join x y =
    match (x, y) with
    | _, _ when ( <= ) ~lhs:x ~rhs:y ->
        y
    | _, _ when ( <= ) ~lhs:y ~rhs:x ->
        x
    | Latest p1, Latest p2 ->
        Latest (PrunePairs.join p1 p2)
    | FalseBranch (x1, p1), FalseBranch (x2, p2) when Pvar.equal x1 x2 ->
        FalseBranch (x1, PrunePairs.join p1 p2)
    | TrueBranch (x1, p1), TrueBranch (x2, p2) when Pvar.equal x1 x2 ->
        TrueBranch (x1, PrunePairs.join p1 p2)
    | FalseBranch (x', pfalse), TrueBranch (y', ptrue)
    | TrueBranch (x', ptrue), FalseBranch (y', pfalse)
      when Pvar.equal x' y' ->
        V (x', ptrue, pfalse)
    | TrueBranch (x1, ptrue1), V (x2, ptrue2, pfalse)
    | V (x2, ptrue2, pfalse), TrueBranch (x1, ptrue1)
      when Pvar.equal x1 x2 ->
        V (x1, PrunePairs.join ptrue1 ptrue2, pfalse)
    | FalseBranch (x1, pfalse1), V (x2, ptrue, pfalse2)
    | V (x2, ptrue, pfalse2), FalseBranch (x1, pfalse1)
      when Pvar.equal x1 x2 ->
        V (x1, ptrue, PrunePairs.join pfalse1 pfalse2)
    | V (x1, ptrue1, pfalse1), V (x2, ptrue2, pfalse2) when Pvar.equal x1 x2 ->
        V (x1, PrunePairs.join ptrue1 ptrue2, PrunePairs.join pfalse1 pfalse2)
    | _, _ ->
        Top


  let widen ~prev ~next ~num_iters:_ = join prev next

  let top = Top

  let forget locs =
    let is_mem_locs x = PowLoc.mem (Loc.of_pvar x) locs in
    function
    | Latest p ->
        Latest (PrunePairs.forget locs p)
    | TrueBranch (x, p) ->
        if is_mem_locs x then Top else TrueBranch (x, PrunePairs.forget locs p)
    | FalseBranch (x, p) ->
        if is_mem_locs x then Top else FalseBranch (x, PrunePairs.forget locs p)
    | V (x, ptrue, pfalse) ->
        if is_mem_locs x then Top
        else V (x, PrunePairs.forget locs ptrue, PrunePairs.forget locs pfalse)
    | Top ->
        Top


  let replace ~from ~to_ x =
    match x with
    | TrueBranch (x, p) when Pvar.equal x from ->
        TrueBranch (to_, p)
    | FalseBranch (x, p) when Pvar.equal x from ->
        FalseBranch (to_, p)
    | V (x, ptrue, pfalse) when Pvar.equal x from ->
        V (to_, ptrue, pfalse)
    | _ ->
        x
end

module Reachability = struct
  module M = PrettyPrintable.MakePPSet (PrunedVal)

  type t = M.t

  let equal = M.equal

  let pp = M.pp

  (* It keeps only symbolic pruned values, because non-symbolic ones are useless to see the
     reachability. *)
  let add v x = if PrunedVal.is_symbolic v then M.add v x else x

  let of_latest_prune latest_prune =
    let of_prune_pairs p = PrunePairs.fold (fun _ v acc -> add v acc) p M.empty in
    match latest_prune with
    | LatestPrune.Latest p | LatestPrune.TrueBranch (_, p) | LatestPrune.FalseBranch (_, p) ->
        of_prune_pairs p
    | LatestPrune.V (_, ptrue, pfalse) ->
        M.inter (of_prune_pairs ptrue) (of_prune_pairs pfalse)
    | LatestPrune.Top ->
        M.empty


  let make latest_prune = of_latest_prune latest_prune

  let add_latest_prune latest_prune x = M.union x (of_latest_prune latest_prune)

  let subst x eval_sym_trace location =
    let exception Unreachable in
    let subst1 x acc =
      let v = PrunedVal.subst x eval_sym_trace location in
      if PrunedVal.is_empty v then raise Unreachable else add v acc
    in
    match M.fold subst1 x M.empty with x -> `Reachable x | exception Unreachable -> `Unreachable
end

module MemReach = struct
  type 'has_oenv t0 =
    { stack_locs: StackLocs.t
    ; mem_pure: MemPure.t
    ; alias: Alias.t
    ; latest_prune: LatestPrune.t
    ; relation: Relation.t
    ; oenv: ('has_oenv, OndemandEnv.t) GOption.t }

  type no_oenv_t = GOption.none t0

  type t = GOption.some t0

  let init : OndemandEnv.t -> t =
   fun oenv ->
    { stack_locs= StackLocs.bot
    ; mem_pure= MemPure.bot
    ; alias= Alias.bot
    ; latest_prune= LatestPrune.top
    ; relation= Relation.empty
    ; oenv= GOption.GSome oenv }


  let ( <= ) ~lhs ~rhs =
    if phys_equal lhs rhs then true
    else
      StackLocs.( <= ) ~lhs:lhs.stack_locs ~rhs:rhs.stack_locs
      && MemPure.( <= ) ~lhs:lhs.mem_pure ~rhs:rhs.mem_pure
      && Alias.( <= ) ~lhs:lhs.alias ~rhs:rhs.alias
      && LatestPrune.( <= ) ~lhs:lhs.latest_prune ~rhs:rhs.latest_prune
      && Relation.( <= ) ~lhs:lhs.relation ~rhs:rhs.relation


  let widen ~prev ~next ~num_iters =
    if phys_equal prev next then prev
    else (
      assert (phys_equal prev.oenv next.oenv) ;
      let oenv = GOption.value prev.oenv in
      { stack_locs= StackLocs.widen ~prev:prev.stack_locs ~next:next.stack_locs ~num_iters
      ; mem_pure= MemPure.widen oenv ~prev:prev.mem_pure ~next:next.mem_pure ~num_iters
      ; alias= Alias.widen ~prev:prev.alias ~next:next.alias ~num_iters
      ; latest_prune= LatestPrune.widen ~prev:prev.latest_prune ~next:next.latest_prune ~num_iters
      ; relation= Relation.widen ~prev:prev.relation ~next:next.relation ~num_iters
      ; oenv= prev.oenv } )


  let join : t -> t -> t =
   fun x y ->
    assert (phys_equal x.oenv y.oenv) ;
    let oenv = GOption.value x.oenv in
    { stack_locs= StackLocs.join x.stack_locs y.stack_locs
    ; mem_pure= MemPure.join oenv x.mem_pure y.mem_pure
    ; alias= Alias.join x.alias y.alias
    ; latest_prune= LatestPrune.join x.latest_prune y.latest_prune
    ; relation= Relation.join x.relation y.relation
    ; oenv= x.oenv }


  let pp : F.formatter -> _ t0 -> unit =
   fun fmt x ->
    F.fprintf fmt "StackLocs:@;%a@;MemPure:@;%a@;Alias:@;%a@;%a" StackLocs.pp x.stack_locs
      MemPure.pp x.mem_pure Alias.pp x.alias LatestPrune.pp x.latest_prune ;
    if Option.is_some Config.bo_relational_domain then
      F.fprintf fmt "@;Relation:@;%a" Relation.pp x.relation


  let unset_oenv : t -> no_oenv_t = function x -> {x with oenv= GOption.GNone}

  let is_stack_loc : Loc.t -> _ t0 -> bool = fun l m -> StackLocs.mem l m.stack_locs

  let find_opt : Loc.t -> _ t0 -> Val.t option = fun l m -> MemPure.find_opt l m.mem_pure

  let find_stack : Loc.t -> _ t0 -> Val.t = fun l m -> Option.value (find_opt l m) ~default:Val.bot

  let find_heap_default : default:Val.t -> ?typ:Typ.t -> Loc.t -> _ t0 -> Val.t =
   fun ~default ?typ l m ->
    IOption.value_default_f (find_opt l m) ~f:(fun () ->
        GOption.value_map m.oenv ~default ~f:(fun oenv -> Val.on_demand ~default ?typ oenv l) )


  let find_heap : ?typ:Typ.t -> Loc.t -> _ t0 -> Val.t =
   fun ?typ l m -> find_heap_default ~default:Val.Itv.top ?typ l m


  let find : ?typ:Typ.t -> Loc.t -> _ t0 -> Val.t =
   fun ?typ l m -> if is_stack_loc l m then find_stack l m else find_heap ?typ l m


  let find_set : ?typ:Typ.t -> PowLoc.t -> _ t0 -> Val.t =
   fun ?typ locs m ->
    let find_join loc acc = Val.join acc (find ?typ loc m) in
    PowLoc.fold find_join locs Val.bot


  let find_alias : Ident.t -> _ t0 -> AliasTarget.t option = fun k m -> Alias.find k m.alias

  let find_simple_alias : Ident.t -> _ t0 -> (Loc.t * IntLit.t option) option =
   fun k m ->
    match Alias.find k m.alias with
    | Some (AliasTarget.Simple l) ->
        Some (l, None)
    | Some (AliasTarget.SimplePlusA (l, i)) ->
        Some (l, Some i)
    | Some (AliasTarget.Empty _ | AliasTarget.Fgets _ | AliasTarget.Nullity _) | None ->
        None


  let find_ret_alias : _ t0 -> AliasTarget.t option = fun m -> Alias.find_ret m.alias

  let load_alias : Ident.t -> AliasTarget.t -> t -> t =
   fun id loc m -> {m with alias= Alias.load id loc m.alias}


  let store_simple_alias : Loc.t -> Exp.t -> t -> t =
   fun loc e m -> {m with alias= Alias.store_simple loc e m.alias}


  let store_empty_alias : Val.t -> Loc.t -> t -> t =
   fun formal loc m -> {m with alias= Alias.store_empty formal loc m.alias}


  let fgets_alias : Ident.t -> PowLoc.t -> t -> t =
   fun id locs m -> {m with alias= Alias.fgets id locs m.alias}


  let add_stack_loc : Loc.t -> t -> t = fun k m -> {m with stack_locs= StackLocs.add k m.stack_locs}

  let add_stack : Loc.t -> Val.t -> t -> t =
   fun k v m ->
    {m with stack_locs= StackLocs.add k m.stack_locs; mem_pure= MemPure.add k v m.mem_pure}


  let replace_stack : Loc.t -> Val.t -> t -> t =
   fun k v m -> {m with mem_pure= MemPure.add k v m.mem_pure}


  let add_heap : Loc.t -> Val.t -> t -> t =
   fun x v m ->
    let v =
      let sym =
        if Itv.is_bottom (Val.get_itv v) then Relation.Sym.bot else Relation.Sym.of_loc x
      in
      let offset_sym, size_sym =
        if ArrayBlk.is_bot (Val.get_array_blk v) then (Relation.Sym.bot, Relation.Sym.bot)
        else (Relation.Sym.of_loc_offset x, Relation.Sym.of_loc_size x)
      in
      {v with Val.sym; Val.offset_sym; Val.size_sym}
    in
    {m with mem_pure= MemPure.add x v m.mem_pure}


  let add_unknown_from :
      Ident.t -> callee_pname:Typ.Procname.t option -> location:Location.t -> t -> t =
   fun id ~callee_pname ~location m ->
    let val_unknown = Val.unknown_from ~callee_pname ~location in
    add_stack (Loc.of_id id) val_unknown m |> add_heap Loc.unknown val_unknown


  let strong_update : PowLoc.t -> Val.t -> t -> t =
   fun locs v m ->
    let strong_update1 l m = if is_stack_loc l m then replace_stack l v m else add_heap l v m in
    PowLoc.fold strong_update1 locs m


  let transformi_mem : f:(Loc.t -> Val.t -> Val.t) -> PowLoc.t -> t -> t =
   fun ~f locs m ->
    let transform_mem1 l m =
      let add, find =
        if is_stack_loc l m then (replace_stack, find_stack)
        else (add_heap, find_heap_default ~default:Val.bot ?typ:None)
      in
      add l (f l (find l m)) m
    in
    PowLoc.fold transform_mem1 locs m


  let transform_mem : f:(Val.t -> Val.t) -> PowLoc.t -> t -> t =
   fun ~f -> transformi_mem ~f:(fun _ v -> f v)


  let weak_update locs v m =
    transformi_mem
      ~f:(fun l v' -> if Loc.represents_multiple_values l then Val.join v' v else v)
      locs m


  let update_mem : PowLoc.t -> Val.t -> t -> t =
   fun ploc v s ->
    if can_strong_update ploc then strong_update ploc v s
    else (
      L.d_printfln_escaped "Weak update for %a <- %a" PowLoc.pp ploc Val.pp v ;
      weak_update ploc v s )


  let remove_temp : Ident.t -> t -> t =
   fun temp m ->
    let l = Loc.of_id temp in
    { m with
      stack_locs= StackLocs.remove l m.stack_locs
    ; mem_pure= MemPure.remove l m.mem_pure
    ; alias= Alias.remove_temp temp m.alias }


  let remove_temps : Ident.t list -> t -> t =
   fun temps m -> List.fold temps ~init:m ~f:(fun acc temp -> remove_temp temp acc)


  let set_prune_pairs : PrunePairs.t -> t -> t =
   fun prune_pairs m -> {m with latest_prune= LatestPrune.Latest prune_pairs}


  let apply_latest_prune : Exp.t -> t -> t * PrunePairs.t =
    let apply1 l v acc = update_mem (PowLoc.singleton l) (PrunedVal.get_val v) acc in
    fun e m ->
      match (m.latest_prune, e) with
      | LatestPrune.V (x, prunes, _), Exp.Var r
      | LatestPrune.V (x, _, prunes), Exp.UnOp (Unop.LNot, Exp.Var r, _) -> (
        match find_simple_alias r m with
        | Some (Loc.Var (Var.ProgramVar y), None) when Pvar.equal x y ->
            (PrunePairs.fold apply1 prunes m, prunes)
        | _ ->
            (m, PrunePairs.empty) )
      | _ ->
          (m, PrunePairs.empty)


  let update_latest_prune : updated_locs:PowLoc.t -> Exp.t -> Exp.t -> t -> t =
   fun ~updated_locs e1 e2 m ->
    match (e1, e2, m.latest_prune) with
    | Lvar x, Const (Const.Cint i), LatestPrune.Latest p ->
        if IntLit.isone i then {m with latest_prune= LatestPrune.TrueBranch (x, p)}
        else if IntLit.iszero i then {m with latest_prune= LatestPrune.FalseBranch (x, p)}
        else {m with latest_prune= LatestPrune.forget updated_locs m.latest_prune}
    | Lvar return, _, _ when Pvar.is_return return -> (
      match Alias.find_ret m.alias with
      | Some (Simple (Var (ProgramVar pvar))) ->
          {m with latest_prune= LatestPrune.replace ~from:pvar ~to_:return m.latest_prune}
      | _ ->
          m )
    | _, _, _ ->
        {m with latest_prune= LatestPrune.forget updated_locs m.latest_prune}


  let get_latest_prune : t -> LatestPrune.t = fun {latest_prune} -> latest_prune

  let get_reachable_locs_from : (Pvar.t * Typ.t) list -> PowLoc.t -> _ t0 -> PowLoc.t =
    let add_reachable1 ~root loc v acc =
      if Loc.equal root loc then PowLoc.union acc (Val.get_all_locs v)
      else if Loc.is_field_of ~loc:root ~field_loc:loc then PowLoc.add loc acc
      else acc
    in
    let rec add_from_locs heap locs acc = PowLoc.fold (add_from_loc heap) locs acc
    and add_from_loc heap loc acc =
      if PowLoc.mem loc acc then acc
      else
        let reachable_locs = MemPure.fold (add_reachable1 ~root:loc) heap PowLoc.empty in
        add_from_locs heap reachable_locs (PowLoc.add loc acc)
    in
    let add_param_locs formals mem acc =
      let add_loc loc _ acc = if Loc.has_param_path formals loc then PowLoc.add loc acc else acc in
      MemPure.fold add_loc mem acc
    in
    fun formals locs m ->
      let locs = add_param_locs formals m.mem_pure locs in
      add_from_locs m.mem_pure locs PowLoc.empty


  let range :
      filter_loc:(Loc.t -> LoopHeadLoc.t option) -> t -> Polynomials.NonNegativePolynomial.t =
   fun ~filter_loc {mem_pure} -> MemPure.range ~filter_loc mem_pure


  let get_relation : t -> Relation.t = fun m -> m.relation

  let is_relation_unsat : t -> bool = fun m -> Relation.is_unsat m.relation

  let lift_relation : (Relation.t -> Relation.t) -> t -> t =
   fun f m -> {m with relation= f m.relation}


  let meet_constraints : Relation.Constraints.t -> t -> t =
   fun constrs -> lift_relation (Relation.meet_constraints constrs)


  let store_relation :
         PowLoc.t
      -> Relation.SymExp.t option * Relation.SymExp.t option * Relation.SymExp.t option
      -> t
      -> t =
   fun locs symexp_opts -> lift_relation (Relation.store_relation locs symexp_opts)


  let forget_locs : PowLoc.t -> t -> t = fun locs -> lift_relation (Relation.forget_locs locs)

  let init_param_relation : Loc.t -> t -> t = fun loc -> lift_relation (Relation.init_param loc)

  let init_array_relation :
         Allocsite.t
      -> offset_opt:Itv.t option
      -> size:Itv.t
      -> size_exp_opt:Relation.SymExp.t option
      -> t
      -> t =
   fun allocsite ~offset_opt ~size ~size_exp_opt ->
    lift_relation (Relation.init_array allocsite ~offset_opt ~size ~size_exp_opt)


  let instantiate_relation : Relation.SubstMap.t -> caller:t -> callee:no_oenv_t -> t =
   fun subst_map ~caller ~callee ->
    { caller with
      relation= Relation.instantiate subst_map ~caller:caller.relation ~callee:callee.relation }


  (* unsound *)
  let set_first_idx_of_null : Loc.t -> Val.t -> t -> t =
   fun loc idx m -> update_mem (PowLoc.singleton (Loc.of_c_strlen loc)) idx m


  (* unsound *)
  let unset_first_idx_of_null : Loc.t -> Val.t -> t -> t =
   fun loc idx m ->
    let old_c_strlen = find_heap (Loc.of_c_strlen loc) m in
    let idx_itv = Val.get_itv idx in
    if Boolean.is_true (Itv.lt_sem idx_itv (Val.get_itv old_c_strlen)) then m
    else
      let new_c_strlen = Val.of_itv ~traces:(Val.get_traces idx) (Itv.incr idx_itv) in
      set_first_idx_of_null loc new_c_strlen m
end

module Mem = struct
  include AbstractDomain.BottomLifted (MemReach)

  type 'has_oenv t0 = 'has_oenv MemReach.t0 AbstractDomain.Types.bottom_lifted

  type no_oenv_t = GOption.none t0

  let bot : t = Bottom

  let init : OndemandEnv.t -> t = fun oenv -> NonBottom (MemReach.init oenv)

  let f_lift_default : default:'a -> ('h MemReach.t0 -> 'a) -> 'h t0 -> 'a =
   fun ~default f m -> match m with Bottom -> default | NonBottom m' -> f m'


  let is_stack_loc : Loc.t -> _ t0 -> bool =
   fun k -> f_lift_default ~default:false (MemReach.is_stack_loc k)


  let find : Loc.t -> _ t0 -> Val.t = fun k -> f_lift_default ~default:Val.bot (MemReach.find k)

  let find_stack : Loc.t -> _ t0 -> Val.t =
   fun k -> f_lift_default ~default:Val.bot (MemReach.find_stack k)


  let find_set : ?typ:Typ.t -> PowLoc.t -> _ t0 -> Val.t =
   fun ?typ k -> f_lift_default ~default:Val.bot (MemReach.find_set ?typ k)


  let find_opt : Loc.t -> _ t0 -> Val.t option =
   fun k -> f_lift_default ~default:None (MemReach.find_opt k)


  let find_alias : Ident.t -> _ t0 -> AliasTarget.t option =
   fun k -> f_lift_default ~default:None (MemReach.find_alias k)


  let find_simple_alias : Ident.t -> _ t0 -> (Loc.t * IntLit.t option) option =
   fun k -> f_lift_default ~default:None (MemReach.find_simple_alias k)


  let find_ret_alias : _ t0 -> AliasTarget.t option =
   fun m -> f_lift_default ~default:None MemReach.find_ret_alias m


  let load_alias : Ident.t -> AliasTarget.t -> t -> t =
   fun id loc -> map ~f:(MemReach.load_alias id loc)


  let load_simple_alias : Ident.t -> Loc.t -> t -> t =
   fun id loc -> load_alias id (AliasTarget.Simple loc)


  let load_empty_alias : Ident.t -> Loc.t -> t -> t =
   fun id loc -> load_alias id (AliasTarget.Empty loc)


  let store_simple_alias : Loc.t -> Exp.t -> t -> t =
   fun loc e -> map ~f:(MemReach.store_simple_alias loc e)


  let store_empty_alias : Val.t -> Loc.t -> t -> t =
   fun formal loc -> map ~f:(MemReach.store_empty_alias formal loc)


  let fgets_alias : Ident.t -> PowLoc.t -> t -> t =
   fun id locs -> map ~f:(MemReach.fgets_alias id locs)


  let add_stack_loc : Loc.t -> t -> t = fun k -> map ~f:(MemReach.add_stack_loc k)

  let add_stack : Loc.t -> Val.t -> t -> t = fun k v -> map ~f:(MemReach.add_stack k v)

  let add_heap : Loc.t -> Val.t -> t -> t = fun k v -> map ~f:(MemReach.add_heap k v)

  let add_unknown_from : Ident.t -> callee_pname:Typ.Procname.t -> location:Location.t -> t -> t =
   fun id ~callee_pname ~location ->
    map ~f:(MemReach.add_unknown_from id ~callee_pname:(Some callee_pname) ~location)


  let add_unknown : Ident.t -> location:Location.t -> t -> t =
   fun id ~location -> map ~f:(MemReach.add_unknown_from id ~callee_pname:None ~location)


  let strong_update : PowLoc.t -> Val.t -> t -> t = fun p v -> map ~f:(MemReach.strong_update p v)

  let get_reachable_locs_from : (Pvar.t * Typ.t) list -> PowLoc.t -> _ t0 -> PowLoc.t =
   fun formals locs ->
    f_lift_default ~default:PowLoc.empty (MemReach.get_reachable_locs_from formals locs)


  let update_mem : PowLoc.t -> Val.t -> t -> t = fun ploc v -> map ~f:(MemReach.update_mem ploc v)

  let transform_mem : f:(Val.t -> Val.t) -> PowLoc.t -> t -> t =
   fun ~f ploc -> map ~f:(MemReach.transform_mem ~f ploc)


  let remove_temps : Ident.t list -> t -> t = fun temps -> map ~f:(MemReach.remove_temps temps)

  let set_prune_pairs : PrunePairs.t -> t -> t =
   fun prune_pairs -> map ~f:(MemReach.set_prune_pairs prune_pairs)


  let apply_latest_prune : Exp.t -> t -> t * PrunePairs.t =
   fun e -> function
    | Bottom ->
        (Bottom, PrunePairs.empty)
    | NonBottom m ->
        let m, prune_pairs = MemReach.apply_latest_prune e m in
        (NonBottom m, prune_pairs)


  let update_latest_prune : updated_locs:PowLoc.t -> Exp.t -> Exp.t -> t -> t =
   fun ~updated_locs e1 e2 -> map ~f:(MemReach.update_latest_prune ~updated_locs e1 e2)


  let get_latest_prune : t -> LatestPrune.t =
   fun m -> f_lift_default ~default:LatestPrune.Top MemReach.get_latest_prune m


  let get_relation : t -> Relation.t =
   fun m -> f_lift_default ~default:Relation.bot MemReach.get_relation m


  let meet_constraints : Relation.Constraints.t -> t -> t =
   fun constrs -> map ~f:(MemReach.meet_constraints constrs)


  let is_relation_unsat m = f_lift_default ~default:true MemReach.is_relation_unsat m

  let store_relation :
         PowLoc.t
      -> Relation.SymExp.t option * Relation.SymExp.t option * Relation.SymExp.t option
      -> t
      -> t =
   fun locs symexp_opts -> map ~f:(MemReach.store_relation locs symexp_opts)


  let forget_locs : PowLoc.t -> t -> t = fun locs -> map ~f:(MemReach.forget_locs locs)

  let[@warning "-32"] init_param_relation : Loc.t -> t -> t =
   fun loc -> map ~f:(MemReach.init_param_relation loc)


  let init_array_relation :
         Allocsite.t
      -> offset_opt:Itv.t option
      -> size:Itv.t
      -> size_exp_opt:Relation.SymExp.t option
      -> t
      -> t =
   fun allocsite ~offset_opt ~size ~size_exp_opt ->
    map ~f:(MemReach.init_array_relation allocsite ~offset_opt ~size ~size_exp_opt)


  let instantiate_relation : Relation.SubstMap.t -> caller:t -> callee:no_oenv_t -> t =
   fun subst_map ~caller ~callee ->
    match callee with
    | Bottom ->
        caller
    | NonBottom callee ->
        map ~f:(fun caller -> MemReach.instantiate_relation subst_map ~caller ~callee) caller


  let unset_oenv = function Bottom -> Bottom | NonBottom m -> NonBottom (MemReach.unset_oenv m)

  let set_first_idx_of_null loc idx = map ~f:(MemReach.set_first_idx_of_null loc idx)

  let unset_first_idx_of_null loc idx = map ~f:(MemReach.unset_first_idx_of_null loc idx)

  let get_c_strlen locs m =
    let get_c_strlen' loc acc =
      match loc with Loc.Allocsite _ -> Val.join acc (find (Loc.of_c_strlen loc) m) | _ -> acc
    in
    PowLoc.fold get_c_strlen' locs Val.bot


  let pp f m = AbstractDomain.BottomLiftedUtils.pp ~pp:MemReach.pp f m
end
