// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class ConstrainedParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // This instruction is used to help the compiler make a call to a virtual function
                // whether the type referenced by the pointer at the top of the stack is a
                // reference type or a value type; the callvirt instruction is not valid on value
                // types and thus appropriate conversion must occur. However, this is not
                // semantically relevant for Infer.
                case Code.Constrained:
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
