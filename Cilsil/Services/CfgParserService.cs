// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Cil.Parsers;
using Cilsil.Extensions;
using Cilsil.Services.Results;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
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

        public bool WriteConsoleProgress { get; private set; }

        private Cfg Cfg;

        public CfgParserService(bool writeConsoleProgress,
                                IEnumerable<MethodDefinition> methods = null,
                                IEnumerable<TypeDefinition> types = null)
        {
            WriteConsoleProgress = writeConsoleProgress;
            Methods = methods;
            Types = types;
        }

        public ServiceExecutionResult Execute()
        {
            Log.WriteLine("Translation stage 3/3: Computing control-flow graph.");
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

            var i = 0;
            var total = Methods.Count();
            Cfg = new Cfg();
            using (var bar = new ProgressBar())
            {
                foreach (var method in Methods)
                {
                    ComputeMethodCfg(method);
                    i++;
                    bar.Report((double)i / total);
                    if (WriteConsoleProgress)
                    {
                        Log.WriteProgressLine(i, total);
                    }
                }
            }
            Log.WriteWarning("Timed out methods: " + TimeoutMethodCount);
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

        /// <summary>
        /// This method should be invoked before translation of a program begins, if applicable. 
        /// It is intended for the default initialization of boolean fields to false in the 
        /// construction of an object, in the absence of there being IL that actually performs this
        /// (this can happen for uninitialized boolean fields). The purpose of adding this is to
        /// combat false positive resource leaks that can occur when users declare IDisposable 
        /// objects with a boolean field indicating whether the object has already been disposed.
        /// </summary>
        /// <param name="state">The program state.</param>
        /// <returns>The node containing the instructions for initializing the boolean fields to 
        /// their default initializations.</returns>
        private CfgNode InitializeInstanceBooleanFields(ProgramState state)
        {
            var objectFields = state.Method.DeclaringType.Fields;
            var newNode = new StatementNode(location: state.CurrentLocation,
                                            kind: StatementNode.StatementNodeKind.MethodBody,
                                            proc: state.ProcDesc);
            state.Cfg.RegisterNode(newNode);
            state.PreviousNode.Successors.Add(newNode);
            newNode.BlockEndOffset = MethodExceptionHandlers.DefaultHandlerEndOffset;

            var thisExpr = new LvarExpression(new LocalVariable("this", state.Method));
            var thisType = Typ.FromTypeReference(state.Method.DeclaringType);
            var thisValueIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var thisValueExpression = new VarExpression(thisValueIdentifier, true);

            newNode.Instructions.Add(
                new Load(thisValueIdentifier, thisExpr, thisType, state.CurrentLocation));

            foreach (var field in objectFields)
            {
                if (field.FieldType.FullName == "System.Boolean" && !field.IsStatic)
                {
                    var falseBoolean = new ConstExpression(new IntRepresentation(0, false, false));
                    var fieldExpression = InstructionParser.CreateFieldExpression(
                        thisValueExpression, field);

                    var fieldStore = new Store(
                        fieldExpression, 
                        falseBoolean, 
                        Typ.FromTypeReferenceNoPointer(field.DeclaringType), 
                        state.CurrentLocation);
                    newNode.Instructions.Add(fieldStore);
                }
            }
            return newNode;
        }

        private void ComputeMethodCfg(MethodDefinition method)
        {
            string methodName;
            try
            {
                methodName = method.GetCompatibleFullName();
                if (Cfg.Procs.ContainsKey(methodName))
                {
                    Log.WriteWarning($"Method with duplicate full name found: {methodName}");
                    return;
                }
                if (method.DebugInformation.SequencePoints.FirstOrDefault() == null)
                {
                    Log.WriteWarning($"Skipping method not found in source code: {methodName}");
                    return;
                }
            } 
            catch (NotImplementedException e)
            {
                Log.WriteWarning($"Skipping method {method.GetCompatibleFullName()}: {e.Message}");
                return;
            }
            catch (NotSupportedException e)
            {
                Log.WriteWarning($"Skipping method {method.GetCompatibleFullName()}: {e.Message}");
                return;
            }

            var programState = new ProgramState(method, Cfg);

            var methodBody = method.Body;
            var unhandledExceptionCase =
                programState.MethodExceptionHandlers.UnhandledExceptionBlock;

            // True if the translation terminates early, false otherwise.
            var translationUnfinished = false;
            int iterationCount = 0;

            if (!method.IsAbstract && methodBody.Instructions.Count > 0)
            {
                try
                {
                    CfgNode initNode = null;
                    // We trigger the special inlining of instructions for the constructors of
                    // objects with Boolean fields. Note this logic will end up adding a node with
                    // only a Load on "this" if the boolean fields are only static, but that should
                    // be a side effect which is both rare and unproblematic. 
                    if (methodName.Contains(".ctor") && 
                        method.DeclaringType.Fields.Select(
                            p => p.FieldType.FullName).Contains("System.Boolean"))
                    {
                        initNode = InitializeInstanceBooleanFields(programState);
                    }

                    programState.PushInstruction(methodBody.Instructions.First(), initNode);
                    do
                    {
                        if (programState.IsTopSnapshotInstructionNull())
                        {
                            Log.WriteWarning("Top instruction is null; terminating translation.");
                            Log.RecordUnfinishedMethod(method.GetCompatibleFullName(),
                                                       method.Body.Instructions.Count);
                            translationUnfinished = true;
                            break;
                        }
                        iterationCount++;
                        var nextInstruction = programState.PopInstruction();
                        // Checks if there is a node for the offset that we can reuse.
                        (var nodeAtOffset, var excessiveVisits) =
                            programState.GetOffsetNode(
                                nextInstruction.Offset,
                                programState.PreviousNode?.BlockEndOffset ??
                                MethodExceptionHandlers.DefaultHandlerEndOffset);
                        // We don't reuse nodes of finally handlers.
                        if (nodeAtOffset != null &&
                            programState.MethodExceptionHandlers
                                        .GetMapTypeFromInstruction(nextInstruction)
                                != MethodExceptionHandlers.MapType.CatchToCatch)
                        {
                            programState.PreviousNode.Successors.Add(nodeAtOffset);
                        }
                        else if (unhandledExceptionCase)
                        {
                            Log.WriteWarning($"Unhandled exception-handling.");
                           Log.RecordUnknownInstruction("unhandled-exception");
                            Log.RecordUnfinishedMethod(programState.Method.GetCompatibleFullName(),
                                                       nextInstruction.RemainingInstructionCount());
                            translationUnfinished = true;
                            break;
                        }
                        else if (excessiveVisits)
                        {
                            TimeoutMethodCount++;
                            Log.WriteWarning("Translation timeout.");
                            Log.RecordUnfinishedMethod(programState.Method.GetCompatibleFullName(),
                                                       nextInstruction.RemainingInstructionCount());
                            translationUnfinished = true;
                            break;
                        }
                        else if (iterationCount > 100000)
                        {
                            TimeoutMethodCount++;
                            Log.WriteWarning("Translation timeout.");
                            Log.RecordUnfinishedMethod(programState.Method.GetCompatibleFullName(),
                                                       nextInstruction.RemainingInstructionCount());
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
                catch (Exception e)
                {
                    translationUnfinished = true;
                    Log.WriteWarning(e.Message);
                    Log.RecordUnfinishedMethod(method.GetCompatibleFullName(),
                                               method.Body.Instructions.Count);

                }
            }

            // We add method to cfg only if its translation is finished. Otherwise, we skip that
            // method.
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
            return (method.Name.Equals("Dispose") ||
                    method.Name.Equals("System.IDisposable.Dispose")) &&
                        method.ReturnType.FullName.Equals("System.Void");
        }
    }
}
