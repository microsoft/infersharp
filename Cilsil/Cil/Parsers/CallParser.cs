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
using System;
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
            var calledMethodName = calledMethod.GetCompatibleFullName();
            if (calledMethodName.Contains("System.Void System.Threading.Monitor::Enter"))
            {
                state.ProcDesc.PdAttributes.IsCSharpSynchronizedMethod = true;
                instrs.Add(CreateLockedAttributeCall(true, calledMethod.Parameters.Count, state));
            }
            else if (calledMethodName.Contains("System.Void System.Threading.Monitor::Exit"))
            {
                instrs.Add(CreateLockedAttributeCall(false, calledMethod.Parameters.Count, state));
            }
            else if (calledMethodName.Contains("op_") && 
                     HandleOperatorMethod(calledMethod.GetCompatibleFullName(), state))
            {
                
                state.PushInstruction(instruction.Next);
                return true;
            }
            else if (MethodDeclaringTypeIsDelegateType(calledMethod) && 
                     calledMethod.Name == "Invoke")
            {
                // We create an SIL call to represent the delegate's "Invoke" method, where the
                // method being called is actually that referred to by the function pointer.
                CreateMethodCall(state,
                                 isVirtual,
                                 calledMethod,
                                 out var _,
                                 out var retId,
                                 out var callArgs,
                                 out var _);
                // The function type is the first argument, which will be of type Tfun in this
                // case.
                if (!(callArgs[0].Type is Tfun methodType))
                {
                    Log.WriteWarning("Unexpected arg on stack: expected function");
                    return false;
                }
                else
                {
                    var methodExpression = callArgs[0].Expression;
                    // The remaining arguments are the actual arguments of the method.
                    callArgs.RemoveAt(0);
                    var methodReturnType = Typ.FromTypeReference(methodType.Method.ReturnType);
                    var callInstr = new Call(retId,
                                             methodReturnType,
                                             methodExpression,
                                             callArgs,
                                             new Call.CallFlags(isVirtual),
                                             state.CurrentLocation);
                    instrs.Add(callInstr);
                    if (!(methodReturnType is Tvoid))
                    {
                        state.PushExpr(new VarExpression(retId), methodReturnType);
                    }
                }
            }
            // The SetResult method is used to set the output of an async method. To model this for
            // Infer, we simply represent it as a return value.
            else if (state.IsMoveNextAsyncMethod() && 
                     calledMethod.Name == "SetResult" && 
                     state.MethodDefinitionToUpdate != null)
            {
                // Async methods that return a "Task" behave like methods that return void; that
                // is, the top expression is the builder field, rather than the secondmost
                // expression as it is in non-void async methods. 
                var isAsyncNonVoidMethod =
                    state.MethodDefinitionToUpdate.ReturnType.GetElementType() !=
                        state.Method.Module.TypeSystem.Void && 
                    !state.TopExpressionIsBuilderField();

                // We treat the SetResult method identically to how we would treat the ret
                // instruction on regular method; if the corresponding async method actually has a
                // return value, we create the return instruction. Otherwise, we simply exit as we
                // would for the return on a regular void method.
                if (isAsyncNonVoidMethod)
                {
                    Expression returnVariable = new LvarExpression(
                        new LocalVariable(Identifier.ReturnIdentifier,
                                          state.MethodDefinitionToUpdate));
                    (var returnValue, var retType) = state.Pop();

                    // The top stack item should refer to the builder argument, which we discard.
                    // If it's not a builder argument, there is something unexpected going on.
                    if (!state.TopExpressionIsBuilderField())
                    {
                        Log.WriteWarning("Async method did not find expected builder field.");
                        return false;
                    }
                    _ = state.Pop();
                    var retInstr = new Store(returnVariable,
                                             returnValue,
                                             retType,
                                             state.CurrentLocation);
                    var retNode = new StatementNode(state.CurrentLocation,
                                                    StatementNode.StatementNodeKind.ReturnStmt,
                                                    state.ProcDesc);
                    retNode.Instructions.Add(retInstr);
                    retNode.Successors = new List<CfgNode> { state.ProcDesc.ExitNode };
                    RegisterNode(state, retNode);
                }
                else
                {
                    _ = state.Pop();
                    state.PreviousNode.Successors.Add(state.ProcDesc.ExitNode);

                }
                return true;
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
                foreach (var arg in callArgs)
                {
                    // If the argument is already passed a reference, we don't want to pass it by
                    // reference to another method; we want to pass its value instead.
                    if (arg.Type is Address address &&
                        address.AddressType == Address.ReferenceKind.Parameter)
                    {
                        var freshIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                        var valueExp = new VarExpression(freshIdentifier);
                        instrs.Add(new Load(freshIdentifier, 
                                            arg.Expression, 
                                            arg.Type, 
                                            state.CurrentLocation));
                        arg.Expression = valueExp;
                    }
                }

                instrs.Add(callInstr);

                // Deref on the object calling in the case of instance method, for null
                // validation on it by Infer. Object "this" is the first argument in the argument list
                // for instance methods.
                if (calledMethod.HasThis && calledMethod.Name != Identifier.ConstructorIdentifier)
                {
                    var thisArg = callArgs.First();
                    if (thisArg.Expression is VarExpression varExpression &&
                        !varExpression.FromThis &&
                        !(thisArg.Type is Address))
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
        /// Reduces System operator methods into their binary expressions.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="state"></param>
        private static bool HandleOperatorMethod(string methodName, ProgramState state)
        {
            BinopExpression.BinopKind kind;
            (var right, _) = state.Peek();
            // The expression is comparing object equality/inequality to null; we represent these
            // as null checks with the corresponding equality/inequality operator.
            if ((methodName.Contains("op_Equality") || methodName.Contains("op_Inequality")) &&
                right.Equals(new ConstExpression(new IntRepresentation(0, false, true))))
            {
                kind = methodName.Contains("op_Equality") ? 
                    BinopExpression.BinopKind.Eq : BinopExpression.BinopKind.Ne;
                (var equalityExp, var type) = state.PopTwoAndApplyBinop(kind);
                state.PushExpr(equalityExp, type);
            }
            // These methods can involve conversion between SqlBoolean and bool; to help the
            // analysis understand the behavior, we simply treat these as nops so that the analysis
            // directly interprets the underlying bool. 
            else if ((methodName.Contains("Boolean::op_True") || 
                      methodName.Contains("Boolean::op_Implicit"))) {}
            else
            {
                return false;
            }
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
