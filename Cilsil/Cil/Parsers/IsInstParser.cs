// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cilsil.Utils;
using Cilsil.Sil.Types;
using Cilsil.Sil.Expressions;
using Cilsil.Sil;

namespace Cilsil.Cil.Parsers
{
    internal class IsInstParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // We ignore the type-checking behavior of isinst, as that is largely
                // runtime-dependent and therefore not statically verifiable in any nontrivial
                // context; we instead translate only its null-checking behavior. To do this, 
                // nothing need be done in particular for the isinst instruction, as the bytecode 
                // which follows it constitutes a null-check for the item on top of the program 
                // stack.
                case Code.Isinst:
                    // Create and store exception expression of filter block.
                    if (state.ExceptionBlockStartToEndOffsets.ContainsKey(instruction.Offset))
                    {
                        var newObjectIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                        var catchExceptionType = instruction.Operand as TypeReference;
                        state.PushExpr(new VarExpression(newObjectIdentifier), 
                                       Typ.FromTypeReference(catchExceptionType));
                    }
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}