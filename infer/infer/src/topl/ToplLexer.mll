(*
 * Copyright (c) 2019-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *)
{
  open !IStd
  open ToplParser

  module L = Logging

  let new_line x y lexbuf =
    let m = x |> String.filter ~f:(Char.equal '\n') |> String.length in
    let n = y |> String.length in
    let open Lexing in
    let lcp = lexbuf.lex_curr_p in
    lexbuf.lex_curr_p <-
      { lcp with pos_lnum = lcp.pos_lnum + m ; pos_bol = lcp.pos_cnum - n } ;
    (INDENT n)

  let quoted = Str.regexp "\\\\\\(.\\)"
  let unquote x = Str.global_replace quoted "\\1" x

  (* We open Caml, because ocamllex generates code that uses Array.make,
  which is not available in Core. Ideally, this should go away. *)
  open! Caml
}

let id_head = ['a'-'z' 'A'-'Z']
let id_tail = ['a'-'z' 'A'-'Z' '0'-'9']*

rule raw_token = parse
  | '\t' { raise Error }
  | ((' '* ("//" [^ '\n']*)? '\n')+ as x) (' '* as y) { new_line x y lexbuf }
  | ' '+ { raw_token lexbuf }
  | "->" { ARROW }
  | '='  { ASGN }
  | ':'  { COLON }
  | ','  { COMMA }
  | '('  { LP }
  | ')'  { RP }
  | '*'  { STAR }
  | '<'  (([^ '<' '>' '\n' '\\'] | ('\\' _))* as x) '>' { CONSTANT (unquote x) }
  | '"' ([^ '"' '\n']* as x) '"' { STRING x }
  | "prefix" { PREFIX }
  | "property" { PROPERTY }
  | "message" { MESSAGE }
  | id_head id_tail as id { ID id }
  | eof { EOF }
  | _ { raise Error }

{
  let token () =
    let indents = ref [0] in
    let scheduled_rc = ref 0 in
    let last_indent () = match !indents with
      | x :: _ -> x
      | [] -> L.(die InternalError) "ToplLexer.indents should be nonempty"
    in
    let add_indent n = indents := n :: !indents in
    let rec drop_to_indent n = match !indents with
      | x :: xs when x > n -> (incr scheduled_rc; indents := xs; drop_to_indent n)
      | x :: _  when x < n -> raise Error (* bad indentation *)
      | _ -> ()
    in
    let rec step lexbuf =
      if !scheduled_rc > 0 then (decr scheduled_rc; RC)
      else match raw_token lexbuf with
        | INDENT n when n > last_indent () -> (add_indent n; LC)
        | INDENT n when n < last_indent () -> (drop_to_indent n; step lexbuf)
        | INDENT _ -> step lexbuf
        | t -> t
    in
    step
}
