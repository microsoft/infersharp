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
                    // Restoring the stack state because the value on stack is 
                    // relevant to CIL compiler only.
                    state.Pop();
                    if (state.PreviousNode is PruneNode pruneNode && !pruneNode.TrueBranch)
                    {
                        var endCatchOffset = state.ExceptionBlockStartToEndOffsets[instruction.Next.Offset];
                        while (instruction.Next.Offset < endCatchOffset)
                        {
                            instruction = instruction.Next;
                        }
                        if (!state.ParsedInstructions.Contains(instruction.Next))
                        {
                            state.PushInstruction(instruction.Next); 
                        }
                    }
                    else if (!state.ParsedInstructions.Contains(instruction.Next))
                    {
                        state.PushInstruction(instruction.Next); 
                    }
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
