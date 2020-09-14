// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Name of a SIL type.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class TypeName
    {
        /// <summary>
        /// The name of the type.
        /// </summary>
        [JsonProperty]
        public string Name { get; protected set; }

        /// <summary>
        /// The runtime System.Type of this instance.
        /// </summary>
        [JsonProperty]
        public string TypeNameKind => GetType().Name;

        /// <summary>
        /// Constructs a CSU type name from a Mono.Cecil TypeDefinition.
        /// </summary>
        /// <param name="typeReference">The Mono.Cecil TypeDefinition.</param>
        /// <returns></returns>
        public static TypeName FromTypeReference(TypeDefinition typeReference) =>
            new CsuTypeName(CsuKind.Class, typeReference.GetCompatibleFullName());

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
            obj is TypeName name && Name == name.Name &&
            TypeNameKind == name.TypeNameKind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Name, TypeNameKind);
    }

    /// <summary>
    /// Used for describing the names of CSU (class, struct, union) types.
    /// </summary>
    /// <seealso cref="TypeName" />
    [JsonObject(MemberSerialization.OptIn)]
    public class CsuTypeName : TypeName
    {
        /// <summary>
        /// The kind of the CSU.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public CsuKind CsuKind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsuTypeName"/> class.
        /// </summary>
        /// <param name="kind">The kind of the CSU type.</param>
        /// <param name="name">The name of the CSU type.</param>
        public CsuTypeName(CsuKind kind, string name)
        {
            CsuKind = kind;
            Name = name;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{CsuKind.ToString()} {Name}";

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
            obj is CsuTypeName name &&
            base.Equals(obj) &&
            CsuKind == name.CsuKind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), CsuKind);
    }
}
