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
                case Code.Leave:
                case Code.Leave_S:
                    var nextInstruction = instruction.Next;
                    var targetTrue = instruction.Operand as Instruction;

                    state.AppendToPreviousNode = false;
                    // If next instruction is not target operand and not jumped from a previous
                    // try/catch block, we connect to the next catch/finally block.
                    if (nextInstruction != null &&
                        targetTrue.Offset != nextInstruction.Offset &&
                        !state.ParsedInstructions.Contains(nextInstruction))
                    {
                        state.PushInstruction(nextInstruction);
                    }
                    state.PushInstruction(targetTrue);
                    return true;
                default:
                    return false;
            }
        }
    }
}