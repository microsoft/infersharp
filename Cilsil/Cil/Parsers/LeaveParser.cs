// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LeaveParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // Leave for try block: indicates normal control flow, avoiding fall-through as a
                // means of exiting it. However, target is not finally block but rather whatever
                // comes after catch/finally.
                // Leave for catch block: target instruction immediately after catch block.
                case Code.Leave:
                case Code.Leave_S:
                    foreach 
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
