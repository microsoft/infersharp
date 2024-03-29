﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class StlocParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            int index;
            switch (instruction.OpCode.Code)
            {
                case Code.Stloc_0:
                    index = 0;
                    break;
                case Code.Stloc_1:
                    index = 1;
                    break;
                case Code.Stloc_2:
                    index = 2;
                    break;
                case Code.Stloc_3:
                    index = 3;
                    break;
                case Code.Stloc_S:
                    try
                    {
                        index = (instruction.Operand as VariableDefinition).Index;
                    }
                    catch (System.InvalidCastException e)
                    {
                        Log.WriteWarning(e.Message);
                        return false;
                    }
                    break;
                case Code.Stloc:
                    try
                    {
                        index = TryGetOperandIndex(instruction);
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

            (var value, var type) = state.Pop();

            // The 0th memory location in the MoveNext() async method stores information concerning
            // the state. To prevent false positive resource leaks, we hack the translation to
            // always store a negative value (the disposal of IDisposable resources only occurs
            // when the state is negative, which occurs when the Awaiter is completed. 
            if (state.IsMoveNextAsyncMethod() && 
                instruction.OpCode.Code == Code.Stloc_0)
            {
                value = new ConstExpression(new IntRepresentation(-1, false, false));
            }

            // Records that the variable stores a boxed value.
            if (type is BoxedValueType boxedValueType)
            {
                state.VariableIndexToBoxedValueType[index] = boxedValueType;
            }
            // A non-boxed value is being stored at the location, so the corresponding record is
            // accordingly updated.
            else if (!(type is BoxedValueType) &&
                     state.VariableIndexToBoxedValueType.ContainsKey(index))
            {
                state.VariableIndexToBoxedValueType.Remove(index);
            }
            else if (IsNullCheckExpression(value))
            {
                state.VariableIndexToNullCheck[index] = ((BinopExpression)value, type);
            }
            else if (!IsNullCheckExpression(value) &&
                     state.VariableIndexToNullCheck.ContainsKey(index))
            {
                state.VariableIndexToNullCheck.Remove(index);
            }
            else if (type.StripPointer() is Tfun functionType)
            {
                state.VariableIndexToFunctionType[index] = functionType;
            }
            else if (!(type.StripPointer() is Tfun) &&
                     state.VariableIndexToFunctionType.ContainsKey(index))
            {
                state.VariableIndexToFunctionType.Remove(index);
            }
            else if (type.IsInstReturnType)
            {
                state.IndicesWithIsInstReturnType.Add(index);
            }
            else if (!type.IsInstReturnType && state.IndicesWithIsInstReturnType.Contains(index))
            {
                state.IndicesWithIsInstReturnType.Remove(index);
            }

            var variable = new LocalVariable(LocalName(index, state.Method), state.Method);
            var storeValueIntoVariable = new Store(new LvarExpression(variable),
                                                   value,
                                                   type,
                                                   state.CurrentLocation);
            var node = AddMethodBodyInstructionsToCfg(state, storeValueIntoVariable);
            RegisterLocalVariable(state, variable, type);
            state.PushInstruction(instruction.Next, node);
            return true;
        }

        private static bool IsNullCheckExpression(Expression expr) =>
            expr is BinopExpression binopExpression && binopExpression.IsNullCheck();
    }
}
