// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Mono.Cecil;
using Newtonsoft.Json;
using System;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Base class for all SIL types.
    /// </summary>
    [JsonObject]
    public abstract class Typ
    {
        /// <summary>
        /// Gets the name of the associated System.Type.
        /// </summary>
        [JsonProperty]
        public virtual string TypeKind => this is Tptr ? "Tptr" : GetType().Name;

        /// <summary>
        /// <c>true</c> if this references the type of the expression produced during the
        /// translation of the isinst instruction, <c>false</c> otherwise. 
        /// </summary>
        public bool IsInstReturnType = false;

        /// <summary>
        /// Strips away a <see cref="Tptr"/> to return the underlying type.
        /// </summary>
        /// <returns>The underlying type.</returns>
        public Typ StripPointer() => this is Tptr tptr ? tptr.Type : this;

        /// <summary>
        /// Creates a SIL type from a <see cref="TypeReference"/>, wrapped in a Tptr if the input
        /// is non-primitive and non-void (in those cases, it returns the corresponding SIL type
        /// without the Tptr wrapper).
        /// </summary>
        /// <param name="typeReference">The type reference.</param>
        /// <returns>The corresponding SIL type, wrapped in a Tptr.</returns>
        public static Typ FromTypeReference(TypeReference typeReference)
        {
            if (typeReference.IsByReference)
            {
                return new Tptr(Tptr.PtrKind.Pk_pointer,
                                FromTypeReference(typeReference.GetUnderlyingType()));
            }
            else
            {
                if (typeReference.IsPrimitive)
                {
                    return FromTypeReferenceNoPointer(typeReference);
                }
                else if (typeReference.FullName == "System.Void")
                {
                    return new Tvoid();
                }
                else
                {
                    return new Tptr(Tptr.PtrKind.Pk_pointer,
                                    FromTypeReferenceNoPointer(typeReference));
                }
            }
        }

        /// <summary>
        /// Creates a SIL Type from a Mono.Cecil TypeReference, without an associated Tptr.
        /// </summary>
        /// <param name="typeReference">The type reference.</param>
        /// <returns>The corresponding SIL type.</returns>
        /// <exception cref="ApplicationException">Unknown typeReference 
        /// {typeReference.FullName}</exception>
        public static Typ FromTypeReferenceNoPointer(TypeReference typeReference)
        {
            if (typeReference.IsPrimitive)
            {
                switch (typeReference.FullName)
                {
                    case "System.Int32":
                    case "Int32":
                        return new Tint(Tint.IntKind.IInt);
                    case "System.UInt32":
                    case "UInt32":
                        return new Tint(Tint.IntKind.IUInt);
                    case "Bool":
                    case "System.Boolean":
                    case "Boolean":
                        return new Tint(Tint.IntKind.IBool);
                    case "System.SByte":
                    case "SByte":
                        return new Tint(Tint.IntKind.ISChar);
                    case "System.Byte":
                    case "Byte":
                        return new Tint(Tint.IntKind.IUChar);
                    case "System.Char":
                    case "Char":
                        return new Tint(Tint.IntKind.IChar);
                    case "System.Double":
                    case "Double":
                        return new Tfloat(Tfloat.FloatKind.FDouble);
                    case "Float":
                    case "System.Float":
                    case "Single":
                    case "System.Single":
                        return new Tfloat(Tfloat.FloatKind.FFloat);
                    case "Long":
                    case "System.Int64":
                    case "Int64":
                        return new Tint(Tint.IntKind.ILong);
                    case "ULong":
                    case "System.UInt64":
                    case "UInt64":
                        return new Tint(Tint.IntKind.IULong);
                    case "Short":
                    case "System.Int16":
                    case "Int16":
                        return new Tint(Tint.IntKind.IShort);
                    case "System.UInt16":
                    case "UInt16":
                        return new Tint(Tint.IntKind.IUShort);
                    case "System.IntPtr":
                    case "IntPtr":
                        return new Tptr(Tptr.PtrKind.Pk_pointer, new Tint(Tint.IntKind.IInt));
                    case "System.UIntPtr":
                        return new Tptr(Tptr.PtrKind.Pk_pointer, new Tint(Tint.IntKind.IUInt));
                    default:
                        throw new ApplicationException(
                            $"Unknown typeReference {typeReference.FullName}");
                }
            }
            else if (typeReference.FullName == "System.Void")
            {
                return new Tvoid();
            }
            else if (typeReference.IsArray)
            {
                var elementType = FromTypeReference(typeReference.GetElementType());
                return new Tarray(elementType);
            }
            else if (typeReference.IsGenericParameter)
            {
                return new Tstruct("System.Object");
            }
            else
            {
                switch (typeReference.FullName)
                {
                    case "System.Boolean":
                        return new Tint(Tint.IntKind.IBool);
                    case "System.Void":
                        return new Tvoid();
                    default:
                        return new Tstruct(typeReference.GetCompatibleFullName());
                }
            }
        }
    }
}
