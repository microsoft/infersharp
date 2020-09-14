// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Various non-primitive types.
    /// </summary>
    public enum CsuKind
    {
        /// <summary>
        /// Used for describing classes.
        /// </summary>
        Class,
        /// <summary>
        /// Used for describing structs (in a C-language context).
        /// </summary>
        Struct,
        /// <summary>
        /// Used for describing unions (in a C-language context).
        /// </summary>
        Union,
        /// <summary>
        /// Used for describing protocols (in a C-language context).
        /// </summary>
        Protocol
    }
}
