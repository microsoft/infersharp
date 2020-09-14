// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class NewarrParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Newarr:
                    Typ arrayContentType;
                    // The array is one-dimensional.
                    if (instruction.Operand is TypeReference instructionType)
                    {
                        arrayContentType = Typ.FromTypeReference(instructionType);
                    }
                    // Then the content type of the array is an array (it is multidimensional).
                    else if (instruction.Operand is ArrayType instructionArrayType)
                    {
                        // Creates a SIL representation of the array content type.
                        arrayContentType = CreateArrayType(
                            Typ.FromTypeReference(instructionArrayType.GetElementType()),
                            instructionArrayType.Rank, state);
                    }
                    else
                    {
                        Log.WriteParserError(instruction.Operand, instruction, state);
                        return false;
                    }
                    var arrayIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    (var arrayLength, _) = state.Pop();

                    var arrayLengthSizeofExp = new SizeofExpression(
                        new Tarray(arrayContentType),
                        "exact",
                        arrayLength);

                    var arrayTypeWithPtr = new Tptr(Tptr.PtrKind.Pk_pointer,
                                                new Tarray(arrayContentType));

                    var args = new List<Call.CallArg>
                    {
                        new Call.CallArg(arrayLengthSizeofExp, arrayTypeWithPtr)
                    };

                    // Represents memory allocation.
                    var callInstr = new Call(returnId: arrayIdentifier,
                                             returnType: arrayTypeWithPtr,
                                             functionExpression: new ConstExpression(
                                                 ProcedureName.BuiltIn__new_array),
                                             args: args,
                                             flags: new Call.CallFlags(),
                                             location: state.CurrentLocation);
                    var newNode = AddMethodBodyInstructionsToCfg(state, callInstr);
                    state.PushExpr(new VarExpression(arrayIdentifier), arrayTypeWithPtr);
                    state.PushInstruction(instruction.Next, newNode);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a SIL array representation. Generates a SIL Tptr of Tarray around the input 
        /// underlying type, with layers equal to the given <paramref name="dimensionCount"/>.
        /// </summary>
        /// <param name="arrayUnderlyingType">The type of the array's underlying elements (i.e. at 
        /// the base dimension of the array). This typ must be encapsulated by a Tptr.</param>
        /// <param name="dimensionCount">Number of dimensions of the array. This must be greater
        /// than 0.</param>
        /// <param name="state">The program state.</param>
        /// <returns>SIL Tptr of Tarray for each dimension.</returns>
        /// <remarks>An example two-dimensional output with <paramref name="arrayUnderlyingType"/>
        /// object* would look like [[object*]*]*.</remarks>
        private Typ CreateArrayType(Typ arrayUnderlyingType,
                                    int dimensionCount,
                                    ProgramState state)
        {
            if (dimensionCount <= 0)
            {
                throw new ArgumentException(
                    state.GetStateDebugInformation(dimensionCount));
            }
            if (!(arrayUnderlyingType is Tptr))
            {
                throw new ArgumentException(
                    state.GetStateDebugInformation(arrayUnderlyingType));
            }
            var nextDimType = arrayUnderlyingType;
            for (var i = 0; i < dimensionCount; i++)
            {
                nextDimType = new Tptr(Tptr.PtrKind.Pk_pointer, new Tarray(nextDimType));
            }
            return nextDimType;
        }
    }
}
