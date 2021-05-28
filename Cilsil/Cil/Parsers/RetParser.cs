﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class RetParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ret:
                    Store retInstr;
                    var retType = state.Method.ReturnType.GetElementType();
                    var retNode = new StatementNode(state.CurrentLocation,
                                                    StatementNode.StatementNodeKind.ReturnStmt,
                                                    state.ProcDesc);
                    if (retType == state.Method.Module.TypeSystem.Void)
                    {
                        state.PreviousNode.Successors.Add(state.ProcDesc.ExitNode);
                    }
                    else
                    {
                        (var returnValue, _) = state.Pop();
                        Expression returnVariable = new LvarExpression(
                            new LocalVariable(Identifier.ReturnIdentifier,
                                              state.Method));

                        if (returnValue is BinopExpression)
                        {
                            // We see that for the auto-generated method op_Inequality in records, 
                            // an equality expression is pushed directly onto the stack and
                            // returned. However, return of an expression is not valid in the SIL -- 
                            // we must inline a variable store and load of the value prior to
                            // subsequently returning it.
                            var inlineReturn = new LocalVariable("inlineReturn", state.Method);
                            var inlineIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);

                            var storeInlineReturn = new Store(new LvarExpression(inlineReturn),
                                                              returnValue,
                                                              Typ.FromTypeReference(retType),
                                                              state.CurrentLocation);
                            AddMethodBodyInstructionsToCfg(state, storeInlineReturn);

                            var loadInlineReturn = new Load(inlineIdentifier,
                                                            new LvarExpression(inlineReturn),
                                                            Typ.FromTypeReference(retType),
                                                            state.CurrentLocation);
                            AddMethodBodyInstructionsToCfg(state, loadInlineReturn);

                            retInstr = new Store(returnVariable,
                                                 new VarExpression(inlineIdentifier),
                                                 Typ.FromTypeReference(retType),
                                                 state.CurrentLocation);
                        }
                        else
                        {
                            retInstr = new Store(returnVariable,
                                                 returnValue,
                                                 Typ.FromTypeReference(retType),
                                                 state.CurrentLocation);
                        }
                        retNode.Instructions.Add(retInstr);
                        retNode.Successors = new List<CfgNode> { state.ProcDesc.ExitNode };
                        RegisterNode(state, retNode);
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}