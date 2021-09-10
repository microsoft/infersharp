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

                    // Leave within try of catch-block.
                    if (exnInfo.TryOffsetToCatchHandlers.ContainsKey(instruction.Offset))
                    {
                        if (state.NodesToLinkWithExceptionBlock.Count > 0)
                        {
                            CfgNode entryNode;
                            Identifier exceptionIdentifier;
                            if (!state.LeaveToExceptionEntryNode.ContainsKey(instruction))
                            {
                                (entryNode, exceptionIdentifier) = GetHandlerEntryNode(
                                    state, exnInfo.TryOffsetToCatchHandlers[instruction.Offset][0]
                                                  .ExceptionHandler);
                                state.LeaveToExceptionEntryNode[instruction] =
                                    (entryNode, exceptionIdentifier);
                                // Exceptional control flow routes through the first of the set of
                                // associated catch handlers; this invocation pushes the catch handler's
                                // first instruction onto the stack and continues the translation from the
                                // handler's catch variable load node. 
                                CreateCatchHandlerEntryBlock(
                                    state,
                                    exnInfo.TryOffsetToCatchHandlers[instruction.Offset][0],
                                    entryNode,
                                    exceptionIdentifier);
                            }
                            else
                            {
                                (entryNode, _) =
                                    state.LeaveToExceptionEntryNode[instruction];
                            }
                            CreateExceptionalEdges(state, entryNode);
                        }

                        state.PushInstruction(target);
                    }
                    // Leave occurs within catch block.
                    else if (exnInfo.CatchOffsetToCatchHandler.ContainsKey(instruction.Offset))
                    {
                        var currentHandler = exnInfo.CatchOffsetToCatchHandler[instruction.Offset];

                        // Exceptional control flow routes through the finally block, if present,
                        // prior to routing control flow to leave.
                        if (currentHandler.FinallyBlock != null && 
                            state.NodesToLinkWithExceptionBlock.Count > 0)
                        {
                            var finallyEntryNode = CreateFinallyExceptionalEntryBlock(
                                state, currentHandler.FinallyBlock);
                            CreateExceptionalEdges(state, finallyEntryNode);
                        }

                        if (currentHandler.FinallyBlock != null)
                        {
                            state.PushInstruction(currentHandler.FinallyBlock.HandlerStart,
                                                  CreateFinallyHandlerNonExceptionalEntry(
                                                      state, currentHandler.FinallyBlock, target));
                        }
                        else
                        {
                            // Control flow routes directly to the target, as there is no finally
                            // block through which to first route it.
                            state.PushInstruction(target);
                        }
                    }
                    // Leave occurs within try of finally block (we leave this as the last option,
                    // as the try block of a finally encompasses all of the try-catch bytecode, in
                    // the case of try-catch-finally.
                    else 
                    {
                        var finallyHandler = exnInfo.TryOffsetToFinallyHandler[instruction.Offset];
                        if (state.NodesToLinkWithExceptionBlock.Count > 0)
                        {
                            var finallyEntryNode =
                                CreateFinallyExceptionalEntryBlock(state, finallyHandler);
                            CreateExceptionalEdges(state, finallyEntryNode);
                        }
                        state.PushInstruction(finallyHandler.HandlerStart,
                                              CreateFinallyHandlerNonExceptionalEntry
                                                  (state, finallyHandler, target));
                    }
                    return true;
                default:
                    return false;
            }            
        }

        /// <summary>
        /// Creates a new node to ensure finally instructions aren't attached to a body node.
        /// </summary>
        private static CfgNode CreateFinallyHandlerNonExceptionalEntry(ProgramState state,
                                                                       ExceptionHandler handler,
                                                                       Instruction leaveTarget)
        {
            CfgNode finallyHandlerStartNode = null;
            (var nodeOffset, _) = state.GetOffsetNode(handler.HandlerStart.Offset);
            if (nodeOffset == null)
            {
                finallyHandlerStartNode = new StatementNode(
                    location: GetHandlerStartLocation(state, handler),
                    kind: StatementNode.StatementNodeKind.MethodBody,
                    proc: state.ProcDesc);
                state.Cfg.RegisterNode(finallyHandlerStartNode);
                state.PreviousNode.Successors.Add(finallyHandlerStartNode);
                state.AppendToPreviousNode = true;
            }
            state.EndfinallyControlFlow = leaveTarget;
            return finallyHandlerStartNode;
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
