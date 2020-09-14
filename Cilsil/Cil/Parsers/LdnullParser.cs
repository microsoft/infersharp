// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdnullParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldnull:
                    state.PushExpr(
                        new ConstExpression(new IntRepresentation(0, false, true)),
                        Typ.FromTypeReference(state.Method.Module.TypeSystem.Object));
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
