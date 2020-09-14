// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// SIL type representing an array.
    /// </summary>
    [JsonObject]
    public class Tarray : Typ
    {
        /// <summary>
        /// The type of the array's elements.
        /// </summary>
        [JsonProperty]
        public Typ ContentType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tarray"/> class.
        /// </summary>
        /// <param name="contentType">Type of the array's elements.</param>
        public Tarray(Typ contentType)
        {
            ContentType = contentType;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{ContentType}[]";

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
            obj is Tarray tarray &&
            EqualityComparer<Typ>.Default.Equals(ContentType, tarray.ContentType);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(ContentType);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Tarray left, Tarray right) =>
            EqualityComparer<Tarray>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Tarray left, Tarray right) => !(left == right);
    }
}
