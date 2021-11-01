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
    internal class LdindParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldind_I:
                case Code.Ldind_I1:
                case Code.Ldind_I2:
                case Code.Ldind_I4:
                case Code.Ldind_I8:
                case Code.Ldind_R4:
                case Code.Ldind_R8:
                case Code.Ldind_U1:
                case Code.Ldind_U2:
                case Code.Ldind_U4:
                case Code.Ldind_Ref:
                    (var pointerExpression, var pointerType) = state.Pop();

                    if (pointerType is Address address)
                    {
                        if (address.AddressType == Address.ReferenceKind.Field)
                        {
                            (var loadFieldValue, var valueIdentifier) = address.LoadValue(state);
                            state.PushExpr(new VarExpression(valueIdentifier),
                                           pointerType.StripPointer());
                            state.PushInstruction(instruction.Next,
                                                  AddMethodBodyInstructionsToCfg(state,
                                                                                 loadFieldValue));
                        }
                        else if (address.AddressType == Address.ReferenceKind.Parameter)
                        {
                            var argumentIdentifier =
                                state.GetIdentifier(Identifier.IdentKind.Normal);
                            var loadArgument = new Load(argumentIdentifier,
                                                        pointerExpression,
                                                        pointerType.StripPointer(),
                                                        state.CurrentLocation);
                            var valueIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                            var loadValue = new Load(valueIdentifier,
                                                     new VarExpression(argumentIdentifier),
                                                     pointerType.StripPointer().StripPointer(),
                                                     state.CurrentLocation);
                            state.PushExpr(new VarExpression(valueIdentifier),
                                           pointerType.StripPointer());
                            state.PushInstruction(instruction.Next,
                                                  AddMethodBodyInstructionsToCfg(state,
                                                                                 loadArgument,
                                                                                 loadValue));

                        }
                    }
                    else
                    {
                        Log.DebugWriteError("Unexpected or unhandled pointer type " +
                                       $"{pointerType.GetType()} on stack.");
                        return false;
                    }

                    state.AppendToPreviousNode = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
