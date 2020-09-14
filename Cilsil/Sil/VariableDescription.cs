// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;

namespace Cilsil.Sil
{
    /// <summary>
    /// Information about a variable.
    /// </summary>
    [JsonObject]
    public class VariableDescription
    {
        /// <summary>
        /// The name of the variable.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// The type of the variable.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableDescription"/> class.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="type">The type of the variable.</param>
        public VariableDescription(string name, Typ type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Name}:{Type}";
    }
}
