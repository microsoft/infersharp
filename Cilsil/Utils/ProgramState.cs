// Copyright (c) Microsoft Corporation.
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
        /// Maps catch/finally block starting offset to ending offset.
        /// </summary>
        public Dictionary<int, int> ExceptionBlockStartToEndOffsets { get; }

        /// <summary>
        /// Maps an catch block starting CFG node offset to exception type.
        /// </summary>
        public Dictionary<int, TypeReference> OffsetToExceptionType { get; }

        /// <summary>
        /// Maps an instruction offset (a unique integer identifier for a CIL instruction which has
        /// been translated) to the CFG node containing the translated SIL instruction as well as 
        /// the program stack immediately prior to the translation of that CIL instruction.
        /// </summary>
        public Dictionary<int, List<(CfgNode Node, ProgramStack Stack)>> OffsetToNode { get; }

        /// <summary>
        /// Maps a variable index to a boxed variable type, if there is one stored at the location.
        /// </summary>
        public Dictionary<int, BoxedValueType> VariableIndexToBoxedValueType { get; }

        /// <summary>
        /// Previous expression registered by return node.
        /// </summary>
        private Expression PreviousReturnedExpression;

        /// <summary>
        /// Previous expression type registered by return node.
        /// </summary>
        private Typ PreviousReturnedType;

        /// <summary>
        /// True if there are remaining instructions to translate, and false otherwise.
        /// </summary>
        public bool HasInstruction => InstructionsStack.Count > 0;

        /// <summary>
        /// True if the peek instruction in instruction stack is at the beginning of an exception handling block, and false otherwise.
        /// </summary>
        public bool NextInstructionInExceptionHandlingBlock
            => ExceptionBlockStartToEndOffsets.ContainsKey(InstructionsStack.Peek().Instruction.Offset);

        /// <summary>
        /// Program stack for the current method. Each entry is an expression and a type for the 
        /// expression.
        /// </summary>
        private ProgramStack ProgramStack;

        /// <summary>
        /// Dangling condition program stack for the current method. Each entry is an expression and a type for the 
        /// expression.
        /// </summary>
        private ProgramStack DanglingConditionProgramStack;

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
        /// Initializes a new instance of the <see cref="ProgramState"/> class.
        /// </summary>
        /// <param name="method">The method being translated.</param>
        /// <param name="cfg">The CFG associated with the method.</param>
        public ProgramState(MethodDefinition method, Cfg cfg)
        {
            Cfg = cfg;
            ProgramStack = new ProgramStack();
            DanglingConditionProgramStack = new ProgramStack();
            Method = method;
            ProcName = new ProcedureName(method);
            ProcDesc = new ProcedureDescription(method, cfg);
            PreviousNode = ProcDesc.StartNode;

            CurrentLocation = Location.FromSequencePoint(
                                        method.DebugInformation.SequencePoints.FirstOrDefault());

            InstructionsStack = new Stack<TranslationSnapshot>();
            ParsedInstructions = new List<Instruction>();

            OffsetToNode = new Dictionary<int, List<(CfgNode Node, ProgramStack Stack)>>();
            VariableIndexToBoxedValueType = new Dictionary<int, BoxedValueType>();

            NextAvailableTemporaryVariableId = 0;

            ExceptionBlockStartToEndOffsets = new Dictionary<int, int>();
            OffsetToExceptionType = new Dictionary<int, TypeReference>();
            PreviousReturnedExpression = new LvarExpression(
                                         new LocalVariable(Identifier.ReturnIdentifier,
                                                           Method));
            PreviousReturnedType = Typ.FromTypeReference(Method.ReturnType);
        }

        /// <summary>
        /// Saves the input node and stack to the map entry corresponding to the current
        /// instruction's offset. 
        /// </summary>
        /// <remarks>The node is saved for the current offset along with the current stack 
        /// state.</remarks>
        public void SaveNodeOffset(CfgNode node, ProgramStack previousStack)
        {
            OffsetToNode
                .GetOrCreateValue(CurrentInstruction.Offset,
                                  new List<(CfgNode Node, ProgramStack Stack)>())
                .Add((node, previousStack));
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
        public (CfgNode, bool) GetOffsetNode(int offset)
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
                            .FirstOrDefault(entry => entry.Stack.IsSubStackOf(ProgramStack))
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
        /// Returns a shallow copy of the current dangling condition program stack.
        /// </summary>
        public ProgramStack GetDanglingConditionProgramStackCopy() => DanglingConditionProgramStack.Clone();

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
            PushExpr(new VarExpression(freshIdentifier), type);
            return new Load(freshIdentifier, expression, type, CurrentLocation);
        }

        /// <summary>
        /// Creates a new identifier. 
        /// </summary>
        /// <param name="kind">The type of identifier to be created.</param>
        /// <param name="name">The name of the identifier to be created. Defaults to a standard 
        /// type-dependent name.</param>
        /// <returns>The new identifier.</returns>
        public Identifier GetIdentifier(Identifier.IdentKind kind, string name = null) =>
            new Identifier()
            {
                Kind = kind,
                Name = name ?? Identifier.StandardNames[kind],
                Stamp = NextAvailableTemporaryVariableId++
            };

        /// <summary>
        /// Pushes an expression and its type onto the stack.
        /// </summary>
        /// <param name="exp">The expression to push.</param>
        /// <param name="type">The type of the expression being pushed.</param>
        public void PushExpr(Expression exp, Typ type)
        {
            ProgramStack.Push((exp, type));
        }

        /// <summary>
        /// Pushes an expression and its type onto the dangling condition stack.
        /// </summary>
        /// <param name="exp">The expression to push.</param>
        /// <param name="type">The type of the expression being pushed.</param>
        public void PushConditionExpr(Expression exp, Typ type)
        {
            DanglingConditionProgramStack.Push((exp, type));
        }

        /// <summary>
        /// Pushes PreviousReturnedExpression and its type onto the stack.
        /// </summary>
        public void PushRetExpr()
        {
            if (ProgramStack.Count == 0 ||
                (ProgramStack.Count > 0 &&
                !ProgramStack.Peek().Item1.Equals(PreviousReturnedExpression)))
            {
                ProgramStack.Push((PreviousReturnedExpression, PreviousReturnedType));
            }
        }

        /// <summary>
        /// Returns the top element of the stack, without removing it.
        /// </summary>
        public (Expression, Typ) Peek() => ProgramStack.Peek();

        /// <summary>
        /// Returns the top element of the dangling condition stack, without removing it.
        /// </summary>
        public (Expression, Typ) DanglingConditionProgramStackPeek() => DanglingConditionProgramStack.Peek();

        /// <summary>
        /// Returns and removes the top element of the stack.
        /// </summary>
        /// <exception cref="ServiceExecutionException">Thrown when popping on empty
        /// stack.</exception>
        public (Expression, Typ) Pop()
        {
            if (ProgramStack.Count == 0 && DanglingConditionProgramStack.Count == 0)
            {
                throw new ServiceExecutionException(
                    $@"Popping on empty stack at method: {
                        Method.GetCompatibleFullName()} instruction: {
                        CurrentInstruction} location: {CurrentLocation}", this);
            }
            else if (ProgramStack.Count > 0)
            {
                return ProgramStack.Pop();
            }
            return PopConditionExpression();
        }

        /// <summary>
        /// Returns and removes the top element of the dangling condition stack.
        /// </summary>
        /// <exception cref="ServiceExecutionException">Thrown when popping on empty
        /// stack.</exception>
        public (Expression, Typ) PopConditionExpression()
        {
            if (DanglingConditionProgramStack.Count == 0)
            {
                throw new ServiceExecutionException(
                    $@"Popping on empty stack at method: {
                        Method.GetCompatibleFullName()} instruction: {
                        CurrentInstruction} location: {CurrentLocation}", this);
            }
            return DanglingConditionProgramStack.Pop();
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
        public (BinopExpression, Typ) PopTwoAndApplyBinop(BinopExpression.BinopKind binopKind)
        {
            (var right, var expressionType) = Pop();
            (var left, _) = Pop();

            // Object-null checks are represented in CIL using Gt. In this case, binop kind should be 
            // updated to Ne.
            if (binopKind == BinopExpression.BinopKind.Gt && 
                right.Equals(new ConstExpression(new IntRepresentation(0, false, true))))
            {
                binopKind = BinopExpression.BinopKind.Ne;
            }

            return (new BinopExpression(binopKind, left, right), expressionType);
        }

        /// <summary>
        /// Pushes an instruction to be parsed.
        /// </summary>
        /// <param name="instruction">Instruction to be parsed.</param>
        /// <param name="node">The node to set <see cref="PreviousNode"/> to, i.e. the node from 
        /// which to continue the translation from when translating the instruction. See 
        /// <see cref="PreviousNode"/> for more info. If this is not provided or is null, 
        /// <see cref="PreviousNode"/> is not updated.</param>
        public void PushInstruction(Instruction instruction, CfgNode node = null) =>
            InstructionsStack.Push(
                new TranslationSnapshot
                {
                    Instruction = instruction,
                    PreviousNode = node ?? PreviousNode,
                    PreviousStack = ProgramStack.Clone(),
                    NextAvailableTemporaryVariableId = NextAvailableTemporaryVariableId,
                    PreviousReturnedType = PreviousReturnedType,
                });

        /// <summary>
        /// Pops an instruction to be parsed.
        /// </summary>
        /// <returns>The instruction to be parsed and its previous node.</returns>
        public (Instruction, CfgNode) PopInstruction()
        {
            var snapshot = InstructionsStack.Pop();
            PreviousNode = snapshot.PreviousNode;
            CurrentInstruction = snapshot.Instruction;
            ProgramStack = snapshot.PreviousStack;
            NextAvailableTemporaryVariableId = snapshot.NextAvailableTemporaryVariableId;

            var currentSequencePoint =
                Method.DebugInformation.GetSequencePoint(CurrentInstruction);
            if (currentSequencePoint != null)
            {
                var newLocation = Location.FromSequencePoint(currentSequencePoint);
                var previousInstruction = CurrentInstruction.Previous;
                while (newLocation.Line - CurrentLocation.Line >= 100 && previousInstruction != null)
                {
                    currentSequencePoint =
                        Method.DebugInformation.GetSequencePoint(previousInstruction);
                    previousInstruction = previousInstruction.Previous;
                    if (currentSequencePoint == null)
                    {
                        continue;
                    }
                    newLocation = Location.FromSequencePoint(currentSequencePoint);
                }
                CurrentLocation = newLocation;
            }
            if (Log.Debug)
            {
                ParsedInstructions.Add(snapshot.Instruction);
            }
            return (CurrentInstruction, PreviousNode);
        }

        /// <summary>
        /// Gets the state debug information.
        /// </summary>
        /// <param name="invalidObject">The invalid object found within the parser
        /// code.</param>
        /// <returns>String representing the debug information.</returns>
        public string GetStateDebugInformation(object invalidObject)
        {
            if (Log.Debug)
            {
                return $"Invalid value {invalidObject?.ToString()}\n" +
                        "====State information====\n" +
                        ProcDesc.ToString() + "\n" +
                        "====Parsed Instructions====\n" +
                        string.Join(",", ParsedInstructions);
            }
            else
            {
                return string.Empty;
            }
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
            /// The expression type registered by return node at this state.
            /// </summary>
            public Typ PreviousReturnedType;

        }
    }
}