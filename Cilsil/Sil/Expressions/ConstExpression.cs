// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Constant expression.
    /// </summary>
    [JsonObject]
    public class ConstExpression : Expression
    {
        /// <summary>
        /// The value of the constant.
        /// </summary>
        [JsonProperty]
        public object ConstValue { get; }

        /// <summary>
        /// The type of the constant.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ConstKind Kind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstExpression"/> class.
        /// </summary>
        /// <param name="value">The value of the constant.</param>
        public ConstExpression(object value)
        {
            Kind = GetKindFromValue(value);
            switch (value)
            {
                case float.NegativeInfinity:
                case double.NegativeInfinity:
                    ConstValue = float.MinValue;
                    break;
                case float.PositiveInfinity:
                case double.PositiveInfinity:
                    ConstValue = float.MaxValue;
                    break;
                default:
                    ConstValue = value;
                    break;
            }
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            switch (Kind)
            {
                case ConstKind.Int:
                case ConstKind.Float:
                case ConstKind.Class:
                    return ConstValue.ToString();
                case ConstKind.Fun:
                    return $"_fun {ConstValue}";
                case ConstKind.Str:
                    return $"\"{ConstValue}\"";
            }
            return "Constant";
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
            obj is ConstExpression expression &&
            CompareConstExpressionValue(ConstValue, expression.ConstValue) &&
            Kind == expression.Kind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(ConstValue, Kind);

        /// <summary>
        /// Determines the type of the constant from its value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="ConstKind"/> corresponding to the value.</returns>
        /// <exception cref="ArgumentException">@"Cannot infer constant kind from value of type 
        ///                            {value.GetType().ToString()}")</exception>
        private static ConstKind GetKindFromValue(object value)
        {
            switch (value)
            {
                case IntRepresentation _:
                    return ConstKind.Int;
                case ProcedureName _:
                    return ConstKind.Fun;
                case string _:
                    return ConstKind.Str;
                case float _:
                case double _:
                    return ConstKind.Float;
                case TypeName _:
                    return ConstKind.Class;
                default:
                    throw new ArgumentException(
                        $@"Cannot infer constant kind from value of type 
                           {value.GetType()}");
            }
        }

        /// <summary>
        /// Helper method for comparing values of a pair of <see cref="ConstExpression"/>.
        /// </summary>
        /// <param name="firstValue">The first value.</param>
        /// <param name="secondValue">The second value.</param>
        /// <returns><c>true</c> if the the values are equal, and <c>false</c> otherwise</returns>
        private static bool CompareConstExpressionValue(object firstValue, object secondValue)
        {
            if (GetKindFromValue(firstValue) == GetKindFromValue(secondValue))
            {
                var tolerance = 0.000000001;
                switch (firstValue)
                {
                    case IntRepresentation intRepresentation:
                        return intRepresentation.Equals(secondValue);
                    case ProcedureName procedureName:
                        return procedureName.Equals(secondValue);
                    case string _:
                        return (string)firstValue == (string)secondValue;
                    case float _:
                        return Math.Abs((float)firstValue - (float)secondValue) < tolerance;
                    case double _:
                        return Math.Abs((double)firstValue - (double)secondValue) < tolerance;
                    case TypeName typeName:
                        return typeName.Equals(secondValue);
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Represents the different types of constants.
        /// </summary>
        public enum ConstKind
        {
            /// <summary>
            /// Integer constants.
            /// </summary>
            Int,
            /// <summary>
            /// Method names.
            /// </summary>
            Fun,
            /// <summary>
            /// String constants.
            /// </summary>
            Str,
            /// <summary>
            /// Float constants.
            /// </summary>
            Float,
            /// <summary>
            /// Class constants.
            /// </summary>
            Class
        }
    }
}
