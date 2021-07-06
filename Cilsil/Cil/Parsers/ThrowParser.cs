// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil.Cil;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;

namespace Cilsil.Cil.Parsers
{
    internal class ThrowParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Throw:
                    var retType = state.Method.ReturnType.GetElementType();
                    var retNode = new StatementNode(state.CurrentLocation,
                                                    StatementNode.StatementNodeKind.ReturnStmt,
                                                    state.ProcDesc);
                    Expression returnVariable = new LvarExpression(
                        new LocalVariable(Identifier.ReturnIdentifier,
                                          state.Method));
                    (var returnValue, _) = state.Pop();
                    var retInstr = new Store(returnVariable,
                                             new ExnExpression(returnValue),
                                             Typ.FromTypeReference(retType),
                                             state.CurrentLocation);
                    AddMethodBodyInstructionsToCfg(state, retInstr);
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
