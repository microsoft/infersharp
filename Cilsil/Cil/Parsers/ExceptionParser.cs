// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Cilsil.Sil;
using Cilsil.Sil.Types;
using Mono.Cecil.Cil;
using Mono.Cecil;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Expressions;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class ExceptionParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            var endBlockOffset = state.ExceptionBlockStartToEndOffsets[state.CurrentInstruction.Offset];
            while (state.ExceptionBlockStartToEndOffsets.ContainsKey(endBlockOffset))
            {
                endBlockOffset = state.ExceptionBlockStartToEndOffsets[endBlockOffset];
            }
               
            /* Load returned variable for exception check. For example:
            
            node1 preds: succs:2 3 exn: Instructions
            n$47=*&amp;return:void;
            *&amp;return:void=null;
            n$48=_fun___unwrap_exception(n$47:void);*/

            // Create exception handler node.
            var exceptionHandlerNode = new StatementNode(state.CurrentLocation,
                                                         StatementNode.StatementNodeKind.ExceptionHandler,
                                                         state.ProcDesc);
            // Load returned excepted expression from stack for exception handling. 
            (var expression, var expressionType) = state.Pop(); 
            var identifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            exceptionHandlerNode.Instructions.Add(
                    new Load(identifier,
                             expression,
                             expressionType,
                             state.CurrentLocation));

            // When exception is encountered, we translate the return output to being a null/none type.
            var exceptionOwnerExpression = new VarExpression(identifier);
            var storeValueIntoVariable = new Store(expression,
                                                   new ConstExpression(new IntRepresentation(0, false, true)),
                                                   expressionType,
                                                   state.CurrentLocation);
            exceptionHandlerNode.Instructions.Add(storeValueIntoVariable);
            
            // Construct an instruction to unwrap exception from returned variable. For example: 
            // n${i}=_fun___unwrap_exception(n${i-1}:last var type*)
            CreateExceptionCall(state,
                                exceptionOwnerExpression,
                                expressionType,
                                out var exceptionType,
                                out var returnVariable,
                                out _,
                                out var exceptionCall);
            exceptionHandlerNode.Instructions.Add(exceptionCall);
            RegisterNode(state, exceptionHandlerNode, true);

            // The variable e allocated when an exception is encountered, 
            // i.e. "try { ... } catch(IOException e") { ... }
            var catchVariable = new LvarExpression(
                                new LocalVariable(Identifier.CatchIdentifier,
                                                    state.Method));
            // Construct a exception catch block when unwrapped exception type is not "System.Object".
            // Since catch block will always have instantiated exception type.
            if (exceptionType != state.Method.Module.TypeSystem.Object)
            {

                state.PushInstruction(instruction, exceptionHandlerNode);
                (instruction, _) = state.PopInstruction();

                /* Create exception allocation call and branch for handling exception. 
                If exception allocation is True, store allocated exception expression as caught 
                exception varaible. For example:

                node2 preds:1 succs:4 exn: Conditional exception handler
                n$49=_fun___instanceof(n$48:java.io.IOException*,sizeof(t=java.io.IOException):void)
                PRUNE(n$49, true);
                *&amp;CatchVar65:void=n$48

                node3 preds:1 succs:5 exn: Conditional exception handler -- Connects to outter/finally block code directly
                n$49=_fun___instanceof(n$48:java.io.IOException*,sizeof(t=java.io.IOException):void)
                PRUNE(!n$49, false);*/ 

                // Create exception allocation call:
                // n${i+1}=_fun___instanceof(n${i}:{Exception_Type}*,sizeof(t={Exception_Type}:void)
                (var exceptionAllocationCall, var exceptionObject) = 
                        CreateExceptionAllocationCall(state, Typ.FromTypeReference(exceptionType));   

                var trueNodeCondition = exceptionObject;
                var falseNodeCondition = new UnopExpression(
                    UnopExpression.UnopKind.LNot, trueNodeCondition, null);
                
                // Create exception handling branch. True if exception is caught. False otherwise.
                (var exceptionTrueNode, var exceptionFalseNode) =
                    CreateExceptionBranchConditionNodes(state,
                                                        trueNodeCondition,
                                                        falseNodeCondition);

                exceptionTrueNode.Instructions.Insert(0, exceptionAllocationCall);
                exceptionFalseNode.Instructions.Insert(0, exceptionAllocationCall);
                
                var catchVarIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                var catchVarStore = new Store(catchVariable,
                                              new VarExpression(returnVariable),
                                              expressionType,
                                              state.CurrentLocation);
                exceptionTrueNode.Instructions.Add(catchVarStore);

                RegisterNode(state, exceptionTrueNode);
                RegisterNode(state, exceptionFalseNode);

                Instruction nextInstructionIfNoException = null;
                
                foreach(var singleInstruction in state.Method.Body.Instructions)
                {
                    if (singleInstruction.Offset >= endBlockOffset + 1)
                    {
                        nextInstructionIfNoException = singleInstruction;
                        break;
                    }
                }
                state.PushExpr(new ExnExpression(new VarExpression(returnVariable)), expressionType);

                if (nextInstructionIfNoException != null)
                {
                    state.PushInstruction(nextInstructionIfNoException, exceptionFalseNode);
                }

                state.PushInstruction(instruction, exceptionTrueNode);
                (instruction, _) = state.PopInstruction();
            }
            // Construct a finally block when unwrapped exception type is "System.Object".
            else
            {
                state.PushRetExpr();
                ParseCilInstruction(instruction, state);
                state.PopInstruction();
                state.PushInstruction(instruction.Next);

                state.PushInstruction(instruction, exceptionHandlerNode);
                (instruction, _) = state.PopInstruction();
            }

            /* Load caught exception variable. For example:
            
            node 4: Preds:2 Succs:6 EXN: 
            n$25=*&CatchVar65:java.lang.Object*;
            *&e:java.lang.Object*=n$25;*/
            var newNode = new StatementNode(location: state.CurrentLocation,
                                            kind: StatementNode.StatementNodeKind.ExceptionHandler,
                                            proc: state.ProcDesc);
            var fieldType = new Tstruct("System.Object");
            var fieldIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            newNode.Instructions.Add(new Load(fieldIdentifier,
                                              catchVariable,
                                              fieldType,
                                              state.CurrentLocation));
            
            // Handles loaded exception expression in finally block.
            if (exceptionType == state.Method.Module.TypeSystem.Object)
            {
                var exceptionVariable = new LvarExpression(
                                        new LocalVariable(Identifier.ByteCodeIdentifier,
                                                          state.Method));
                newNode.Instructions.Add(new Store(exceptionVariable,
                                                   new VarExpression(fieldIdentifier),
                                                   fieldType,
                                                   state.CurrentLocation));
                state.PushExpr(exceptionVariable, fieldType);
            }
            else
            {
                state.AppendToPreviousNode = true;
            }

            RegisterNode(state, newNode);
             
            // The first copy of this stack item was popped in the invocation of the
            // constructor, so we push another on.
            state.PushExpr(new VarExpression(fieldIdentifier), fieldType);
            state.PushInstruction(instruction, newNode);

            // Append the next instruction (which should be stloc, for representing
            // storage of the constructed object into a local variable) to this new node.
            (instruction, _) = state.PopInstruction();
            ParseCilInstruction(instruction, state);

            return true; 
        }


        /// <summary>
        /// Creates a exception method call returned via out parameter.
        /// </summary>
        /// <param name="state">Current program state.</param>
        /// <param name="exceptedExpression">The excepted expression.</param>
        /// <param name="exceptedExpressionType">The type of excepted expression.</param>
        /// <param name="returnType">The return type of the exception method being called.</param>
        /// <param name="returnVariable">Identifies the variable returned by the method.</param>
        /// <param name="callArgs">The method arguments.</param>
        /// <param name="exceptionCall">The exception Call SIL instruction.</param>
        private void CreateExceptionCall(ProgramState state,
                                         Expression exceptedExpression,
                                         Typ exceptedExpressionType,
                                         out TypeReference returnType,
                                         out Identifier returnVariable,
                                         out List<Call.CallArg> callArgs,
                                         out Call exceptionCall)
        {
            callArgs = new List<Call.CallArg>();
            try
            {
                returnType = state.OffsetToExceptionType[state.CurrentInstruction.Offset];
            }   
            catch(KeyNotFoundException)
            {
                returnType = state.Method.Module.TypeSystem.Object;
            }
            
            var funcExp = new ConstExpression(ProcedureName.BuiltIn__unwrap_exception);
            callArgs.Add(new Call.CallArg(exceptedExpression, exceptedExpressionType));
            var callFlags = new Call.CallFlags(false, false, false);
            returnVariable = state.GetIdentifier(Identifier.IdentKind.Normal);

            if (returnType != state.Method.Module.TypeSystem.Object)
            {
                state.PushExpr(new VarExpression(returnVariable), Typ.FromTypeReference(returnType));
            }
            exceptionCall = new Call(returnId: returnVariable,
                                     returnType: Typ.FromTypeReference(returnType),
                                     functionExpression: funcExp,
                                     args: callArgs,
                                     flags: callFlags,
                                     location: state.CurrentLocation);
        }

        /// <summary>
        /// Creates and returns a new instance of <see cref="Call"/> instruction indicating 
        /// exception memory allocation and a VarExpression denoting the new exception object.
        /// </summary>
        /// <param name="state">Current program state.</param>
        /// <param name="exceptionType">Exception type of current exception block.</param>
        /// <returns>Instruction representing the exception memory allocation, as well as the 
        /// VarExpression denoting the new exception object.</returns>
        private (Call, VarExpression) CreateExceptionAllocationCall(ProgramState state, Typ exceptionType)
        {
            var newObjectIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var callFlags = new Call.CallFlags(isVirtual: false,
                                               noReturn: false,
                                               isObjCBlock: false);
            var args = new List<Call.CallArg>();

            if (state.GetProgramStackCopy().Count != 0)
            {
                (var expression, var type) = state.Pop();
                args.Add(new Call.CallArg(expression, type));
            }
            args.Add(
                new Call.CallArg(new SizeofExpression(exceptionType.StripPointer(), "exact"), new Tvoid()));

            return (new Call(newObjectIdentifier,
                             exceptionType,
                             new ConstExpression(ProcedureName.BuiltIn__instanceof),
                             args,
                             callFlags,
                             state.CurrentLocation),
                    new VarExpression(newObjectIdentifier));
        }

        private (CfgNode, CfgNode) CreateExceptionBranchConditionNodes(ProgramState state,
                                                              Expression trueNodeCondition,
                                                              Expression falseNodeCondition)
        {
            var truePruneNode = new PruneNode(state.CurrentLocation,
                                              true,
                                              PruneNode.PruneNodeKind.MethodBody,
                                              Prune.IfKind.Ik_if,
                                              state.ProcDesc);

            var falsePruneNode = new PruneNode(state.CurrentLocation,
                                               false,
                                               PruneNode.PruneNodeKind.MethodBody,
                                               Prune.IfKind.Ik_if,
                                               state.ProcDesc);
            truePruneNode.Instructions.Add(new Prune(trueNodeCondition,
                                                     true,
                                                     Prune.IfKind.Ik_if,
                                                     state.CurrentLocation));
            falsePruneNode.Instructions.Add(new Prune(falseNodeCondition,
                                                      false,
                                                      Prune.IfKind.Ik_if,
                                                      state.CurrentLocation));

            return (truePruneNode, falsePruneNode);
        }
    }
}