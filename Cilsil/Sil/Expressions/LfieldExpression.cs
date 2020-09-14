// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    ///<summary>
    /// Expression for representing a class or object field.
    ///</summary>
    [JsonObject]
    public class LfieldExpression : Expression
    {
        /// <summary>
        /// The field owner (object for an instance field, class for a static field).
        /// </summary>
        [JsonProperty]
        public Expression Expression { get; }

        /// <summary>
        /// An identifier for the field.
        /// </summary>
        [JsonProperty]
        public FieldIdentifier Identifier { get; }

        /// <summary>
        /// The type associated with the field.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LfieldExpression"/> class.
        /// </summary>
        /// <param name="fieldParentExpression">Represents the field owner (object for an instance
        /// field, class for a static field).</param>
        /// <param name="identifier">An identifier for the field.</param>
        /// <param name="type">The type of the field owner.</param>
        public LfieldExpression(Expression fieldParentExpression,
                                FieldIdentifier identifier,
                                Typ type)
        {
            Expression = fieldParentExpression;
            Identifier = identifier;
            Type = type;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Expression.ToString()}.{Identifier}";

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
            obj is LfieldExpression expression &&
            EqualityComparer<Expression>.Default.Equals(Expression, expression.Expression) &&
            EqualityComparer<FieldIdentifier>.Default.Equals(Identifier, expression.Identifier) &&
            EqualityComparer<Typ>.Default.Equals(Type, expression.Type);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Expression, Identifier, Type);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(LfieldExpression left, LfieldExpression right) =>
            EqualityComparer<LfieldExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(LfieldExpression left, LfieldExpression right) =>
            !(left == right);
    }
}
