﻿// Copyright (c) Microsoft Corporation.
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
    /// TODO: use https://nlog-project.org or log4net instead of this class.
    /// </summary>
    public static class Log
    {
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
        public static Dictionary<string, long> ElapseTimePerMethod { get; } =
            new Dictionary<string, long>();

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
        public static void RecordMethodElapseTime(MethodDefinition method, long elapseTime)
        {
            if (ElapseTimePerMethod.ContainsKey(method.FullName))
            {
                ElapseTimePerMethod[method.FullName] += elapseTime;
            }
            else
            {
                ElapseTimePerMethod.Add(method.FullName, elapseTime);
            }
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void PrintProcessTime(long elapseTime)
        {
            WriteLine($"E2E Elapse Time: {elapseTime} ms");
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void PrintAllUnknownInstruction()
        {
            WriteLine("Unknown instructions:");
            foreach (var instr in UnknownInstructions.OrderBy(kv => kv.Value))
            {
                WriteLine($"{instr.Key}: {instr.Value}");
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
        public static void WriteWarning(string s) => WriteLine(s, ConsoleColor.Yellow);

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteParserError(object invalidObject,
                                            Instruction instruction,
                                            ProgramState state)
        {
            WriteError($"Unable to complete translation of {instruction?.ToString()}:");
            WriteError(state.GetStateDebugInformation(invalidObject));
        }

        private static int ComputePercent(double n, double total) =>
            (int)Math.Round(100.0 * n / total, MidpointRounding.ToEven);
    }
}
