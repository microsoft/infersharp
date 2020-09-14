// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Represents the application of a unary operator to an expression.
    /// </summary>
    [JsonObject]
    public class UnopExpression : Expression
    {
        /// <summary>
        /// The unary operator.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public UnopKind Operator { get; }

        /// <summary>
        /// The expression to which the unary operator is applied.
        /// </summary>
        [JsonProperty]
        public Expression Expression { get; }

        /// <summary>
        /// The type associated with the expression.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnopExpression"/> class.
        /// </summary>
        /// <param name="op">The operator.</param>
        /// <param name="expression">The expression to which the operator is applied.</param>
        /// <param name="type">The type associated with the expression.</param>
        public UnopExpression(UnopKind op, Expression expression, Typ type)
        {
            Operator = op;
            Expression = expression;
            Type = type;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{UnopKindToString(Operator)}({Expression}):{Type}";

        /// <summary>
        /// Converts the unary operator to a string.
        /// </summary>
        /// <param name="kind">The unary operator kind.</param>
        /// <returns></returns>
        public static string UnopKindToString(UnopKind kind)
        {
            switch (kind)
            {
                case UnopKind.Neg:
                    return "-";
                case UnopKind.BNot:
                    return "~";
                case UnopKind.LNot:
                    return "!";
            }
            return "Unop";
        }

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
            obj is UnopExpression expression &&
            Operator == expression.Operator &&
            EqualityComparer<Expression>.Default.Equals(Expression, expression.Expression) &&
            EqualityComparer<Typ>.Default.Equals(Type, expression.Type);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Operator, Expression, Type);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(UnopExpression left, UnopExpression right) =>
            EqualityComparer<UnopExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(UnopExpression left, UnopExpression right) =>
            !(left == right);

        /// <summary>
        /// The different kinds of unary operators.
        /// </summary>
        public enum UnopKind
        {
            /// <summary>
            /// Negation (-)
            /// </summary>
            Neg,
            /// <summary>
            /// Bitwise complement (~)
            /// </summary>
            BNot,
            /// <summary>
            /// Logical not (!)
            /// </summary>
            LNot
        }
    }
}
