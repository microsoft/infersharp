// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Utils;
using Mono.Cecil.Cil;

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
                case Code.Throw:
                    var exnInfo = state.MethodExceptionHandlers;
                    var mapType = exnInfo.GetMapTypeFromInstruction(instruction);
                    // This is null if the instruction is throw.
                    var target = instruction.Operand as Instruction;

                    if (instruction.OpCode.Code == Code.Throw)
                    {
                        (var returnValue, _) = state.Pop();
                        var retNode = CreateExceptionReturnNode(state,
                                                                returnValue,
                                                                state.CurrentLocation);
                        // Before control flow leaves this block via the throw, we need to route
                        // control flow through the finally block.
                        if (exnInfo.TryOffsetToFinallyHandler.ContainsKey(instruction.Offset))
                        {
                            var finallyHandler =
                                exnInfo.TryOffsetToFinallyHandler[instruction.Offset].Item1;
                            state.PushInstruction(finallyHandler.HandlerStart,
                                                  CreateFinallyHandlerNonExceptionalEntry(
                                                      state, finallyHandler, null, retNode));
                        }
                        else
                        {
                            RegisterNode(state, retNode);
                        }
                        return true;
                    }

                    // Leave within try of catch-block.
                    if (mapType == MethodExceptionHandlers.MapType.TryToCatch)
                    {
                        if (target != null)
                        {
                            exnInfo.TryOffsetToFinallyHandler.TryGetValue(
                                instruction.Offset, out var currentFinallyHandler);
                            exnInfo.TryOffsetToFinallyHandler.TryGetValue(
                                target.Offset, out var targetFinallyHandler);
                            // If target is associated with a different finally handler than is
                            // the present instruction, then we need to route control flow through
                            // this finally block first.
                            if (currentFinallyHandler.Item1?.HandlerStart?.Offset != 
                                targetFinallyHandler.Item1?.HandlerStart?.Offset)
                            {
                                state.PushInstruction(
                                    currentFinallyHandler.Item1.HandlerStart,
                                    CreateFinallyHandlerNonExceptionalEntry(
                                        state, currentFinallyHandler.Item1, target));
                            }
                            else
                            {
                                state.PushInstruction(target);
                            }
                        }
                    }
                    // Leave occurs within catch block.
                    else if (mapType == MethodExceptionHandlers.MapType.CatchToCatch)
                    {
                        var exceptionHandler = 
                            exnInfo.GetExceptionHandlerAtInstruction(instruction);

                        // We now handle regular control flow to the target. 
                        if (target != null)
                        {
                            if (exceptionHandler != null &&
                                exceptionHandler.HandlerType == ExceptionHandlerType.Finally)
                            {
                                state.PushInstruction(
                                    exceptionHandler.HandlerStart,
                                    CreateFinallyHandlerNonExceptionalEntry(
                                        state, exceptionHandler, target));
                            }
                            else
                            {
                                // Control flow routes directly to the target, as there is no finally
                                // block through which to first route it.
                                state.PushInstruction(target);
                            }
                        }
                    }
                    // Narrowest containing interval for offset is the try of a finally block,
                    // since leave can't occur within a finally handler.
                    else if (mapType == MethodExceptionHandlers.MapType.TryToFinally)
                    {
                        var finallyHandler = 
                            exnInfo.TryOffsetToFinallyHandler[instruction.Offset].Item1;
                        if (target != null)
                        {
                            state.PushInstruction(finallyHandler.HandlerStart,
                                                  CreateFinallyHandlerNonExceptionalEntry
                                                      (state, finallyHandler, target));
                        }
                    }
                    return true;
                case Code.Rethrow:
                    var exceptionType = 
                        state.MethodExceptionHandlers
                             .CatchOffsetToCatchHandler[instruction.Offset]
                             .Item1
                             .ExceptionHandler
                             .CatchType;
                    (var memoryAllocationCall, var objectVariable) = CreateMemoryAllocationCall(
                        exceptionType, state);
                    state.PreviousNode.Instructions.Add(memoryAllocationCall);
                    var rethrowNode = CreateExceptionReturnNode(state,
                                        objectVariable,
                                        state.CurrentLocation);
                    // Before control flow leaves this block via the throw, we need to route
                    // control flow through the finally block.
                    if (state.MethodExceptionHandlers
                             .TryOffsetToFinallyHandler
                             .ContainsKey(instruction.Offset))
                    {
                        var finallyHandler =
                            state.MethodExceptionHandlers
                                 .TryOffsetToFinallyHandler[instruction.Offset]
                                 .Item1;
                        state.PushInstruction(finallyHandler.HandlerStart,
                                              CreateFinallyHandlerNonExceptionalEntry(
                                                  state, finallyHandler, null, rethrowNode));
                    }
                    else
                    {
                        RegisterNode(state, rethrowNode);
                    }
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a new node to ensure finally instructions aren't attached to a body node.
        /// </summary>
        private static CfgNode CreateFinallyHandlerNonExceptionalEntry(
            ProgramState state, ExceptionHandler handler, 
            Instruction leaveTarget, CfgNode endFinallyThrowNode = null)
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
            if (leaveTarget != null)
            {
                state.EndfinallyControlFlow = leaveTarget;
            }
            if (endFinallyThrowNode != null)
            {
                state.EndfinallyThrowNode = endFinallyThrowNode;
            }
            return finallyHandlerStartNode;
        }

    }
}
