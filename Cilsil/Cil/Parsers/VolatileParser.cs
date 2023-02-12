// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class VolatileParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // This instruction is used to specify that the address is a volatile one (i.e.
                // one that can be referenced externally to the current thread of execution)
                // and that the results of reading that location cannot be cached/that multiple
                // stores cannot be suppressed. This is not relevant for Infer, so we treat it as a
                // nop. 
                case Code.Volatile:
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
