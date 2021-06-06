// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Cilsil.Sil;
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
                case Code.Endfilter:
                case Code.Endfinally:  
                    var nextInstruction = instruction.Next;
                    if (state.ExceptionBlockStartToEndOffsets.ContainsKey(nextInstruction.Offset))
                    {
                        state.PushRetExpr();
                    }
                    state.PushInstruction(nextInstruction);
                    return true;
                default:
                    return false;
            }
        }
    }
}