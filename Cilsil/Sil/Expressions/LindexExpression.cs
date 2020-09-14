// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Represents an array index.
    /// </summary>
    [JsonObject]
    public class LindexExpression : Expression
    {
        /// <summary>
        /// The array whose index is being referenced.
        /// </summary>
        [JsonProperty]
        public Expression Array { get; }

        /// <summary>
        /// The index to reference from the array.
        /// </summary>
        [JsonProperty]
        public Expression Index { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LindexExpression"/> class.
        /// </summary>
        /// <param name="array">The array whose index is being referenced.</param>
        /// <param name="index">The index to reference from the array.</param>
        public LindexExpression(Expression array, Expression index)
        {
            Array = array;
            Index = index;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Array.ToString()}[{Index.ToString()}]";

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
        public override bool Equals(object obj) =>
            obj is LindexExpression expression &&
            EqualityComparer<Expression>.Default.Equals(Array, expression.Array) &&
            EqualityComparer<Expression>.Default.Equals(Index, expression.Index);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Array, Index);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(LindexExpression left, LindexExpression right) =>
            EqualityComparer<LindexExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(LindexExpression left, LindexExpression right) =>
            !(left == right);
    }
}
