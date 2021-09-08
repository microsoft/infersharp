// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Mono.Cecil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Sil
{
    /// <summary>
    /// Method signature information.
    /// </summary>
    [JsonObject]
    public class ProcedureName
    {
        // Names for common built-in procedures. Their counterparts in the Infer source code are
        // located at /InferSharp/infer/infer/src/IR/BuiltinDecl.ml.
        #region            
        /// <summary>
        /// Class name for built-in objects.
        /// </summary>
        private const string BuiltInClassName = "BuiltIn";

        /// <summary>
        /// Standard procedure name for object initialization calls, interpreted specially by the
        /// backend.
        /// </summary>
        public static readonly ProcedureName BuiltIn__new =
            new ProcedureName("__new",
                              new List<string>(),
                              BuiltInClassName,
                              string.Empty,
                              true);

        /// <summary>
        /// Standard procedure name for array initialization calls, interpreted specially by the
        /// backend.
        /// </summary>
        public static readonly ProcedureName BuiltIn__new_array =
            new ProcedureName("__new_array",
                              new List<string>(),
                              BuiltInClassName,
                              string.Empty,
                              true);

        /// <summary>
        /// Standard procedure name used in exception-handling translation, interpreted specially
        /// by the backend.
        /// </summary>
        public static readonly ProcedureName BuiltIn__unwrap_exception =
            new ProcedureName("__unwrap_exception",
                              new List<string>(),
                              BuiltInClassName,
                              string.Empty,
                              true);

        /// <summary>
        /// Standard procedure name used for translating type-checking bytecode.
        /// </summary>
        public static readonly ProcedureName BuiltIn__instanceof =
            new ProcedureName("__instanceof",
                              new List<string>(),
                              BuiltInClassName,
                              string.Empty,
                              true);

        /// <summary>
        /// Standard procedure name used for setting the locked attribute, interpreted specially
        /// by the backend.
        /// </summary>
        public static readonly ProcedureName BuiltIn__set_locked_attribute =
            new ProcedureName("__set_locked_attribute",
                              new List<string>(),
                              BuiltInClassName,
                              string.Empty,
                              true);

        /// <summary>
        /// Standard procedure name used for deleting the locked attribute, interpreted specially
        /// by the backend.
        /// </summary>
        public static readonly ProcedureName BuiltIn__delete_locked_attribute =
            new ProcedureName("__delete_locked_attribute",
                              new List<string>(),
                              BuiltInClassName,
                              string.Empty,
                              true);
        #endregion

        /// <summary>
        /// The name of the procedure.
        /// </summary>
        [JsonProperty]
        public string MethodName { get; }

        /// <summary>
        /// The parameters of the procedure.
        /// </summary>
        [JsonProperty]
        public List<string> Parameters { get; }

        /// <summary>
        /// The name of the declaring type of the procedure.
        /// </summary>
        [JsonProperty]
        public string ClassName { get; }

        /// <summary>
        /// The type of the return value.
        /// </summary>
        [JsonProperty]
        public string ReturnType { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is static.
        /// </summary>
        [JsonProperty]
        public bool IsStatic { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureName"/> class.
        /// </summary>
        /// <param name="method">The <see cref="MethodReference"/> storing information about the
        /// procedure.</param>
        public ProcedureName(MethodReference method)
        {
            MethodName = method.Name;
            IsStatic = !method.HasThis;
            Parameters = method.Parameters.Select(
                p => p.ParameterType.GetCompatibleFullName()).ToList();
            ClassName = method.DeclaringType.GetCompatibleFullName();
            ReturnType = method.ReturnType.GetCompatibleFullName();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureName"/> class.
        /// </summary>
        /// <param name="methodName">The method name.</param>
        /// <param name="parameters">The method parameters.</param>
        /// <param name="className">The name of the class possessing the method.</param>
        /// <param name="returnType">The return type.</param>
        /// <param name="isStatic"><c>true</c> if the method is static, <c>false</c> 
        /// otherwise</param>.
        [JsonConstructor]
        public ProcedureName(string methodName,
                             List<string> parameters,
                             string className,
                             string returnType,
                             bool isStatic)
        {
            MethodName = methodName;
            Parameters = parameters;
            ClassName = className;
            ReturnType = returnType;
            IsStatic = isStatic;
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
        public override bool Equals(object obj)
        {
            if (obj is ProcedureName other)
            {
                return MethodName == other.MethodName &&
                    Parameters.SequenceEqual(other.Parameters) &&
                    ClassName == other.ClassName &&
                    ReturnType == other.ReturnType &&
                    IsStatic == other.IsStatic;
            }
            return false;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var s = $"{ClassName}.{MethodName}";
            if (!string.IsNullOrWhiteSpace(ReturnType))
            {
                s = $"{ReturnType} {s}";
            }
            if (IsStatic)
            {
                s = $"static {s}";
            }

            s += $"({string.Join(", ", Parameters)})";

            return s;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(
            MethodName, Parameters, ClassName, ReturnType, IsStatic);
    }
}
