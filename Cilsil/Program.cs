// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Cil.Parsers;
using Cilsil.Extensions;
using Cilsil.Services;
using Cilsil.Services.Results;
using Cilsil.Sil;
using Mono.Cecil;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace Cilsil
{
    /// <summary>
    /// Entry point for the CIL to SIL translation pipeline.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            var translateCommand = new Command("translate")
            {
                new Argument<string[]>("paths"),
                new Option("--dot")
                {
                    Argument = new Argument<string>(),
                    Description = "Output dot file for the control flow graph, see " +
                                  "https://www.graphviz.org/ for more information"
                },
                new Option("--outcfg")
                {
                    Argument = new Argument<string>(),
                    Description = "Output CFG JSON file path"
                },
                new Option("--cfgtxt")
                {
                    Argument = new Argument<string>(),
                    Description = "Output CFG txt file path"
                },
                new Option("--outtenv")
                {
                    Argument = new Argument<string>(),
                    Description = "Output type environment JSON file path"
                },
                new Option("--debug")
                {
                    Argument = new Argument<bool>(),
                    Description = "Output debug information"
                },
                new Option("--extprogress")
                {
                    Argument = new Argument<bool>(),
                    Description = "Print progress for extension scenario"
                }
            };
            translateCommand.Handler =
                CommandHandler.Create<string[], string, string, string, string, bool, bool>(Translate);
            var printCommand = new Command("print")
            {
                new Option("--procs", "A comma-separated procedure names to print")
                {
                    Argument = new Argument<string>()
                },
                new Argument<string[]>("files")
            };
            printCommand.Handler = CommandHandler.Create<string[], string>(PrintFiles);

            var rootCommand = new RootCommand()
            {
                translateCommand,
                printCommand
            };

            rootCommand.InvokeAsync(args).Wait();
        }

        /// <summary>
        /// Runs the translation pipeline. Note that the variable names must match the command-line
        /// argument names, which is why the capitalization pattern departs from that used in the
        /// rest of the repository.
        /// </summary>
        /// <param name="paths">The paths to the DLLs to translate.</param>
        /// <param name="outcfg">The CFG output path.</param>
        /// <param name="cfgtxt">The CFG text representation output path.</param>
        /// <param name="outtenv">The type environment output path.</param>
        /// <param name="dot">The dot file (used for visualizing the computed CFG) output path.</param>
        /// <param name="debug">The flag for printing debug output.</param>
        /// <param name="extprogress">If <c>true</c>, output progress for extension 
        /// scenario when input binaries are adequately large.</param>
        public static void Translate(string[] paths = null,
                                     string outcfg = null,
                                     string cfgtxt = null,
                                     string outtenv = null,
                                     string dot = null,
                                     bool debug = false,
                                     bool extprogress = false)
        {
            Log.SetDebugMode(debug);

            (var cfg, var tenv) = ExecuteTranslation(paths, extprogress);

            File.WriteAllText(cfgtxt ?? "./cfg.txt", cfg.ToString());
            cfg.WriteToFile(outcfg);
            tenv.WriteToFile(outtenv);

            if (!string.IsNullOrWhiteSpace(dot))
            {
                var fullDotPath = Path.GetFullPath(dot);
                Log.WriteLine($"Dot file generated at {fullDotPath}");
                cfg.GenerateDotFile(fullDotPath);
            }
        }

        /// <summary>
        /// Executes the translation.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="extensionProgress">If <c>true</c>, periodically write progress to console
        /// if the input binaries are adequately large.</param>
        /// <returns>The computed cfg and type environment.</returns>
        public static (Cfg, TypeEnvironment) ExecuteTranslation(string[] paths, 
                                                                bool extensionProgress = false)
        {
            (var assemblies, var totalSize) = GetAssemblies(paths);

            InstructionParser.RegisterAllKnownParsers();

            var reportProgressExtension = totalSize > 1e7 && extensionProgress;

            var decompilationService = new DecompilationService(assemblies, reportProgressExtension);
            Dictionary<string, string> typeToAssembly = new Dictionary<string, string>();
            foreach (var assemblyPath in assemblies)
            {
                try
                {
                    var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters()
                    {
                        ReadSymbols = false
                    });
                    var types = module.GetTypes();
                    foreach (var type in types)
                    {
                        typeToAssembly[type.GetCompatibleFullName()] = assemblyPath;
                    }
                }
                catch { }
            }


                var tenvParser = new TenvParserService(typeToAssembly, reportProgressExtension);
            var cfgParser = new CfgParserService(reportProgressExtension);

            var result = decompilationService
                .Execute()
                .ThenExecute(tenvParser)
                .ThenExecute(cfgParser);

            var tenv = result.GetResult<TenvParserResult>().TypeEnvironment;
            var cfg = result.GetResult<CfgParserResult>().Cfg;
            var disposableTypeNames = new HashSet<string>();
            foreach (var typeName in tenvParser.AssemblyToDisposableTypes.Keys)
            {
                var typeList = tenvParser.AssemblyToDisposableTypes[typeName];
                foreach (var type in typeList)
                {
                    disposableTypeNames.Add(type);
                }
            }

            var newTenvEntries = tenv.TypeEntries.Where(i => disposableTypeNames.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value);

            tenv.TypeEntries = newTenvEntries;

            Log.PrintAllUnknownInstruction();
            Log.WriteLine();
            Log.PrintCoverageStats(result.GetResult<CfgParserResult>().Methods);

            return (cfg, tenv);
        }

        private static (IEnumerable<string>, long) GetAssemblies(IEnumerable<string> paths)
        {
            var assemblies = new List<string>();
            long totalSize = 0;
            foreach (var p in paths)
            {
                try
                {
                    if (File.GetAttributes(p).HasFlag(FileAttributes.Directory))
                    {
                        assemblies.AddRange(
                            Directory.GetFiles(p, "*.netmodule", SearchOption.AllDirectories));
                        assemblies.AddRange(
                            Directory.GetFiles(p, "*.dll", SearchOption.AllDirectories));
                        assemblies.AddRange(
                            Directory.GetFiles(p, "*.exe", SearchOption.AllDirectories));
                    }
                    else
                    {
                        assemblies.Add(p);
                        totalSize += new FileInfo(p).Length;
                    }
                }
                catch (FileNotFoundException e)
                {
                    Log.WriteError($"Input DLL {e.FileName} not found.");
                    continue;
                }
            }
            return (assemblies, totalSize);
        }

        private static void PrintFiles(string[] files = null, string procs = null)
        {
            foreach (var file in files)
            {
                var jsonText = File.ReadAllText(file);
                if (Cfg.FromJson(jsonText) is Cfg cfg)
                {
                    PrintCfg(cfg, procs);
                }
                else if (TypeEnvironment.FromJson(jsonText) is TypeEnvironment tenv)
                {
                    Log.WriteLine(tenv.ToString());
                }
                else
                {
                    Log.WriteError($"Input file {file} is not a cfg or tenv\n");
                }
            }
        }

        private static void PrintCfg(Cfg cfg, string procs = null)
        {
            var procsToPrint = procs?.Split(",");
            Log.WriteLine(cfg.ToString(procsToPrint));
        }
    }
}
