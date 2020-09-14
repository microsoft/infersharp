// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="TypeDefinition"/>.
    /// </summary>
    public static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Gets nested types (for example, inner classes) from a given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The nested types.</returns>
        public static IEnumerable<TypeDefinition> GetAllNestedTypes(this TypeDefinition type)
        {
            if (type.HasNestedTypes)
            {
                return type.NestedTypes.SelectMany(t => t.GetAllNestedTypes()).Append(type);
            }
            else
            {
                return new List<TypeDefinition>() { type };
            }
        }
    }
}
