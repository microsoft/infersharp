// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Cil.Parsers;
using Cilsil.Extensions;
using Cilsil.Services.Results;
using Cilsil.Sil;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Services
{
    internal class CfgParserService : IService
    {
        private int TimeoutMethodCount = 0;

        public IEnumerable<MethodDefinition> Methods { get; private set; }

        public IEnumerable<TypeDefinition> Types { get; private set; }

        private Cfg Cfg;

        public CfgParserService(IEnumerable<MethodDefinition> methods = null,
                                IEnumerable<TypeDefinition> types = null)
        {
            Methods = methods;
            Types = types;
        }

        public ServiceExecutionResult Execute()
        {
            if (Methods == null)
            {
                if (Types == null)
                {
                    throw new ServiceExecutionException(
                        $"{nameof(CfgParserService)} must be executed with inputs. Input can " +
                         "be passed via constructor or by executing the service after " +
                        nameof(DecompilationService));
                }
                else
                {
                    Methods = Types.SelectMany(t => t.Methods).Where(m => m.HasBody);
                }
            }

            Cfg = new Cfg();
            foreach (var method in Methods)
            {
                System.Diagnostics.Stopwatch watch = null;
                if (Log.Debug)
                {
                    watch = System.Diagnostics.Stopwatch.StartNew();
                }

                bool success = ComputeMethodCfg(method);

                if (success && Log.Debug)
                {
                    watch.Stop();

                    Log.RecordMethodElapseTime(method, watch.ElapsedMilliseconds);
                }
            }
            Log.WriteError("Timed out methods: " + TimeoutMethodCount);
            return new CfgParserResult(Cfg, Methods);
        }

        public ServiceExecutionResult ExecuteFromResult(ServiceExecutionResult result)
        {
            if (result.GetResult<DecompilationResult>() is DecompilationResult decompilationResult)
            {
                Methods = decompilationResult
                            .TypesWithSymbols
                            .SelectMany(t => t.Methods)
                            .Where(m => m.HasBody);
            }
            else
            {
                throw new ServiceExecutionException(
                    $"{nameof(CfgParserService)} must be run after " +
                    nameof(DecompilationService));
            }

            return Execute();
        }

        private bool ComputeMethodCfg(MethodDefinition method)
        {
            var methodName = method.GetCompatibleFullName();
            if (Cfg.Procs.ContainsKey(methodName))
            {
                Log.WriteWarning($"Method with duplicate full name found: {methodName }");
                return false;
            }

            var programState = new ProgramState(method, Cfg);

            var methodBody = method.Body;

            // True if the translation terminates early, false otherwise.
            var translationUnfinished = false;
            foreach (var exceptionHandler in methodBody.ExceptionHandlers)
            {
                var exceptionHandlingBlock = exceptionHandler;
                var exceptionHandlingBlockStartOffset = -1;
                var exceptionHandlingBlockEndOffset = -1;
                TypeReference catchType = null;
                try
                {
                    switch (exceptionHandlingBlock.HandlerType)
                    {
                        case ExceptionHandlerType.Catch:
                            catchType = exceptionHandlingBlock.CatchType;
                            exceptionHandlingBlockStartOffset = exceptionHandlingBlock.HandlerStart.Offset;
                            exceptionHandlingBlockEndOffset = exceptionHandlingBlock.HandlerEnd.Offset;
                            break;

                        case ExceptionHandlerType.Finally:
                            exceptionHandlingBlockStartOffset = exceptionHandlingBlock.HandlerStart.Offset;
                            exceptionHandlingBlockEndOffset = exceptionHandlingBlock.HandlerEnd.Offset;
                            break;

                        case ExceptionHandlerType.Filter:
                            // Example: catch (ArgumentException e) when (e.ParamName == "…")   
                            // Adds associated try block node offsets to a hashset.
                            catchType = programState.Method.Module.Import(typeof(System.Exception));
                            exceptionHandlingBlockStartOffset = exceptionHandlingBlock.FilterStart.Offset;
                            exceptionHandlingBlockEndOffset = exceptionHandlingBlock.HandlerEnd.Offset;
                            break;

                        case ExceptionHandlerType.Fault:
                        // uncommon case: fault block
                        // Example: fault {}
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.WriteWarning($"Exception on processing exception handling blocks: {e.Message}.");
                    continue;
                }
                if (exceptionHandlingBlockStartOffset != -1)
                {
                    programState.ExceptionBlockStartToEndOffsets.Add(exceptionHandlingBlockStartOffset, exceptionHandlingBlockEndOffset);
                    if (catchType != null)
                    {
                        programState.OffsetToExceptionType.Add(exceptionHandlingBlockStartOffset, catchType);
                    }
                }
            }

            if (!method.IsAbstract && methodBody.Instructions.Count > 0)
            {
                (programState, translationUnfinished) =
                    ParseInstructions(methodBody.Instructions.FirstOrDefault(),
                                      programState,
                                      translationUnfinished);
            }
            // We add method to cfg only if its translation is finished. Otherwise, we skip that method.
            if (translationUnfinished && !IsDisposeFunction(method))
            {
                // Deregisters resources of skipped method.
                programState.ProcDesc.DeregisterResources(Cfg);
                return false;
            }
            else
            {
                // Sets exception sink node as default exception node for all nodes in the graph.
                foreach (var node in programState.ProcDesc.Nodes)
                {

                    if (node.ExceptionNodes.Count == 0)
                    {
                        node.ExceptionNodes.Add(programState.ProcDesc.ExceptionSinkNode);
                    }
                    if (node.Successors.Count == 0 && node != programState.ProcDesc.ExitNode)
                    {
                        node.Successors.Add(programState.ProcDesc.ExitNode);
                    }
                    else if (node.Successors.Count > 1 && node.Successors.Contains(programState.ProcDesc.ExitNode))
                    {
                        node.Successors.Remove(programState.ProcDesc.ExitNode);
                    }
                }

                // Exception node for start and exception sink should be exit, exception node for exit 
                // should be empty.
                programState.ProcDesc.StartNode.ExceptionNodes.Clear();
                programState.ProcDesc.ExitNode.ExceptionNodes.Clear();
                programState.ProcDesc.ExceptionSinkNode.ExceptionNodes.Clear();
                programState.ProcDesc.StartNode.ExceptionNodes.Add(programState.ProcDesc.ExitNode);
                programState.ProcDesc.ExceptionSinkNode.ExceptionNodes.Add(
                    programState.ProcDesc.ExitNode);

                SetNodePredecessors(programState);

                Cfg.Procs.Add(methodName, programState.ProcDesc);
                return true;
            }
        }

        private (ProgramState, bool) ParseInstructions(Instruction instruction,
                                                       ProgramState state,
                                                       bool translationUnfinished)
        {
            state.PushInstruction(instruction);
            do
            {
                System.Diagnostics.Stopwatch watch = null;
                if (Log.Debug)
                {
                    watch = System.Diagnostics.Stopwatch.StartNew();
                }

                (var nextInstruction, _) = state.PopInstruction();

                var inExceptionHandler = state.ExceptionBlockStartToEndOffsets.ContainsKey(nextInstruction.Offset);

                (var nodeAtOffset, var excessiveVisits) =
                    state.GetOffsetNode(nextInstruction.Offset);

                if (nodeAtOffset != null)
                {
                    state.PreviousNode.Successors.Add(nodeAtOffset);
                }
                else if (excessiveVisits)
                {
                    TimeoutMethodCount++;
                    Log.WriteError("Translation timeout.");
                    Log.RecordUnfinishedMethod(state.Method.GetCompatibleFullName(),
                                               nextInstruction.RemainingInstructionCount());
                    translationUnfinished = true;
                    break;
                }
                else if (inExceptionHandler &&
                         !InstructionParser.ParseExceptionCilInstruction(nextInstruction, state) &&
                         !InstructionParser.ParseCilInstruction(nextInstruction, state))
                {
                    Log.RecordUnfinishedMethod(state.Method.GetCompatibleFullName(),
                                               nextInstruction.RemainingInstructionCount());
                    translationUnfinished = true;
                    break;
                }
                else if (!inExceptionHandler &&
                         !InstructionParser.ParseCilInstruction(nextInstruction, state))
                {
                    Log.RecordUnfinishedMethod(state.Method.GetCompatibleFullName(),
                                               nextInstruction.RemainingInstructionCount());
                    translationUnfinished = true;
                    break;
                }

                if (Log.Debug)
                {
                    watch.Stop();

                    Log.RecordInstructionCountAndElapseTime(state.Method, nextInstruction, watch.Elapsed.TotalMilliseconds * 1000000);
                }
            } while (state.HasInstruction);

            return (state, translationUnfinished);
        }

        private void SetNodePredecessors(ProgramState programState)
        {
            var done = new HashSet<CfgNode>();
            var todo = new Queue<CfgNode>();
            todo.Enqueue(programState.ProcDesc.StartNode);
            while (todo.Count > 0)
            {
                var n = todo.Dequeue();
                if (done.Add(n))
                {
                    foreach (var s in n.Successors)
                    {
                        s.Predecessors.Add(n);
                        todo.Enqueue(s);
                    }
                    foreach (var s in n.ExceptionNodes)
                    {
                        if (s != programState.ProcDesc.ExceptionSinkNode)
                        {
                            todo.Enqueue(s);
                        }

                    }
                }
            }
            if (!programState.ProcDesc.ExceptionSinkNode.Successors.Contains(
                 programState.ProcDesc.ExitNode))
            {
                programState.ProcDesc.ExceptionSinkNode.Successors.Add(programState.ProcDesc.ExitNode);
            }
            programState.ProcDesc.ExitNode.Predecessors.Add(programState.ProcDesc.ExceptionSinkNode);
        }

        /// <summary>
        /// Checks if the target method is a Dispose function.
        /// </summary>
        /// <param name="method">Target method to be checked.</param>
        private bool IsDisposeFunction(MethodDefinition method)
        {
            return (method.Name.Equals("Dispose") || method.Name.Equals("System.IDisposable.Dispose"))
                && method.ReturnType.FullName.Equals("System.Void");
        }
    }
}
