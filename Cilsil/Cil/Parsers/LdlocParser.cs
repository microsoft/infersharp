// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdlocParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            int index;
            CfgNode node = null;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldloc:
                case Code.Ldloca:
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
                case Code.Ldloc_0:
                    index = 0;
                    break;
                case Code.Ldloc_1:
                    index = 1;
                    break;
                case Code.Ldloc_2:
                    index = 2;
                    break;
                case Code.Ldloc_3:
                    index = 3;
                    break;
                case Code.Ldloc_S:
                case Code.Ldloca_S:
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
                default:
                    return false;
            }

            (var variableExpression, var variableType) = CreateLocal(index, state.Method);

            // Updates the type to the appropriate boxed one if the variable contains a boxed
            // value.
            if (state.VariableIndexToBoxedValueType.ContainsKey(index))
            {
                variableType = state.VariableIndexToBoxedValueType[index];
            }
            else if (state.IndicesWithIsInstReturnType.Contains(index))
            {
                variableType = new Tint(Tint.IntKind.IBool, true);
            }

            if (instruction.OpCode.Code == Code.Ldloca || instruction.OpCode.Code == Code.Ldloca_S)
            {
                // Stores the variable as the value stored at the address expression.
                state.PushExpr(variableExpression, new Address(Tptr.PtrKind.Pk_pointer,
                                                               variableType,
                                                               variableExpression));
            }
            else if (state.VariableIndexToNullCheck.ContainsKey(index))
            {
                state.PushExpr(state.VariableIndexToNullCheck[index].expr,
                               state.VariableIndexToNullCheck[index].type);
            }
            else
            {
                // Loads the value at the heap location onto the stack.
                var variableInstruction = state.PushAndLoad(variableExpression,
                                                            variableType);
                node = AddMethodBodyInstructionsToCfg(state, variableInstruction);
                state.AppendToPreviousNode = true;
            }
            state.PushInstruction(instruction.Next, node);
            return true;
        }
    }
}
