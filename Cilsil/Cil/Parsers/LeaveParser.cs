// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class LeaveParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // Leave for try block: indicates normal control flow, avoiding fall-through as a
                // means of exiting it. However, target is not finally block but rather whatever
                // comes after catch/finally.
                // Leave for catch block: target instruction immediately after catch block.
                case Code.Leave:
                case Code.Leave_S:
                    var exnInfo = state.MethodExceptionHandlers;
                    var target = instruction.Operand as Instruction;
                    state.PushInstruction(target);
                    // Leave occurs within try block and has catch handler(s).
                    if (exnInfo.TryOffsetToCatchHandlers.ContainsKey(instruction.Offset))
                    {
                        (var entryNode, var exceptionIdentifier) = GetHandlerEntryNode(
                            state, exnInfo.TryOffsetToCatchHandlers[instruction.Offset][0]
                                          .ExceptionHandler);
                        // Exceptional control flow routes through the first of the set of
                        // associated catch handlers; this invocation pushes the catch handler's
                        // first instruction onto the stack and continues the translation from the
                        // handler's catch variable load node. 
                        CreateCatchHandlerEntryBlock(
                            state, 
                            exnInfo.TryOffsetToCatchHandlers[instruction.Offset][0],
                            entryNode, 
                            exceptionIdentifier);
                        CreateExceptionalEdges(state, entryNode);

                        // Regular control flow routes through the finally block, if present, prior
                        // to routing control flow to leave.
                        if (exnInfo.TryOffsetToFinallyHandler.ContainsKey(instruction.Offset))
                        {
                            var finallyHandler =
                                exnInfo.TryOffsetToFinallyHandler[instruction.Offset];
                            // Creates a new node to ensure finally instructions aren't attached
                            // to 
                            var finallyHandlerStartNode = new StatementNode(
                                location: GetHandlerStartLocation(state, finallyHandler),
                                kind: StatementNode.StatementNodeKind.MethodBody,
                                proc: state.ProcDesc);

                            state.Cfg.RegisterNode(finallyHandlerStartNode);
                            state.PreviousNode.Successors.Add(finallyHandlerStartNode);
                            state.AppendToPreviousNode = true;

                            state.PushInstruction(finallyHandler.HandlerStart, 
                                                  finallyHandlerStartNode);
                        }
                    }
                    // Leave occurs within try block without an associated catch handler.
                    else if (exnInfo.TryOffsetToFinallyHandler.ContainsKey(instruction.Offset))
                    {
                        var finallyHandler =
                            exnInfo.TryOffsetToFinallyHandler[instruction.Offset];
                        var finallyEntryNode =
                            CreateFinallyExceptionalEntryBlock(state, finallyHandler);
                        CreateExceptionalEdges(state, finallyEntryNode);
                        (var loadCatchVarNode, _) =
                            GetHandlerCatchVarNode(state, finallyHandler);
                        state.PushInstruction(finallyHandler.HandlerStart, state.PreviousNode);
                        state.PushInstruction(finallyHandler.HandlerStart, loadCatchVarNode);
                    }
                    // Leave occurs within catch block, as leave can only occur in one of the two.
                    else
                    {
                        var currentHandler = exnInfo.CatchOffsetToCatchHandler[instruction.Offset];
                        // Exceptional control flow routes through the finally block, if present,
                        // prior to routing control flow to leave.
                        if (currentHandler.FinallyBlock != null)
                        {
                            var finallyEntryNode =
                                CreateFinallyExceptionalEntryBlock(state,
                                                                   currentHandler.FinallyBlock);
                            CreateExceptionalEdges(state, finallyEntryNode);
                            (var loadCatchVarNode, _) =
                                GetHandlerCatchVarNode(state, currentHandler.FinallyBlock);
                            state.PushInstruction(currentHandler.FinallyBlock.HandlerStart,
                                                  loadCatchVarNode);
                        }

                        if (currentHandler.NextCatchBlock != null)
                        {
                            (_, var exceptionIdentifier) = GetHandlerEntryNode(
                                state, currentHandler.FirstCatchHandler);
                            // Continues translation with catch handler's first instruction from
                            // the handler's catch variable load node.
                            CreateCatchHandlerEntryBlock(
                                state, 
                                currentHandler.NextCatchBlock, 
                                currentHandler.CatchHandlerLatestFalseEntryNode, 
                                exceptionIdentifier);
                        }
                    }

                    return true;
                default:
                    return false;
            }            
        }

        private static void CreateCatchHandlerEntryBlock(ProgramState state, 
                                                         ExceptionHandlerNode handlerNode,
                                                         CfgNode handlerEntryPredecessor, 
                                                         Identifier exceptionIdentifier)
        {
            (var trueBranch, var falseBranch) = CreateExceptionTypeCheckBranchNodes(
                state, handlerNode.ExceptionHandler, exceptionIdentifier);
            (var loadCatchVarNode, _) = GetHandlerCatchVarNode(
                state, handlerNode.ExceptionHandler);
            handlerNode.CatchHandlerLatestFalseEntryNode = falseBranch;
            handlerEntryPredecessor.Successors.Add(trueBranch);
            handlerEntryPredecessor.Successors.Add(falseBranch);
            trueBranch.Successors.Add(loadCatchVarNode);

            // The CIL specification dictates that the exception object is on top of
            // the stack when the catch handler is entered; the first instruction of
            // the catch handler will handle the object pushed onto the stack.
            state.PushExpr(new VarExpression(exceptionIdentifier),
                           new Tptr(Tptr.PtrKind.Pk_pointer,
                                    new Tstruct("System.Object")));
            state.PushInstruction(handlerNode.ExceptionHandler.HandlerStart,
                                  loadCatchVarNode);
        }

        private static void CreateExceptionalEdges(ProgramState state, CfgNode entryNode)
        {
            foreach (var node in state.NodesToLinkWithExceptionBlock)
            {
                node.ExceptionNodes.Add(entryNode);
            }
            state.NodesToLinkWithExceptionBlock = new List<CfgNode>();
        }

    }
}
