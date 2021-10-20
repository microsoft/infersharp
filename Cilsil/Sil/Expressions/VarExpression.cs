// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// A temporary variable, which does not correspond to an actual variable of the program (and
    /// which is thus not a heap location).
    /// </summary>
    internal class VarExpression : Expression
    {
        /// <summary>
        /// The identifier by which to distinguish the variable.
        /// </summary>
        public Identifier Identifier { get; }

        /// <summary>
        /// If <c>true</c>, this expression is from the load of "this"; otherwise, <c>false</c>.
        /// </summary>
        public readonly bool FromThis;

        /// <summary>
        /// Initializes a new instance of the <see cref="VarExpression"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="fromThis">If <c>true</c>, this variable references "this".</param>
        public VarExpression(Identifier id, bool fromThis = false)
        {
            Identifier = id;
            FromThis = fromThis;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => Identifier.ToString();

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this 
        /// instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this
        /// instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; 
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj is VarExpression expression
            && EqualityComparer<Identifier>.Default.Equals(Identifier, expression.Identifier);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Identifier);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(VarExpression left, VarExpression right) =>
            EqualityComparer<VarExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(VarExpression left, VarExpression right) =>
            !(left == right);
    }
}
