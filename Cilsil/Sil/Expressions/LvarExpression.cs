// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// An expression representing a program variable (i.e. a heap location).
    /// </summary>
    [JsonObject]
    public class LvarExpression : Expression
    {
        /// <summary>
        /// The program variable.
        /// </summary>
        [JsonProperty]
        public ProgramVariable Pvar { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LvarExpression"/> class.
        /// </summary>
        /// <param name="pvar">The program variable.</param>
        public LvarExpression(ProgramVariable pvar)
        {
            Pvar = pvar;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"&{Pvar.ToString()}";

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
            obj is LvarExpression expression &&
            EqualityComparer<ProgramVariable>.Default.Equals(Pvar, expression.Pvar);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Pvar);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(LvarExpression left, LvarExpression right) =>
            EqualityComparer<LvarExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(LvarExpression left, LvarExpression right) =>
            !(left == right);
    }
}
