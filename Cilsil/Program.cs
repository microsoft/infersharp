// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Cil.Parsers;
using Cilsil.Services;
using Cilsil.Services.Results;
using Cilsil.Sil;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;

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
                new Option("--printprocs")
                {
                    Argument = new Argument<string>(),
                    Description =
                        "Comma-separated list of procedures to print out for debugging purposes"
                },
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
                new Option("--outelapsetime")
                {
                    Argument = new Argument<string>(),
                    Description =
                        "Output type environment JSON file path for debugging purposes"
                },
            };
            translateCommand.Handler =
                CommandHandler.Create<string[], string, string, string, string, string, string>(Translate);
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
        /// <param name="printprocs">Comma-separated string used to identify the translated 
        /// procedure descriptions to print. A procedure description is matched if its corresponding
        /// procedure name contains a comma-delineated part as a substring. If this is null, all 
        /// procedure descriptions are printed.</param>
        /// <param name="outcfg">The CFG output path.</param>
        /// <param name="cfgtxt">The CFG text representation output path.</param>
        /// <param name="outtenv">The type environment output path.</param>
        /// <param name="dot">The dot file (used for visualizing the computed CFG) output
        /// path.</param>
        /// <param name="outelapsetime">The elapse time output path (used for visualizing the elapse 
        /// time per method).</param>
        public static void Translate(string[] paths = null,
                                     string printprocs = null,
                                     string outcfg = null,
                                     string cfgtxt = null,
                                     string outtenv = null,
                                     string dot = null,
                                     string outelapsetime = null)
        {
            (var cfg, var tenv) = ExecuteTranslation(paths, printprocs, outelapsetime);

            File.WriteAllText(cfgtxt ?? "./cfg.txt", cfg.ToString());
            cfg.WriteToFile(outcfg);
            tenv.WriteToFile(outtenv);

            if (!string.IsNullOrWhiteSpace(outelapsetime))
            {
                var fullElapseTimePath = Path.GetFullPath(outelapsetime);
                File.WriteAllText(fullElapseTimePath, JsonSerializer.Serialize(Log.ElapseTimePerMethod));
                var fullInstrElapseTimePath = Path.GetFullPath(outelapsetime.Replace(".json", "_offset.json"));
                File.WriteAllText(fullInstrElapseTimePath, JsonSerializer.Serialize(Log.ElapseTimeAndCountPerOffset));
            }

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
        /// <param name="printprocs">The printprocs.</param>
        /// <param name="outelapsetime">The elapse time output path (used for visualizing the elapse 
        /// time per method).</param>
        /// <returns></returns>
        public static (Cfg, TypeEnvironment) ExecuteTranslation(string[] paths,
                                                                string printprocs = null,
                                                                string outelapsetime = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var assemblies = GetAssemblies(paths);

            InstructionParser.RegisterAllKnownParsers();

            var decompilationService = new DecompilationService(assemblies);
            var tenvParser = new TenvParserService();
            var cfgParser = new CfgParserService(outelapsetime: outelapsetime);

            var result = decompilationService
                .Execute()
                .ThenExecute(tenvParser)
                .ThenExecute(cfgParser);

            var tenv = result.GetResult<TenvParserResult>().TypeEnvironment;
            var cfg = result.GetResult<CfgParserResult>().Cfg;

            if (printprocs != null)
            {
                PrintCfg(cfg, printprocs);
            }

            Log.PrintAllUnknownInstruction();
            Log.WriteLine();
            Log.PrintCoverageStats(result.GetResult<CfgParserResult>().Methods);
            
            watch.Stop();

            if (!string.IsNullOrWhiteSpace(outelapsetime))
            {
                Log.WriteLine();
                Log.PrintProcessTime(watch.ElapsedMilliseconds);
            }

            return (cfg, tenv);
        }

        private static IEnumerable<string> GetAssemblies(IEnumerable<string> paths)
        {
            var assemblies = new List<string>();
            foreach (var p in paths)
            {
                try
                {
                    if (File.GetAttributes(p).HasFlag(FileAttributes.Directory))
                    {
                        assemblies.AddRange(
                            Directory.GetFiles(p, "*.dll", SearchOption.AllDirectories));
                        assemblies.AddRange(
                            Directory.GetFiles(p, "*.exe", SearchOption.AllDirectories));
                    }
                    else
                    {
                        assemblies.Add(p);
                    }
                }
                catch (FileNotFoundException e)
                {
                    Log.WriteError($"Input DLL {e.FileName} not found.");
                    continue;
                }
            }
            return assemblies;
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
