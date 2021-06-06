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

                    if (targetTrue.Offset != nextInstruction.Offset &&
                        state.ExceptionBlockStartToEndOffsets.ContainsKey(nextInstruction.Offset))
                    {
                        state.PushRetExpr();
                        state.PushInstruction(nextInstruction);
                    }
                    // When the next instruction is in finally block, we ignore jumping to the 
                    // target true instruction.
                    if (state.ExceptionBlockStartToEndOffsets.ContainsKey(nextInstruction.Offset) &&
                        !state.OffsetToExceptionType.ContainsKey(nextInstruction.Offset) &&
                        targetTrue.Offset != nextInstruction.Offset)
                    {
                        return true;
                    }
                    state.PushInstruction(targetTrue);
                    return true;
                default:
                    return false;
            }
        }
    }
}