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
    internal class BoxParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Box:
                    (var value, var type) = state.Pop();

                    var boxedValueIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    var boxedObjectVariable = new VarExpression(boxedValueIdentifier);
                    var boxedObjectType = new BoxedValueType(Tptr.PtrKind.Pk_pointer,
                                                             new Tstruct("System.Object"),
                                                             value,
                                                             type);
                    var callFlags = new Call.CallFlags(false, false, false);

                    // The value in question is boxed into a generic object.
                    var objectAllocationCall =
                        new Call(boxedValueIdentifier,
                                 boxedObjectType,
                                 new ConstExpression(ProcedureName.BuiltIn__new),
                                 new List<Call.CallArg> 
                                 { 
                                     new Call.CallArg(
                                         new SizeofExpression(
                                             boxedObjectType.StripPointer(), "exact"), 
                                         boxedObjectType) 
                                 },
                                 callFlags,
                                 state.CurrentLocation);

                    var objectConstructorCall =
                        new Call(state.GetIdentifier(Identifier.IdentKind.Normal),
                                 new Tvoid(),
                                 new ConstExpression(new ProcedureName(".ctor",
                                                                       new List<string>(),
                                                                       "System.Object",
                                                                       "System.Void",
                                                                       false)),
                                 new List<Call.CallArg>
                                 {
                                     new Call.CallArg(boxedObjectVariable, boxedObjectType)
                                 },
                                 callFlags,
                                 state.CurrentLocation);

                    var node = new StatementNode(state.CurrentLocation, 
                                                 StatementNode.StatementNodeKind.Call, 
                                                 state.ProcDesc, 
                                                 comment: "System.Void System.Object::.ctor()");
                    node.Instructions.Add(objectAllocationCall);
                    node.Instructions.Add(objectConstructorCall);
                    RegisterNode(state, node);

                    state.PushExpr(boxedObjectVariable, boxedObjectType);
                    state.PushInstruction(instruction.Next, node);
                    state.AppendToPreviousNode = true;

                    return true;
                default:
                    return false;
            }
        }
    }
}
