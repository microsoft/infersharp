// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil
{
    /// <summary>
    /// Variable identifier.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Identifier
    {
        // String identifiers for for various common constructs.
        #region
        /// <summary>
        /// String which appears in constructor method names.
        /// </summary>
        public const string ConstructorIdentifier = ".ctor";

        /// <summary>
        /// String for identifying returned variables.
        /// </summary>
        public const string ReturnIdentifier = "return";

        /// <summary>
        /// String used to denote the possessing object for an instance field or method.
        /// </summary>
        public const string ThisIdentifier = "this";

        /// <summary>
        /// Synthetic identifier. Used to represent variables local to finally block.
        /// </summary>
        public const string SyntheticIdentifier = "bcvar";

        /// <summary>
        /// The identifier for the thrown exception upon an exceptional entry into an
        /// exception-handling block.
        /// </summary>
        public const string CatchVarIdentifier = "CatchVar";
        #endregion

        /// <summary>
        /// Mapping of identifier string representation to current stamp 
        /// </summary>
        public static Dictionary<string, int> NameMap { get; } = new Dictionary<string, int>();

        /// <summary>
        /// Mapping of identifier types to their string representations.
        /// </summary>
        public static Dictionary<IdentKind, string> StandardNames { get; } =
            new Dictionary<IdentKind, string>()
            {
                { IdentKind.None, "n" },
                { IdentKind.Primed, "p" },
                { IdentKind.Normal, "n" },
                { IdentKind.Footprint, "f" }
            };

        /// <summary>
        /// The type of identifier.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public IdentKind Kind { get; set; }

        /// <summary>
        /// String used as part of the way to distinguish between identifiers.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// Integer used to distinguish between identifiers which share the same 
        /// <see cref="Name"/>.
        /// </summary>
        [JsonProperty]
        public int Stamp { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (Kind == IdentKind.None)
            {
                return "_";
            }
            return $"{Name}${Stamp}";
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
            obj is Identifier identifier &&
            Kind == identifier.Kind &&
            Name == identifier.Name &&
            Stamp == identifier.Stamp;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Kind, Name, Stamp);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Identifier left, Identifier right) =>
            EqualityComparer<Identifier>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Identifier left, Identifier right) => !(left == right);

        /// <summary>
        /// Different identifier types. 
        /// </summary>
        public enum IdentKind
        {
            /// <summary>
            /// A "null" ident. Used when an instruction requires an id, but the ident should not
            /// be read or reused -- for example, when creating a deref on an object for testing
            /// its non-nullness.
            /// </summary>
            None,
            /// <summary>
            /// Used in Infer's backend, but not in the frontend translations.
            /// </summary>
            Primed,
            /// <summary>
            /// Used in most scenarios requiring an identifer, such as loading heap values into an
            /// identifier, or for object initialization.
            /// </summary>
            Normal,
            /// <summary>
            /// Used in Infer's backend, but not in the frontend translations.
            /// </summary>
            Footprint
        }
    }
}
