// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class StfldParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            var fieldReference = instruction.Operand as FieldReference;
            // An object expression for an instance field and a class expression for a static
            // field.
            Expression fieldParentExpression;
            // The expression to set the field to.
            Expression expressionToStore;
            switch (instruction.OpCode.Code)
            {
                case Code.Stfld:
                    (expressionToStore, _) = state.Pop();
                    (fieldParentExpression, _) = state.Pop();
                    break;
                case Code.Stsfld:
                    (expressionToStore, _) = state.Pop();
                    fieldParentExpression = new LvarExpression(
                        new GlobalVariable(fieldReference.DeclaringType.GetCompatibleFullName()));
                    break;
                default:
                    return false;
            }
            var fieldStore = new Store(CreateFieldExpression(fieldParentExpression, fieldReference),
                                       expressionToStore,
                                       Typ.FromTypeReferenceNoPointer(fieldReference.DeclaringType),
                                       state.CurrentLocation);
            var node = AddMethodBodyInstructionsToCfg(state, fieldStore);
            state.PushInstruction(instruction.Next, node);
            return true;
        }
    }
}
