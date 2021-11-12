// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Instructions;
using Cilsil.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Sil
{
    /// <summary>
    /// A vertex in a CFG.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class CfgNode
    {
        /// <value>
        /// Identifier for the node.
        /// </value>
        [JsonProperty("nd_id")]
        public long Id { get; set; }

        /// <summary>
        /// The temporary variables of this node.
        /// </summary>
        [JsonProperty("nd_temps")]
        public List<Identifier> TemporaryVariables { get; set; }

        /// <summary>
        /// Dead program variable before executing the instruction.
        /// </summary>
        [JsonProperty]
        public List<ProgramVariable> NdDeadPvarsBefore { get; set; }

        /// <summary>
        /// Dead program variable after executing the instruction.
        /// </summary>
        [JsonProperty]
        public List<ProgramVariable> NdDeadPvarsAfter { get; set; }

        /// <summary>
        /// Exception nodes for this node; control flow goes to these if an exception is raised.
        /// </summary>
        public List<CfgNode> ExceptionNodes { get; set; }

        /// <summary>
        /// The identifiers of the exception nodes for this node.
        /// </summary>
        [JsonProperty("nd_exn_ids")]
        public IEnumerable<long> ExceptionNodesId => ExceptionNodes.Select(n => n.Id);

        /// <summary>
        /// The SIL instructions of this node.
        /// </summary>
        [JsonProperty("nd_instrs")]
        public List<SilInstruction> Instructions { get; set; }

        /// <summary>
        /// The source code location of the instruction at which this node is created.
        /// </summary>
        [JsonProperty("nd_loc")]
        public Location Location { get; set; }

        /// <summary>
        /// The predecessors of this node.
        /// </summary>
        public List<CfgNode> Predecessors { get; set; }

        /// <summary>
        /// The identifiers of the predecessors of this node.
        /// </summary>
        [JsonProperty("nd_pred_ids")]
        public IEnumerable<long> PredecessorIds => Predecessors.Select(p => p.Id);

        /// <summary>
        /// The successors of this node.
        /// </summary>
        public List<CfgNode> Successors { get; set; }

        /// <summary>
        /// Gets or sets the handler end offset.
        /// </summary>
        /// <value>
        /// The handler end offset.
        /// </value>
        public int BlockEndOffset { get; set; }

        /// <summary>
        /// The identifiers of the successors of this node.
        /// </summary>
        [JsonProperty("nd_succ_ids")]
        public IEnumerable<long> SuccessorIds => Successors.Select(s => s.Id);

        /// <summary>
        /// The procedure description associated with this node. The setter will add this node to
        /// it, if it is non-null.
        /// </summary>
        public ProcedureDescription Proc
        {
            get => _Proc;
            set
            {
                _Proc = value;
                if (_Proc != null && !_Proc.Nodes.Contains(this))
                {
                    _Proc.Nodes.Add(this);
                }
            }
        }

        /// <summary>
        /// The identifier for the procedure.
        /// </summary>
        [JsonProperty("nd_proc_id")]
        public long ProcId => Proc.PdId;

        /// <summary>
        /// The runtime <see cref="System.Type"/> associated with this instance.
        /// </summary>
        [JsonProperty("nd_kind")]
        public string NodeKind => GetType().Name;

        private static long NextId => _NextId++;

        private static long _NextId = 0;

        private ProcedureDescription _Proc;

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgNode"/> class.
        /// </summary>
        /// <param name="location">The source code location to which this node refers.</param>
        /// <param name="proc">The procedure description associated with this node.</param>
        protected CfgNode(Location location, ProcedureDescription proc = null)
        {
            Id = NextId;

            TemporaryVariables = new List<Identifier>();
            NdDeadPvarsBefore = new List<ProgramVariable>();
            NdDeadPvarsAfter = new List<ProgramVariable>();
            ExceptionNodes = new List<CfgNode>();
            Instructions = new List<SilInstruction>();
            Predecessors = new List<CfgNode>();
            Successors = new List<CfgNode>();

            Location = location;
            Proc = proc;

            BlockEndOffset = MethodExceptionHandlers.DefaultHandlerEndOffset;
        }

        /// <summary>
        /// Converts to string, inserting the runtime System.Type of this instance into the output.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => ToStringCustom(NodeKind);

        /// <summary>
        /// Converts to string; inserts the input string into the output.
        /// </summary>
        /// <param name="customKind">Kind of the custom.</param>
        /// <returns>
        /// A <see cref="string" /> that represents this instance
        /// </returns>
        protected string ToStringCustom(string customKind)
        {
            var s = new List<string>
            {
                $"node{Id} " +
                $"preds: {string.Join(" ", PredecessorIds)} " +
                $"succs: {string.Join(" ", SuccessorIds)} " +
                $"exn: {string.Join(" ", ExceptionNodesId)} {customKind}"
            };
            s.AddRange(Instructions.Select(i => i.ToString()));
            return string.Join("\n", s);
        }
    }

    /// <summary>
    /// Source node of the CFG.
    /// </summary>
    /// <seealso cref="CfgNode" />
    [JsonObject(MemberSerialization.OptIn)]
    public class StartNode : CfgNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartNode"/> class.
        /// </summary>
        /// <param name="location">The beginning of the method (the first of the Mono.Cecil
        /// sequence points).</param>
        /// <param name="proc">The procedure description associated with this node.</param>
        public StartNode(Location location, ProcedureDescription proc = null)
            : base(location, proc)
        {
        }
    }

    /// <summary>
    /// Sink node of the CFG. 
    /// </summary>
    /// <seealso cref="Cilsil.Sil.CfgNode" />
    [JsonObject(MemberSerialization.OptIn)]
    public class ExitNode : CfgNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExitNode"/> class.
        /// </summary>
        /// <param name="location">The end of the method (the last of the Mono.Cecil
        /// sequence points).</param>
        /// <param name="proc">The procedure description associated with this node.</param>
        public ExitNode(Location location, ProcedureDescription proc = null) : base(location, proc)
        {
        }
    }

    /// <summary>
    /// Stores SIL statements.
    /// </summary>
    /// <seealso cref="CfgNode" />
    [JsonObject(MemberSerialization.OptIn)]
    public class StatementNode : CfgNode
    {
        /// <summary>
        /// The kind of statement node.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public StatementNodeKind StmtNodeKind { get; }

        /// <summary>
        /// Used as an argument for certain node types; this is null if the node type does not
        /// require this argument.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StmtNodeComment { get; }

        /// <summary>
        /// Used for nodes which contain a statement.
        /// </summary>
        /// <param name="location">The program location to which this corresponds.</param>
        /// <param name="kind">The SIL statement type of this node.</param>
        /// <param name="comment">Certain node types take arguments; for example, for constructor
        /// calls, this can be the constructor name.</param>
        /// <param name="proc">The procedure description with which this node is
        /// associated.</param>
        public StatementNode(Location location,
                             StatementNodeKind kind,
                             ProcedureDescription proc,
                             string comment = null)
            : base(location, proc)
        {
            StmtNodeKind = kind;
            StmtNodeComment = comment;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => ToStringCustom(StmtNodeKind.ToString());

        /// <summary>
        /// Statement node types.
        /// </summary>
        public enum StatementNodeKind
        {
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            AssertionFailure,
            /// <summary>
            /// Used as an extra node between a join node and the exit node; otherwise, nullify and
            /// abstract instructions cannot be added after a conditional.
            /// </summary>
            BetweenJoinAndExit,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            BinaryConditionalStmtInit,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            BinaryOperatorStmt,
            /// <summary>
            /// Used for nodes that involve method calls (for example, object initialization and
            /// method invocations).
            /// </summary>
            Call,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            CallObjCNew,
            /// <summary>
            /// Used for handling class cast exceptions.
            /// </summary>
            ClassCastException,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            ConditionalStmtBranch,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            ConstructorInit,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            CXXDynamicCast,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            CXXNewExpr,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            CXXStdInitializerListExpr,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            CXXTypeidExpr,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            DeclStmt,
            /// <summary>
            /// Not currently used in any frontend.
            /// </summary>
            DefineBody,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            Destruction,
            /// <summary>
            /// Used in exception-handling prune nodes.
            /// </summary>
            ExceptionHandler,
            /// <summary>
            /// Not currently used in any frontend.
            /// </summary>
            ExceptionsSink,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            FallbackNode,
            /// <summary>
            /// Used for handling finally in exception-handling.
            /// </summary>
            FinallyBranch,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            GCCAsmStmt,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            GenericSelectionExpr,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            IfStmtBranch,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            InitializeDynamicArrayLength,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            InitListExp,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            MessageCall,
            /// <summary>
            /// Used for storing instructions which correspond to a method body.
            /// </summary>
            MethodBody,
            /// <summary>
            /// Used in the Java frontend to translate the monitorenter JVM instruction, which
            /// locks a given object.
            /// </summary>
            MonitorEnter,
            /// <summary>
            /// Used in the Java frontend to translate the monitorexit JVM instruction, which
            /// unlocks a given object.
            /// </summary>
            MonitorExit,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            ObjCCPPThrow,
            /// <summary>
            /// Used in the Java frontend for representing array out-of-bound instructions.
            /// </summary>
            OutOfBound,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            ReturnStmt,
            /// <summary>
            /// Currently only used in the clang frontend. Represents goto instructions.
            /// </summary>
            Skip,
            /// <summary>
            /// Currently only used in the clang frontend. Used for translating the body of a 
            /// switch statement.
            /// </summary>
            SwitchStmt,
            /// <summary>
            /// Currently only used in the Java frontend. Used for assuming that the "this" object
            /// is not null in Java bytecode check assertions.
            /// </summary>
            ThisNotNull,
            /// <summary>
            /// Currently only used in the Java frontend. Used for translating throw in exception
            /// handling.
            /// </summary>
            Throw,
            /// <summary>
            /// Currently only used in the Java frontend. Used for creating null pointer exceptions
            /// when a given expression is found to be null.
            /// </summary>
            ThrowNPE,
            /// <summary>
            /// Currently only used in the clang frontend.
            /// </summary>
            UnaryOperator
        }
    }

    /// <summary>
    /// One of two node types for handling branch instructions. Used when instruction is part of a
    /// loop.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class JoinNode : CfgNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinNode"/> class.
        /// </summary>
        /// <param name="location">The source code location to which this node refers.</param>
        /// <param name="proc">The procedure description associated with this node.</param>
        public JoinNode(Location location, ProcedureDescription proc = null) : base(location, proc)
        {
        }
    }

    /// <summary>
    /// One of two node types for handling branch instructions. Used when instruction is not part 
    /// of a loop.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class PruneNode : CfgNode
    {
        /// <summary>
        /// <c>true</c> if control should flow to this node when the boolean expression from which
        /// this node is translated evaluates to true. <c>false</c> if control should not flow to
        /// this node.
        /// </summary>
        [JsonProperty]
        public bool TrueBranch { get; }

        /// <summary>
        /// The type of Prune node.
        /// </summary>
        [JsonProperty("prune_node_kind")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PruneNodeKind Kind { get; }

        /// <summary>
        /// The type of Prune instruction associated with this node.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Prune.IfKind IfKind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgNode"/> class.
        /// </summary>
        /// <param name="location">The program location to which this corresponds.</param>
        /// <param name="trueBranch"><c>true</c> if control should flow to this node when the 
        /// boolean expression from which this node is translated evaluates to true. <c>false</c> 
        /// if control should not flow to this node.</param>
        /// <param name="kind">The type of Prune node.</param>
        /// <param name="ifKind">The type of Prune instruction associated with this node.</param>
        /// <param name="proc">The procedure description with which this node is
        /// associated.</param>
        public PruneNode(Location location,
                         bool trueBranch,
                         PruneNodeKind kind,
                         Prune.IfKind ifKind,
                         ProcedureDescription proc = null) : base(location, proc)
        {
            TrueBranch = trueBranch;
            Kind = kind;
            IfKind = ifKind;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => ToStringCustom(Kind.ToString());

        /// <summary>
        /// Types of Prune nodes.
        /// </summary>
        public enum PruneNodeKind
        {
            /// <summary>
            /// Exception handling prune nodes. 
            /// </summary>
            ExceptionHandler,
            /// <summary>
            /// Control flows to this node if the boolean expression corresponding to this node
            /// evaluates to true.
            /// </summary>
            TrueBranch,
            /// <summary>
            /// Control flows to this node if the boolean expression corresponding to this node
            /// evaluates to false.
            /// </summary>
            FalseBranch,
            /// <summary>
            /// Used in the Java frontend only, for checking if a given index is within a given
            /// array's bounds.
            /// </summary>
            InBound,
            /// <summary>
            /// Used in the Java frontend only, for checking if a given object can be casted to a
            /// given object type.
            /// </summary>
            IsInstance,
            /// <summary>
            /// For storing general method body control-flow branching instructions.
            /// </summary>
            MethodBody,
            /// <summary>
            /// Used in the Java frontend only, for checking if a given expression is a null
            /// pointer.
            /// </summary>
            NotNull
        }
    }

    /// <summary>
    /// One of two node types for handling branch instructions. Used when instruction is not part 
    /// of a loop.
    /// </summary>
    /// <seealso cref="CfgNode" />
    [JsonObject]
    public class SkipNode : CfgNode
    {
        /// <summary>
        /// Describes the context in which this node is being used.
        /// </summary>
        [JsonProperty]
        public string SkipNodeComment { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipNode"/> class.
        /// </summary>
        /// <param name="location">The source code location to which this node refers.</param>
        /// <param name="comment">Describes the context in which this node is being used.</param>
        /// <param name="proc">The procedure description associated with this node.</param>
        public SkipNode(Location location,
                        string comment,
                        ProcedureDescription proc = null) : base(location, proc)
        {
            SkipNodeComment = comment;
        }
    }
}
