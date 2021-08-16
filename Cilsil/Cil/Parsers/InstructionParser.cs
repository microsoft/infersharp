// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
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
using System.Reflection;
using ProcStack = System.Collections.Generic.Stack<(Cilsil.Sil.Expressions.Expression Expression,
                                                    Cilsil.Sil.Types.Typ Type)>;

namespace Cilsil.Cil.Parsers
{
    internal abstract class InstructionParser
    {
        /// <summary>
        /// List of registered parsers. Only those parsers will be used in the translation.
        /// </summary>
        public static List<InstructionParser> Parsers { get; private set; } =
            new List<InstructionParser>();

        /// <summary>
        /// Program stack before translating the current instruction.
        /// </summary>
        private ProcStack PreviousProgramStack;

        private bool RememberNodeOffset;

        /// <summary>
        /// Abstract method for parsers to implement.
        /// </summary>
        /// <param name="instruction"><see cref="Instruction"/> to be parsed.</param>
        /// <param name="state">Current program state.</param>
        /// <returns><c>true</c> if the instruction was parsed successfully, <c>false</c> 
        /// otherwise.</returns>
        protected abstract bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state);

        /// <summary>
        /// Registers a given parser.
        /// </summary>
        public static void RegisterParser(InstructionParser parser)
        {
            if (!Parsers.Any(p => p.GetType().FullName == parser.GetType().FullName))
            {
                Parsers.Add(parser);
            }
        }

        /// <summary>
        /// Registers all known parsers in the current assembly.
        /// </summary>
        public static void RegisterAllKnownParsers()
        {
            var parsers = Assembly
                            .GetExecutingAssembly()
                            .GetTypes()
                            .Where(t => t.IsClass
                                        && !t.IsAbstract
                                        && t.IsSubclassOf(typeof(InstructionParser)));
            foreach (var t in parsers)
            {
                var parser = (InstructionParser)Activator.CreateInstance(t);
                RegisterParser(parser);
            }
        }

        /// <summary>
        /// Attempts to parse an instruction with the registered instruction parsers.
        /// </summary>
        /// <param name="instruction"><see cref="Instruction"/> to be parsed.</param>
        /// <param name="state">Current program state.</param>
        /// <returns><c>true</c> if instruction is successfully translated, <c>false</c> 
        /// otherwise.</returns>
        public static bool ParseCilInstruction(Instruction instruction, ProgramState state)
        {
            var previousProgramStack = state.GetProgramStackCopy();
            foreach (var parser in Parsers)
            {
                parser.PreviousProgramStack = previousProgramStack;
                parser.RememberNodeOffset = true;
                if (parser.ParseCilInstructionInternal(instruction, state))
                {
                    return true;
                }
            }
            Log.WriteError($"Unable to parse instruction {instruction.OpCode.Code}");
            Log.RecordUnknownInstruction(instruction.OpCode.Code.ToString());
            return false;
        }

        /// <summary>
        /// Registers a node in the CFG and set it as the successor of the previous node.
        /// </summary>
        /// <param name="state">Current program state.</param>
        /// <param name="node">Node to register.</param>
        protected void RegisterNode(ProgramState state, CfgNode node)
        {
            state.Cfg.RegisterNode(node);
            state.PreviousNode.Successors.Add(node);
            if (state.MethodExceptionHandlers.GetExceptionHandlerAtInstruction(
                    state.CurrentInstruction) != null)
            {
                state.NodesToLinkWithExceptionBlock.Add(node);
            }
            if (RememberNodeOffset)
            {
                state.SaveNodeOffset(node, PreviousProgramStack);
                RememberNodeOffset = false;
            }
        }

        protected static Location GetHandlerStartLocation(ProgramState state,
                                                          ExceptionHandler handler) =>
            Location.FromSequencePoint(
                state.Method.DebugInformation.GetSequencePoint(handler.HandlerStart));

        protected static Location GetHandlerEndLocation(ProgramState state,
                                                        ExceptionHandler handler) =>
            Location.FromSequencePoint(
                state.Method.DebugInformation.GetSequencePoint(handler.HandlerEnd.Previous));


        private static LvarExpression GetHandlerCatchVar(ProgramState state,
                                                         ExceptionHandler handler) =>
            new LvarExpression(new LocalVariable(Identifier.CatchVarIdentifier + 
                                                   handler.HandlerStart.Offset.ToString(),
                                                 state.Method));

        /// <summary>
        /// Creates an entry node for representing exceptional control flow into an exception
        /// handler; in it, the return value is nullified and the unwrap exception function is
        /// applied to it, which causes the exception to be stored in a catch variable.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="handler">The exception handler for which the node is being 
        /// created.</param>
        /// <returns>The created entry node, as well as the identifier in which the exception is 
        /// stored.</returns>
        private static (CfgNode, Identifier) CreateExceptionEntryNode(ProgramState state, 
                                                                      ExceptionHandler handler)
        {
            var handlerStartLocation = GetHandlerStartLocation(state, handler);
            var returnIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var returnExpression = new LvarExpression(
                new LocalVariable(Identifier.ReturnIdentifier, state.Method));
            var returnType = Typ.FromTypeReference(state.Method.ReturnType);

            var getReturnValue = new Load(returnIdentifier,
                                          returnExpression,
                                          returnType,
                                          handlerStartLocation);
            var deactivateException = new Store(returnExpression,
                                                new ConstExpression(
                                                    new IntRepresentation(0, false, true)),
                                                returnType,
                                                handlerStartLocation);

            var exceptionIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var unwrapReturnValue = new Call(exceptionIdentifier,
                                             returnType,
                                             new ConstExpression(
                                                 ProcedureName.BuiltIn__unwrap_exception),
                                             new List<Call.CallArg>(),
                                             new Call.CallFlags(),
                                             handlerStartLocation);

            var node = new StatementNode(handlerStartLocation,
                                         StatementNode.StatementNodeKind.ExceptionHandler,
                                         state.ProcDesc);

            node.Instructions = new List<SilInstruction> { getReturnValue,
                                                           deactivateException,
                                                           unwrapReturnValue };
            state.Cfg.RegisterNode(node);
            return (node, exceptionIdentifier);
        }

        /// TODO: Need to add is_csharp to Pvar.ml to parse CatchVar accordingly.
        /// <summary>
        /// Helper method for creating a component of the entry block to exception-handling blocks,
        /// in which the thrown exception stored in the CatchVar is handled.
        /// </summary>
        /// <param name="state">The program state.</param>
        /// <param name="handler">The exception handler for which the node is being
        /// created.</param>
        /// <returns>The node in which the caught exception variable is loaded, as well as the
        /// synthetic exception variable created, if the handler is finally.</returns>
        private static (CfgNode, LvarExpression) CreateLoadCatchVarNode(ProgramState state,
                                                                        ExceptionHandler handler)
        {
            LvarExpression syntheticExceptionVariable = null;
            var handlerStartLocation = GetHandlerStartLocation(state, handler);
            var exceptionIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var exceptionType = new Tptr(Tptr.PtrKind.Pk_pointer, new Tstruct("System.Object"));

            var catchVarLoad = new Load(exceptionIdentifier,
                                        GetHandlerCatchVar(state, handler),
                                        exceptionType,
                                        state.CurrentLocation);

            var node = new StatementNode(location: handlerStartLocation,
                                         kind: StatementNode.StatementNodeKind.MethodBody,
                                         proc: state.ProcDesc);
            node.Instructions.Add(catchVarLoad);
            switch (handler.HandlerType)
            {
                case ExceptionHandlerType.Catch:
                    state.AppendToPreviousNode = true;
                    break;
                case ExceptionHandlerType.Finally:
                    // In this case, the exception catch variable is stored into a synthetic
                    // variable we create here.
                    syntheticExceptionVariable =
                        new LvarExpression(new LocalVariable(state.GetSyntheticVariableName(),
                                                             state.Method));
                    var storeIntoSyntheticVariable = new Store(
                        syntheticExceptionVariable, 
                        new VarExpression(exceptionIdentifier),
                        exceptionType, 
                        handlerStartLocation);
                    node.Instructions.Add(storeIntoSyntheticVariable);
                    (var entryNode, _) = GetHandlerEntryNode(state, handler);
                    var finallyBranchNode = CreateFinallyExceptionBranchNode(state, handler);
                    entryNode.Successors.Add(finallyBranchNode);
                    finallyBranchNode.Successors.Add(node);

                    node.ExceptionNodes.Add(entryNode);
                    break;
                default:
                    return (null, null);
            }
            state.Cfg.RegisterNode(node);
            return (node, syntheticExceptionVariable);
        }


        /// <summary>
        /// Gets the node in which the exception stored in the exception handler's catch variable
        /// is loaded. Part of exceptional control flow entry into the handler block.
        /// </summary>
        /// <param name="handler">The exception handler.</param>
        /// <param name="state">The state.</param>
        /// <returns>The node.</returns>
        protected static (CfgNode, LvarExpression) GetHandlerCatchVarNode(ProgramState state,
                                                                          ExceptionHandler handler)
        {
            if (!state.ExceptionHandlerToCatchVarNode.ContainsKey(handler))
            {
                state.ExceptionHandlerToCatchVarNode[handler] = 
                    CreateLoadCatchVarNode(state, handler);
            }
            return state.ExceptionHandlerToCatchVarNode[handler];
        }

        protected static CfgNode CreateFinallyExceptionalEntryBlock(ProgramState state,
                                                                    ExceptionHandler handler)
        {
            (var entryNode, _) = GetHandlerEntryNode(state, handler);
            var finallyBranchNode = CreateFinallyExceptionBranchNode(state, handler);
            (var loadCatchVarNode, _) = GetHandlerCatchVarNode(state, handler);
            entryNode.Successors.Add(finallyBranchNode);
            finallyBranchNode.Successors.Add(loadCatchVarNode);
            return entryNode;
        }

        /// <summary>
        /// Gets the entry node for either the set of catch handlers or the finally handler. There 
        /// should only be one exception identifier per catch set or finally handler, with the
        /// nodes being copy of that created for the first of the set. When this method is invoked 
        /// within the translation of any catch handler, the argument should reference
        /// the first of the set of handlers). 
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The exception entry node, as well as the identifier for the unwrapped 
        /// exception.</returns>
        protected static (CfgNode, Identifier) GetHandlerEntryNode(ProgramState state, 
                                                                   ExceptionHandler handler)
        {
            if (!state.ExceptionHandlerSetToEntryNode.ContainsKey(handler)) 
            {
                state.ExceptionHandlerSetToEntryNode[handler] = CreateExceptionEntryNode(state, 
                                                                                         handler);
                return state.ExceptionHandlerSetToEntryNode[handler];
            }
            else
            {
                var copy = new StatementNode(state.CurrentLocation,
                                             StatementNode.StatementNodeKind.ExceptionHandler,
                                             state.ProcDesc);
                copy.Instructions = 
                    state.ExceptionHandlerSetToEntryNode[handler].node.Instructions;
                state.Cfg.RegisterNode(copy);
                return (copy, state.ExceptionHandlerSetToEntryNode[handler].id);
            }
        }

        protected static CfgNode CreateFinallyExceptionBranchNode(ProgramState state, 
                                                               ExceptionHandler handler)
        {
            var node = new StatementNode(GetHandlerStartLocation(state, handler), 
                                         StatementNode.StatementNodeKind.FinallyBranch, 
                                         state.ProcDesc);
            state.Cfg.RegisterNode(node);
            return node;
        }

        protected static (CfgNode, CfgNode) CreateExceptionTypeCheckBranchNodes(
            ProgramState state, ExceptionHandler handler, Identifier exceptionIdentifier)
        {

            var handlerStartLocation = GetHandlerStartLocation(state, handler);
            var exceptionExpression = new VarExpression(exceptionIdentifier);
            var isInstIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var isInstArgs = new List<Call.CallArg>
                    {
                        new Call.CallArg(exceptionExpression,
                                         Typ.FromTypeReference(handler.CatchType)),
                        new Call.CallArg(
                            new SizeofExpression(
                                Typ.FromTypeReferenceNoPointer(handler.CatchType),
                                SizeofExpression.SizeofExpressionKind.exact),
                            new Tvoid())
                    };
            // We don't mark the function output as an isinst output, as there is no load or store
            // of it.
            var isInstCall = new Call(isInstIdentifier,
                                      new Tint(Tint.IntKind.IBool),
                                      new ConstExpression(ProcedureName.BuiltIn__instanceof),
                                      isInstArgs,
                                      new Call.CallFlags(),
                                      handlerStartLocation);

            var isInstOutputExpression = new VarExpression(isInstIdentifier);
            var pruneTrueInstruction = new Prune(isInstOutputExpression,
                                                 true,
                                                 Prune.IfKind.Ik_switch,
                                                 handlerStartLocation);

            var pruneFalseInstruction = new Prune(new UnopExpression(UnopExpression.UnopKind.LNot,
                                                                     isInstOutputExpression,
                                                                     null),
                                                  false,
                                                  Prune.IfKind.Ik_switch,
                                                  handlerStartLocation);

            var setCatchVarInstruction = new Store(GetHandlerCatchVar(state, handler),
                                                   exceptionExpression,
                                                   Typ.FromTypeReference(state.Method.ReturnType),
                                                   handlerStartLocation);

            var pruneTrueNode = new PruneNode(handlerStartLocation,
                                              true,
                                              PruneNode.PruneNodeKind.ExceptionHandler,
                                              Prune.IfKind.Ik_switch,
                                              state.ProcDesc);

            var pruneFalseNode = new PruneNode(handlerStartLocation,
                                               false,
                                               PruneNode.PruneNodeKind.ExceptionHandler,
                                               Prune.IfKind.Ik_switch,
                                               state.ProcDesc);


            pruneTrueNode.Instructions.AddRange(new List<SilInstruction> 
            { 
                isInstCall, pruneTrueInstruction, setCatchVarInstruction 
            });

            pruneFalseNode.Instructions.AddRange(new List<SilInstruction>
            {
                isInstCall, pruneFalseInstruction
            });
            state.Cfg.RegisterNode(pruneTrueNode);
            state.Cfg.RegisterNode(pruneFalseNode);
            return (pruneTrueNode, pruneFalseNode);
        }

        /// <summary>
        /// Creates a node for returning an exceptional value; does not register the node in the
        /// CFG.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="returnValue">The exceptional value to be returned.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        protected static CfgNode CreateExceptionReturnNode(ProgramState state,
                                                           Expression returnValue,
                                                           Location location)
        {
            var retType = state.Method.ReturnType.GetElementType();
            var retNode = new StatementNode(location,
                                            StatementNode.StatementNodeKind.ReturnStmt,
                                            state.ProcDesc);
            var returnVariable = new LvarExpression(new LocalVariable(Identifier.ReturnIdentifier,
                                                    state.Method));
            var retInstr = new Store(returnVariable,
                                     new ExnExpression(returnValue),
                                     Typ.FromTypeReference(retType),
                                     location);
            retNode.Instructions.Add(retInstr);
            retNode.Successors = new List<CfgNode> { state.ProcDesc.ExitNode };
            return retNode;
        }

        /// <summary>
        /// Creates and returns a <see cref="Call"/> instruction indicating object memory 
        /// allocation and the temporary identifier for the new object. Examples of CIL
        /// instructions for which this method is used include initobj and newobj.
        /// </summary>
        /// <param name="newObjectReference">The type to be allocated.</param>
        /// <param name="state">Current program state.</param>
        /// <returns>Instruction representing the memory allocation, as well as the identifier for 
        /// the new object.</returns>
        protected static (Call, VarExpression) CreateMemoryAllocationCall(
            TypeReference newObjectReference, ProgramState state)
        {
            var type = Typ.FromTypeReference(newObjectReference);
            var newObjectIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var callFlags = new Call.CallFlags(isVirtual: false,
                                               noReturn: false,
                                               isObjCBlock: false);
            var args = new List<Call.CallArg>
            {
                new Call.CallArg(
                    new SizeofExpression(type.StripPointer(), 
                                         SizeofExpression.SizeofExpressionKind.exact), 
                    type)
            };

            return (new Call(newObjectIdentifier,
                             type,
                             new ConstExpression(ProcedureName.BuiltIn__new),
                             args,
                             callFlags,
                             state.CurrentLocation),
                    new VarExpression(newObjectIdentifier));
        }

        /// <summary>
        /// Creates a <see cref="CfgNode"/> containing instructions for memory allocation as well
        /// as constructor invocation.
        /// </summary>
        /// <param name="type">The SIL type to be allocate memory for.</param>
        /// <param name="state">Current program state.</param>
        /// <returns>Node with the object allocation instructions, as well as the variable which 
        /// represents the new object.</returns>
        protected static (CfgNode, VarExpression) CreateObjectAllocationNode(Tptr type,
                                                                             ProgramState state)
        {
            var typeName = type.StripPointer().ToString();

            var newObjectIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal);
            var newObjectVariable = new VarExpression(newObjectIdentifier);
            var callFlags = new Call.CallFlags(isVirtual: false,
                                               noReturn: false,
                                               isObjCBlock: false);
            var objectAllocationCall =
                new Call(newObjectIdentifier,
                         type,
                         new ConstExpression(ProcedureName.BuiltIn__new),
                         new List<Call.CallArg>
                         {
                                     new Call.CallArg(
                                         new SizeofExpression(
                                             type.StripPointer(), 
                                             SizeofExpression.SizeofExpressionKind.exact),
                                         type)
                         },
                         callFlags,
                         state.CurrentLocation);

            var objectConstructorCall =
                new Call(state.GetIdentifier(Identifier.IdentKind.Normal),
                         new Tvoid(),
                         new ConstExpression(new ProcedureName(".ctor",
                                                               new List<string>(),
                                                               typeName,
                                                               "System.Void",
                                                               false)),
                         new List<Call.CallArg>
                         {
                             new Call.CallArg(newObjectVariable, type)
                         },
                         callFlags,
                         state.CurrentLocation);

            var node = new StatementNode(state.CurrentLocation,
                                         StatementNode.StatementNodeKind.Call,
                                         state.ProcDesc,
                                         comment: $"System.Void {typeName}::.ctor()");

            node.Instructions.Add(objectAllocationCall);
            node.Instructions.Add(objectConstructorCall);

            return (node, newObjectVariable);
        }

        /// <summary>
        /// Creates a field expression given an expression for the field's parent as well as the
        /// field reference.
        /// </summary>
        /// <param name="fieldParentExpression">An expression for the parent of the field;
        /// represents a class for a static field, and an instance of the class for an instance 
        /// field.</param>
        /// <param name="fieldReference">A <see cref="FieldReference"/> which describes the field
        /// for which the expression is being created.</param>
        /// <returns>The <see cref="LfieldExpression"/> for the given field.</returns>
        protected static LfieldExpression CreateFieldExpression(Expression fieldParentExpression,
                                                                FieldReference fieldReference)
        {
            return new LfieldExpression(fieldParentExpression,
                                        new FieldIdentifier(fieldReference.GetCompatibleFullName(),
                                                            Typ.FromTypeReferenceNoPointer(
                                                                fieldReference.FieldType)),
                                        Typ.FromTypeReferenceNoPointer(
                                            fieldReference.DeclaringType));
        }

        /// <summary>
        /// Creates a method call returned via out parameter.
        /// </summary>
        /// <param name="state">Current program state.</param>
        /// <param name="isVirtual">True if method call is virtual, false otherwise.</param>
        /// <param name="calledMethod">The method being called.</param>
        /// <param name="returnType">The return type of the method being called.</param>
        /// <param name="returnVariable">Identifies the variable returned by the method.</param>
        /// <param name="callArgs">The method arguments.</param>
        /// <param name="methodCall">The Call SIL instruction.</param>
        /// <param name="isConstructorCall"><c>true</c> if the call is for a constructor,
        /// <c>false</c> otherwise.</param>
        protected static void CreateMethodCall(ProgramState state,
                                               bool isVirtual,
                                               MethodReference calledMethod,
                                               out TypeReference returnType,
                                               out Identifier returnVariable,
                                               out List<Call.CallArg> callArgs,
                                               out Call methodCall,
                                               bool isConstructorCall = false)
        {
            callArgs = new List<Call.CallArg>();
            returnType = calledMethod.ReturnType;
            var paramCount = calledMethod.Parameters.Count;
            if (calledMethod.HasThis) { paramCount++; }
            if (isConstructorCall)
            {
                // In this case, the "this" argument of the constructor is located at the top of
                // the stack; we remove it and place it at the front of the argument list.
                (var thisExpr, var thisType) = state.Pop();
                callArgs.Add(new Call.CallArg(thisExpr, thisType));
                paramCount--;
            }
            var funcExp = new ConstExpression(new ProcedureName(calledMethod));
            callArgs.AddRange(state.PopMany(paramCount)
                                   .Select(p => new Call.CallArg(p.Item1, p.Item2))
                                   .ToList());
            var callFlags = new Call.CallFlags(isVirtual, false, false);
            returnVariable = state.GetIdentifier(Identifier.IdentKind.Normal);
            methodCall = new Call(returnId: returnVariable,
                                  returnType: Typ.FromTypeReference(returnType),
                                  functionExpression: funcExp,
                                  args: callArgs,
                                  flags: callFlags,
                                  location: state.CurrentLocation);
        }

        /// <summary>
        /// Helper method for creating a deref instruction on a given object, typically used for
        /// validating the null-state of an object prior to calling a member method of it.
        /// </summary>
        /// <param name="objectToDereference">Variable corresponding to the object to 
        /// dereference.</param>
        /// <param name="type">The type of the object being dereferenced.</param>
        /// <param name="state">The program state for the SIL instruction.</param>
        /// <returns>SIL instruction representing the dereference.</returns>
        protected static Load CreateDereference(VarExpression objectToDereference,
                                                Typ type,
                                                ProgramState state)
        {
            var noId = state.GetIdentifier(Identifier.IdentKind.None);
            return new Load(identifierAssignedTo: noId,
                            lvalue: objectToDereference,
                            type: type.StripPointer(),
                            location: state.CurrentLocation);
        }

        /// <summary>
        /// Creates a local variable from a given method and variable index; creates the address of
        /// a local variable if the appropriate parameter value is specified.
        /// </summary>
        /// <param name="index">An identifier for a local variable. A compiled local variable loses
        /// its variable name, which is replaced instead by an index.</param>
        /// <param name="method">The method in which the local variable is initialized.</param>
        /// <returns>The local variable expression (or address) and its associated type.</returns>
        protected (LvarExpression, Typ) CreateLocal(int index,
                                                    MethodDefinition method)
        {
            var name = LocalName(index);
            // If the variable is by reference, its corresponding type will have an &, which we do
            // not want.
            var variableType = method.Body.Variables[index].VariableType;
            return (new LvarExpression(new LocalVariable(name, method)),
                    Typ.FromTypeReference(variableType));
        }

        /// <summary>
        /// Creates the argument variable.
        /// </summary>
        /// <param name="index">The argument's index.</param>
        /// <param name="method">The method with which the argument is associated.</param>
        /// <returns>Expression representing the argument and its type.</returns>
        protected (LvarExpression, Typ) CreateArg(int index,
                                                  MethodDefinition method)
        {
            TypeReference type;
            var name = ArgumentName(index, method);
            if (method.HasThis)
            {
                if (index == 0)
                {
                    type = method.DeclaringType;
                }
                else
                {
                    type = method.Parameters[index - 1].ParameterType;
                }
            }
            else
            {
                type = method.Parameters[index].ParameterType;
            }
            return (new LvarExpression(new LocalVariable(name, method)),
                    Typ.FromTypeReference(type));
        }

        /// <summary>
        /// Converts the given local variable index identifier to its string representation. 
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The string representation.</returns>
        protected string LocalName(int index) => $"%{index}";

        /// <summary>
        /// Gets the argument name for the given index and method.
        /// </summary>
        protected string ArgumentName(int index, MethodDefinition method)
        {
            if (method.HasThis)
            {
                if (index == 0)
                {
                    return "this";
                }
                else
                {
                    return method.Parameters[index - 1].Name;
                }
            }
            else
            {
                return method.Parameters[index].Name;
            }
        }

        /// <summary>
        /// Adds method body instructions to the CFG; either adds to the 
        /// <see cref="ProgramState.PreviousNode"/> or creates a new node to store those
        /// instructions.
        /// </summary>
        /// <param name="state">Current program state.</param>
        /// <param name="instructions">Instructions to add.</param>
        /// <returns>The node to which the instructions were added.</returns>
        protected CfgNode AddMethodBodyInstructionsToCfg(ProgramState state,
                                                         params SilInstruction[] instructions)
        {
            if (state.AppendToPreviousNode && !(state.PreviousNode is PruneNode))
            {
                state.PreviousNode.Instructions.AddRange(instructions);
                state.AppendToPreviousNode = false;
                return state.PreviousNode;
            }
            else
            {
                var newNode = new StatementNode(location: state.CurrentLocation,
                                                kind: StatementNode.StatementNodeKind.MethodBody,
                                                proc: state.ProcDesc);
                newNode.Instructions.AddRange(instructions);
                RegisterNode(state, newNode);
                return newNode;
            }
        }

        /// <summary>
        /// Registers the local variables to the proc attributes. This method should be called 
        /// whenever new local variables are initialized.
        /// </summary>
        /// <param name="state">Current program state.</param>
        /// <param name="lvar">Variable to register.</param>
        /// <param name="type">Type of the variable.</param>
        protected void RegisterLocalVariable(ProgramState state, LocalVariable lvar, Typ type)
        {
            if (!state.ProcDesc.PdAttributes.Locals.Any(l => l.Name == lvar.PvName))
            {
                state.ProcDesc.PdAttributes.Locals.Add(
                    new ProcedureAttributes.Local(name: lvar.PvName,
                                                  type: type,
                                                  modifyInBlock: false,
                                                  isConstExpr: false));
            }
        }
    }
}
