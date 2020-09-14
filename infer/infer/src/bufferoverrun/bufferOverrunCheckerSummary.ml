(*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd
module POCondSet = BufferOverrunProofObligations.ConditionSet

type t = POCondSet.summary_t

let pp = POCondSet.pp_summary
