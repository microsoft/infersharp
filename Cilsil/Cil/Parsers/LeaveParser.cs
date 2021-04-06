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
                case Code.Leave_S:
                    var nextInstruction = instruction.Next;
                    var targetTrue = instruction.Operand as Instruction;

                    state.AppendToPreviousNode = false;
                    // If next instruction is not target operand and not jumped from connected catch/finally block, 
                    // we push it to stack to be processed later
                    if (nextInstruction != null &&
                        targetTrue.Offset != nextInstruction.Offset &&
                        !state.JumpedToConnectedExceptionBlock)
                    {
                        state.PushInstruction(nextInstruction);
                    }
                    state.PushInstruction(targetTrue);
                    state.JumpedToConnectedExceptionBlock = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}