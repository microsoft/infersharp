// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;
using System.Collections.Generic;

namespace Cilsil.Services.Results
{
    internal class DecompilationResult : ServiceExecutionResult
    {
        public IEnumerable<ModuleDefinition> ModuleDefinitionsWithSymbols { get; }

        public IEnumerable<ModuleDefinition> ModuleDefinitionsWithNoSymbols { get; }

        public IEnumerable<TypeDefinition> TypesWithSymbols { get; }

        public IEnumerable<TypeDefinition> TypesWithNoSymbols { get; }

        public DecompilationResult(IEnumerable<ModuleDefinition> moduleDefinitionsWithSymbols,
                                   IEnumerable<ModuleDefinition> moduleDefinitionsWithNoSymbols,
                                   IEnumerable<TypeDefinition> typesWithSymbols,
                                   IEnumerable<TypeDefinition> typesWithNoSymbols)
        {
            ModuleDefinitionsWithSymbols = moduleDefinitionsWithSymbols;
            ModuleDefinitionsWithNoSymbols = moduleDefinitionsWithNoSymbols;
            TypesWithSymbols = typesWithSymbols;
            TypesWithNoSymbols = typesWithNoSymbols;
        }
    }
}
