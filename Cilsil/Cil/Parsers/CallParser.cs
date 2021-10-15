// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Cil.Parsers
{
    internal class CallParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            bool isVirtual;
            switch (instruction.OpCode.Code)
            {
                case Code.Call:
                    isVirtual = false;
                    break;
                case Code.Callvirt:
                    isVirtual = true;
                    break;
                default:
                    return false;
            }

            var instrs = new List<SilInstruction>();

            var calledMethod = instruction.Operand as MethodReference;
            if (calledMethod.GetCompatibleFullName()
                            .Contains("System.Void System.Threading.Monitor::Enter"))
            {
                state.ProcDesc.PdAttributes.IsCSharpSynchronizedMethod = true;
                instrs.Add(CreateLockedAttributeCall(true, calledMethod.Parameters.Count, state));
            }
            else if (calledMethod.GetCompatibleFullName()
                                 .Contains("System.Void System.Threading.Monitor::Exit"))
            {
                instrs.Add(CreateLockedAttributeCall(false, calledMethod.Parameters.Count, state));
            }
            else
            {
                CreateMethodCall(state,
                                 isVirtual,
                                 calledMethod,
                                 out var retTypeRef,
                                 out var retId,
                                 out var callArgs,
                                 out var callInstr);

                instrs.Add(callInstr);

                // Deref on the object calling in the case of instance method, for null
                // validation on it by Infer. Object "this" is the first argument in the argument list
                // for instance methods.
                if (calledMethod.HasThis && calledMethod.Name != Identifier.ConstructorIdentifier)
                {
                    var thisArg = callArgs.First();
                    if (thisArg.Expression is VarExpression varExpression)
                    {
                        instrs.Insert(0, CreateDereference(varExpression, thisArg.Type, state));
                    }
                }

                var returnType = Typ.FromTypeReference(retTypeRef);
                if (!(returnType is Tvoid))
                {
                    state.PushExpr(new VarExpression(retId), returnType);
                }
            }

            var callNode = new StatementNode(location: state.CurrentLocation,
                                             kind: StatementNode.StatementNodeKind.Call,
                                             proc: state.ProcDesc,
                                             comment: calledMethod.GetCompatibleFullName());
            callNode.Instructions.AddRange(instrs);
            RegisterNode(state, callNode);

            state.PushInstruction(instruction.Next, callNode);
            state.AppendToPreviousNode = true;
            return true;
        }

        /// <summary>
        /// Returns a <see cref="Call"/> instruction denoting either the beginning or end of a
        /// program section guarded by a monitor, depending on whether the locked attribute is
        /// respectively set or deleted.
        /// </summary>
        /// <param name="setLockedAttribute"></param>
        /// <param name="paramCount"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static Call CreateLockedAttributeCall(
            bool setLockedAttribute, int paramCount, ProgramState state)
        {
            var returnIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var callFlags = new Call.CallFlags(isVirtual: false,
                                               noReturn: false,
                                               isObjCBlock: false);

            // There are up to two arguments (monitor object and a boolean indicating the result of
            // the attempt to acquire the lock, passed by reference), but the latter need only be
            // removed from the stack without being reflected in the translation.
            var arg = state.PopMany(paramCount).First();
            var callArgs = new List<Call.CallArg> { new Call.CallArg(arg.Item1, arg.Item2) };

            return setLockedAttribute ? new Call(returnIdentifier,
                                        new Tvoid(),
                                        new ConstExpression(
                                           ProcedureName.BuiltIn__set_locked_attribute),
                                           callArgs,
                                           callFlags,
                                           state.CurrentLocation)
                                     : new Call(returnIdentifier,
                                                new Tvoid(),
                                                new ConstExpression(
                                                    ProcedureName.
                                                        BuiltIn__delete_locked_attribute),
                                                callArgs,
                                                callFlags,
                                                state.CurrentLocation);
        }
    }
}
