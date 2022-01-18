// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Expression for representing the size of a variable. For example, used for object and array
    /// creation.
    /// </summary>
    [JsonObject]
    public class SizeofExpression : Expression
    {
        /// <summary>
        /// The type of the variable.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// The subtype.
        /// </summary>
        [JsonProperty]
        public string Kind { get; }

        /// <summary>
        /// The number of elements stored in the array. Note that this value, tracked by symbolic
        /// execution, may differ from the static length, for example when the array is 
        /// over-allocated. 
        /// </summary>
        [JsonProperty]
        public Expression DynamicLength { get; }

        /// <summary>
        /// The number of bytes returned by the evaluation of [sizeof(typ)], if this is statically 
        /// known (is null if it is not).
        /// </summary>
        [JsonProperty]
        public int? NumberBytes { get; }

        /// <summary>
        /// Constructor for a [sizeof(typ)] kind of expression.
        /// </summary>
        /// <param name="type">The type of the expression.</param>
        /// <param name="kind">The subtype.</param>
        /// <param name="dynamicLength">The number of elements stored in the array. Note that this 
        /// value, tracked by symbolic execution, may differ from the static length, for example 
        /// when the array is over-allocated. </param>
        /// <param name="numberBytes">The number of bytes returned by the evaluation of 
        /// [sizeof(typ)], where typ is the underlying data type, if this is statically known (is 
        /// null if it is not).</param>
        public SizeofExpression(Typ type, SizeofExpressionKind kind, Expression dynamicLength = null,
                                int? numberBytes = null)
        {
            Type = type;
            Kind = kind.ToString();
            NumberBytes = numberBytes;
            DynamicLength = dynamicLength;
        }

        /// <summary>
        /// The supported kinds of sizeof expressions. 
        /// </summary>
        public enum SizeofExpressionKind
        {
            /// <summary>
            /// Denotes the current type only.
            /// </summary>
            exact,
            /// <summary>
            /// Denotes the current type and a list of types that are not their subtypes.
            /// </summary>
            instof,
            /// <summary>
            /// Denotes the sizeof expression used for casting.
            /// </summary>
            cast
        };

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"sizeof({Type}, {Kind})";

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
            obj is SizeofExpression expression &&
            EqualityComparer<Typ>.Default.Equals(Type, expression.Type) &&
            Kind == expression.Kind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Type, Kind);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(SizeofExpression left, SizeofExpression right) =>
            EqualityComparer<SizeofExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(SizeofExpression left, SizeofExpression right) =>
            !(left == right);
    }
}
