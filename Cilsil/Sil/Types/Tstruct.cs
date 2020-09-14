// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Structured value type name.
    /// </summary>
    /// <seealso cref="Typ" />
    [JsonObject]
    public class Tstruct : Typ
    {
        /// <value>
        /// Identifier for the structure.
        /// </value>
        [JsonProperty]
        public string StructName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tstruct"/> class.
        /// </summary>
        /// <param name="structName">Identifier for the structure.</param>
        public Tstruct(string structName)
        {
            StructName = structName;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => StructName;

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
            obj is Tstruct tstruct && StructName == tstruct.StructName;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(StructName);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Tstruct left, Tstruct right) =>
            EqualityComparer<Tstruct>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Tstruct left, Tstruct right) => !(left == right);
    }
}
