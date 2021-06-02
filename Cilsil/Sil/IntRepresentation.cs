// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;

namespace Cilsil.Sil
{
    /// <summary>
    /// Wrapper which stores information about an integer.
    /// </summary>
    [JsonObject]
    public class IntRepresentation
    {
        /// <summary>
        /// The integer value.
        /// </summary>
        [JsonProperty]
        public long Value { get; }

        /// <summary>
        /// <c>true</c> if this instance is unsigned; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool Unsigned { get; }

        /// <summary>
        /// <c>true</c> if this instance is a pointer; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsPointer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntRepresentation"/> class.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <param name="unsigned"><c>true</c> if unsigned; otherwise, <c>false</c>.</param>
        /// <param name="isPointer"><c>true</c> if this instance is a pointer; otherwise
        /// <c>false</c>.</param>
        public IntRepresentation(long value, bool unsigned, bool isPointer)
        {
            Value = value;
            Unsigned = unsigned;
            IsPointer = isPointer;
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
            obj is IntRepresentation intRepresentation &&
            Unsigned == intRepresentation.Unsigned &&
            IsPointer == intRepresentation.IsPointer &&
            Value == intRepresentation.Value;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Unsigned, IsPointer, Value);

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => Value == 0 && IsPointer ? "null" : Value.ToString();
    }
}
