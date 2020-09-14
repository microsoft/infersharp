(*
 * Copyright (c) 2019-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)

open! IStd

val get_all_nodes_upwards_until : Procdesc.Node.t -> Procdesc.Node.t list -> Control.GuardNodes.t
(**
  Starting from the start_nodes, find all the nodes upwards until the
  target is reached, i.e picking up predecessors which have not been
  already added to the found_nodes
*)

val get_loop_head_to_source_nodes : Procdesc.t -> Procdesc.Node.t list Procdesc.NodeMap.t
(**
  Since there could be multiple back-edges per loop, collect all source nodes per loop head.
  loop_head (target of back-edges) --> source nodes
*)

val get_control_maps :
     Procdesc.Node.t list Procdesc.NodeMap.t
  -> Control.loop_control_maps * Control.GuardNodes.t Procdesc.NodeMap.t
(**
  Get a pair of maps (exit_map, loop_head_to_guard_map) where
  exit_map : exit_node -> loop_head set (i.e. target of the back edges) 
  loop_head_to_guard_map : loop_head -> guard_nodes and
  guard_nodes contains the nodes that may affect the looping behavior, i.e. 
  occur in the guard of the loop conditional.
*)
