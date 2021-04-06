// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class EndBlockParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Endfinally:                                                                                           
                    state.PushInstruction(instruction.Next);
                    state.JumpedToConnectedExceptionBlock = false;
                    return true;
                default:
                    return false;
            }
        }
    }
}
