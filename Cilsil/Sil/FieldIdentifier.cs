// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil
{
    /// <summary>
    /// Field identifier.
    /// </summary>
    [JsonObject]
    public class FieldIdentifier
    {
        /// <summary>
        /// The valid full name of the field identifier, typically obtained from a 
        /// <see cref="Mono.Cecil.FieldReference"/> parsed via 
        /// <see cref="Cilsil.Extensions.FieldReferenceExtensions"/>.
        /// </summary>
        [JsonProperty]
        public string FieldName { get; }

        /// <summary>
        /// The field type.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// The field annotations.
        /// </summary>
        [JsonProperty]
        public ItemAnnotation Annotation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldIdentifier"/> class.
        /// </summary>
        /// <param name="name">The valid full name of the field identifier, typically obtained from
        /// a <see cref="Mono.Cecil.FieldReference"/> parsed via 
        /// <see cref="Extensions.FieldReferenceExtensions"/>.</param>
        /// <param name="type">The field type.</param>
        /// <param name="annotation">The field annotations.</param>
        public FieldIdentifier(string name, Typ type, ItemAnnotation annotation = null)
        {
            FieldName = name;
            Type = type;
            Annotation = annotation ?? new ItemAnnotation();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{FieldName}:{Type}";

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; 
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) =>
            obj is FieldIdentifier identifier &&
            FieldName == identifier.FieldName &&
            EqualityComparer<Typ>.Default.Equals(Type, identifier.Type);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(FieldName, Type);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(FieldIdentifier left, FieldIdentifier right) =>
            EqualityComparer<FieldIdentifier>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(FieldIdentifier left, FieldIdentifier right) =>
            !(left == right);
    }
}
