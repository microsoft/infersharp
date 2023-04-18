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

        private readonly Dictionary<string, MethodDefinition> MoveNextMethodCompleteNameToMatchedMethodDefinition;

        private HashSet<string> MatchedProperMethodNames;

        private int MoveNextMethodsNotMatched = 0;

        public CfgParserService(bool writeConsoleProgress,
                                IEnumerable<MethodDefinition> methods = null,
                                IEnumerable<TypeDefinition> types = null)
        {
            WriteConsoleProgress = writeConsoleProgress;
            Methods = methods;
            Types = types;
            MoveNextMethodCompleteNameToMatchedMethodDefinition = new Dictionary<string, MethodDefinition>();
        }
        private string ExtractClassAndName(string completeMethodName)
        {
            var removeReturnType = completeMethodName.Split(' ')[1];
            var removeArgs = removeReturnType.Split('(')[0];
            return removeArgs;
        }

        private string GetClassAndNameFromMoveNext(string completeMethodName)
        {
            var removeReturnType = completeMethodName.Split(' ')[1];
            var className = removeReturnType.Split('$')[0];
            var methodNameStart = completeMethodName.IndexOf('<') + 1;
            var methodNameEnd = completeMethodName.IndexOf('>');
            if (methodNameStart < 0 || methodNameEnd < 0)
            {
                return null;
            }
            var methodNameLength = methodNameEnd - methodNameStart;
            var methodName = completeMethodName.Substring(methodNameStart, methodNameLength);
            return className + "::" + methodName;
        }

        private void MatchMoveNextMethodsToProperName()
        {
            var moveNextMethodCompleteNames = new HashSet<string>();
            var methodClassAndNameToCompleteName = new Dictionary<string, string>();
            var completeNameToMethodDefinition = new Dictionary<string, MethodDefinition>();
            
            foreach (var method in Methods)
            {
                var methodName = method.GetCompatibleFullName();
                methodClassAndNameToCompleteName[ExtractClassAndName(methodName)] = methodName;
                completeNameToMethodDefinition[methodName] = method;

                if (methodName.Contains("MoveNext()"))
                {
                    moveNextMethodCompleteNames.Add(methodName);
                }
            }

            foreach (var moveNextMethodName in moveNextMethodCompleteNames)
            {
                var classAndName = GetClassAndNameFromMoveNext(moveNextMethodName);
                if (classAndName == null || 
                    !methodClassAndNameToCompleteName.TryGetValue(
                        GetClassAndNameFromMoveNext(moveNextMethodName),
                                                    out var completeName))
                {
                    MoveNextMethodsNotMatched++;
                }
                else
                {
                    MoveNextMethodCompleteNameToMatchedMethodDefinition[moveNextMethodName] =
                        completeNameToMethodDefinition[completeName];
                }
            }
            Log.WriteWarning("Total MoveNext methods: " + moveNextMethodCompleteNames.Count);
            MatchedProperMethodNames = new HashSet<string>(
                MoveNextMethodCompleteNameToMatchedMethodDefinition.Values.Select(
                    m => m.GetCompatibleFullName()));

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

            MatchMoveNextMethodsToProperName();

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
            Log.WriteWarning(
                "Number of MoveNext methods not matched: " + MoveNextMethodsNotMatched);
            Log.WriteWarning("Number of MoveNext methods matched: " +
                             MoveNextMethodCompleteNameToMatchedMethodDefinition.Count);
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

        private static (CfgNode, VarExpression) InitializeNodeWithLoadThis(ProgramState state)
        {
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

            return (newNode, thisValueExpression);
        }

        private static void AddDefaultInstanceFieldInitializationInstruction(
            CfgNode node, 
            VarExpression thisValueExpression, 
            FieldDefinition field, 
            Expression defaultValue,
            ProgramState state)
        {
            var fieldExpression = InstructionParser.CreateFieldExpression(
                thisValueExpression, field);

            var fieldStore = new Store(
                fieldExpression,
                defaultValue,
                Typ.FromTypeReferenceNoPointer(field.DeclaringType),
                state.CurrentLocation);
            node.Instructions.Add(fieldStore);
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
            (var newNode, var thisValueExpression) = InitializeNodeWithLoadThis(state);
            var objectFields = state.Method.DeclaringType.Fields;

            foreach (var field in objectFields)
            {
                if (field.FieldType.FullName == "System.Boolean" && !field.IsStatic)
                {
                    var falseBoolean = new ConstExpression(new IntRepresentation(0, false, false));
                    AddDefaultInstanceFieldInitializationInstruction(
                        newNode, thisValueExpression, field, falseBoolean, state);
                }
            }
            return newNode;
        }

        private CfgNode InitializeAsyncStateFields(ProgramState state)
        {
            (var newNode, var thisValueExpression) = InitializeNodeWithLoadThis(state);
            var objectFields = state.Method.DeclaringType.Fields;
            foreach (var field in objectFields)
            {
                if (field.FullName.Contains("__state"))
                {
                    var initialState = 
                        new ConstExpression(new IntRepresentation(-1, false, false));
                    AddDefaultInstanceFieldInitializationInstruction(
                        newNode, thisValueExpression, field, initialState, state);
                }
                // Fields storing the input parameters of the method (async methods have their
                // parameters represented as fields) shouldn't have either '<' or '>'.
                else if (!(field.Name.Contains('<') || field.Name.Contains('>')))
                {
                    var localVariableExpression = 
                        new LvarExpression(
                            new LocalVariable(field.Name, state.MethodDefinitionToUpdate));
                    var localVariableType = Typ.FromTypeReference(field.FieldType);
                    var variableLoadIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
                    var variableLoadExpression = new VarExpression(variableLoadIdentifier);
                    var variableLoad = new Load(
                        variableLoadIdentifier, 
                        localVariableExpression, 
                        localVariableType, 
                        state.CurrentLocation);
                    newNode.Instructions.Add(variableLoad);
                    AddDefaultInstanceFieldInitializationInstruction(
                        newNode, thisValueExpression, field, variableLoadExpression, state);
                }
            }
            return newNode;
        }

        private void ComputeMethodCfg(MethodDefinition method)
        {
            var methodName = method.GetCompatibleFullName();

            if (MatchedProperMethodNames.Contains(methodName))
            {
                return;
            }
            try 
            {
                if (Cfg.Procs.ContainsKey(methodName))
                {
                    Log.WriteWarning($"Method with duplicate full name found: {methodName}");
                    return;

                }
            } 
            catch (NotImplementedException e)
            {
                Log.WriteWarning($"Skipping method {method.GetCompatibleFullName()}: {e.Message}");
                return;
            }

            var programState = new ProgramState(method, Cfg);

            if (MoveNextMethodCompleteNameToMatchedMethodDefinition.ContainsKey(methodName))
            {
                var matchedMethod =
                    MoveNextMethodCompleteNameToMatchedMethodDefinition[methodName];
                programState.MethodDefinitionToUpdate = matchedMethod;
            }
            
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
                    // There are no async constructors in .NET, so the cases won't overlap. Note
                    // that the MethodDefinitionToUpdate will be null when we couldn't successfully
                    // match the MoveNext method signature to its original.
                    else if (programState.IsMoveNextAsyncMethod() && 
                             programState.MethodDefinitionToUpdate != null)
                    {
                        initNode = InitializeAsyncStateFields(programState);
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
                            Log.WriteWarning("Translation timeout on " + methodName);
                            Log.RecordUnfinishedMethod(programState.Method.GetCompatibleFullName(),
                                                       nextInstruction.RemainingInstructionCount());
                            translationUnfinished = true;
                            break;
                        }
                        else if (iterationCount > 100000)
                        {
                            TimeoutMethodCount++;
                            Log.WriteWarning("Translation timeout on " + methodName);
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

                // Exception node for start and exception sink should be exit, exception node for
                // exit should be empty.
                programState.ProcDesc.StartNode.ExceptionNodes.Clear();
                programState.ProcDesc.ExitNode.ExceptionNodes.Clear();
                programState.ProcDesc.ExceptionSinkNode.ExceptionNodes.Clear();
                programState.ProcDesc.StartNode.ExceptionNodes.Add(programState.ProcDesc.ExitNode);
                programState.ProcDesc.ExceptionSinkNode.ExceptionNodes.Add(
                    programState.ProcDesc.ExitNode);

                SetNodePredecessors(programState);
                if (MoveNextMethodCompleteNameToMatchedMethodDefinition.ContainsKey(methodName))
                {
                    var matchedMethod =
                        MoveNextMethodCompleteNameToMatchedMethodDefinition[methodName];
                    programState.ProcDesc.UpdateMethodDefinitionForAsync(matchedMethod);
                    // This occurs in the rare case that there is a duplicate on matched MoveNext
                    // methods.
                    if (Cfg.Procs.ContainsKey(matchedMethod.GetCompatibleFullName()))
                    {
                        Cfg.Procs.Add(methodName, programState.ProcDesc);
                    }
                    else
                    {
                        Cfg.Procs.Add(matchedMethod.GetCompatibleFullName(), programState.ProcDesc);
                    }
                }
                else
                {
                    Cfg.Procs.Add(methodName, programState.ProcDesc);
                }
                
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
