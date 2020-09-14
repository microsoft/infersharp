// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System;

namespace Cilsil.Sil.Instructions
{
    /// <summary>
    /// Represents the storage of a value into the heap.
    /// </summary>
    /// <seealso cref="SilInstruction" />
    [JsonObject]
    public class Store : SilInstruction
    {
        /// <summary>
        /// The heap address in which the value is to be stored. For example, this can be 
        /// represented via a variable or array expression.
        /// </summary>
        [JsonProperty]
        public Expression Lvalue { get; }

        /// <summary>
        /// The value to be stored.
        /// </summary>
        [JsonProperty]
        public Expression Rvalue { get; }

        /// <summary>
        /// The type associated with the lvalue.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Store"/> class.
        /// </summary>
        /// <param name="lvalue">The heap address in which the value is to be stored.</param>
        /// <param name="rvalue">The value to be stored.</param>
        /// <param name="type">The type associated with the lvalue.</param>
        /// <param name="location">The location in the program to which this instruction
        /// corresponds.</param>
        public Store(Expression lvalue,
                     Expression rvalue,
                     Typ type,
                     Location location) : base(location)
        {
            if (lvalue == null || rvalue == null || type == null || location == null) 
            {
                throw new ArgumentException("Inputs to this constructor cannot be null.");
            }
            Lvalue = lvalue;
            Rvalue = rvalue;
            Type = type;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"*{Lvalue}:{Type}={Rvalue} [{Location}];";
    }
}
