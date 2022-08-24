// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="TypeReference"/>.
    /// </summary>
    public static class TypeReferenceExtensions
    {
        /// <summary>
        /// Extracts the underlying type if the <see cref="TypeReference"/> is a reference and 
        /// otherwise leaves the type unchanged.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static TypeReference GetUnderlyingType(this TypeReference type) =>
            type.IsByReference ? type.GetElementType() : type;

        /// <summary>
        /// Gets the valid full name of the given <see cref="TypeReference"/>.
        /// </summary>
        /// <param name="type">The type reference.</param>
        /// <returns>The valid full name.</returns>
        public static string GetCompatibleFullName(this TypeReference type)
        {
            if (type.IsGenericInstance)
            {
                type = type.GetElementType();
            }
            var fullName = type.FullName.Replace("/", "$");

            var genericParams = type.GenericParameters.Select(p => p.Name);
            var genericParamsString = string.Join(",", genericParams);
            if (genericParamsString == "T")
            {
                genericParamsString = "!0";
            }

            return type.HasGenericParameters ? $"{fullName}<{genericParamsString}>" : fullName;
        }

        /// <summary>
        /// Gets all generic parameters of a given <see cref="TypeReference"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All generic parameters of the type.</returns>
        public static IEnumerable<GenericParameter> GetAllGenericParameters(
            this TypeReference type)
        {
            return type == null ? Enumerable.Empty<GenericParameter>()
                                : type.GenericParameters
                                    .Concat(type.GetElementType().GenericParameters)
                                    .Concat(type.DeclaringType.GetAllGenericParameters());
        }
    }
}
