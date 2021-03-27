// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class BinopParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            BinopExpression.BinopKind kind;
            switch (instruction.OpCode.Code)
            {
                case Code.Add:
                    kind = BinopExpression.BinopKind.PlusA;
                    break;
                case Code.Sub:
                    kind = BinopExpression.BinopKind.MinusA;
                    break;
                case Code.Mul:
                    kind = BinopExpression.BinopKind.Mult;
                    break;
                case Code.Div:
                case Code.Div_Un:
                    kind = BinopExpression.BinopKind.Div;
                    break;
                case Code.Rem:
                case Code.Rem_Un:
                    kind = BinopExpression.BinopKind.Mod;
                    break;
                case Code.Cgt:
                case Code.Cgt_Un:
                    if (instruction.Previous.OpCode.Code == Code.Ldnull)
                    {
                        // Object-null checks are represented in CIL using Cg.
                        kind = BinopExpression.BinopKind.Ne;
                    }
                    else
                    {
                        kind = BinopExpression.BinopKind.Gt;
                    }
                    break;
                case Code.Clt:
                case Code.Clt_Un:
                    kind = BinopExpression.BinopKind.Lt;
                    break;
                case Code.Ceq:
                    kind = BinopExpression.BinopKind.Eq;
                    break;
                case Code.And:
                    if (state.Peek().Item2 is Tint intTypeAnd)
                    {
                        switch (intTypeAnd.Kind)
                        {
                            case Tint.IntKind.IBool:
                                kind = BinopExpression.BinopKind.LAnd;
                                break;
                            default:
                                kind = BinopExpression.BinopKind.BAnd;
                                break;
                        }
                    }
                    else
                    {
                        kind = BinopExpression.BinopKind.BAnd;
                    }
                    break;
                case Code.Or:
                    if (state.Peek().Item2 is Tint intTypeOr)
                    {
                        switch (intTypeOr.Kind)
                        {
                            case Tint.IntKind.IBool:
                                kind = BinopExpression.BinopKind.LOr;
                                break;
                            default:
                                kind = BinopExpression.BinopKind.BOr;
                                break;
                        }
                    }
                    else
                    {
                        kind = BinopExpression.BinopKind.BOr;
                    }
                    break;
                case Code.Xor:
                    kind = BinopExpression.BinopKind.BXor;
                    break;
                case Code.Shl:
                    kind = BinopExpression.BinopKind.Shiftlt;
                    break;
                case Code.Shr:
                case Code.Shr_Un:
                    kind = BinopExpression.BinopKind.Shiftrt;
                    break;
                default:
                    return false;
            }

            (_, var binopType) = state.Peek();
            (var exp, _) = state.PopTwoAndApplyBinop(kind);
            state.PushExpr(exp, binopType);
            state.PushInstruction(instruction.Next);

            return true;
        }
    }
}
