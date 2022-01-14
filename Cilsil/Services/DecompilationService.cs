// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Services.Results;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Services
{
    internal class DecompilationService : IService
    {
        /// <summary>
        /// If set to <c>true</c>, parsing service produces the tenv; otherwise,
        /// parsing service produces the cfg.
        /// </summary>
        public bool WriteConsoleProgress { set; get; }
        public bool ParseTenv { set; get; }
        public IEnumerable<string> AssemblyPaths { get; }

        public DecompilationService(IEnumerable<string> assemblyPaths, bool writeConsoleProgress)
        {
            WriteConsoleProgress = writeConsoleProgress;
            AssemblyPaths = assemblyPaths;
        }

        public ServiceExecutionResult Execute()
        {
            var readerParams = new ReaderParameters()
            {
                ReadSymbols = true
            };
            var readerParamsWithoutSymbols = new ReaderParameters()
            {
                ReadSymbols = false
            };

            Log.WriteLine("Translation stage 1/3: Loading binaries.");
            IEnumerable<ModuleDefinition> modulesWithNoSymbols = new List<ModuleDefinition>();
            IEnumerable<ModuleDefinition> modulesWithSymbols = new List<ModuleDefinition>();
            var i = 0;
            var numAssemblies = AssemblyPaths.Count();
            using (var bar = new ProgressBar())
            {
                modulesWithSymbols = AssemblyPaths.Select(p =>
                {
                    try
                    {
                        return ModuleDefinition.ReadModule(p, readerParams);
                    }
                    catch
                    {
                        // This try catch block handles cases that the dll file is corrupted or
                        // broken.
                        try
                        {
                            modulesWithNoSymbols =
                                modulesWithNoSymbols.Append(
                                    ModuleDefinition.ReadModule(p, readerParamsWithoutSymbols));
                        }
                        catch
                        {
                            return null;
                        }

                        return null;
                    }
                    finally
                    {
                        i++;
                        bar.Report((double)i / numAssemblies);
                        if (WriteConsoleProgress)
                        {
                            Log.WriteProgressLine(i, numAssemblies);
                        }
                    }
                }).ToList();
            }
            modulesWithSymbols = modulesWithSymbols
                .Where(p => p != null).Distinct(new ModuleComparer());
            modulesWithNoSymbols = modulesWithNoSymbols
                .Where(p => p != null).Distinct(new ModuleComparer());

            var typesWithSymbols = modulesWithSymbols
                .SelectMany(m => m.Types)
                .SelectMany(t => t.GetAllNestedTypes());
            var typeWithNoSymbols = modulesWithNoSymbols
                .SelectMany(m => m.Types)
                .SelectMany(t => t.GetAllNestedTypes());
            return new DecompilationResult(modulesWithSymbols,
                                           modulesWithNoSymbols,
                                           typesWithSymbols,
                                           typeWithNoSymbols);
        }

        public ServiceExecutionResult ExecuteFromResult(ServiceExecutionResult result) =>
            throw new ApplicationException("");

        private class ModuleComparer : IEqualityComparer<ModuleDefinition>
        {
            public bool Equals(ModuleDefinition x, ModuleDefinition y) =>
                x.Name == y.Name && x.RuntimeVersion == y.RuntimeVersion;

            public int GetHashCode(ModuleDefinition obj) =>
                obj.Name.GetHashCode() ^ obj.RuntimeVersion.GetHashCode();
        }
    }
}
