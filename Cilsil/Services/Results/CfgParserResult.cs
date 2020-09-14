// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Mono.Cecil;
using System.Collections.Generic;

namespace Cilsil.Services.Results
{
    /// <summary>
    /// Stores the output of the CFG parsing service.
    /// </summary>
    /// <seealso cref="Cilsil.Services.ServiceExecutionResult" />
    internal class CfgParserResult : ServiceExecutionResult
    {
        /// <summary>
        /// The CFG associated with this result.
        /// </summary>
        public Cfg Cfg { get; }

        /// <summary>
        /// The methods whose translation comprises this CFG.
        /// </summary>
        public IEnumerable<MethodDefinition> Methods { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgParserResult"/> class.
        /// </summary>
        /// <param name="cfg">The CFG associated with this result.</param>
        /// <param name="methods">The methods whose translation comprises this CFG.</param>
        public CfgParserResult(Cfg cfg, IEnumerable<MethodDefinition> methods)
        {
            Cfg = cfg;
            Methods = methods;
        }
    }
}
