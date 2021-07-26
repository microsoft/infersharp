// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdftnParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {    
                case Code.Ldftn:
                    var calledMethod = instruction.Operand as MethodReference;
                    var returnType = calledMethod.ReturnType;

                    var fieldIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    (var value, var type) = state.Peek();
                    if (value is VarExpression variableExpression)
                    {
                        var variableId = variableExpression.Identifier.Stamp;
                        var functionExpression = new VarExpression(state.GetIdentifier(Identifier.IdentKind.Normal, 
                                                                                       Identifier.ThisIdentifier,
                                                                                       variableId));
                        state.PreviousNode.Instructions.Add(new Load(fieldIdentifier,
                                                                     functionExpression,
                                                                     Typ.FromTypeReferenceNoPointer(
                                                                     state.Method.DeclaringType),
                                                                     state.CurrentLocation));
                    }
                    
                    state.AppendToPreviousNode = true;
                    state.PushExpr(new VarExpression(fieldIdentifier), 
                                   Typ.FromTypeReference(calledMethod.DeclaringType));
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }            
        }
    }
}