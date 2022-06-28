// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cilsil.Sil
{
    /// <summary>
    /// A collection of all the control-flow-graphs (CFG) for all the methods in the libraries
    /// passed as input to the translation pipeline.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Cfg
    {
        /// <summary>
        /// Mapping from procedure identifier to the corresponding procedure description.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, ProcedureDescription> Procs { get; set; }

        /// <summary>
        /// List of all nodes in all the control-flow-graphs of a file. The different cfgs store
        /// pointers to the nodes in this list.
        /// </summary>
        [JsonProperty]
        public List<CfgNode> Nodes { get; set; }

        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings()
            {
                ReferenceResolverProvider = () => new IdReferenceResolver(),
                ContractResolver = ContractResolver.SnakeCasePropertyNameContractResolver
            };

        /// <summary>
        /// The order in which procedures should be analyzed.
        /// </summary>
        [JsonProperty]
        public List<ProcedureName> PrioritySet { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cfg"/> class. Creates empty objects for
        /// the procedure map, node list, and prioritized analysis list.
        /// </summary>
        public Cfg()
        {
            Procs = new Dictionary<string, ProcedureDescription>();
            Nodes = new List<CfgNode>();
            PrioritySet = new List<ProcedureName>();
        }

        /// <summary>
        /// Adds the node to the CFG's aggregated <see cref="Nodes"/> list.
        /// </summary>
        /// <param name="node">The node to register.</param>
        public void RegisterNode(CfgNode node) => Nodes.Add(node);

        /// <summary>
        /// Removes all nodes corresponding to the input procedure ID from the CFG's aggregated 
        /// <see cref="Nodes"/> list.
        /// </summary>
        /// <param name="procId">The ID of the procedure whose nodes are to be removed from the 
        /// CFG.</param>
        public void DeregisterProcedureNodes(long procId) => Nodes.RemoveAll(item => item.ProcId == procId);

        /// <summary>
        /// Generates the dot file used for cfg visualization.
        /// </summary>
        /// <param name="filePath">The path to output the CFG dot file.</param>
        public void GenerateDotFile(string filePath)
        {
            var graph = new AdjacencyGraph<CfgNode, Edge<CfgNode>>();
            foreach (var node in Nodes)
            {
                graph.AddVertex(node);
                graph.AddEdgeRange(node.Successors.Select(s => new Edge<CfgNode>(node, s)));
            }

            var graphviz = new GraphvizAlgorithm<CfgNode, Edge<CfgNode>>(graph,
                                                                         filePath,
                                                                         GraphvizImageType.Png);
            graphviz.FormatVertex += FormatVertexEventHandler;
            var output = graphviz.Generate();
            File.WriteAllText(filePath, output);

            void FormatVertexEventHandler(object sender, FormatVertexEventArgs<CfgNode> e)
            {
                e.VertexFormatter.Label = $"{e.Vertex.Id} {e.Vertex.NodeKind}";
                e.VertexFormatter.ToolTip = e.Vertex.ToString();
            }
        }

        /// <summary>
        /// Converts this instance to a JSON string.
        /// </summary>
        /// <returns>A string representation of the CFG.</returns>
        public string ToJson() => JsonConvert.SerializeObject(this, JsonSerializerSettings);

        /// <summary>
        /// Writes to file.
        /// </summary>
        /// <param name="path">The file path.</param>
        public void WriteToFile(string path)
        {
            var ascii = Encoding.GetEncoding(
                "us-ascii",
                new EncoderReplacementFallback("_"),
                new DecoderReplacementFallback("|")
            );
            var serializer = JsonSerializer.Create(JsonSerializerSettings);

            var output = path ?? "./cfg.json";
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            using (var fs = File.Open(output, FileMode.CreateNew, FileAccess.Write))
            using (var streamWriter = new StreamWriter(fs, ascii))
            using (var writer = new JsonTextWriter(streamWriter))
            {
                serializer.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Deserializes a JSON string into the corresponding CFG.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The CFG.</returns>
        public static Cfg FromJson(string json) =>
            JsonConvert.DeserializeObject<Cfg>(json, JsonSerializerSettings);

        /// <summary>
        /// Converts all of the procedure descriptions comprising the CFG into their string
        /// representation.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => ToString(null);

        /// <summary>
        /// Converts the subset of procedure descriptions identified by the input procedure names
        /// to their string representations. A procedure name is matched if that name contains an 
        /// input string as a substring.
        /// </summary>
        /// <param name="proceduresToPrint">Names which identify the subset of procedure 
        /// descriptions to print; all procedures are printed if this is null.</param>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public string ToString(IEnumerable<string> proceduresToPrint)
        {
            IEnumerable<KeyValuePair<string, ProcedureDescription>> procsToPrint;
            if (proceduresToPrint != null)
            {
                procsToPrint = Procs.Where(kv => proceduresToPrint.Any(pn => kv.Key.Contains(pn)));
            }
            else
            {
                procsToPrint = Procs;
            }

            var procStrings = procsToPrint.Select(
                kv => $"{kv.Key}\n===============\n{kv.Value.ToString()}");

            return string.Join("\n\n\n", procStrings);
        }
    }
}
