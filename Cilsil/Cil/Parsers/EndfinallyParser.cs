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
                    var handler = state.MethodExceptionHandlers.FinallyEndToHandler[instruction];
                    // In this case, we have reached endfinally of the finally handler during
                    // the first translation of exceptional control flow of the handler.
                    if (!state.FinallyHandlerToExceptionExit.ContainsKey(handler) &&
                        state.ExceptionHandlerToCatchVarNode.ContainsKey(handler))
                    {
                        (_, var syntheticExceptionVariable) = 
                            GetHandlerCatchVarNode(state, handler);
                        var exceptionIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                        var exceptionType = new Tptr(Tptr.PtrKind.Pk_pointer, 
                                                     new Tstruct("System.Object"));
                        var catchVarLoad = new Load(exceptionIdentifier,
                                                    syntheticExceptionVariable,
                                                    exceptionType,
                                                    state.CurrentLocation);

                        var exceptionReturnNode = CreateExceptionReturnNode(
                            state, 
                            new VarExpression(exceptionIdentifier), 
                            GetHandlerEndLocation(state, handler));
                        exceptionReturnNode.Instructions.Insert(0, catchVarLoad);
                        state.Cfg.RegisterNode(exceptionReturnNode);
                        state.PreviousNode.Successors.Add(exceptionReturnNode);
                        state.FinallyHandlerToExceptionExit[handler] = exceptionReturnNode;
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
