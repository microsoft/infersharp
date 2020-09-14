// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Type variable. Used in the clang frontend to handle C++ template variables.
    /// </summary>
    /// <seealso cref="Typ" />
    [JsonObject]
    public class Tvar : Typ
    {
        /// <value>
        /// The name of the type variable.
        /// </value>
        [JsonProperty]
        public TypeName TypeName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tvar"/> class.
        /// </summary>
        /// <param name="typeName">The name of the type variable.</param>
        public Tvar(TypeName typeName)
        {
            TypeName = typeName;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => TypeName.ToString();

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
            obj is Tvar tvar && EqualityComparer<TypeName>.Default.Equals(TypeName, tvar.TypeName);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(TypeName);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Tvar left, Tvar right) =>
            EqualityComparer<Tvar>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Tvar left, Tvar right) => !(left == right);
    }
}
