// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class StindParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Stind_I:
                case Code.Stind_I1:
                case Code.Stind_I2:
                case Code.Stind_I4:
                case Code.Stind_I8:
                case Code.Stind_R4:
                case Code.Stind_R8:
                case Code.Stind_Ref:
                    Load loadExpressionValue;
                    Identifier freshIdentifier;
                    Store setExpressionValue;

                    (var value, _) = state.Pop();
                    (var pointerExpression, var pointerType) = state.Pop();

                    if (pointerType is Address address)
                    {
                        if (address.AddressType == Address.ReferenceKind.Parameter)
                        {
                            freshIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);

                            loadExpressionValue = new Load(freshIdentifier,
                                                           address.Value,
                                                           pointerType.StripPointer(),
                                                           state.CurrentLocation);

                            setExpressionValue =
                                new Store(new VarExpression(freshIdentifier),
                                          value,
                                          pointerType.StripPointer().StripPointer(),
                                          state.CurrentLocation);

                            state.PushInstruction(
                                instruction.Next,
                                AddMethodBodyInstructionsToCfg(state,
                                                               loadExpressionValue,
                                                               setExpressionValue));
                        }
                        else if (address.AddressType == Address.ReferenceKind.Field)
                        {
                            setExpressionValue = new Store(pointerExpression,
                                                           value,
                                                           pointerType.StripPointer(),
                                                           state.CurrentLocation);
                            state.PushInstruction(
                                instruction.Next,
                                AddMethodBodyInstructionsToCfg(state,
                                                               setExpressionValue));
                        }
                    }
                    else if (pointerType is Tptr)
                    {
                        setExpressionValue = new Store(pointerExpression,
                                                       value,
                                                       pointerType,
                                                       state.CurrentLocation);
                        state.PushInstruction(
                            instruction.Next,
                            AddMethodBodyInstructionsToCfg(state,
                                                           setExpressionValue));
                    }
                    else
                    {
                        Log.instance.Warn("Unexpected pointer type " +
                                       $"{pointerType.GetType()} on stack.");
                        return false;
                    }

                    return true;
                default:
                    return false;
            }
        }
    }
}
