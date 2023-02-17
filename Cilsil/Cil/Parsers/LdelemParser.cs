// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdelemParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {

            switch (instruction.OpCode.Code)
            {
                case Code.Ldelem_Any:
                case Code.Ldelem_I:
                case Code.Ldelem_I1:
                case Code.Ldelem_I2:
                case Code.Ldelem_I4:
                case Code.Ldelem_I8:
                case Code.Ldelem_R4:
                case Code.Ldelem_R8:
                case Code.Ldelem_Ref:
                    (var arrayIndex, _) = state.Pop();
                    (var array, var type) = state.Pop();

                    var typeNoPointer = type.StripPointer();

                    if (!(array is VarExpression arrayVar) || 
                        !(typeNoPointer is Tarray arrayTypeNoPtr))
                    {
                        Log.WriteParserWarning(type, instruction, state);
                        return false;
                    }
                    var derefArray = CreateDereference(arrayVar, type, state);

                    var tempIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    var arrayIndexLoad = new Load(identifierAssignedTo: tempIdentifier,
                                                  lvalue: new LindexExpression(
                                                      array, arrayIndex),
                                                  type: arrayTypeNoPtr.ContentType,
                                                  location: state.CurrentLocation);

                    var newNode = AddMethodBodyInstructionsToCfg(state,
                                                                 derefArray,
                                                                 arrayIndexLoad);

                    state.PushExpr(new VarExpression(tempIdentifier),
                                   arrayTypeNoPtr.ContentType);
                    state.AppendToPreviousNode = true;
                    state.PushInstruction(instruction.Next, newNode);
                    return true;
            }
            return false;
        }
    }
}