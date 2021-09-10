// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Cil.Parsers;
using Cilsil.Extensions;
using Cilsil.Services.Results;
using Cilsil.Sil;
using Cilsil.Utils;
using Mono.Cecil;
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
                ComputeMethodCfg(method);
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

        private void ComputeMethodCfg(MethodDefinition method)
        {
            var methodName = method.GetCompatibleFullName();
            if (Cfg.Procs.ContainsKey(methodName))
            {
                Log.WriteWarning($"Method with duplicate full name found: {methodName }");
                return;
            }

            var programState = new ProgramState(method, Cfg);

            var methodBody = method.Body;

            // True if the translation terminates early, false otherwise.
            var translationUnfinished = false;

            if (!method.IsAbstract && methodBody.Instructions.Count > 0 &&
                !programState.MethodExceptionHandlers.UnhandledExceptionBlock &&
                programState.MethodExceptionHandlers.NoNestedTryCatchFinally() &&
                programState.MethodExceptionHandlers.NoFinallyEndWithThrow())
            {
                programState.PushInstruction(methodBody.Instructions.First());
                do
                {
                    var nextInstruction = programState.PopInstruction();
                    // Checks if there is a node for the offset that we can reuse.
                    (var nodeAtOffset, var excessiveVisits) =
                        programState.GetOffsetNode(
                            nextInstruction.Offset, 
                            programState.PreviousNode?.BlockEndOffset ?? 
                            MethodExceptionHandlers.DefaultHandlerEndOffset);
                    // We don't reuse nodes of finally handlers.
                    if (nodeAtOffset != null && 
                        !programState.MethodExceptionHandlers
                                     .FinallyOffsetToFinallyHandler
                                     .ContainsKey(nextInstruction.Offset) &&
                        !programState.MethodExceptionHandlers
                                     .CatchOffsetToCatchHandler
                                     .ContainsKey(nextInstruction.Offset))
                    {
                        programState.PreviousNode.Successors.Add(nodeAtOffset);
                    }
                    else if (excessiveVisits)
                    {
                        TimeoutMethodCount++;
                        Log.WriteError("Translation timeout.");
                        Log.RecordUnfinishedMethod(programState.Method.GetCompatibleFullName(),
                                                   nextInstruction.RemainingInstructionCount());
                        translationUnfinished = true;
                        break;
                    }
                    else if (!InstructionParser.ParseCilInstruction(nextInstruction, programState))
                    {
                        Log.RecordUnfinishedMethod(programState.Method.GetCompatibleFullName(),
                                                   nextInstruction.RemainingInstructionCount());
                        translationUnfinished = true;
                        break;
                    }
                } while (programState.HasInstruction);
            }

            // We add method to cfg only if its translation is finished. Otherwise, we skip that method.
            if (translationUnfinished && !IsDisposeFunction(method))
            {
                // Deregisters resources of skipped method.
                programState.ProcDesc.DeregisterResources(Cfg);
            }
            else
            {
                // Sets exception sink node as default exception node for all nodes in the graph.
                foreach (var node in programState.ProcDesc.Nodes)
                {
                    // Nodes linked with exception handlers should not be linked with the sink, and
                    // will already have the corresponding nodes linked.
                    if (node.ExceptionNodes.Count == 0)
                    {
                        node.ExceptionNodes.Add(programState.ProcDesc.ExceptionSinkNode);
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
            }
        }

        private static void SetNodePredecessors(ProgramState programState)
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
                    if (n.ExceptionNodes.Count > 0 && 
                        n.ExceptionNodes[0] != programState.ProcDesc.ExceptionSinkNode)
                    {
                        todo.Enqueue(n.ExceptionNodes[0]);
                    }
                }
            }
            programState.ProcDesc.ExceptionSinkNode.Successors.Add(programState.ProcDesc.ExitNode);
            programState.ProcDesc.ExitNode.Predecessors.Add(
                programState.ProcDesc.ExceptionSinkNode);
        }

        /// <summary>
        /// Checks if the target method is a Dispose function.
        /// </summary>
        /// <param name="method">Target method to be checked.</param>
        private static bool IsDisposeFunction(MethodDefinition method)
        {
            return (method.Name.Equals("Dispose") || method.Name.Equals("System.IDisposable.Dispose"))
                && method.ReturnType.FullName.Equals("System.Void");
        }
    }
}
