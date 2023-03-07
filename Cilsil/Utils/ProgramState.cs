﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Services;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using ProgramStack = System.Collections.Generic.Stack<(Cilsil.Sil.Expressions.Expression Expression, Cilsil.Sil.Types.Typ Type)>;

namespace Cilsil.Utils
{
    /// <summary>
    /// Contains program translation state information over the course of a translation.
    /// </summary>
    public class ProgramState
    {
        private const int NodeVisitTimeoutThreshold = 1000;

        /// <summary>
        /// The CFG associated with the method.
        /// </summary>
        public Cfg Cfg { get; }

        /// <summary>
        /// The method being translated.
        /// </summary>
        public MethodDefinition Method { get; }

        /// <summary>
        /// The name associated with the method being translated.
        /// </summary>
        public ProcedureName ProcName { get; }

        /// <summary>
        /// The SIL procedure description associated with a method.
        /// </summary>
        public ProcedureDescription ProcDesc { get; }

        /// <summary>
        /// Marks the node from which a procedure translation should continue. For code which
        /// follows a linear path, this refers to the node storing the previously translated
        /// instruction. For code which is non-linear, i.e. it has a skip or a jump, this node
        /// refers to the branching point.
        /// </summary>
        public CfgNode PreviousNode { get; set; }

        /// <summary>
        /// If this is true, then the translated SIL instruction should be appended to the CFG node
        /// identified by <see cref="PreviousNode"/>.
        /// </summary>
        public bool AppendToPreviousNode { get; set; }

        /// <summary>
        /// Source code location of the instruction currently being parsed.
        /// </summary>
        public Location CurrentLocation { get; private set; }

        /// <summary>
        /// The instruction currently being parsed.
        /// </summary>
        public Instruction CurrentInstruction { get; private set; }

        /// <summary>
        /// List of instructions which were parsed prior to the current one.
        /// </summary>
        public List<Instruction> ParsedInstructions { get; private set; }

        /// <summary>
        /// Maps an instruction offset (a unique integer identifier for a CIL instruction which has
        /// been translated) to the CFG node containing the translated SIL instruction as well as 
        /// the program stack immediately prior to the translation of that CIL instruction.
        /// </summary>
        public Dictionary<int, List<(CfgNode Node,
                                    ProgramStack Stack,
                                    int PredecessorBlockEndOffset)>> OffsetToNode
        { get; }

        /// <summary>
        /// Maps a variable index to a boxed variable type, if there is one stored at the location.
        /// </summary>
        public Dictionary<int, BoxedValueType> VariableIndexToBoxedValueType { get; }

        /// <summary>
        /// Maps a variable index to the null check expression, if there is one stored at the 
        /// location.
        /// </summary>
        public Dictionary<int,
                          (BinopExpression expr, Typ type)> VariableIndexToNullCheck
        { get; }

        /// <summary>
        /// Maps a variable index to a function type, if there is one stored at the location.
        /// </summary>
        public Dictionary<int, Tfun> VariableIndexToFunctionType { get; }

        /// <summary>
        /// Tracks indices at which the expression stored is produced from the translation of the
        /// isinst instruction.
        /// </summary>
        public HashSet<int> IndicesWithIsInstReturnType { get; }

        /// <summary>
        /// True if there are remaining instructions to translate, and false otherwise.
        /// </summary>
        public bool HasInstruction => InstructionsStack.Count > 0;

        /// <summary>
        /// Program stack for the current method. Each entry is an expression and a type for the 
        /// expression.
        /// </summary>
        private ProgramStack ProgramStack;

        /// <summary>
        /// Stack which stores instructions to be translated, along with extra information about 
        /// the translation state. This is pushed to and popped from over the course of the
        /// translation.
        /// </summary>
        private readonly Stack<TranslationSnapshot> InstructionsStack;

        /// <summary>
        /// The next available integer identifier for temporary variables at this state.
        /// </summary>
        private int NextAvailableTemporaryVariableId;

        /// <summary>
        /// The next available integer identifier for synthetic variables. See 
        /// <see cref="Identifier.SyntheticIdentifier"/> for more information.
        /// </summary>
        private int NextAvailableSyntheticVariableId;

        /// <summary>
        /// <c>true</c> if the top instruction is in a try or catch block; <c>false</c> otherwise. 
        /// </summary>
        public bool InstructionInTryOrCatch;

        /// <summary>
        /// Maps each exception handler to the node in which its catch variable is loaded as well
        /// as the associated synthetic variable into which the exception catch variable is stored,
        /// in case of finally handler (null otherwise).
        /// </summary>
        public Dictionary<ExceptionHandler,
                         (CfgNode node, LvarExpression variable)> ExceptionHandlerToCatchVarNode;

        /// <summary>
        /// The exception handler to its entry node as well as the identifier for the unwrapped 
        /// exception.
        /// </summary>
        public Dictionary<ExceptionHandler,
                          (CfgNode node, Identifier id)> ExceptionHandlerSetToEntryNode;

        /// <summary>
        /// Maps finally handler to the exceptional exit node created for it, if it has been
        /// created yet.
        /// </summary>
        public Dictionary<ExceptionHandler, CfgNode> FinallyHandlerToExceptionExit;

        /// <summary>
        /// The instruction through which control flow should be routed when endfinally is
        /// encountered; this is set as the leave target when a non-exceptional entry into a
        /// finally block is created.
        /// </summary>
        public Instruction EndfinallyControlFlow;

        /// <summary>
        /// Null by default. When control flow is exited via a throw instruction, this is the throw
        /// node to be added at the end of the translation of the finally block, when one exists.
        /// </summary>
        public CfgNode EndfinallyThrowNode;

        /// <summary>
        /// Contains information about the program's exception handlers.
        /// </summary>
        public MethodExceptionHandlers MethodExceptionHandlers;

        /// <summary>
        /// <c>true</c> if the current translation of finally is exceptional; <c>false</c> 
        /// otherwise. 
        /// </summary>
        public bool FinallyExceptionalTranslation;

        /// <summary>
        /// Specifically for async methods, this is the method definition that we want to update 
        /// the async method definition to (for example, needed for when we create program
        /// variables).
        /// </summary>
        public MethodDefinition MethodDefinitionToUpdate = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramState"/> class.
        /// </summary>
        /// <param name="method">The method being translated.</param>
        /// <param name="cfg">The CFG associated with the method.</param>
        public ProgramState(MethodDefinition method, Cfg cfg)
        {
            Cfg = cfg;
            ProgramStack = new ProgramStack();
            Method = method;
            ProcName = new ProcedureName(method);
            ProcDesc = new ProcedureDescription(method, cfg);
            PreviousNode = ProcDesc.StartNode;

            CurrentLocation = Location.FromSequencePoint(
                method.DebugInformation.SequencePoints.FirstOrDefault());

            InstructionsStack = new Stack<TranslationSnapshot>();
            ParsedInstructions = new List<Instruction>();

            MethodExceptionHandlers = new MethodExceptionHandlers(method.Body);
            InstructionInTryOrCatch = false;

            OffsetToNode = new Dictionary<int, List<(CfgNode Node, ProgramStack Stack, int)>>();
            VariableIndexToBoxedValueType = new Dictionary<int, BoxedValueType>();
            VariableIndexToNullCheck = new Dictionary<int, (BinopExpression, Typ)>();
            VariableIndexToFunctionType = new Dictionary<int, Tfun>();

            ExceptionHandlerToCatchVarNode = new Dictionary<ExceptionHandler,
                                                            (CfgNode, LvarExpression)>();
            FinallyHandlerToExceptionExit = new Dictionary<ExceptionHandler, CfgNode>();
            ExceptionHandlerSetToEntryNode = new Dictionary<ExceptionHandler,
                                                            (CfgNode node, Identifier id)>();

            FinallyExceptionalTranslation = false;

            IndicesWithIsInstReturnType = new HashSet<int>();
            NextAvailableTemporaryVariableId = 0;
            NextAvailableSyntheticVariableId = 0;
        }

        /// <summary>
        /// Saves the input node and stack to the map entry corresponding to the current
        /// instruction's offset. 
        /// </summary>
        /// <remarks>The node is saved for the current offset along with the current stack 
        /// state.</remarks>
        public void SaveNodeOffset(CfgNode node,
                                   ProgramStack previousStack,
                                   int previousNodeHandlerEndOffset)
        {
            // If not a catch block, where the stack is expected to contain the thrown exception,
            // and the previous node is from a different handler block, we empty the saved stack,
            // as there should not be anything on the stack when transferring control between
            // different handler blocks.
            if (MethodExceptionHandlers.GetMapTypeFromInstruction(CurrentInstruction) !=
                    MethodExceptionHandlers.MapType.CatchToCatch &&
                node.BlockEndOffset != PreviousNode.BlockEndOffset)
            {
                OffsetToNode
                    .GetOrCreateValue(CurrentInstruction.Offset,
                                      new List<(CfgNode Node, ProgramStack Stack, int)>())
                    .Add((node,
                          new ProgramStack(),
                          previousNodeHandlerEndOffset));
            }
            else
            {
                OffsetToNode
                    .GetOrCreateValue(CurrentInstruction.Offset,
                                      new List<(CfgNode Node, ProgramStack Stack, int)>())
                    .Add((node,
                          previousStack,
                          previousNodeHandlerEndOffset));
            }
        }

        /// <summary>
        /// Gets the node for the given offset to be reused, if available.
        /// </summary>
        /// <returns>Node to be reused, if one exists, null otherwise. Additionally, a boolean 
        /// indicating <c>true</c> if there are greater than a threshold number of nodes which have
        /// been created for that instruction offset, and <c>false</c> otherwise.</returns>
        /// <remarks>For a node to be reusable for a particular offset, the corresponding program 
        /// stack must be a list subset (i.e. guarantees it has the necessary stack instructions 
        /// required at that state).</remarks>
        public (CfgNode, bool) GetOffsetNode(
            int offset,
            int predecessorHandlerBlockEndOffset = MethodExceptionHandlers.DefaultHandlerEndOffset)
        {
            if (OffsetToNode.ContainsKey(offset))
            {
                if (OffsetToNode[offset].Count > NodeVisitTimeoutThreshold)
                {
                    return (null, true);
                }
                else
                {
                    // CIL instructions that manipulate the stack are reused in breaks and it is 
                    // possible for stack-manipulating instructions to be reused with different stack 
                    // state. This can cause unexpected behaviors as translate instructions may get 
                    // reused but for the wrong stack state.
                    // This prevents this issue by making sure that the stack is exactly the same 
                    // before we try to reuse it.
                    return (OffsetToNode[offset]
                            .FirstOrDefault(
                                entry => entry.Stack.IsSubStackOf(ProgramStack) &&
                                    entry.PredecessorBlockEndOffset ==
                                    predecessorHandlerBlockEndOffset)
                            .Node, false);
                }
            }
            return (null, false);
        }

        /// <summary>
        /// Returns a shallow copy of the current program stack.
        /// </summary>
        public ProgramStack GetProgramStackCopy() => ProgramStack.Clone();

        /// <summary>
        /// Returns <c>true</c> if program stack is empty; false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool ProgramStackIsEmpty() => ProgramStack.Count == 0;

        /// <summary>
        /// Creates an instruction which loads the input expression into a fresh identifier. 
        /// Pushes the variable expression enclosing that identifier onto the program stack; 
        /// returns the load instruction.
        /// </summary>
        /// <param name="expression">The expression to load into the fresh identifier.</param>
        /// <param name="type">The type of the expression.</param>
        /// <returns>The <see cref="Load"/> instruction created from the inputs.</returns>
        public Load PushAndLoad(Expression expression, Typ type)
        {
            var freshIdentifier = GetIdentifier(Identifier.IdentKind.Normal);
            var isThis = false;
            if (expression is LvarExpression variable && variable.Pvar.PvName == "this")
            {
                isThis = true;
            }
            PushExpr(new VarExpression(freshIdentifier, isThis), type);
            return new Load(freshIdentifier, expression, type, CurrentLocation);
        }

        /// <summary>
        /// Creates a new identifier. 
        /// </summary>
        /// <param name="kind">The type of identifier to be created.</param>
        /// <param name="name">The name of the identifier to be created. Defaults to a standard 
        /// type-dependent name.</param>
        /// <returns>The new identifier.</returns>
        public Identifier GetIdentifier(
            Identifier.IdentKind kind, string name = null) =>
            new Identifier()
            {
                Kind = kind,
                Name = name ?? Identifier.StandardNames[kind],
                Stamp = NextAvailableTemporaryVariableId++,
            };

        /// <summary>
        /// Retrieves a fresh synthetic variable name.
        /// </summary>
        /// <returns>The name.</returns>
        public string GetSyntheticVariableName() =>
            Identifier.SyntheticIdentifier + NextAvailableSyntheticVariableId++;

        /// <summary>
        /// Pushes an expression and its type onto the stack.
        /// </summary>
        /// <param name="exp">The expression to push.</param>
        /// <param name="type">The type of the expression being pushed.</param>
        public void PushExpr(Expression exp, Typ type) => ProgramStack.Push((exp, type));

        /// <summary>
        /// Returns the top element of the stack, without removing it.
        /// </summary>
        public (Expression, Typ) Peek() => ProgramStack.Peek();

        /// <summary>
        /// Returns and removes the top element of the stack.
        /// </summary>
        /// <exception cref="ServiceExecutionException">Thrown when popping on empty
        /// stack.</exception>
        public (Expression, Typ) Pop()
        {
            if (ProgramStack.Count == 0)
            {
                throw new ServiceExecutionException(
                    $@"Popping on empty stack at method: {Method.GetCompatibleFullName()} instruction: {CurrentInstruction} location: {CurrentLocation}", this);
            }
            return ProgramStack.Pop();
        }

        /// <summary>
        /// Pops multiple elements from the stack, returning them such that the last item popped
        /// from the stack will be the first item in the returned enumerable.
        /// </summary>
        /// <param name="count">The number of elements to remove from the stack.</param>
        /// <returns>The list of items popped, ordered in reverse to the order in which they are
        /// popped.</returns>
        public List<(Expression, Typ)> PopMany(int count) =>
            Enumerable.Range(0, count).Select(_ => Pop()).Reverse().ToList();

        /// <summary>
        /// Pops two elements from the stack and use them as the operands of a binary operation 
        /// expression. The first item popped is the right operand of the expression, and the
        /// second item popped is the left operand.
        /// </summary>
        /// <param name="binopKind">The type of the binary operation.</param>
        /// <returns>The binary operation expression as well as the type associated with its
        /// underlying operands.</returns>
        public (Expression, Typ) PopTwoAndApplyBinop(BinopExpression.BinopKind binopKind)
        {
            (var right, var rightExpressionType) = Pop();
            (var left, var leftExpressionType) = Pop();
            var binopOutputType = rightExpressionType;
            // In this case, the expression is a boolean comparison on the expression produced from
            // an isinst translation, which itself is already a boolean value; we simply return
            // this value.
            if (binopKind == BinopExpression.BinopKind.Gt && leftExpressionType.IsInstReturnType)
            {
                return (left, leftExpressionType);
            }
            // Object-null checks are represented in CIL using Gt. In this case, binop kind should
            // be updated to Ne.
            if (binopKind == BinopExpression.BinopKind.Gt &&
                right.Equals(new ConstExpression(new IntRepresentation(0, false, true))))
            {
                binopKind = BinopExpression.BinopKind.Ne;
            }

            if (binopKind == BinopExpression.BinopKind.Lt ||
                binopKind == BinopExpression.BinopKind.Gt ||
                binopKind == BinopExpression.BinopKind.Le ||
                binopKind == BinopExpression.BinopKind.Ge ||
                binopKind == BinopExpression.BinopKind.Eq ||
                binopKind == BinopExpression.BinopKind.Ne)
            {
                binopOutputType = new Tint(Tint.IntKind.IBool);
            }

            return (new BinopExpression(binopKind, left, right), binopOutputType);
        }

        /// <summary>
        /// Pushes an instruction to be parsed. 
        /// </summary>
        /// <param name="instruction">Instruction to be parsed.</param>
        /// <param name="node">The node to set <see cref="PreviousNode"/> to, i.e. the node from 
        /// which to continue the translation from when translating the instruction. See 
        /// <see cref="PreviousNode"/> for more info. If this is not provided or is null, 
        /// <see cref="PreviousNode"/> is not updated.</param>
        public void PushInstruction(Instruction instruction, CfgNode node = null)
        {
            // Control flow can enter a try-block either by jump to the first instruction or
            // fall-through from the previous one; we start to save the nodes if control flow
            // transitions from unhandled code to handled code. 
            InstructionsStack.Push(
                new TranslationSnapshot
                {
                    Instruction = instruction,
                    PreviousNode = node ?? PreviousNode,
                    PreviousStack = ProgramStack.Clone(),
                    NextAvailableTemporaryVariableId = NextAvailableTemporaryVariableId,
                    FinallyExceptionalTranslation = FinallyExceptionalTranslation,
                    EndfinallyControlFlow = EndfinallyControlFlow,
                });
        }

        /// <summary>
        /// Pushes the next instruction to be parsed, but specially for the beginning of a catch
        /// handler. In this case, the program stack of the snapshot contains only the expression
        /// referring to the exception, which is exactly what the compiler expects. 
        /// </summary>
        /// <param name="instruction">The first instruction of the handler.</param>
        /// <param name="node">The node beginning the handler from which to continue 
        /// translation.</param>
        /// <param name="exceptionIdentifier">The identifier for the exception.</param>
        public void PushInstructionCatchHandlerStart(Instruction instruction, 
                                                     CfgNode node, 
                                                     Identifier exceptionIdentifier)
        {
            var handlerStack = new ProgramStack();
            handlerStack.Push(
                (new VarExpression(exceptionIdentifier),
                 new Tptr(Tptr.PtrKind.Pk_pointer,
                          new Tstruct("System.Object"))));
            InstructionsStack.Push(
                new TranslationSnapshot
                {
                    Instruction = instruction,
                    PreviousNode = node ?? PreviousNode,
                    PreviousStack = handlerStack,
                    NextAvailableTemporaryVariableId = NextAvailableTemporaryVariableId,
                    FinallyExceptionalTranslation = FinallyExceptionalTranslation,
                    EndfinallyControlFlow = EndfinallyControlFlow,
                });
        }

        /// <summary>
        /// Checks null-state of the top snapshot instruction of the stack.
        /// </summary>
        /// <returns><c>True</c> if instruction null; <c>false</c> otherwise.</returns>
        public bool IsTopSnapshotInstructionNull()
        {
            return InstructionsStack.Peek().Instruction == null;
        }

        /// <summary>
        /// Pops an instruction to be parsed.
        /// </summary>
        /// <returns>The instruction to be parsed.</returns>
        public Instruction PopInstruction()
        {
            var snapshot = InstructionsStack.Pop();
            PreviousNode = snapshot.PreviousNode;
            CurrentInstruction = snapshot.Instruction;
            ProgramStack = snapshot.PreviousStack;
            NextAvailableTemporaryVariableId = snapshot.NextAvailableTemporaryVariableId;
            FinallyExceptionalTranslation = snapshot.FinallyExceptionalTranslation;
            EndfinallyControlFlow = snapshot.EndfinallyControlFlow;

            var currentSequencePoint =
                Method.DebugInformation.GetSequencePoint(CurrentInstruction);
            // Line number is sometimes extremely high, for example with branching instructions.
            if (currentSequencePoint != null && currentSequencePoint.StartLine < 10000000)
            {
                var newLocation = Location.FromSequencePoint(currentSequencePoint);
                CurrentLocation = newLocation;
            }
            ParsedInstructions.Add(snapshot.Instruction);
            return CurrentInstruction;
        }

        /// <summary>
        /// Gets the state debug information.
        /// </summary>
        /// <param name="invalidObject">The invalid object found within the parser
        /// code.</param>
        /// <returns>String representing the debug information.</returns>
        public string GetStateDebugInformation(object invalidObject)
        {
            return $"Invalid value {invalidObject?.ToString()}\n" +
                    "====State information====\n" +
                    ProcDesc.ToString() + "\n" +
                    "====Parsed Instructions====\n" +
                    string.Join(",", ParsedInstructions);
        }

        /// <summary>
        /// Stores information about translation state.
        /// </summary>
        private struct TranslationSnapshot
        {
            /// <summary>
            /// The instruction translated in this state.
            /// </summary>
            public Instruction Instruction;

            /// <summary>
            /// The predecessor node with respect to the CFG. See <see cref="PreviousNode"/>.
            /// </summary>
            public CfgNode PreviousNode;

            /// <summary>
            /// The program stack prior to translating the instruction of this state.
            /// </summary>
            public ProgramStack PreviousStack;

            /// <summary>
            /// The next available integer identifier for temporary variables at this state.
            /// </summary>
            public int NextAvailableTemporaryVariableId;

            /// <summary>
            /// <c>true</c> if translation of the finally block from the current state should be
            /// exceptional; otherwise, <c>false</c>.
            /// </summary>
            public bool FinallyExceptionalTranslation;

            /// <summary>
            /// The instruction to route control flow after the endfinally block.
            /// </summary>
            public Instruction EndfinallyControlFlow;
        }
    }
}
