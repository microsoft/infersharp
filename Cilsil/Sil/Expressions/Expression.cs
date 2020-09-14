// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Represents program expressions.
    /// </summary>
    [JsonObject]
    public abstract class Expression
    {
        /// <summary>
        /// Gets the name of the associated System.Type.
        /// </summary>
        [JsonProperty]
        public string ExprKind => GetType().Name;
    }
}
