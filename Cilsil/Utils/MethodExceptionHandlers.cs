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
        // Internal fields for managing exception handler information. Assumes that try-catch, 
        // try-finally are not nested, though try-catch-finally is supported.
        #region
        private readonly Dictionary<(Instruction tryStart, Instruction tryEnd),
                                    List<ExceptionHandlerNode>> TryBoundsToCatchHandlers =
            new Dictionary<(Instruction tryStart, Instruction tryEnd), 
                           List<ExceptionHandlerNode>>();

        private readonly Dictionary<(Instruction tryStart, Instruction tryEnd),
                                    ExceptionHandler> TryBoundsToFinallyHandlers =
            new Dictionary<(Instruction tryStart, Instruction tryEnd), ExceptionHandler>();

        private readonly Dictionary<(Instruction catchStart, Instruction catchEnd),
                                    ExceptionHandlerNode> CatchBoundsToCatchHandler =
            new Dictionary<(Instruction catchStart, Instruction catchEnd), ExceptionHandlerNode>();

        private readonly Dictionary<(Instruction finallyStart, Instruction finallyEnd),
                                   ExceptionHandler> FinallyBoundsToFinallyHandler =
            new Dictionary<(Instruction finallyStart, Instruction finallyEnd), ExceptionHandler>();
        #endregion

        // Public fields for managing exception handler information. Assumes that try-catch, 
        // try-finally are not nested, though try-catch-finally is supported.
        #region
        /// <summary>
        /// Maps the last instruction of a finally handler to the handler.
        /// </summary>
        public readonly Dictionary<Instruction, ExceptionHandler> FinallyEndToHandler = 
            new Dictionary<Instruction, ExceptionHandler>();

        /// <summary>
        /// Maps offsets within try-blocks of try-catch to the corresponding catch handler set.
        /// </summary>
        public readonly Dictionary<int, List<ExceptionHandlerNode>> TryOffsetToCatchHandlers;

        /// <summary>
        /// Maps offsets within try-blocks of try-finally to the corresponding finally handler.
        /// </summary>
        public readonly Dictionary<int, ExceptionHandler> TryOffsetToFinallyHandler;

        /// <summary>
        /// Maps offsets within catch-blocks to the corresponding catch handler.
        /// </summary>
        public readonly Dictionary<int, ExceptionHandlerNode> CatchOffsetToCatchHandler;

        /// <summary>
        /// Maps offsets within finally-blocks to the corresponding finally handler.
        /// </summary>
        public readonly Dictionary<int, ExceptionHandler> FinallyOffsetToFinallyHandler;
        #endregion

        /// <summary>
        /// The default handler end offset, overwritten when it can be found.
        /// </summary>
        public const int DefaultHandlerEndOffset = -1;

        private static bool InstructionBlockWithinBounds(
            (Instruction start, Instruction end) block,
            (Instruction start, Instruction end) bounds) =>
                block.start.Offset >= bounds.start.Offset && block.end.Offset <= bounds.end.Offset;

        /// <summary>
        /// <c>true</c> if there is an unsupported exception block; <c>false</c> otherwise.
        /// </summary>
        public bool UnhandledExceptionBlock = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodExceptionHandlers"/> class.
        /// </summary>
        /// <param name="methodBody">The body of the method. for which to extract exception handler
        /// information.</param>
        public MethodExceptionHandlers(MethodBody methodBody)
        {
            foreach (var exceptionHandler in methodBody.ExceptionHandlers)
            {
                var tryBounds = (exceptionHandler.TryStart, exceptionHandler.TryEnd.Previous);
                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Catch:
                        var catchHandlerNode = new ExceptionHandlerNode(exceptionHandler);
                        var catchBounds = (exceptionHandler.HandlerStart, 
                                           exceptionHandler.HandlerEnd.Previous);
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
                        FinallyBoundsToFinallyHandler[(
                            exceptionHandler.HandlerStart, 
                            exceptionHandler.HandlerEnd.Previous)] = exceptionHandler;
                        FinallyEndToHandler[exceptionHandler.HandlerEnd.Previous] =
                            exceptionHandler;
                        break;
                    case ExceptionHandlerType.Filter:
                    case ExceptionHandlerType.Fault:
                    default:
                        UnhandledExceptionBlock = true;
                        break;
                }
            }

            foreach (var catchTry in TryBoundsToCatchHandlers.Keys)
            {
                TryBoundsToCatchHandlers[catchTry].Sort((x, y) => 
                    x.ExceptionHandler.HandlerStart.Offset.CompareTo(
                        y.ExceptionHandler.HandlerStart.Offset));

                for (int i = 0; i < TryBoundsToCatchHandlers[catchTry].Count - 1; i++)
                {
                    TryBoundsToCatchHandlers[catchTry][i].NextCatchBlock = 
                        TryBoundsToCatchHandlers[catchTry][i + 1];
                }
                for (int i = 1; i < TryBoundsToCatchHandlers[catchTry].Count; i++)
                {
                    TryBoundsToCatchHandlers[catchTry][i].PreviousCatchBlock =
                        TryBoundsToCatchHandlers[catchTry][i - 1];
                }

                foreach (var catchHandlerNode in TryBoundsToCatchHandlers[catchTry])
                {
                    catchHandlerNode.FirstCatchHandler = 
                        TryBoundsToCatchHandlers[catchTry][0].ExceptionHandler;
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
            FinallyOffsetToFinallyHandler = ConvertBoundsToOffsets(FinallyBoundsToFinallyHandler);
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
            var finallyBoundsList = new List<(int start, int end)>();

            foreach ((var tryStart, var tryEnd) in TryBoundsToCatchHandlers.Keys)
            {
                boundsList.Add((tryStart.Offset, tryEnd.Offset));
            }
            foreach(var handler in CatchBoundsToCatchHandler.Values)
            {
                boundsList.Add((handler.ExceptionHandler.HandlerStart.Offset, 
                                handler.ExceptionHandler.HandlerEnd.Previous.Offset));
            }
            foreach(var handler in TryBoundsToFinallyHandlers.Values)
            {
                boundsList.Add((handler.HandlerStart.Offset, 
                                handler.HandlerEnd.Previous.Offset));

                finallyBoundsList.Add((handler.HandlerStart.Offset,
                                       handler.HandlerEnd.Previous.Offset));
                finallyBoundsList.Add((handler.TryStart.Offset,
                                       handler.TryEnd.Previous.Offset));
            }
            boundsList.Sort ((x, y) => x.end.CompareTo(y.end));
            finallyBoundsList.Sort((x, y) => x.end.CompareTo(y.end));

            for (int i = 1; i < boundsList.Count; i++)
            {
                if (boundsList[i-1].end > boundsList[i].start)
                {
                    return false;
                }
            }
            for (int i = 1; i < finallyBoundsList.Count; i++)
            {
                if (finallyBoundsList[i - 1].end > finallyBoundsList[i].start)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Used for excluding translation of methods with finally blocks ending in throw.
        /// </summary>
        /// <returns><c>true</c> if there is no finally block that ends with throw, <c>false</c>
        /// otherwise.</returns>
        public bool NoFinallyEndWithThrow()
        {
            foreach(var finallyEnd in FinallyEndToHandler.Keys)
            {
                if (finallyEnd.OpCode.Code == Code.Throw)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the end offset for the most immediate surrounding exception handling block at a 
        /// given offset; returns the default handler end offset if the offset is not found within 
        /// exception handlers. Note the order of checks; for example, if an offset is in the try 
        /// of a try-catch, the try-catch try end is returned, as opposed to the try-finally 
        /// try end if there is an enclosing finally block.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>The end offset of the most immediate surrounding exception-handling block; 
        /// returns the default value if the offset is not in a block.</returns>
        public int GetBlockEndOffsetFromOffset(int offset)
        {
            if (TryOffsetToCatchHandlers.ContainsKey(offset))
            {
                return TryOffsetToCatchHandlers[offset][0].ExceptionHandler
                                                          .TryEnd
                                                          .Previous
                                                          .Offset;
            }
            else if (CatchOffsetToCatchHandler.ContainsKey(offset))
            {
                return CatchOffsetToCatchHandler[offset].ExceptionHandler
                                                        .HandlerEnd
                                                        .Previous
                                                        .Offset;
            }
            else if (TryOffsetToFinallyHandler.ContainsKey(offset))
            {
                return TryOffsetToFinallyHandler[offset].TryEnd.Previous.Offset;
            }

            else if (FinallyOffsetToFinallyHandler.ContainsKey(offset))
            {
                return FinallyOffsetToFinallyHandler[offset].HandlerEnd.Previous.Offset;
            }
            return DefaultHandlerEndOffset;
;
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
