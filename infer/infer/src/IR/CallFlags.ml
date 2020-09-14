(*
 * Copyright (c) 2009-2013, Monoidics ltd.
 * Copyright (c) 2013-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

(** The Smallfoot Intermediate Language: Call Flags *)

open! IStd
module F = Format

(** Flags for a procedure call *)
type t =
  { cf_assign_last_arg: bool
  ; cf_injected_destructor: bool
  ; cf_interface: bool
  ; cf_is_objc_block: bool
  ; cf_noreturn: bool
  ; cf_virtual: bool
  ; cf_with_block_parameters: bool }
[@@deriving compare]

let pp f
    ({ cf_assign_last_arg
     ; cf_injected_destructor
     ; cf_interface
     ; cf_is_objc_block
     ; cf_noreturn
     ; cf_with_block_parameters
     ; cf_virtual }[@warning "+9"]) =
  if cf_assign_last_arg then F.pp_print_string f " assign_last" ;
  if cf_injected_destructor then F.pp_print_string f " injected" ;
  if cf_interface then F.pp_print_string f " interface" ;
  if cf_is_objc_block then F.pp_print_string f " objc_block" ;
  if cf_noreturn then F.pp_print_string f " noreturn" ;
  if cf_with_block_parameters then F.pp_print_string f " block_params" ;
  if cf_virtual then F.pp_print_string f " virtual" ;
  ()


let default =
  { cf_assign_last_arg= false
  ; cf_injected_destructor= false
  ; cf_interface= false
  ; cf_is_objc_block= false
  ; cf_noreturn= false
  ; cf_with_block_parameters= false
  ; cf_virtual= false }
