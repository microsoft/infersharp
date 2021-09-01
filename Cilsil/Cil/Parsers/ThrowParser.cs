// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil.Cil;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class ThrowParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // TODO: implement rethrow as well. 
                case Code.Throw:
                    (var returnValue, _) = state.Pop();
                    var retNode = CreateExceptionReturnNode(state,
                                                            returnValue, 
                                                            state.CurrentLocation);
                    RegisterNode(state, retNode);
                    return true;
                default:
                    return false;
            }
        }
    }
}
