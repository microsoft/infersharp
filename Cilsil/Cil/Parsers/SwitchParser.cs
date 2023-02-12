// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class SwitchParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Switch:
                    (var goToExpression, var type) = state.Pop();
                    var array = instruction.Operand as Instruction[];
                    for (int i = 0; i < array.Length; i++)
                    {
                        var arrayIndexExpression = 
                            new ConstExpression(new IntRepresentation(i, false, false));
                        var trueNodeCondition = new BinopExpression(
                            BinopExpression.BinopKind.Eq,
                            goToExpression,
                            arrayIndexExpression);
                        var falseNodeCondition = new UnopExpression(UnopExpression.UnopKind.LNot,
                                                                    trueNodeCondition,
                                                                    null);
                        (var truePruneNode, var falsePruneNode) =
                            CreateBranchConditionNodes(state,
                                                       trueNodeCondition,
                                                       falseNodeCondition);
                        RegisterNode(state, truePruneNode);
                        RegisterNode(state, falsePruneNode);

                        state.PushInstruction(array[i], truePruneNode);
                        state.PreviousNode = falsePruneNode;
                    }
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
