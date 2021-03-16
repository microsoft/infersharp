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
                    if (nextInstruction != null &&
                        targetTrue.Offset != nextInstruction.Offset)
                    {
                        state.PushInstruction(nextInstruction);
                    }
                    state.PushInstruction(targetTrue);
                    state.OffLeave = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}