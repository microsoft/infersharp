(*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
module DomMem = BufferOverrunDomain.Mem

type t = DomMem.no_oenv_t

let pp = DomMem.pp
