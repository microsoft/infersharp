// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System;

namespace Cilsil.Sil.Instructions
{
    /// <summary>
    /// Represents the loading of a value from the heap into a temporary variable.
    /// </summary>
    /// <seealso cref="SilInstruction" />
    [JsonObject]
    public class Load : SilInstruction
    {
        /// <summary>
        /// The identifier of the temporary variable used to store the expression.
        /// </summary>
        [JsonProperty]
        public Identifier Identifier { get; }

        /// <summary>
        /// The heap address whose value is to be loaded into the identifier.
        /// </summary>
        [JsonProperty]
        public Expression Expression { get; }

        /// <summary>
        /// The type associated with the lvalue.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Load"/> class.
        /// </summary>
        /// <param name="identifierAssignedTo">The identifier to store the expression.</param>
        /// <param name="lvalue">The heap address from which to load the value.</param>
        /// <param name="type">The type associated with the lvalue.</param>
        /// <param name="location">The location in the program to which this instruction
        /// corresponds.</param>
        public Load(Identifier identifierAssignedTo,
                    Expression lvalue,
                    Typ type,
                    Location location) : base(location)
        {
            if (identifierAssignedTo == null || lvalue == null || type == null || location == null)
            {
                throw new ArgumentException("Inputs to this method cannot be null.");
            }
            Identifier = identifierAssignedTo;
            Expression = lvalue;
            Type = type;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Identifier}=*{Expression}:{Type} [{Location}];";
    }
}
