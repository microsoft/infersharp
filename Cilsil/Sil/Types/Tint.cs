// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Integer type.
    /// </summary>
    /// <seealso cref="Typ" />
    [JsonObject]
    public class Tint : Typ
    {
        /// <summary>
        /// The kind of integer.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public IntKind Kind;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tint"/> class.
        /// </summary>
        /// <param name="kind">The kind of integer.</param>
        public Tint(IntKind kind)
        {
            Kind = kind;
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
        public override bool Equals(object obj) => obj is Tint tint && Kind == tint.Kind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Kind);

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => Kind.ToString().ToLower().Substring(1);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Tint left, Tint right) =>
            EqualityComparer<Tint>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Tint left, Tint right) => !(left == right);

        /// <summary>
        /// Describes the various types of integers.
        /// </summary>
        public enum IntKind
        {
            /// <summary>
            /// Character.
            /// </summary>
            IChar,
            /// <summary>
            /// Signed character.
            /// </summary>
            ISChar,
            /// <summary>
            /// Unsigned character.
            /// </summary>
            IUChar,
            /// <summary>
            /// Boolean.
            /// </summary>
            IBool,
            /// <summary>
            /// Integer.
            /// </summary>
            IInt,
            /// <summary>
            /// Unsigned integer.
            /// </summary>
            IUInt,
            /// <summary>
            /// Short.
            /// </summary>
            IShort,
            /// <summary>
            /// Unsigned short.
            /// </summary>
            IUShort,
            /// <summary>
            /// Long.
            /// </summary>
            ILong,
            /// <summary>
            /// Unsigned long.
            /// </summary>
            IULong,
            /// <summary>
            /// Long long (or _int64 on Microsoft Visual C)
            /// </summary>
            ILongLong,
            /// <summary>
            /// Unsigned long long (or unsigned _int64 on Microsoft Visual C)
            /// </summary>
            IULongLong,
            /// <summary>
            /// __int128_t
            /// </summary>
            I128,
            /// <summary>
            /// __uint128_t
            /// </summary>
            IU128
        }
    }
}
