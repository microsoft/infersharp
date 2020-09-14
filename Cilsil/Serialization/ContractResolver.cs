// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json.Serialization;

namespace Cilsil.Serialization
{
    /// <summary>
    /// Contract resolver used for JSON serialization.
    /// </summary>
    public static class ContractResolver
    {
        /// <summary>
        /// The snake case property name contract resolver.
        /// </summary>
        public static DefaultContractResolver SnakeCasePropertyNameContractResolver =>
            new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };
    }
}
