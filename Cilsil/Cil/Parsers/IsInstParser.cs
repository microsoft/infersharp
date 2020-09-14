// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class IsInstParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // We ignore the type-checking behavior of isinst, as that is largely
                // runtime-dependent and therefore not statically verifiable in any nontrivial
                // context; we instead translate only its null-checking behavior. To do this, 
                // nothing need be done in particular for the isinst instruction, as the bytecode 
                // which follows it constitutes a null-check for the item on top of the program 
                // stack.
                case Code.Isinst:
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
