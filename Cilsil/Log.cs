// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil
{
    /// <summary>
    /// Log level used for controlling console output.
    /// </summary>
    public enum LogLevel {
        /// <summary>
        /// Allows all warnings and errors related to translation.
        /// </summary>
        Debug,

        /// <summary>
        /// Production log level omits warnings and errors related to translation,
        /// but keeps warnings and errors related to user input, etc.
        /// </summary>
        Production
    }

    /// <summary>
    /// TODO: use https://nlog-project.org or log4net instead of this class.
    /// </summary>
    public static class Log
    {
        static LogLevel logLevel = LogLevel.Production;

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void SetLogLevel (LogLevel level) => logLevel = level;

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static Dictionary<string, int> UnfinishedMethods { get; } =
            new Dictionary<string, int>();

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static Dictionary<string, int> UnknownInstructions { get; } =
            new Dictionary<string, int>();

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void RecordUnfinishedMethod(string methodName, int remainingInstructions) =>
            UnfinishedMethods[methodName] = remainingInstructions;

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void RecordUnknownInstruction(string instruction)
        {
            if (UnknownInstructions.ContainsKey(instruction))
            {
                UnknownInstructions[instruction] += 1;
            }
            else
            {
                UnknownInstructions.Add(instruction, 1);
            }
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void PrintAllUnknownInstruction()
        {
            DebugWriteLine("Unknown instructions:");
            foreach (var instr in UnknownInstructions.OrderBy(kv => kv.Value))
            {
                DebugWriteLine($"{instr.Key}: {instr.Value}");
            }
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void PrintCoverageStats(IEnumerable<MethodDefinition> methods)
        {
            var totalMethodCount = methods.Count();
            var failMethodCount = UnfinishedMethods.Count;
            var succMethodCount = totalMethodCount - failMethodCount;
            var totalInstr = methods.Sum(m => m.Body.Instructions.Count);
            var failInstr = UnfinishedMethods.Sum(kv => kv.Value);
            var succInstr = totalInstr - failInstr;

            WriteLine("Coverage Statistics:\n");
            WriteLine($@"Method successfully translated: {succMethodCount} ({
                ComputePercent(succMethodCount, totalMethodCount)}%)");
            WriteLine($@"Method partially translated: {failMethodCount} ({
                ComputePercent(failMethodCount, totalMethodCount)}%)");
            WriteLine($@"Instructions translated: {succInstr} ({
                ComputePercent(succInstr, totalInstr)}%)");
            WriteLine($@"Instructions skipped: {failInstr} ({
                ComputePercent(failInstr, totalInstr)}%)");
            WriteLine("======================================\n");
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteLine() => Console.WriteLine();

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteLine(string s) => Console.WriteLine(s);

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void DebugWriteLine(string s)
        {
            if (logLevel == LogLevel.Debug)
            {
                Console.WriteLine(s);
            }
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteLine(string s, ConsoleColor c)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(s);
            Console.ForegroundColor = prevColor;
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void Write(string s) => Console.Write(s);

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void Write(string s, ConsoleColor c)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.Write(s);
            Console.ForegroundColor = prevColor;
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteError(string s) => WriteLine(s, ConsoleColor.Red);

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void DebugWriteError(string s)
        {
            if (logLevel == LogLevel.Debug)
            {
                WriteLine(s, ConsoleColor.Red);
            }
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteWarning(string s) => WriteLine(s, ConsoleColor.Yellow);

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void DebugWriteWarning(string s)
        {
            if (logLevel == LogLevel.Debug)
            {
                WriteLine(s, ConsoleColor.Red);
            }
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteParserError(object invalidObject,
                                            Instruction instruction,
                                            ProgramState state)
        {
            DebugWriteError($"Unable to complete translation of {instruction?.ToString()}:");
            DebugWriteError(state.GetStateDebugInformation(invalidObject));
        }

        private static int ComputePercent(double n, double total) =>
            (int)Math.Round(100.0 * n / total, MidpointRounding.ToEven);
    }
}
