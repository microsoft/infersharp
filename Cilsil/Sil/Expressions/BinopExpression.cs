// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil.Expressions
{
    /// <summary>
    /// Binary operator.
    /// </summary>
    [JsonObject]
    public class BinopExpression : Expression
    {
        /// <summary>
        /// The operator associated with the expression.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public BinopKind Operator { get; }

        /// <summary>
        /// The left operand.
        /// </summary>
        [JsonProperty]
        public Expression Left { get; }

        /// <summary>
        /// The right operand.
        /// </summary>
        [JsonProperty]
        public Expression Right { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinopExpression"/> class.
        /// </summary>
        /// <param name="op">The operator associated with the expression.</param>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        public BinopExpression(BinopKind op, Expression left, Expression right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Determines if the binary expression is a null check.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if is a null check; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNullCheck() => 
            (Operator == BinopKind.Ne || Operator == BinopKind.Eq) &&
            (Right is ConstExpression constExpr) &&
            constExpr.Equals(new ConstExpression(new IntRepresentation(0, false, true)));
        
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() =>
            $"{Left} {BinopKindToString(Operator)} {Right}";

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
            obj is BinopExpression expression &&
            Operator == expression.Operator &&
            EqualityComparer<Expression>.Default.Equals(Left, expression.Left) &&
            EqualityComparer<Expression>.Default.Equals(Right, expression.Right);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(Operator, Left, Right);

        /// <summary>
        /// Converts binary operators to their string representation.
        /// </summary>
        /// <param name="kind">The binary operator.</param>
        /// <returns></returns>
        public static string BinopKindToString(BinopKind kind)
        {
            switch (kind)
            {
                case BinopKind.PlusA:
                case BinopKind.PlusPI:
                    return "+";
                case BinopKind.MinusA:
                case BinopKind.MinusPI:
                case BinopKind.MinusPP:
                    return "-";
                case BinopKind.Mult:
                    return "*";
                case BinopKind.Div:
                    return "/";
                case BinopKind.Mod:
                    return "%";
                case BinopKind.Shiftlt:
                    return "<<";
                case BinopKind.Shiftrt:
                    return ">>";
                case BinopKind.Lt:
                    return "<";
                case BinopKind.Gt:
                    return ">";
                case BinopKind.Le:
                    return "<=";
                case BinopKind.Ge:
                    return ">=";
                case BinopKind.Eq:
                    return "==";
                case BinopKind.Ne:
                    return "!=";
                case BinopKind.BAnd:
                    return "&";
                case BinopKind.BXor:
                    return "^";
                case BinopKind.BOr:
                    return "|";
                case BinopKind.LAnd:
                    return "&&";
                case BinopKind.LOr:
                    return "||";
                case BinopKind.PtrFld:
                    return "->";
            }
            return "Binop";
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(BinopExpression left, BinopExpression right) =>
            EqualityComparer<BinopExpression>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(BinopExpression left, BinopExpression right) =>
            !(left == right);

        /// <summary>
        /// Enum for representing binary operators.
        /// </summary>
        public enum BinopKind
        {
            /// <summary>
            /// Arithmetic addition.
            /// </summary>
            PlusA,
            /// <summary>
            /// Pointer integer shifts, addition (pointer + integer).
            /// </summary>
            PlusPI,
            /// <summary>
            /// Arithmetic subtraction.
            /// </summary>
            MinusA,
            /// <summary>
            /// Pointer integer shifts, subtraction (pointer - integer).
            /// </summary>
            MinusPI,
            /// <summary>
            /// Pointer subtraction (pointer - pointer).
            /// </summary>
            MinusPP,
            /// <summary>
            /// Arithmetic multiplication.
            /// </summary>
            Mult,
            /// <summary>
            /// Arithmetic division.
            /// </summary>
            Div,
            /// <summary>
            /// Modular arithmetic.
            /// </summary>
            Mod,
            /// <summary>
            /// Bitwise left shift.
            /// </summary>
            Shiftlt,
            /// <summary>
            /// Bitwise right shift.
            /// </summary>
            Shiftrt,
            /// <summary>
            /// Arithmetic less than.
            /// </summary>
            Lt,
            /// <summary>
            /// Arithmetic greater than.
            /// </summary>
            Gt,
            /// <summary>
            /// Arithmetic less than or equal to.
            /// </summary>
            Le,
            /// <summary>
            /// Arithmetic greater than or equal to.
            /// </summary>
            Ge,
            /// <summary>
            /// == (arithmetic comparison).
            /// </summary>
            Eq,
            /// <summary>
            /// != (arithmetic comparison).
            /// </summary>
            Ne,
            /// <summary>
            /// Bitwise and.
            /// </summary>
            BAnd,
            /// <summary>
            /// Bitwise exclusive-or.
            /// </summary>
            BXor,
            /// <summary>
            /// Bitwise inclusive-or.
            /// </summary>
            BOr,
            /// <summary>
            /// Logical and. Does not always evaluate both operands.
            /// </summary>
            LAnd,
            /// Logical or. Does not always evaluate both operands.
            LOr,
            /// <summary>
            /// Field offset via pointer to field: takes the address of a csu and a Cptr_to_fld
            /// constant to form an Lfield expression (see prop.ml).
            /// </summary>
            PtrFld
        }
    }
}
