// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class BrParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            Expression conditionTrueExpression;
            Typ conditionTrueExpressionType;
            var targetTrue = instruction.Operand as Instruction;
            var targetFalse = instruction.Next;

            switch (instruction.OpCode.Code)
            {
                case Code.Br:
                case Code.Br_S:
                    state.PushInstruction(targetTrue);
                    return true;

                case Code.Brtrue:
                case Code.Brtrue_S:
                    (conditionTrueExpression, conditionTrueExpressionType) = state.Pop();
                    break;

                case Code.Brfalse:
                case Code.Brfalse_S:
                    (conditionTrueExpression, conditionTrueExpressionType) = state.Pop();
                    targetTrue = instruction.Next;
                    targetFalse = instruction.Operand as Instruction;
                    break;

                case Code.Blt:
                case Code.Blt_S:
                case Code.Blt_Un:
                case Code.Blt_Un_S:
                    (conditionTrueExpression, conditionTrueExpressionType) =
                        state.PopTwoAndApplyBinop(BinopExpression.BinopKind.Lt);
                    break;

                case Code.Bgt:
                case Code.Bgt_S:
                case Code.Bgt_Un:
                case Code.Bgt_Un_S:
                    (conditionTrueExpression, conditionTrueExpressionType) =
                        state.PopTwoAndApplyBinop(BinopExpression.BinopKind.Gt);
                    break;

                case Code.Ble:
                case Code.Ble_S:
                case Code.Ble_Un:
                case Code.Ble_Un_S:
                    (conditionTrueExpression, conditionTrueExpressionType) =
                        state.PopTwoAndApplyBinop(BinopExpression.BinopKind.Le);
                    break;

                case Code.Bge:
                case Code.Bge_S:
                case Code.Bge_Un:
                case Code.Bge_Un_S:
                    (conditionTrueExpression, conditionTrueExpressionType) =
                        state.PopTwoAndApplyBinop(BinopExpression.BinopKind.Ge);
                    break;

                case Code.Beq:
                case Code.Beq_S:
                    (conditionTrueExpression, conditionTrueExpressionType) =
                        state.PopTwoAndApplyBinop(BinopExpression.BinopKind.Eq);
                    break;

                case Code.Bne_Un:
                case Code.Bne_Un_S:
                    (conditionTrueExpression, conditionTrueExpressionType) =
                        state.PopTwoAndApplyBinop(BinopExpression.BinopKind.Ne);
                    break;
                default:
                    return false;
            }

            Expression trueNodeCondition;

            // If the stack expression is a nullable type, we need to use a null test expression,
            // where the type being non-null drives control to the true branch.
            if ((conditionTrueExpression is VarExpression ||
                 conditionTrueExpression is LvarExpression) &&
                conditionTrueExpressionType is Tptr)
            {
                trueNodeCondition =
                    new BinopExpression(BinopExpression.BinopKind.Ne,
                                        conditionTrueExpression,
                                        new ConstExpression(
                                            new IntRepresentation(0, false, true)));
            }
            else
            {
                trueNodeCondition = conditionTrueExpression;
            }

            var falseNodeCondition = new UnopExpression(
                UnopExpression.UnopKind.LNot, trueNodeCondition, null);
            (var truePruneNode, var falsePruneNode) =
                CreateBranchConditionNodes(state,
                                           trueNodeCondition,
                                           falseNodeCondition);

            RegisterNode(state, truePruneNode);
            RegisterNode(state, falsePruneNode);

            state.PushInstruction(targetFalse, falsePruneNode);
            state.PushInstruction(targetTrue, truePruneNode);

            return true;
        }
    }
}
