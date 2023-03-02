// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Mono.Cecil;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Sil
{
    /// <summary>
    /// Procedure information. 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ProcedureDescription
    {
        private static long NextId => _NextId++;

        private static long _NextId = 0;

        /// <summary>
        /// Identifier for the procedure description.
        /// </summary>
        [JsonProperty]
        public long PdId { get; }

        /// <summary>
        /// Procedure description properties.
        /// </summary>
        [JsonProperty]
        public ProcedureAttributes PdAttributes { get; set; }

        /// <summary>
        /// CFG nodes of the procedure.
        /// </summary>
        public List<CfgNode> Nodes { get; set; }

        /// <summary>
        /// Identifiers for the CFG nodes of the procedure.
        /// </summary>
        [JsonProperty("pd_nodes")]
        public IEnumerable<long> NodeIds => Nodes.Select(n => n.Id);

        /// <summary>
        /// The source node of the procedure's CFG.
        /// </summary>
        public CfgNode StartNode { get; set; }

        /// <summary>
        /// The identifier of the source node of the procedure's CFG.
        /// </summary>
        [JsonProperty("pd_start_node")]
        public long StartNodeId => StartNode.Id;

        /// <summary>
        /// The sink node of the procedure's CFG.
        /// </summary>
        public CfgNode ExitNode { get; set; }

        /// <summary>
        /// The identifier of the sink node of the procedure's CFG.
        /// </summary>
        [JsonProperty("pd_exit_node")]
        public long ExitNodeId => ExitNode.Id;

        /// <summary>
        /// The exception sink node of the procedure's CFG.
        /// </summary>
        public CfgNode ExceptionSinkNode { get; set; }

        /// <summary>
        /// The identifier of the exception sink node of the procedure's CFG.
        /// </summary>
        [JsonProperty("pd_exn_node")]
        public long ExceptionSinkNodeId => ExceptionSinkNode.Id;

        private Location GetEndSourceCodeLocation(
            MethodDefinition methodDefinition, bool computeFirst)
        {
            var defaultInstruction = computeFirst ?
                methodDefinition.Body.Instructions.First() :
                methodDefinition.Body.Instructions.Last();
            var current = defaultInstruction;

            var location = Location.FromSequencePoint(
                methodDefinition.DebugInformation.GetSequencePoint(current));

            while (!location.IsSourceCodeLocation() || location.IsDummy())
            {
                current = computeFirst ? current.Next : current.Previous;
                if (current == null)
                {
                    return Location.FromSequencePoint(
                        methodDefinition.DebugInformation.GetSequencePoint(defaultInstruction));
                }
                location = Location.FromSequencePoint(
                    methodDefinition.DebugInformation.GetSequencePoint(current));
            }
            return location;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureDescription"/> class.
        /// </summary>
        /// <param name="methodDefinition">The <see cref="MethodDefinition"/> from which to create
        /// the description.</param>
        /// <param name="cfg">The CFG with which this procedure description is associated.</param>
        public ProcedureDescription(MethodDefinition methodDefinition, Cfg cfg)
        {
            PdId = NextId;

            var parameters = methodDefinition.Parameters.Select(
                p => new VariableDescription(p.Name, Typ.FromTypeReference(p.ParameterType)));
            if (!methodDefinition.IsStatic)
            {
                parameters = parameters.Prepend(
                    new VariableDescription(Identifier.ThisIdentifier,
                                            Typ.FromTypeReference(
                                                methodDefinition.DeclaringType)));
            }

            var firstLocation = GetEndSourceCodeLocation(methodDefinition, true);
            var lastLocation = GetEndSourceCodeLocation(methodDefinition, false);
            var current = methodDefinition.Body.Instructions.First();

            PdAttributes = new ProcedureAttributes()
            {
                Access = methodDefinition.IsPublic ?
                         ProcedureAttributes.ProcedureAccessKind.Public :
                         methodDefinition.IsPrivate ?
                         ProcedureAttributes.ProcedureAccessKind.Private :
                         ProcedureAttributes.ProcedureAccessKind.Default,
                Formals = parameters.ToList(),
                RetType = Typ.FromTypeReference(methodDefinition.ReturnType),
                Loc = firstLocation,
                ProcName = new ProcedureName(methodDefinition)
            };
            
            foreach (var attribute in methodDefinition.CustomAttributes)
            {
                // Although this ignores annotation parameters that may be present, the annotation
                // parameters are not yet used in any way in the Infer analysis.
                PdAttributes.MethodAnnotations.AddAnnotationNoParameter(
                    attribute.AttributeType.ToString());
            }

            Nodes = new List<CfgNode>();
            StartNode = new StartNode(firstLocation, this);
            ExitNode = new ExitNode(lastLocation, this);
            ExceptionSinkNode = new StatementNode(firstLocation,
                                                  StatementNode.StatementNodeKind.ExceptionsSink,
                                                  proc: this);

            cfg.RegisterNode(StartNode);
            cfg.RegisterNode(ExitNode);
            cfg.RegisterNode(ExceptionSinkNode);
        }

        /// <summary>
        /// Helper method for updating a procedure description's associated method.
        /// </summary>
        /// <param name="newMethod">The new method.</param>
        public void UpdateMethodDefinition(MethodDefinition newMethod)
        {
            var parameters = newMethod.Parameters.Select(
                p => new VariableDescription(p.Name, Typ.FromTypeReference(p.ParameterType)));
            if (!newMethod.IsStatic)
            {
                parameters = parameters.Prepend(
                    new VariableDescription(Identifier.ThisIdentifier,
                                            Typ.FromTypeReference(
                                                newMethod.DeclaringType)));
            }

            PdAttributes.Access = 
                newMethod.IsPublic ? ProcedureAttributes.ProcedureAccessKind.Public :
                newMethod.IsPrivate ? ProcedureAttributes.ProcedureAccessKind.Private :
                                      ProcedureAttributes.ProcedureAccessKind.Default;
            PdAttributes.Formals = parameters.ToList();
            PdAttributes.RetType = Typ.FromTypeReference(newMethod.ReturnType);
            PdAttributes.ProcName = new ProcedureName(newMethod);

            foreach (var attribute in newMethod.CustomAttributes)
            {
                // Although this ignores annotation parameters that may be present, the annotation
                // parameters are not yet used in any way in the Infer analysis.
                PdAttributes.MethodAnnotations.AddAnnotationNoParameter(
                    attribute.AttributeType.ToString());
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
            var procAttrString = PdAttributes.ToString();

            return string.Join("\n\n", Nodes.Select(n => n.ToString()).Prepend(procAttrString));
        }

        /// <summary>
        /// Deregisters resources if their associated method is skipped.
        /// </summary>
        /// <param name="cfg">The CFG with which this procedure description is associated.</param>
        public void DeregisterResources(Cfg cfg)
        {
            // Decrements _NextId, which was incremented when registering the procedure.
            _NextId--;
            cfg.DeregisterProcedureNodes(PdId);
        }
    }
}
