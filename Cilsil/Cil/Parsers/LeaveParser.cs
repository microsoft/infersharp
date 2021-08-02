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
                    // Leave occurs within try block.
                    if (exnInfo.TryOffsetToCatchHandlers.ContainsKey(instruction.Offset))
                    {
                        // Exceptional control flow routes through the first of the set of
                        // associated catch handlers.
                        var entryNode = CreateCatchEntryBlock(
                            state, exnInfo.TryOffsetToCatchHandlers[instruction.Offset][0]);
                        CreateExceptionalEdges(state, entryNode);
                        // Regular control flow routes through the finally block, if present, prior
                        // to routing control flow to leave.
                        if (exnInfo.TryOffsetToFinallyHandler.ContainsKey(instruction.Offset))
                        {
                            var finallyHandler =
                                exnInfo.TryOffsetToFinallyHandler[instruction.Offset];
                            state.PushInstruction(finallyHandler.HandlerStart, state.PreviousNode);
                        }
                    }
                    // Leave occurs within catch block, as leave can only occur in one of the two.
                    else
                    {
                        var currentHandler = exnInfo.CatchOffsetToCatchHandler[instruction.Offset];
                        // Exceptional control flow routes through the finally block, if present,
                        // prior to routing control flow to leave.
                        if (currentHandler.FinallyBlock != null)
                        {
                        }
                        if (currentHandler.NextCatchBlock != null)
                        {
                            var entryNode = 
                        }
                        // Last handler; 
                        else
                        {

                        }

                    }

                    return true;
                default:
                    return false;
            }            
        }

        private static CfgNode CreateCatchEntryBlock(ProgramState state, ExceptionHandlerNode handlerNode)
        {
            (var entryNode, var exceptionIdentifier) = CreateExceptionEntryNode(
                state, handlerNode.ExceptionHandler);
            (var trueBranch, var falseBranch) = CreateExceptionTypeCheckBranchNodes(
                state, handlerNode.ExceptionHandler, exceptionIdentifier);
            var loadCatchVarNode = GetHandlerCatchVarNode(
                state, handlerNode.ExceptionHandler);
            handlerNode.CatchHandlerLatestFalseEntryNode = falseBranch;
            entryNode.Successors.Add(trueBranch);
            entryNode.Successors.Add(falseBranch);
            trueBranch.Successors.Add(loadCatchVarNode);
            // The CIL specification dictates that the exception object is on top of
            // the stack when the catch handler is entered; the first instruction of
            // the catch handler will handle the object pushed onto the stack.
            state.PushExpr(new VarExpression(exceptionIdentifier),
                           new Tptr(Tptr.PtrKind.Pk_pointer,
                                    new Tstruct("System.Object")));
            state.PushInstruction(handlerNode.ExceptionHandler.HandlerStart,
                                  trueBranch);
            return entryNode;
        }

        private static CfgNode CreateFinallyExceptionalEntryBlock(ProgramState state, 
                                                                  ExceptionHandler handler)
        {
            (var entryNode, var exceptionIdentifier) = CreateExceptionEntryNode(state, handler);
            var finallyBranchNode = GetFinallyExceptionBranchNode(state, handler);
            var loadCatchVarNode = GetHandlerCatchVarNode(state, handler);
            entryNode.Successors.Add(finallyBranchNode);
            finallyBranchNode.Successors.Add(loadCatchVarNode);
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
