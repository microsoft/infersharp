// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
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
                    // This instruction was reached through non-exceptional control flow.
                    if (state.EndfinallyControlFlow != null)
                    {
                        // We continue translation with that operand from the end of the finally
                        // block, now that finally block has been translated.
                        state.PushInstruction(state.EndfinallyControlFlow);
                        state.EndfinallyControlFlow = null;
                    }
                    // This instruction was reached through exceptional control flow.
                    else 
                    {
                        var handler = 
                            state.MethodExceptionHandlers.FinallyEndToHandler[instruction];
                        if (!state.FinallyHandlerToExceptionExit.ContainsKey(handler))
                        {
                            state.FinallyHandlerToExceptionExit[handler] =
                                CreateFinallyExceptionExitNode(state, handler);
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
