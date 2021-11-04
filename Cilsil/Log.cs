// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil
{
    /// <summary>
    /// NLog instance and helper methods for logging to console.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// NLog logger instance
        /// </summary>
        public static Logger instance = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Dynamically enable or disable debug mode by keeping or removing the debugMode Nlog rule.
        /// </summary>
        public static void SetDebugMode(bool isDebugMode)
        {
            if (!isDebugMode)
            {
                var config = LogManager.Configuration;
                config.RemoveRuleByName("debugMode");
                LogManager.Configuration = config;
            }
        }


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
            instance.Warn("Unknown instructions:");
            foreach (var instr in UnknownInstructions.OrderBy(kv => kv.Value))
            {
                instance.Warn($"{instr.Key}: {instr.Value}");
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

            instance.Info("Coverage Statistics:\n");
            instance.Info($@"Method successfully translated: {succMethodCount} ({
                ComputePercent(succMethodCount, totalMethodCount)}%)");
            instance.Info($@"Method partially translated: {failMethodCount} ({
                ComputePercent(failMethodCount, totalMethodCount)}%)");
            instance.Info($@"Instructions translated: {succInstr} ({
                ComputePercent(succInstr, totalInstr)}%)");
            instance.Info($@"Instructions skipped: {failInstr} ({
                ComputePercent(failInstr, totalInstr)}%)");
            instance.Info("======================================\n");
        }

        /// <summary>
        /// TODO: use https://nlog-project.org or log4net instead of this class.
        /// </summary>
        public static void WriteParserWarning(object invalidObject,
                                            Instruction instruction,
                                            ProgramState state)
        {
            instance.Warn($"Unable to complete translation of {instruction?.ToString()}:");
            instance.Warn(state.GetStateDebugInformation(invalidObject));
        }

        private static int ComputePercent(double n, double total) =>
            (int)Math.Round(100.0 * n / total, MidpointRounding.ToEven);
    }
}
