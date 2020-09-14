(*
 * Copyright (c) 2016-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
module L = Logging

(** Module for builtin functions with their symbolic execution handler *)

type args =
  { pdesc: Procdesc.t
  ; instr: Sil.instr
  ; tenv: Tenv.t
  ; prop_: Prop.normal Prop.t
  ; path: Paths.Path.t
  ; ret_id_typ: Ident.t * Typ.t
  ; args: (Exp.t * Typ.t) list
  ; proc_name: Typ.Procname.t
  ; loc: Location.t
  ; exe_env: Exe_env.t }

type ret_typ = (Prop.normal Prop.t * Paths.Path.t) list

type t = args -> ret_typ

type registered = t

(** builtin function names for which we do symbolic execution *)
let builtin_functions = Typ.Procname.Hash.create 4

let check_register_populated () =
  (* check if BuiltinDefn were loaded before accessing register *)
  if Int.equal (Typ.Procname.Hash.length builtin_functions) 0 then
    L.(die InternalError) "Builtins were not initialized"


(** get the symbolic execution handler associated to the builtin function name *)
let get name : t option =
  try Some (Typ.Procname.Hash.find builtin_functions name)
  with Caml.Not_found -> check_register_populated () ; None


(** register a builtin [Typ.Procname.t] and symbolic execution handler *)
let register proc_name sym_exe_fun : registered =
  Typ.Procname.Hash.replace builtin_functions proc_name sym_exe_fun ;
  sym_exe_fun


(** print the functions registered *)
let pp_registered fmt () =
  let builtin_names = ref [] in
  Typ.Procname.Hash.iter (fun name _ -> builtin_names := name :: !builtin_names) builtin_functions ;
  builtin_names := List.sort ~compare:Typ.Procname.compare !builtin_names ;
  let pp pname = Format.fprintf fmt "%a@\n" Typ.Procname.pp pname in
  Format.fprintf fmt "Registered builtins:@\n  @[" ;
  List.iter ~f:pp !builtin_names ;
  Format.fprintf fmt "@]@."


(** print the builtin functions and exit *)
let print_and_exit () =
  pp_registered Format.std_formatter () ;
  L.exit 0
