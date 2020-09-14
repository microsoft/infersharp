// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Type cast.
    /// </summary>
    [JsonObject]
    public class CastExpression : Expression
    {
        /// <summary>
        /// The type the expression is being cast to.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// The expression being cast.
        /// </summary>
        [JsonProperty]
        public Expression Expression { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CastExpression"/> class.
        /// </summary>
        /// <param name="type">The type the expression is being cast to.</param>
        /// <param name="expression">The expression being cast.</param>
        public CastExpression(Typ type, Expression expression)
        {
            Type = type;
            Expression = expression;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"({Type}){Expression}";

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
            obj is CastExpression expression &&
            EqualityComparer<Typ>.Default.Equals(Type, expression.Type) &&
            EqualityComparer<Expression>.Default.Equals(Expression, expression.Expression);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Type, Expression);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(CastExpression left, CastExpression right) =>
            EqualityComparer<CastExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(CastExpression left, CastExpression right) =>
            !(left == right);
    }
}
