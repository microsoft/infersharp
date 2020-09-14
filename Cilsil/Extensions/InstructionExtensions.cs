// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil.Cil;

namespace Cilsil.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Instruction"/>.
    /// </summary>
    public static class InstructionExtensions
    {
        /// <summary>
        /// Counts number of instructions after the given <see cref="Instruction"/>.
        /// </summary>
        /// <param name="instruction">The instruction from which to count.</param>
        /// <returns>The number of instructions after the given instruction.</returns>
        public static int RemainingInstructionCount(this Instruction instruction)
        {
            var count = 0;
            var temp = instruction;
            while (temp.Next != null)
            {
                count++;
                temp = temp.Next;
            }
            return count;
        }
    }
}
