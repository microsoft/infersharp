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
                    state.PushInstruction(instruction.Next); 
                    return true;
                case Code.Endfinally:  
                    if (state.ExceptionBlockStartToEndOffsets.ContainsKey(instruction.Next.Offset))
                    {
                        state.PushRetExpr();
                    }
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}