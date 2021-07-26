using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Cilsil.Utils
{
    /// <summary>
    /// Contains information about a method's exception handling blocks, along with utility methods
    /// for extracting that information.
    /// </summary>
    public class MethodExceptionHandlers
    {
        private readonly Dictionary<(Instruction tryStart, Instruction tryEnd),
                                    List<ExceptionHandlerNode>> TryBoundsToCatchHandlers =
            new Dictionary<(Instruction tryStart, Instruction tryEnd), 
                           List<ExceptionHandlerNode>>();

        private readonly Dictionary<(Instruction tryStart, Instruction tryEnd),
                                    ExceptionHandler> TryBoundsToFinallyHandlers =
            new Dictionary<(Instruction tryStart, Instruction tryEnd), ExceptionHandler>();

        private readonly Dictionary<(Instruction tryStart, Instruction tryEnd),
                                    ExceptionHandlerNode> CatchBoundsToCatchHandler =
            new Dictionary<(Instruction tryStart, Instruction tryEnd), ExceptionHandlerNode>();

        public readonly Dictionary<int, List<ExceptionHandlerNode>> TryOffsetToCatchHandlers;

        public readonly Dictionary<int, ExceptionHandler> TryOffsetToFinallyHandler;

        public readonly Dictionary<int, ExceptionHandlerNode> CatchOffsetToCatchHandler;

        private static bool InstructionWithinBounds(Instruction instruction, 
                                             (Instruction start, Instruction end) bounds) =>
            instruction.Offset >= bounds.start.Offset && instruction.Offset <= bounds.end.Offset;

        private static bool InstructionBlockWithinBounds(
            (Instruction start, Instruction end) block,
            (Instruction start, Instruction end) bounds) =>
                block.start.Offset >= bounds.start.Offset && block.end.Offset <= bounds.end.Offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodExceptionHandlers"/> class.
        /// </summary>
        /// <param name="methodBody">The body of the method. for which to extract exception handler
        /// information.</param>
        public MethodExceptionHandlers(MethodBody methodBody)
        {
            foreach (var exceptionHandler in methodBody.ExceptionHandlers)
            {
                var tryBounds = (exceptionHandler.TryStart, exceptionHandler.TryEnd);
                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Catch:
                        var catchHandlerNode = new ExceptionHandlerNode(exceptionHandler);
                        var catchBounds = (exceptionHandler.HandlerStart, 
                                           exceptionHandler.HandlerEnd);
                        if (TryBoundsToCatchHandlers.ContainsKey(tryBounds))
                        {
                            TryBoundsToCatchHandlers[tryBounds].Add(catchHandlerNode);
                        }
                        else
                        {
                            TryBoundsToCatchHandlers.Add(
                                tryBounds, 
                                new List<ExceptionHandlerNode> { catchHandlerNode });
                        }
                        CatchBoundsToCatchHandler[catchBounds] = catchHandlerNode;
                        break;
                    case ExceptionHandlerType.Finally:
                        TryBoundsToFinallyHandlers[tryBounds] = exceptionHandler;
                        break;
                    default:
                        break;
                }
            }

            foreach (var catchTry in TryBoundsToCatchHandlers.Keys)
            {
                TryBoundsToCatchHandlers[catchTry].Sort((x, y) => 
                    x.ExceptionHandler.TryStart.Offset.CompareTo(
                        y.ExceptionHandler.TryStart.Offset));

                for (int i = 0; i < TryBoundsToCatchHandlers[catchTry].Count - 1; i++)
                {
                    TryBoundsToCatchHandlers[catchTry][i].NextCatchBlock = 
                        TryBoundsToCatchHandlers[catchTry][i + 1];
                }
            }

            foreach (var catchTry in TryBoundsToCatchHandlers.Keys)
            {
                foreach (var finallyTry  in TryBoundsToFinallyHandlers.Keys)
                {
                    if (InstructionBlockWithinBounds(catchTry, finallyTry))
                    {
                        foreach (var handler in TryBoundsToCatchHandlers[catchTry])
                        {
                            handler.FinallyBlock = TryBoundsToFinallyHandlers[finallyTry];
                        }
                    }
                    // Each set of handlers can map to only one finally block.
                    continue;
                }
            }

            TryOffsetToCatchHandlers = ConvertBoundsToOffsets(TryBoundsToCatchHandlers);
            TryOffsetToFinallyHandler = ConvertBoundsToOffsets(TryBoundsToFinallyHandlers);
            CatchOffsetToCatchHandler = ConvertBoundsToOffsets(CatchBoundsToCatchHandler);
        }

        /// <summary>
        /// Returns the exception handler associated with the instruction -- a catch handler in the
        /// case of an instruction within the try block of a try-catch or try-catch-finally or a
        /// finally handler for try-finally, and a finally handler in the case of a catch
        /// instruction in try-catch-finally. We assume there is no try-catch nested within a
        /// finally block or in a catch-block.
        /// </summary>
        /// <param name="instruction">The instruction from which to determine the
        /// exception-handling control flow.</param>
        /// <returns>The exception handler in any of the cases described above.</returns>
        public ExceptionHandler GetExceptionHandlerAtInstruction(Instruction instruction)
        {
            if (TryOffsetToCatchHandlers.ContainsKey(instruction.Offset))
            {
                return TryOffsetToCatchHandlers[instruction.Offset][0].ExceptionHandler;
            }
            else if (TryOffsetToFinallyHandler.ContainsKey(instruction.Offset))
            {
                return TryOffsetToFinallyHandler[instruction.Offset];
            }
            else if (CatchOffsetToCatchHandler.ContainsKey(instruction.Offset))
            {
                return CatchOffsetToCatchHandler[instruction.Offset].ExceptionHandler;
            }
            return null;
        }

        /// <summary>
        /// This method determines that there are no nested exception-handling blocks, aside from
        /// the catch block necessarily being nested in the try block of a try-catch-finally.
        /// </summary>
        /// <returns><c>true</c> if the catch try, catch, and finally block offsets are all 
        /// mutually exclusive.</returns>
        public bool NoNestedTryCatchFinally()
        {
            // For any try-catch-finally block, the try associated with the finally block includes
            // the whole catch block (and therefore also the try associated with the catch block).
            var boundsList = new List<(int start, int end)>();
            foreach((var tryStart, var tryEnd) in TryBoundsToCatchHandlers.Keys)
            {
                boundsList.Add((tryStart.Offset, tryEnd.Offset));
            }
            foreach(var handler in CatchBoundsToCatchHandler.Values)
            {
                boundsList.Add((handler.ExceptionHandler.HandlerStart.Offset, 
                                handler.ExceptionHandler.HandlerEnd.Offset));
            }
            foreach(var handler in TryBoundsToFinallyHandlers.Values)
            {
                boundsList.Add((handler.HandlerStart.Offset, handler.HandlerEnd.Offset));
            }
            boundsList.Sort ((x, y) => x.end.CompareTo(y.end));
            for (int i = 1; i < boundsList.Count; i++)
            {
                if (boundsList[i-1].end > boundsList[i].start)
                {
                    return false;
                }
            }
            return true;
        }

        private static Dictionary<int, T> ConvertBoundsToOffsets<T>(
            Dictionary<(Instruction, Instruction), T> boundsToObject)
        {
            var converted = new Dictionary<int, T>();
            foreach ((var start, var end) in boundsToObject.Keys)
            {
                for (int i = start.Offset; i <= end.Offset; i++)
                {
                    converted[i] = boundsToObject[(start, end)];
                }
            }
            return converted;
        }
    }
}
