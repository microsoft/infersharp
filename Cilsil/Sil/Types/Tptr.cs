// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Pointer to a SIL type.
    /// </summary>
    [JsonObject]
    public class Tptr : Typ
    {
        /// <summary>
        /// The kind of pointer.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public PtrKind Kind { get; }

        /// <summary>
        /// The type associated with the underlying expression.
        /// </summary>
        [JsonProperty]
        public Typ Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tptr"/> class.
        /// </summary>
        /// <param name="ptrKind">The kind of pointer.</param>
        /// <param name="type">The type associated with the underlying expression.</param>
        public Tptr(PtrKind ptrKind, Typ type)
        {
            Kind = ptrKind;
            Type = type;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            switch (Kind)
            {
                case PtrKind.Pk_pointer:
                    return $"{Type}*";
                case PtrKind.Pk_reference:
                    return $"&{Type}";
                case PtrKind.Pk_objc_weak:
                    return $"{Type}__weak *";
                case PtrKind.Pk_objc_unsafe_unretained:
                    return $"{Type}__unsafe_unretained *";
                case PtrKind.Pk_objc_autoreleasing:
                    return $"{Type}__autoreleasing";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this 
        /// instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this 
        /// instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; 
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) =>
            obj is Tptr tptr &&
            Kind == tptr.Kind &&
            EqualityComparer<Typ>.Default.Equals(Type, tptr.Type);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Kind, Type);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Tptr left, Tptr right) =>
            EqualityComparer<Tptr>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Tptr left, Tptr right) => !(left == right);

        /// <summary>
        /// Describes the various types of pointers.
        /// </summary>
        public enum PtrKind
        {
            /// <summary>
            /// C/C++, Java, C#, Objc standard/__strong pointer.
            /// </summary>
            Pk_pointer,
            /// <summary>
            /// C++ reference.
            /// </summary>
            Pk_reference,
            /// <summary>
            /// Obj-C __weak pointer.
            /// </summary>
            Pk_objc_weak,
            /// <summary>
            /// Obj-C __unsafe_unretained pointer. 
            /// </summary>
            Pk_objc_unsafe_unretained,
            /// <summary>
            /// Obj-C __autoreleasing pointer.
            /// </summary>
            Pk_objc_autoreleasing
        }
    }

    /// <summary>
    /// Represents boxed value types.
    /// </summary>
    public class BoxedValueType : Tptr
    {
        /// <summary>
        /// The underlying boxed value.
        /// </summary>
        [JsonIgnore]
        public Expression BoxedVal;

        /// <summary>
        /// The type of the boxed value.
        /// </summary>
        [JsonIgnore]
        public Typ BoxedValType;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxedValueType"/> class.
        /// </summary>
        /// <param name="ptrKind">The kind of pointer.</param>
        /// <param name="type">The type of the pointer; specifically for boxed values, this is an
        /// object type.</param>
        /// <param name="boxedValue">The underlying boxed value - i.e. the primitive value.</param>
        /// <param name="boxedValueType">The type of the underlying boxed value.</param>
        public BoxedValueType(PtrKind ptrKind,
                              Typ type,
                              Expression boxedValue,
                              Typ boxedValueType) : base(ptrKind, type)
        {
            BoxedVal = boxedValue;
            BoxedValType = boxedValueType;
        }
    }

    /// <summary>
    /// Type for representing the address of an underlying expression, such as a local variable or
    /// a field.
    /// </summary>
    /// <seealso cref="Tptr" />
    public class Address : Tptr
    {
        /// <summary>
        /// The underlying expression.
        /// </summary>
        [JsonIgnore]
        public Expression Value;

        /// <summary>
        /// The type of address.
        /// </summary>
        [JsonIgnore]
        public ReferenceKind AddressType;

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class.
        /// </summary>
        /// <param name="ptrKind">The kind of pointer.</param>
        /// <param name="type">The type associated with the underlying expression.</param>
        /// <param name="value">The underlying expression.</param>
        /// <param name="referenceKind">The type of address.</param>
        public Address(PtrKind ptrKind,
                       Typ type,
                       Expression value,
                       ReferenceKind referenceKind = ReferenceKind.Generic) : base(ptrKind, type)
        {
            Value = value;
            AddressType = referenceKind;
        }

        /// <summary>
        /// Extracts the expression (for example, variable or field expression) referenced by the
        /// pointer.
        /// </summary>
        /// <param name="state">The program state.</param>
        /// <returns>The instruction used to extract the expression from the pointer as well as the 
        /// identifier in which the expression is stored.</returns>
        public (Load, Identifier) LoadValue(ProgramState state)
        {
            var expressionIdentifier = state.GetIdentifier(Identifier.IdentKind.Normal,
                                                           description: $"{Value}");
            return (new Load(expressionIdentifier, Value, Type, state.CurrentLocation),
                    expressionIdentifier);
        }

        /// <summary>
        /// Used to distinguish between different types of references. 
        /// </summary>
        public enum ReferenceKind
        {
            /// <summary>
            /// Used to denote a reference to a field.
            /// </summary>
            Field,
            /// <summary>
            /// Used to denote a reference to a parameter passed by reference.
            /// </summary>
            Parameter,
            /// <summary>
            /// Used to denote any reference which is none of the other ones.
            /// </summary>
            Generic
        }
    }
}
