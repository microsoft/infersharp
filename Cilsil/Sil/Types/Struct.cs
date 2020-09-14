// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Class for storing data about non-primitive types.
    /// </summary>
    [JsonObject]
    public class Struct
    {
        /// <value>
        /// The instance fields of the type.
        /// </value>
        [JsonProperty]
        public IEnumerable<FieldIdentifier> InstanceFields { get; }

        /// <summary>
        /// The static fields of the type.
        /// </summary>
        [JsonProperty]
        public IEnumerable<FieldIdentifier> StaticFields { get; }

        /// <summary>
        /// All of the classes and interfaces which can reach this type in its inheritance tree.
        /// </summary>
        [JsonProperty]
        public IEnumerable<CsuTypeName> Supers { get; }

        /// <summary>
        /// The methods of the type.
        /// </summary>
        [JsonProperty]
        public IEnumerable<ProcedureName> Methods { get; set; }

        /// <summary>
        /// The source code annotations of the type.
        /// </summary>
        [JsonProperty]
        public IEnumerable<ItemAnnotation> Annotations { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Struct"/> class.
        /// </summary>
        /// <param name="instanceFields">The instance fields of the type.</param>
        /// <param name="staticFields">The static fields of the type.</param>
        /// <param name="supers">All of the classes and interfaces which can reach this type in its
        /// inheritance tree.</param>
        /// <param name="methods">The methods of the type.</param>
        /// <param name="annotations">The source code annotations of the type.</param>
        public Struct(IEnumerable<FieldIdentifier> instanceFields,
                      IEnumerable<FieldIdentifier> staticFields,
                      IEnumerable<string> supers,
                      IEnumerable<ProcedureName> methods,
                      IEnumerable<ItemAnnotation> annotations = null)
        {
            InstanceFields = instanceFields;
            StaticFields = staticFields;
            Supers = supers.Select(s => new CsuTypeName(CsuKind.Class, s));
            Methods = methods;
            Annotations = annotations ?? new List<ItemAnnotation>();
        }
    }
}
