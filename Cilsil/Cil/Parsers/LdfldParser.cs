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
    internal class LdfldParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            var field = instruction.Operand as FieldReference;
            // An object expression for an instance field and a class expression for a static
            // field.
            Expression fieldOwnerExpression;
            Typ fieldType;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldfld:
                case Code.Ldflda:
                    (var objectExpression, var objectExpressionType) = state.Pop();
                    // Handles the case in which the address of the owning object is on the stack.
                    if (objectExpressionType is Address address)
                    {
                        // The value at the address is the parent expression, matching how the
                        // field is initialized.
                        var addressValueIdentifier = state.GetIdentifier(
                            Identifier.IdentKind.Normal);
                        state.PreviousNode.Instructions.Add(
                            new Load(addressValueIdentifier,
                                     address.Value,
                                     Typ.FromTypeReferenceNoPointer(field.DeclaringType),
                                     state.CurrentLocation));
                        fieldOwnerExpression = new VarExpression(addressValueIdentifier);
                    }
                    // The expression represents the object itself.
                    else
                    {
                        fieldOwnerExpression = objectExpression;
                    }
                    break;
                case Code.Ldsfld:
                case Code.Ldsflda:
                    fieldOwnerExpression = new LvarExpression(
                        new GlobalVariable(field.DeclaringType.GetCompatibleFullName()));
                    break;
                default:
                    return false;
            }
            // An identifier to store the field expression.
            var fieldIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);

            fieldType = Typ.FromTypeReferenceNoPointer(field.FieldType);
            var fieldExpression = CreateFieldExpression(fieldOwnerExpression, field);

            if (instruction.OpCode.Code == Code.Ldfld || instruction.OpCode.Code == Code.Ldsfld)
            {
                state.PreviousNode.Instructions.Add(new Load(fieldIdentifier,
                                                             fieldExpression,
                                                             Typ.FromTypeReferenceNoPointer(
                                                                 field.DeclaringType),
                                                             state.CurrentLocation));
                state.AppendToPreviousNode = true;
                state.PushExpr(new VarExpression(fieldIdentifier), fieldType);
            }
            // Instruction is either Ldflda or Ldsflda.
            else
            {
                var fieldAddressVariableType = 
                    new Address(Tptr.PtrKind.Pk_pointer,
                                Typ.FromTypeReferenceNoPointer(
                                    field.FieldType),
                                fieldExpression,
                                referenceKind: Address.ReferenceKind.Field);
                state.PushExpr(fieldExpression, fieldAddressVariableType);                
            }
            state.PushInstruction(instruction.Next);
            return true;
        }
    }
}