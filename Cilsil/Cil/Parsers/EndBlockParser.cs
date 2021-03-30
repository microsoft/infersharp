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
                    state.OffLeave = false;
                    return true;
                default:
                    return false;
            }
        }
    }
}
