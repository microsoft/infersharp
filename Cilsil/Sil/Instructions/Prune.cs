// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cilsil.Sil.Instructions
{
    /// <summary>
    /// Represents a state prune depending on the value of the corresponding boolean condition --
    /// the condition dictates control flow (true branch or false branch).
    /// </summary>
    /// <seealso cref="SilInstruction" />
    [JsonObject]
    public class Prune : SilInstruction
    {
        /// <summary>
        /// Boolean expression; if it evaluates to true, then this branch is evaluated.
        /// </summary>
        [JsonProperty]
        public Expression Condition { get; }

        /// <summary>
        /// True if this is the node corresponding to the true branch; false if this node
        /// corresponds to the false branch.
        /// </summary>
        [JsonProperty]
        public bool TrueBranch { get; }

        /// <summary>
        /// The type of prune instruction. 
        /// </summary>
        [JsonProperty("if_kind")]
        [JsonConverter(typeof(StringEnumConverter))]
        public IfKind Kind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Prune"/> class.
        /// </summary>
        /// <param name="condition">Boolean expression; if it evaluates to true, then this branch 
        /// is evaluated.</param>
        /// <param name="trueBranch">True if this is the node corresponding to the true branch; 
        /// false if this node corresponds to the false branch.</param>
        /// <param name="kind">The type of prune instruction.</param>
        /// <param name="location">The source code location of this instruction.</param>
        public Prune(Expression condition,
                     bool trueBranch,
                     IfKind kind,
                     Location location) : base(location)
        {
            Condition = condition;
            TrueBranch = trueBranch;
            Kind = kind;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $@"PRUNE ({Condition}, {
            TrueBranch.ToString()}) [{Location}];";

        /// <summary>
        /// Types of prune instructions.
        /// </summary>
        public enum IfKind
        {
            /// <summary>
            /// Boolean expressions, and [exp ? exp : exp].
            /// </summary>
            Ik_bexp,
            /// <summary>
            /// "Do while" conditions.
            /// </summary>
            Ik_dowhile,
            /// <summary>
            /// "For" loop conditions.
            /// </summary>
            Ik_for,
            /// <summary>
            /// "If" conditions.
            /// </summary>
            Ik_if,
            /// <summary>
            /// Obtained from logical and as well as logical or.
            /// </summary>
            Ik_andor,
            /// <summary>
            /// "While" conditions.
            /// </summary>
            Ik_while,
            /// <summary>
            /// "Switch" conditions.
            /// </summary>
            Ik_switch
        }
    }
}
