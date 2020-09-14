// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Serialization;
using Cilsil.Sil.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Cilsil.Sil
{
    /// <summary>
    /// Type information storage for the assemblies input to the translation pipeline.
    /// </summary>
    [JsonArray(allowNullItems: false)]
    public class TypeEnvironment : IEnumerable<TypeEntry>
    {
        /// <summary>
        /// Gets or sets the <see cref="TypeEntry"/> with the specified type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public TypeEntry this[string typeName]
        {
            get => TypeEntries[typeName];
            set => TypeEntries[typeName] = value;
        }

        private readonly Dictionary<string, TypeEntry> TypeEntries;

        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings()
            {
                ContractResolver = ContractResolver.SnakeCasePropertyNameContractResolver
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeEnvironment"/> class.
        /// </summary>
        public TypeEnvironment()
        {
            TypeEntries = new Dictionary<string, TypeEntry>();
        }

        /// <summary>
        /// Determines whether the type identified by the given name is present in the environment.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>
        ///   <c>true</c> if the environment contains the input name; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsType(string typeName) => TypeEntries.ContainsKey(typeName);

        /// <summary>
        /// Returns an enumerator that iterates through the types in the environment.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the types in the environment.
        /// </returns>
        public IEnumerator<TypeEntry> GetEnumerator() => TypeEntries.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => TypeEntries.Values.GetEnumerator();

        /// <summary>
        /// Converts the environment to JSON.
        /// </summary>
        /// <returns>The JSON output as a string.</returns>
        public string ToJson() => JsonConvert.SerializeObject(this, JsonSerializerSettings);

        /// <summary>
        /// Writes to file.
        /// </summary>
        /// <param name="path">The file path.</param>
        public void WriteToFile(string path)
        {
            var serializer = JsonSerializer.Create(JsonSerializerSettings);
            using (var streamWriter = new StreamWriter(path ?? "./tenv.json"))
            using (var writer = new JsonTextWriter(streamWriter))
            {
                serializer.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Deserializes the given string into a type environment.
        /// </summary>
        /// <param name="json">The JSON.</param>
        /// <returns></returns>
        public static TypeEnvironment FromJson(string json) =>
            JsonConvert.DeserializeObject<TypeEnvironment>(json, JsonSerializerSettings);

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => base.ToString();
    }

    /// <summary>
    /// An entry of the type environment.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TypeEntry
    {
        /// <summary>
        /// The name of the type.
        /// </summary>
        [JsonProperty]
        public TypeName TypeName { get; set; }

        /// <summary>
        /// The structure containing information about the type.
        /// </summary>
        [JsonProperty]
        public Struct TypeStruct { get; set; }
    }
}
