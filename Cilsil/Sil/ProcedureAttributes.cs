// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Sil
{
    /// <summary>
    /// Procedure properties.
    /// </summary>
    [JsonObject]
    public class ProcedureAttributes
    {
        /// <summary>
        /// Access modifier for the procedure.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ProcedureAccessKind Access { get; set; } = ProcedureAccessKind.Default;

        /// <summary>
        /// Name of the procedure.
        /// </summary>
        [JsonProperty]
        public ProcedureName ProcName { get; set; }

        /// <summary>
        /// Names and types of variables captured in blocks.
        /// </summary>
        [JsonProperty]
        public List<VariableDescription> Captured { get; set; } = new List<VariableDescription>();

        /// <summary>
        /// Exceptions thrown by the procedure.
        /// </summary>
        [JsonProperty]
        public List<string> Exceptions { get; set; } = new List<string>();

        /// <summary>
        /// Name and type of formal parameters to the procedure.
        /// </summary>
        [JsonProperty]
        public List<VariableDescription> Formals { get; set; } = new List<VariableDescription>();

        /// <summary>
        /// Return value and list of parameters of the method.
        /// </summary>
        [JsonProperty]
        public MethodAnnotation MethodAnnotations { get; set; } = new MethodAnnotation();

        /// <summary>
        /// List of indices of formals that are const-qualified.
        /// </summary>
        [JsonProperty]
        public List<int> ConstFormals { get; set; } = new List<int>();

        /// <summary>
        /// Local variables of the procedure.
        /// </summary>
        [JsonProperty]
        public List<Local> Locals { get; set; } = new List<Local>();

        /// <summary>
        /// <c>true</c> if this instance is abstract; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsAbstract { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is a bridge method; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsBridgeMethod { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is defined; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsDefined { get; set; } = true;

        /// <summary>
        /// <c>true</c> if this instance is generated; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsGenerated { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is an Objective-C instance method; otherwise, 
        /// <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsObjCInstanceMethod { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is synthetic method; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsSyntheticMethod { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is a C++ method annotated with "noexcept"; otherwise, 
        /// <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsCppNoExceptMethod { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is a Java synchronized method; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsJavaSynchronizedMethod { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is a model; otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsModel { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is a clone specialized for dynamic dispatch handling; 
        /// otherwise, <c>false</c>.
        /// </summary>
        /// <value>
        /// </value>
        [JsonProperty]
        public bool IsSpecialized { get; set; } = false;

        /// <summary>
        /// <c>true</c> if this instance is variadic (only supported for C-language procedures); 
        /// otherwise, <c>false</c>.
        /// </summary>
        [JsonProperty]
        public bool IsVariadic { get; set; } = false;

        /// <summary>
        /// Location of this procedure in the source code.
        /// </summary>
        [JsonProperty]
        public Location Loc { get; set; }

        /// <summary>
        /// Source file where the procedure was captured.
        /// </summary>
        [JsonProperty]
        public SourceFile TranslationUnit { get; set; }

        /// <summary>
        /// Type of the return value.
        /// </summary>
        [JsonProperty]
        public Typ RetType { get; set; } = new Tvoid();

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var formalsString = string.Join(", ", Formals.Select(f => $"({f.ToString()})"));
            var localsString = string.Join(", ", Locals.Select(l => $"{l.ToString()}"));
            return $@"{{
ProcName: {ProcName.ToString()}
Access: {Access.ToString()}
Formals: {formalsString}
Location: {Loc.ToString()}
Locals: {localsString}
ReturnType: {RetType.ToString()}
}}";
        }

        /// <summary>
        /// Method local variable. Stores name, type, and attributes.
        /// </summary>
        [JsonObject]
        public class Local
        {
            /// <summary>
            /// The name of the local variable.
            /// </summary>
            [JsonProperty]
            public string Name { get; }

            /// <summary>
            /// The type of the local variable.
            /// </summary>
            [JsonProperty]
            public Typ Type { get; }

            /// <summary>
            /// Refers to  __block attribute of Objective-C variables. <c>true</c> if it will be 
            /// modified inside a block; otherwise, <c>false</c>.
            /// </summary>
            [JsonProperty]
            public bool ModifyInBlock { get; }

            /// <summary>
            ///   <c>true</c> if this instance is a constant expression; otherwise, <c>false</c>.
            /// </summary>
            [JsonProperty]
            public bool IsConstExpr { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Local"/> class.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="type">The type.</param>
            /// <param name="modifyInBlock">Refers to  __block attribute of Objective-C variables. 
            /// <c>true</c> if it will be modified inside a block; otherwise, <c>false</c>.</param>
            /// <param name="isConstExpr">if set to <c>true</c> [is constant expr].</param>
            public Local(string name, Typ type, bool modifyInBlock, bool isConstExpr)
            {
                Name = name;
                Type = type;
                ModifyInBlock = modifyInBlock;
                IsConstExpr = isConstExpr;
            }

            /// <summary>
            /// Converts to string.
            /// </summary>
            /// <returns>
            /// A <see cref="string" /> that represents this instance.
            /// </returns>
            public override string ToString() =>
                $@"{{ name= {Name}; typ= {Type.ToString()}; modify_in_block= {
                    ModifyInBlock}; is_constexp= {IsConstExpr}}}";
        }

        /// <summary>
        /// Describes the access modifier of the procedure.
        /// </summary>
        public enum ProcedureAccessKind
        {
            /// <summary>
            /// Default access.
            /// </summary>
            Default,
            /// <summary>
            /// Private access.
            /// </summary>
            Private,
            /// <summary>
            /// Public access.
            /// </summary>
            Public,
            /// <summary>
            /// Protected access.
            /// </summary>
            Protected
        }
    }
}
