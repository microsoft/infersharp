// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class StackopParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Pop:
                    state.Pop();
                    break;
                case Code.Dup:
                    (var exp, var typ) = state.Peek();
                    state.PushExpr(exp, typ);
                    break;
                default:
                    return false;
            }
            state.PushInstruction(instruction.Next);
            return true;
        }
    }
}
