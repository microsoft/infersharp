// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil.Cil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;

namespace Cilsil.Sil
{
    /// <summary>
    /// Source file information.
    /// </summary>
    [JsonObject]
    public class SourceFile
    {
        /// <summary>
        /// The type of the path to the source file (relative or absolute).
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public PathType PathType { get; set; }

        /// <summary>
        /// The path to the source file.
        /// </summary>
        [JsonProperty]
        public string Path { get; set; }

        /// <summary>
        /// Returns a source file with an empy path.
        /// </summary>
        public static SourceFile Empty =>
            new SourceFile() { Path = string.Empty, PathType = PathType.Relative };

        /// <summary>
        /// Creates a <see cref="SourceFile"/> type from a <see cref="SequencePoint"/> using a 
        /// relative pathname.
        /// </summary>
        /// <param name="sequencePoint">The sequence point.</param>
        /// <returns>The file type.</returns>
        public static SourceFile FromSequencePoint(SequencePoint sequencePoint)
        {
            var path = sequencePoint.Document.Url;
            // Absolute path on Windows
            if (System.IO.Path.IsPathRooted(path) && path.Contains(":"))
            {
                var currLocation = new Uri(Directory.GetCurrentDirectory());
                var fileLocation = new Uri(path);
                path = currLocation.MakeRelativeUri(fileLocation).OriginalString;
            }

            return new SourceFile()
            {
                PathType = PathType.Relative,
                Path = path
            };
        }
    }
}

namespace Cilsil
{
    /// <summary>
    /// Different file system path types.
    /// </summary>
    public enum PathType
    {
        /// <summary>
        /// Path which points to the same location, regardless of the current working directory.
        /// </summary>
        Absolute,
        /// <summary>
        /// Path for which the location is relative to the current working directory.
        /// </summary>
        Relative
    }
}
