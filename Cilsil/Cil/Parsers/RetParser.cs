// Copyright (c) Microsoft Corporation.
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
                    var retType = state.Method.ReturnType;
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
                        var retInstr = new Store(lvalue: returnVariable,
                                                 rvalue: returnValue,
                                                 type: Typ.FromTypeReference(retType),
                                                 location: state.CurrentLocation);
                        var retNode = new StatementNode(state.CurrentLocation,
                                                        StatementNode.StatementNodeKind.ReturnStmt,
                                                        state.ProcDesc);
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
