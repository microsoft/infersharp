// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Float type.
    /// </summary>
    /// <seealso cref="Typ" />
    [JsonObject]
    public class Tfloat : Typ
    {
        /// <summary>
        /// The kind of float.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public FloatKind Kind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tfloat"/> class.
        /// </summary>
        /// <param name="kind">The kind of float.</param>
        public Tfloat(FloatKind kind)
        {
            Kind = kind;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => "float";

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
        public override bool Equals(object obj) => obj is Tfloat tfloat && Kind == tfloat.Kind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Kind);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Tfloat left, Tfloat right) =>
            EqualityComparer<Tfloat>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Tfloat left, Tfloat right) => !(left == right);

        /// <summary>
        /// Describes the various types of floats.
        /// </summary>
        public enum FloatKind
        {
            /// <summary>
            /// Single-precision float.
            /// </summary>
            FFloat,
            /// <summary>
            /// Double-precision float.
            /// </summary>
            FDouble,
            /// <summary>
            /// Long double float.
            /// </summary>
            FLongDouble
        }
    }
}