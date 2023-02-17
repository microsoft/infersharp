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
    internal class LdlenParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldlen:
                    (var array, var type) = state.Pop();

                    var typeNoPointer = type.StripPointer();

                    if (!(array is VarExpression arrayVar) ||
                        !(typeNoPointer is Tarray))
                    {
                        Log.WriteParserWarning(type, instruction, state);
                        return false;
                    }
                    var derefArray = CreateDereference(arrayVar, type, state);
                    var tempIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    var arrayLengthType = new Tint(Tint.IntKind.IInt);
                    var arrayLengthCall = new Call(tempIdentifier,
                                                   arrayLengthType,
                                                   new ConstExpression(
                                                       ProcedureName.BuiltIn__get_array_length),
                                                   new List<Call.CallArg> 
                                                   { 
                                                       new Call.CallArg(array, type) 
                                                   },
                                                   new Call.CallFlags(),
                                                   state.CurrentLocation);
                    var newNode = AddMethodBodyInstructionsToCfg(state, derefArray, arrayLengthCall);
                    state.PushExpr(new VarExpression(tempIdentifier),
                                   arrayLengthType);

                    state.PushInstruction(instruction.Next, newNode);
                    state.AppendToPreviousNode = true;

                    return true;
                default:
                    return false;
            }
        }
    }
}
