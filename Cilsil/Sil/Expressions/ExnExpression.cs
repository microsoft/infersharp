// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Exception expression.
    /// </summary>
    [JsonObject]
    public class ExnExpression : Expression
    {
        /// <summary>
        /// The expression that has exception.
        /// </summary>
        [JsonProperty]
        public Expression Expression { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExnExpression"/> class.
        /// </summary>
        /// <param name="expression">The expression that has exception.</param>
        public ExnExpression(Expression expression)
        {
            Expression = expression;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"EXN {Expression}";

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
            obj is ExnExpression expression &&
            EqualityComparer<Expression>.Default.Equals(Expression, expression.Expression);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Expression);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ExnExpression left, ExnExpression right) =>
            EqualityComparer<ExnExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ExnExpression left, ExnExpression right) =>
            !(left == right);
    }
}
