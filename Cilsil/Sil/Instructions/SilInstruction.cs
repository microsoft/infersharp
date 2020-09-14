// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;

namespace Cilsil.Sil.Instructions
{
    /// <summary>
    /// Base class for instructions of the Smallfoot Intermediate Language (SIL).
    /// </summary>
    [JsonObject]
    public abstract class SilInstruction
    {
        /// <summary>
        /// The source code location of this instruction.
        /// </summary>
        [JsonProperty]
        public Location Location { get; }

        /// <summary>
        /// The system classname of this instruction.
        /// </summary>
        [JsonProperty]
        public string InstructionKind => GetType().Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SilInstruction"/> class.
        /// </summary>
        /// <param name="location">The source code location of this instruction.</param>
        public SilInstruction(Location location)
        {
            Location = location;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => InstructionKind;
    }
}
