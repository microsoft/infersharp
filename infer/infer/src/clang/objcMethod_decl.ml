(*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)
(* Given a list of declarations in an interface returns list of methods *)

open! IStd

let get_methods (from_decl : CAst_utils.procname_from_decl) tenv decl_list =
  let get_method list_methods decl =
    match decl with
    | Clang_ast_t.ObjCMethodDecl _ ->
        let method_name = from_decl ~tenv decl in
        method_name :: list_methods
    | _ ->
        list_methods
  in
  List.fold_left ~f:get_method decl_list ~init:[]


let add_missing_methods tenv class_tn_name missing_methods =
  match Tenv.lookup tenv class_tn_name with
  | Some ({methods} as struct_typ) ->
      let new_methods = CGeneral_utils.append_no_duplicates_methods methods missing_methods in
      ignore (Tenv.mk_struct tenv ~default:struct_typ ~methods:new_methods class_tn_name)
  | _ ->
      ()
