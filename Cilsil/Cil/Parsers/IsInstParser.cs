// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class IsInstParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Isinst:
                    (var objectExpression, var objectType) = state.Pop();
                    var typeToCheck = instruction.Operand as TypeReference;
                    var returnIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    var returnType = new Tint(Tint.IntKind.IBool, true);
                    var builtinFunctionExpression = new ConstExpression(
                        ProcedureName.BuiltIn__instanceof);
                    var sizeofExpression = new SizeofExpression(
                        Typ.FromTypeReferenceNoPointer(typeToCheck), 
                        SizeofExpression.SizeofExpressionKind.instof);
                    var args = new List<Call.CallArg> 
                    { 
                        new Call.CallArg(objectExpression, objectType), 
                        new Call.CallArg(sizeofExpression, new Tvoid())
                    };
                    var callInstruction = new Call(
                        returnIdentifier, 
                        returnType, 
                        builtinFunctionExpression, 
                        args, 
                        new Call.CallFlags(), 
                        state.CurrentLocation);
                    var newNode = AddMethodBodyInstructionsToCfg(state, callInstruction);
                    state.PushExpr(new VarExpression(returnIdentifier), returnType);
                    state.PushInstruction(instruction.Next, newNode);
                    return true;
                default:
                    return false;
            }
        }
    }
}
