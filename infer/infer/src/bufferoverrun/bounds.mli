(*
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
module F = Format

module Bound : sig
  type t

  type eval_sym = t Symb.Symbol.eval

  val compare : t -> t -> int

  val equal : t -> t -> bool

  val pp_mark : markup:bool -> F.formatter -> t -> unit

  val of_int : int -> t

  val of_big_int : Z.t -> t

  val minf : t

  val mone : t

  val zero : t

  val one : t

  val z255 : t

  val pinf : t

  val of_normal_path :
       (unsigned:bool -> Symb.SymbolPath.t -> Symb.Symbol.t)
    -> unsigned:bool
    -> Symb.SymbolPath.partial
    -> t

  val of_offset_path :
    (unsigned:bool -> Symb.SymbolPath.t -> Symb.Symbol.t) -> Symb.SymbolPath.partial -> t

  val of_length_path :
    (unsigned:bool -> Symb.SymbolPath.t -> Symb.Symbol.t) -> Symb.SymbolPath.partial -> t

  val is_zero : t -> bool

  val is_not_infty : t -> bool

  val is_minf : t -> bool

  val is_pinf : t -> bool

  val is_symbolic : t -> bool

  val le : t -> t -> bool

  val lt : t -> t -> bool

  val gt : t -> t -> bool

  val eq : t -> t -> bool

  val xcompare : t PartialOrder.xcompare

  val underapprox_min : t -> t -> t

  val overapprox_min : t -> t -> t

  val underapprox_max : t -> t -> t

  val overapprox_max : t -> t -> t

  val widen_l : t -> t -> t

  val widen_l_thresholds : thresholds:Z.t list -> t -> t -> t

  val widen_u : t -> t -> t

  val widen_u_thresholds : thresholds:Z.t list -> t -> t -> t

  val is_const : t -> Z.t sexp_option

  val plus_l : t -> t -> t

  val plus_u : t -> t -> t

  val mult_const_l : Ints.NonZeroInt.t -> t -> t

  val mult_const_u : Ints.NonZeroInt.t -> t -> t

  val neg : t -> t

  val div_const_l : t -> Ints.NonZeroInt.t -> t sexp_option

  val div_const_u : t -> Ints.NonZeroInt.t -> t sexp_option

  val get_symbols : t -> Symb.SymbolSet.t

  val are_similar : t -> t -> bool

  val subst_lb : t -> eval_sym -> t AbstractDomain.Types.bottom_lifted

  val subst_ub : t -> eval_sym -> t AbstractDomain.Types.bottom_lifted

  val simplify_bound_ends_from_paths : t -> t

  val is_same_symbol : t -> t -> Symb.SymbolPath.t option

  val exists_str : f:(string -> bool) -> t -> bool
end

module BoundTrace : sig
  include PrettyPrintable.PrintableOrderedType

  val length : t -> int

  val make_err_trace : depth:int -> t -> Errlog.loc_trace
end

type ('c, 's, 't) valclass = Constant of 'c | Symbolic of 's | ValTop of 't

module NonNegativeBound : sig
  type t [@@deriving compare]

  val of_loop_bound : Location.t -> Bound.t -> t

  val of_modeled_function : string -> Location.t -> Bound.t -> t

  val pp : hum:bool -> Format.formatter -> t -> unit

  val make_err_trace : t -> string * Errlog.loc_trace

  val zero : Location.t -> t

  val int_lb : t -> Ints.NonNegativeInt.t

  val int_ub : t -> Ints.NonNegativeInt.t option

  val classify : t -> (Ints.NonNegativeInt.t, t, BoundTrace.t) valclass

  val subst :
       Typ.Procname.t
    -> Location.t
    -> t
    -> Bound.eval_sym
    -> (Ints.NonNegativeInt.t, t, BoundTrace.t) valclass
end
