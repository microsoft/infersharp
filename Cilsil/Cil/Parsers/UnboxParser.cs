// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace Cilsil.Cil.Parsers
{
    internal class UnboxParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // Note that if unbox is applied on an object which is interprocedurally returned, 
                // the object on the stack will not be a boxedValueType and unbox will fail. This
                // is a result of being unable to statically verify the type of the boxed object.
                case Code.Unbox:
                case Code.Unbox_Any:
                    (_, var objType) = state.Pop();
                    if (objType is BoxedValueType boxedValueType)
                    {
                        var value = boxedValueType.BoxedVal;
                        var valueType = boxedValueType.BoxedValType;
                        if (instruction.OpCode.Code == Code.Unbox)
                        {
                            state.PushExpr(value, new Address(Tptr.PtrKind.Pk_pointer,
                                                              valueType,
                                                              value));
                        }
                        else if (instruction.OpCode.Code == Code.Unbox_Any)
                        {
                            state.PushExpr(value, valueType);
                        }
                    }
                    else
                    {
                        if (instruction.Operand is TypeReference type)
                        {
                            var silType = Typ.FromTypeReferenceNoPointer(type);
                            var defaultBoxedValue = GetDefaultBoxedValue(silType);
                            if (defaultBoxedValue != null)
                            {
                                state.PushExpr(defaultBoxedValue, silType);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a <see cref="ConstExpression"/> representing a default boxed value, to be used 
        /// in cases where we can recognize the expected primitive type and the type retrieved from 
        /// the stack is not a <see cref="BoxedValueType"/>.
        /// </summary>
        /// <param name="silType">The type for which to retrieve a default value.</param>
        /// <returns>The default value, if the input type is recognized; null otherwise.</returns>
        private ConstExpression GetDefaultBoxedValue(Typ silType)
        {
            if (silType is Tint silInt)
            {
                switch (silInt.Kind)
                {
                    // Cases represented using an unsigned integer.
                    case Tint.IntKind.IUChar:
                    case Tint.IntKind.IUInt:
                    case Tint.IntKind.IUShort:
                    case Tint.IntKind.IULong:
                    case Tint.IntKind.IULongLong:
                    case Tint.IntKind.IU128:
                        return new ConstExpression(new IntRepresentation(0, true, false));
                    // Cases represented using a signed integer.
                    // Booleans are represented as signed integers in LdcParser, because their
                    // values are pushed using ldc.i4.0/ldc.i4.1.
                    case Tint.IntKind.IBool:
                    case Tint.IntKind.IChar:
                    case Tint.IntKind.ISChar:
                    case Tint.IntKind.IInt:
                    case Tint.IntKind.IShort:
                    case Tint.IntKind.ILong:
                    case Tint.IntKind.ILongLong:
                    case Tint.IntKind.I128:
                        return new ConstExpression(new IntRepresentation(0, false, false));
                    default:
                        throw new ArgumentException("Unhandled initobj SIL integer type.");
                }
            }
            else if (silType is Tfloat silFloat)
            {
                switch (silFloat.Kind)
                {
                    case Tfloat.FloatKind.FFloat:
                    case Tfloat.FloatKind.FDouble:
                    case Tfloat.FloatKind.FLongDouble:
                        return new ConstExpression(0F);
                    default:
                        Log.WriteWarning("Unhandled initobj SIL float or double type.");
                        return null;
                }
            }
            else
            {
                Log.WriteWarning($"Unable to produce default value for type {silType}");
                return null;
            }
        }

    }
}
