// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdargParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            int index;
            CfgNode node = null;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldarg:
                    try
                    {
                        index = (int)instruction.Operand;
                    }
                    catch (System.InvalidCastException e)
                    {
                        Log.WriteWarning(e.Message);
                        return false;
                    }
                    break;
                case Code.Ldarg_0:
                    index = 0;
                    break;
                case Code.Ldarg_1:
                    index = 1;
                    break;
                case Code.Ldarg_2:
                    index = 2;
                    break;
                case Code.Ldarg_3:
                    index = 3;
                    break;
                case Code.Ldarg_S:
                case Code.Ldarga:
                case Code.Ldarga_S:
                    // Sequence accounts for the implict "this" argument, if applicable.
                    try
                    {
                        index = (instruction.Operand as ParameterDefinition).Sequence;
                    }
                    catch (System.InvalidCastException e)
                    {
                        Log.WriteWarning(e.Message);
                        return false;
                    }
                    break;
                default:
                    return false;
            }

            (var argVar, var argType) = CreateArg(index, state.Method);

            if (instruction.OpCode.Code == Code.Ldarga || instruction.OpCode.Code == Code.Ldarga_S)
            {
                var argAddressType = new Address(Tptr.PtrKind.Pk_pointer,
                                                 argType,
                                                 argVar,
                                                 referenceKind: Address.ReferenceKind.Parameter);
                state.PushExpr(argVar, argAddressType);
            }
            else
            {
                if (ParameterIsByReference(index, state.Method))
                {
                    // Strips out the pointer because the type produced by CreateArg already
                    // carries it.
                    var argAddressType =
                        new Address(Tptr.PtrKind.Pk_pointer,
                                    argType.StripPointer(),
                                    argVar,
                                    referenceKind: Address.ReferenceKind.Parameter);
                    state.PushExpr(argVar, argAddressType);
                }
                else
                {
                    var loadArgument = state.PushAndLoad(argVar, argType);
                    node = AddMethodBodyInstructionsToCfg(state, loadArgument);
                    state.AppendToPreviousNode = true;
                }
            }
            state.PushInstruction(instruction.Next, node);
            return true;
        }

        private bool ParameterIsByReference(int index, MethodDefinition method)
        {
            if (method.HasThis)
            {
                if (index == 0)
                {
                    return false;
                }
                return method.Parameters[index - 1].ParameterType.IsByReference;
            }
            else
            {
                return method.Parameters[index].ParameterType.IsByReference;
            }
        }
    }
}
