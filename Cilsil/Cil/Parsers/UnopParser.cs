// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class UnopParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            UnopExpression.UnopKind kind;
            switch (instruction.OpCode.Code)
            {
                case Code.Not:
                    kind = UnopExpression.UnopKind.BNot;
                    break;
                case Code.Neg:
                    kind = UnopExpression.UnopKind.Neg;
                    break;
                default:
                    return false;
            }
            var (exp, typ) = state.Pop();
            state.PushExpr(new UnopExpression(kind, exp, typ), typ);
            state.PushInstruction(instruction.Next);
            return true;
        }
    }
}
