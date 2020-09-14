// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Sil.Instructions
{
    /// <summary>
    /// Represents method calls.
    /// </summary>
    /// <seealso cref="SilInstruction" />
    [JsonObject]
    public class Call : SilInstruction
    {
        /// <summary>
        /// Identifier for the output returned by the method call.
        /// </summary>
        [JsonProperty]
        public Identifier ReturnVar { get; }

        /// <summary>
        /// The type of the output returned by the method call.
        /// </summary>
        [JsonProperty]
        public Typ ReturnType { get; }

        /// <summary>
        /// Expression representing the method call.
        /// </summary>
        [JsonProperty]
        public Expression FunctionExpression { get; }

        /// <summary>
        /// Arguments of the method being called.
        /// </summary>
        [JsonProperty]
        public IEnumerable<CallArg> Args { get; }

        /// <summary>
        /// Flags which describe properties about the method.
        /// </summary>
        [JsonProperty]
        public CallFlags Flags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Call"/> class.
        /// </summary>
        /// <param name="returnId">Identifier for the output returned by the method call.</param>
        /// <param name="returnType">The type of the output returned by the method call.</param>
        /// <param name="functionExpression">Expression representing the method call.</param>
        /// <param name="args">Arguments of the method being called.</param>
        /// <param name="flags">Flags which describe properties about the method.</param>
        /// <param name="location">The program location in which the method is being 
        /// called.</param>
        public Call(Identifier returnId,
                    Typ returnType,
                    Expression functionExpression,
                    IEnumerable<CallArg> args,
                    CallFlags flags,
                    Location location) : base(location)
        {
            ReturnVar = returnId;
            ReturnType = returnType;
            FunctionExpression = functionExpression;
            Args = args;
            Flags = flags;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var s = $"{ReturnVar}:{ReturnType}={FunctionExpression} " +
                    $"({string.Join(", ", Args.Select(a => a.ToString()))})";
            if (Flags.IsVirtual)
            {
                s += " virtual ";
            }
            s += $" [{Location}];";
            return s;
        }

        /// <summary>
        /// Represents an argument of a method call.
        /// </summary>
        public class CallArg
        {
            /// <summary>
            /// The argument expression.
            /// </summary>
            public Expression Expression { get; }

            /// <summary>
            /// The argument type.
            /// </summary>
            public Typ Type { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CallArg"/> class.
            /// </summary>
            /// <param name="expression">The argument expression.</param>
            /// <param name="type">The argument type.</param>
            public CallArg(Expression expression, Typ type)
            {
                Expression = expression;
                Type = type;
            }

            /// <summary>
            /// Converts to string.
            /// </summary>
            /// <returns>
            /// A <see cref="string" /> that represents this instance.
            /// </returns>
            public override string ToString() => $"{Expression}:{Type}";
        }

        /// <summary>
        /// Flags which describe properties about the method.
        /// </summary>
        [JsonObject]
        public class CallFlags
        {
            /// <summary>
            /// Gets a value indicating whether this instance is virtual.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is virtual; otherwise, <c>false</c>.
            /// </value>
            [JsonProperty("cf_virtual")]
            public bool IsVirtual { get; }

            /// <summary>
            /// Gets a value indicating whether called method has a return value.
            /// </summary>
            /// <value>
            ///   <c>true</c> if called method has no return value; otherwise, <c>false</c>.
            /// </value>
            [JsonProperty("cf_noreturn")]
            public bool NoReturn { get; }

            /// <summary>
            /// Gets a value indicating whether this instance is an objective c block.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is objective c block; otherwise, <c>false</c>.
            /// </value>
            [JsonProperty("cf_is_objc_block")]
            public bool IsObjCBlock { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CallFlags"/> class.
            /// </summary>
            /// <param name="isVirtual">True if method is virtual, false otherwise.</param>
            /// <param name="noReturn">True if called method has no return value, false 
            /// otherwise.</param>
            /// <param name="isObjCBlock">True if called method is an objective c block, false
            /// otherwise.</param>
            public CallFlags(bool isVirtual = false,
                             bool noReturn = false,
                             bool isObjCBlock = false)
            {
                IsVirtual = isVirtual;
                NoReturn = noReturn;
                IsObjCBlock = isObjCBlock;
            }
        }
    }
}
