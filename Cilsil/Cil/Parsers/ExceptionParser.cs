using Cilsil.Utils;
using Cilsil.Sil;
using Cilsil.Sil.Types;
using Mono.Cecil.Cil;
using Mono.Cecil;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Cil.Parsers
{
    internal class ExceptionParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            var endBlockOffset = state.ExceptionBlockStartToEndOffsets[state.CurrentInstruction.Offset];
            
            /*node38 preds: succs:36 37 exn:3 Instructions
            n$47=*&amp;return:void [line 55];   # load return
            *&amp;return:void=null [line 55];
            n$48=_fun___unwrap_exception(n$47:void) [line 55];*/

            // Create exception handler node.
            var exceptionHandlerNode = new StatementNode(state.CurrentLocation,
                                                         StatementNode.StatementNodeKind.ExceptionHandler,
                                                         state.ProcDesc);
            // Load expression from stack. 
            (var expression, var expressionType) = state.Pop(); 
            var identifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            exceptionHandlerNode.Instructions.Add(
                    new Load(identifier,
                             expression,
                             expressionType,
                             state.CurrentLocation));

            // Clear the loaded expression to null.
            var exceptionOwnerExpression = new VarExpression(identifier);
            var storeValueIntoVariable = new Store(expression,
                                                   new ConstExpression(new IntRepresentation(0, false, true)),
                                                   expressionType,
                                                   state.CurrentLocation);
            exceptionHandlerNode.Instructions.Add(storeValueIntoVariable);
            
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

            Expression catchVariable = null;
            if (exceptionType != state.Method.Module.TypeSystem.Object)
            {
                state.PushInstruction(instruction, exceptionHandlerNode);
                instruction = state.PopInstruction();

                /*node36 preds:38 succs:18 exn:3 Conditional exception handler
                n$49=_fun___instanceof(n$48:java.io.IOException*,sizeof(t=java.io.IOException):void) [line 55];
                PRUNE(n$49, true); [line 55];
                *&amp;CatchVar65:void=n$48 [line 55];*/

                /*node37 preds:38 succs:35 exn:3 Conditional exception handler -- Connects to outter/finally block code directly
                n$49=_fun___instanceof(n$48:java.io.IOException*,sizeof(t=java.io.IOException):void) [line 55];
                PRUNE(!n$49, false); [line 55];*/ 

                // n${i+1}=_fun___instanceof(n${i}:{Exception_Type}*,sizeof(t={Exception_Type}:void)
                (var exceptionAllocationCall, var objectVariable) = 
                        CreateExceptionAllocationCall(state, Typ.FromTypeReference(exceptionType));   

                var trueNodeCondition = objectVariable;
                var falseNodeCondition = new UnopExpression(
                    UnopExpression.UnopKind.LNot, trueNodeCondition, null);
                (var exceptionTrueNode, var exceptionFalseNode) =
                    CreateExceptionBranchConditionNodes(state,
                                                        trueNodeCondition,
                                                        falseNodeCondition);

                exceptionTrueNode.Instructions.Insert(0, exceptionAllocationCall);
                exceptionFalseNode.Instructions.Insert(0, exceptionAllocationCall);
                
                // Create catch exception variable expression
                catchVariable = new LvarExpression(
                                new LocalVariable(Identifier.CatchIdentifier,
                                                    state.Method));
                var catchVarIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                var catchVarStore = new Store(catchVariable,
                                              new VarExpression(returnVariable),
                                              expressionType,
                                              state.CurrentLocation);
                exceptionTrueNode.Instructions.Add(catchVarStore);

                RegisterNode(state, exceptionTrueNode);
                RegisterNode(state, exceptionFalseNode);

                Instruction targetFalse = null;
                
                foreach(var single_instruction in state.Method.Body.Instructions)
                {
                    if (single_instruction.Offset >= endBlockOffset + 1)
                    {
                        targetFalse = single_instruction;
                        break;
                    }
                }
                state.PushExpr(new VarExpression(returnVariable), expressionType);

                state.PushInstruction(targetFalse, exceptionFalseNode);

                state.PushInstruction(instruction, exceptionTrueNode);
                instruction = state.PopInstruction();
            }
            else
            {
                state.ExceptionBlockStartToEndOffsets.Remove(state.CurrentInstruction.Offset);
                state.PushInstruction(instruction.Next);

                state.PushInstruction(instruction, exceptionHandlerNode);
                instruction = state.PopInstruction();
                
                // Create catch exception variable expression
                catchVariable = new LvarExpression(
                                new LocalVariable(Identifier.CatchIdentifier,
                                                  state.Method));
                var catchVarIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                var catchVarStore = new Store(catchVariable,
                                              new VarExpression(returnVariable),
                                              expressionType,
                                              state.CurrentLocation);
            }

            /* node 18: Preds: [36] Succs: [19] EXN: [40] 
            n$25=*&CatchVar65:java.lang.Object* [line 55];
            *&e:java.lang.Object*=n$25 [line 55];*/
            var newNode = new StatementNode(location: state.CurrentLocation,
                                            kind: StatementNode.StatementNodeKind.ExceptionHandler,
                                            proc: state.ProcDesc);
            var fieldType = new Tstruct("System.Object");
            var fieldIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            newNode.Instructions.Add(new Load(fieldIdentifier,
                                              catchVariable,
                                              fieldType,
                                              state.CurrentLocation));
            
            // Handles load expression in finally block
            if (exceptionType == state.Method.Module.TypeSystem.Object)
            {
                var exceptionVariable = new LvarExpression(
                                        new LocalVariable(Identifier.BCIdentifier,
                                                          state.Method));
                newNode.Instructions.Add(new Store(exceptionVariable,
                                                   new VarExpression(fieldIdentifier),
                                                   fieldType,
                                                   state.CurrentLocation));
                state.PushExpr(exceptionVariable, fieldType);
            }
            else{
                state.AppendToPreviousNode = true;
            }

            RegisterNode(state, newNode);
             
            // The first copy of this stack item was popped in the invocation of the
            // constructor, so we push another on.
            state.PushExpr(new VarExpression(fieldIdentifier), fieldType);
            state.PushInstruction(instruction, newNode);

            // Append the next instruction (which should be stloc, for representing
            // storage of the constructed object into a local variable) to this new node.
            instruction = state.PopInstruction();
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
                state.PushExpr(new VarExpression(returnVariable), Typ.FromTypeReference(returnType));
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