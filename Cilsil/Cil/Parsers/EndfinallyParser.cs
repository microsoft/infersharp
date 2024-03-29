﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class EndfinallyParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // Throw can end a finally block but it is discouraged by the compiler and should
                // be rare. 
                case Code.Endfinally:
                    var exceptionHandler = state.MethodExceptionHandlers
                                                .GetExceptionHandlerAtInstruction(instruction);
                    // This instruction was reached through non-exceptional control flow.
                    if (!state.FinallyExceptionalTranslation)
                    {

                        // In this case, translation of the finally block was prompted by a
                        // throw instruction; we then terminate the CFG branch with the throw
                        // node via RegisterNode.
                        if (state.EndfinallyThrowNode != null)
                        {
                            RegisterNode(state, state.EndfinallyThrowNode);
                            state.EndfinallyThrowNode = null;
                        }
                        else
                        {
                            // We continue translation with that operand from the end of
                            // the finally block, now that finally block has been
                            // translated. Routing through the next finally block, if there is one
                            // enclosing this block, will get handled at the leave instruction but
                            // if there is a different handler applied to this instruction, we must
                            // route there first.
                            if (state.MethodExceptionHandlers
                                     .TryOffsetToFinallyHandler
                                     .ContainsKey(instruction.Offset))
                            {
                                HandleFinallyControlFlowForHandlerTransition(
                                    state, instruction, state.EndfinallyControlFlow);
                            }
                            else
                            {
                                state.PushInstruction(state.EndfinallyControlFlow);
                            }
                        }
                    }
                    // This instruction was reached through exceptional control flow.
                    else
                    {
                        var handler = state.MethodExceptionHandlers.FinallyEndToHandler[instruction];
                        if (!state.FinallyHandlerToExceptionExit.ContainsKey(handler))
                        {
                            var exceptionExitNode = CreateFinallyExceptionExitNode(state, handler);
                            state.FinallyHandlerToExceptionExit[handler] = exceptionExitNode;

                            // We route control flow through the next finally handler, if there is
                            // one.
                            if (exceptionHandler != null &&
                                exceptionHandler.HandlerType == ExceptionHandlerType.Finally)
                            {
                                var finallyBranchNode = 
                                    CreateFinallyExceptionBranchNode(state, handler);
                                exceptionExitNode.Successors.Add(finallyBranchNode);
                                state.PushInstruction(exceptionHandler.HandlerStart,
                                                      finallyBranchNode);
                            }
                        }
                        else
                        {
                            state.PreviousNode.Successors.Add(
                                state.FinallyHandlerToExceptionExit[handler]);
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
