// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;
using System;
using System.Linq;

namespace Cilsil.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="MethodReference"/>.
    /// </summary>
    public static class MethodReferenceExtensions
    {
        /// <summary>
        /// Gets the valid full method signature associated with the given 
        /// <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="methodReference">The method reference.</param>
        /// <returns>The valid full method signature.</returns>
        public static string GetCompatibleFullName(this MethodReference methodReference)
        {
            var genericParams = methodReference.GenericParameters.Concat(
                methodReference.DeclaringType.GetAllGenericParameters());
            var procName = methodReference.GetFullNameWithoutReturnType().Replace("/", "$");
            procName = genericParams.Aggregate(
                procName,
                (pn, p) => pn.Replace(p.Name, methodReference.Module.TypeSystem.Object.FullName));
            procName = methodReference.ReturnType.GetCompatibleFullName() + " " + procName;
            if (methodReference.DeclaringType != null)
            {
                procName = procName.Replace(methodReference.DeclaringType.FullName,
                                            methodReference.DeclaringType.GetCompatibleFullName());
            }
            return procName;
        }

        /// <summary>
        /// Gets the full name of the method associated with the given 
        /// <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="methodReference">The method reference.</param>
        /// <returns>The full name of the method.</returns>
        /// <exception cref="ApplicationException">Failed to get fullname without return type for: 
        /// {methodReference.FullName}</exception>
        public static string GetFullNameWithoutReturnType(this MethodReference methodReference)
        {
            var tokens = methodReference.FullName.Split(" ");
            if (tokens.Length > 2)
            {
                Log.WriteError($"More than two space-separated tokens in: {methodReference.FullName}", debug: true);
            }
            return string.Join(" ", tokens.Skip(1));
        }
    }
}
