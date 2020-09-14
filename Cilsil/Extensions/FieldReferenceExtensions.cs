// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;

namespace Cilsil.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="FieldReference"/>.
    /// </summary>
    public static class FieldReferenceExtensions
    {
        /// <summary>
        /// Gets the valid full name of the <see cref="FieldReference"/>.
        /// </summary>
        /// <param name="fieldReference">The field reference.</param>
        /// <returns>The valid full name.</returns>
        public static string GetCompatibleFullName(this FieldReference fieldReference) =>
            fieldReference.DeclaringType.GetCompatibleFullName() + "." + fieldReference.Name;
    }
}
