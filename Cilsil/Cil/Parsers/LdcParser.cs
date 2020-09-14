// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdcParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            ConstExpression constExp = null;
            Typ type = null;

            switch (instruction.OpCode.Code)
            {
                case Code.Ldc_I4_M1:
                    (constExp, type) = MakeInt(-1);
                    break;
                case Code.Ldc_I4:
                    (constExp, type) = MakeInt((int)instruction.Operand);
                    break;
                case Code.Ldc_I4_S:
                    (constExp, type) = MakeInt((sbyte)instruction.Operand);
                    break;
                case Code.Ldc_I4_0:
                    (constExp, type) = MakeInt(0);
                    break;
                case Code.Ldc_I4_1:
                    (constExp, type) = MakeInt(1);
                    break;
                case Code.Ldc_I4_2:
                    (constExp, type) = MakeInt(2);
                    break;
                case Code.Ldc_I4_3:
                    (constExp, type) = MakeInt(3);
                    break;
                case Code.Ldc_I4_4:
                    (constExp, type) = MakeInt(4);
                    break;
                case Code.Ldc_I4_5:
                    (constExp, type) = MakeInt(5);
                    break;
                case Code.Ldc_I4_6:
                    (constExp, type) = MakeInt(6);
                    break;
                case Code.Ldc_I4_7:
                    (constExp, type) = MakeInt(7);
                    break;
                case Code.Ldc_I4_8:
                    (constExp, type) = MakeInt(8);
                    break;
                case Code.Ldc_I8:
                    (constExp, _) = MakeInt((long)instruction.Operand);
                    type = new Tint(Tint.IntKind.ILongLong);
                    break;
                case Code.Ldc_R4:
                    constExp = new ConstExpression((float)instruction.Operand);
                    type = new Tfloat(Tfloat.FloatKind.FFloat);
                    break;
                case Code.Ldc_R8:
                    constExp = new ConstExpression((double)instruction.Operand);
                    type = new Tfloat(Tfloat.FloatKind.FDouble);
                    break;
                case Code.Ldstr:
                    constExp = new ConstExpression((string)instruction.Operand);
                    type = new Tptr(Tptr.PtrKind.Pk_pointer, new Tstruct("System.String"));
                    break;
                default:
                    return false;
            }

            state.PushExpr(constExp, type);
            state.PushInstruction(instruction.Next);

            return true;
        }

        private (ConstExpression, Typ) MakeInt(long value) =>
            (
                new ConstExpression(new IntRepresentation(value, false, false)),
                new Tint(Tint.IntKind.IInt)
            );
    }
}
