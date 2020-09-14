(*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

(** Configuration options *)

val run :
  (input:string -> output:string option -> compile_only:bool -> 'a) -> 'a
(** [run main] parses command line options, performs some imperative
    initialization, and then executes [main] passing the configuration
    options. *)
