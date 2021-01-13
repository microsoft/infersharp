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
    internal class LdtokenParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldtoken:
                    var token = instruction.Operand;
                    string runtimeHandleTypeString;

                    if (token is TypeReference)
                    {
                        runtimeHandleTypeString = "System.RuntimeTypeHandle";
                    }
                    else if (token is FieldReference)
                    {
                        runtimeHandleTypeString = "System.RuntimeFieldHandle";
                    }
                    else if (token is MethodReference)
                    {
                        runtimeHandleTypeString = "System.RuntimeMethodHandle";
                    }
                    else
                    {
                        Log.WriteError($"Unexpected instruction operand {instruction.Operand}");
                        return false;
                    }
                    var runtimeHandleStructType = new Tstruct(runtimeHandleTypeString);
                    // In keeping with how we handle structs when translating Initobj, we
                    // initialize an object in the SIL to represent the value type.
                    var runtimeHandleIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    var runtimeHandleVariable = new VarExpression(runtimeHandleIdentifier);
                    var runtimeHandleType = new Tptr(Tptr.PtrKind.Pk_pointer, 
                                                     runtimeHandleStructType);
                    var callFlags = new Call.CallFlags(false, false, false);
                    var objectAllocationCall =
                        new Call(runtimeHandleIdentifier,
                                 runtimeHandleType,
                                 new ConstExpression(ProcedureName.BuiltIn__new),
                                 new List<Call.CallArg>
                                 {
                                     new Call.CallArg(
                                         new SizeofExpression(
                                             runtimeHandleType.StripPointer(), "exact"),
                                         runtimeHandleType)
                                 },
                                 callFlags,
                                 state.CurrentLocation);

                    var objectConstructorCall =
                        new Call(state.GetIdentifier(Identifier.IdentKind.Normal),
                                 new Tvoid(),
                                 new ConstExpression(new ProcedureName(".ctor",
                                                                       new List<string>(),
                                                                       runtimeHandleTypeString,
                                                                       "System.Void",
                                                                       false)),
                                 new List<Call.CallArg>
                                 {
                                     new Call.CallArg(runtimeHandleVariable, runtimeHandleType)
                                 },
                                 callFlags,
                                 state.CurrentLocation);

                    var node = new StatementNode(
                        state.CurrentLocation,
                        StatementNode.StatementNodeKind.Call,
                        state.ProcDesc,
                        comment: $"System.Void {runtimeHandleTypeString} ::.ctor()");

                    node.Instructions.Add(objectAllocationCall);
                    node.Instructions.Add(objectConstructorCall);
                    RegisterNode(state, node);

                    state.PushExpr(runtimeHandleVariable, runtimeHandleType);
                    state.PushInstruction(instruction.Next, node);
                    state.AppendToPreviousNode = true;

                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
