using Mono.Cecil.Cil;
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
        public readonly Dictionary<int, 
                                   (List<ExceptionHandlerNode>, int)> TryOffsetToCatchHandlers;

        /// <summary>
        /// Maps offsets within try-blocks of try-finally to the corresponding finally handler.
        /// </summary>
        public readonly Dictionary<int, (ExceptionHandler, int)> TryOffsetToFinallyHandler;

        /// <summary>
        /// Maps offsets within catch-blocks to the corresponding catch handler.
        /// </summary>
        public readonly Dictionary<int, (ExceptionHandlerNode, int)> CatchOffsetToCatchHandler;

        /// <summary>
        /// Maps offsets within finally-blocks to the corresponding finally handler and the .
        /// </summary>
        public readonly Dictionary<int, (ExceptionHandler, int)> FinallyOffsetToFinallyHandler;
        #endregion

        /// <summary>
        /// Used for describing the most closely nested handler for an offset.
        /// </summary>
        public enum MapType
        {
            /// <summary>
            /// Try offset to corresponding catch handler.
            /// </summary>
            TryToCatch,
            /// <summary>
            /// Try offset to corresponding finally handler.
            /// </summary>
            TryToFinally,
            /// <summary>
            /// Catch offset to corresponding catch handler.
            /// </summary>
            CatchToCatch,
            /// <summary>
            /// Finally offset to corresponding finally handler.
            /// </summary>
            FinallyToFinally,
            /// <summary>
            /// No handler associated with offset.
            /// </summary>
            None
        }

        /// <summary>
        /// Computes the narrowest interval containing the given offset. 
        /// </summary>
        /// <param name="instruction">The offset for which to contain the interval.</param>
        /// <returns>The MapType corresponding to the narrowest interval at the given 
        /// offset.</returns>
        public MapType GetMapTypeFromInstruction(Instruction instruction)
        {
            var offset = instruction.Offset;
            var mapType = MapType.None;
            var intervalContainingOffsetSize = int.MaxValue;
            if (TryOffsetToCatchHandlers.ContainsKey(offset) && 
                TryOffsetToCatchHandlers[offset].Item2 < intervalContainingOffsetSize)
            {
                mapType = MapType.TryToCatch;
                intervalContainingOffsetSize = TryOffsetToCatchHandlers[offset].Item2;
            }
            if (TryOffsetToFinallyHandler.ContainsKey(offset) &&
                TryOffsetToFinallyHandler[offset].Item2 < intervalContainingOffsetSize)
            {
                mapType = MapType.TryToFinally;
                intervalContainingOffsetSize = TryOffsetToFinallyHandler[offset].Item2;
            }
            if (CatchOffsetToCatchHandler.ContainsKey(offset) &&
                CatchOffsetToCatchHandler[offset].Item2 < intervalContainingOffsetSize)
            {
                mapType = MapType.CatchToCatch;
                intervalContainingOffsetSize = CatchOffsetToCatchHandler[offset].Item2;
            }
            if (FinallyOffsetToFinallyHandler.ContainsKey(offset) &&
                FinallyOffsetToFinallyHandler[offset].Item2 < intervalContainingOffsetSize)
            {
                mapType = MapType.FinallyToFinally;
            }
            //TODO: CHECK THAT ALL THE LEAVE/ENDFINALLY MAP TO CORRECT HANDLER
            return mapType;
        }

        /// <summary>
        /// The default handler end offset, overwritten when it can be found.
        /// </summary>
        public const int DefaultHandlerEndOffset = -1;

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
                if (exceptionHandler.HandlerStart == null || exceptionHandler.HandlerEnd == null)
                {
                    UnhandledExceptionBlock = true;
                    break;
                }

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

            // need to redo here.
            foreach (var catchTry in TryBoundsToCatchHandlers.Keys)
            {
                var catchHandler = TryBoundsToCatchHandlers[catchTry][0].ExceptionHandler;
                // For a try-catch-finally block, the end of the finally block should be the target
                // of the catch blocks; the beginnings of the trys of the catch/finally blocks
                // should also match.
                var catchEnd = (Instruction) catchHandler.HandlerEnd.Previous.Operand;
                var catchStart = catchHandler.TryStart;
                if (TryBoundsToFinallyHandlers.ContainsKey((catchStart, catchEnd)))
                {
                    foreach (var handlerNode in TryBoundsToCatchHandlers[catchTry])
                    {
                        handlerNode.FinallyBlock = 
                            TryBoundsToFinallyHandlers[(catchStart, catchEnd)];
                    }
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
            var offset = instruction.Offset;
            switch (GetMapTypeFromInstruction(instruction))
            {
                case MapType.TryToCatch:
                    return TryOffsetToCatchHandlers[offset].Item1[0].ExceptionHandler;
                case MapType.TryToFinally:
                    return TryOffsetToFinallyHandler[offset].Item1;
                case MapType.CatchToCatch:
                    return CatchOffsetToCatchHandler[offset].Item1.ExceptionHandler;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Used for excluding translation of methods with finally blocks ending in throw.
        /// </summary>
        /// <returns><c>true</c> if there is no finally block that ends with throw, <c>false</c>
        /// otherwise.</returns>
        public bool NoFinallyEndWithThrow()
        {
            foreach (var finallyEnd in FinallyEndToHandler.Keys)
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
        /// <param name="instruction">The offset.</param>
        /// <returns>The end offset of the most immediate surrounding exception-handling block; 
        /// returns the default value if the offset is not in a block.</returns>
        public int GetBlockEndOffsetFromInstruction(Instruction instruction)
        {
            var offset = instruction.Offset;
            var mapType = GetMapTypeFromInstruction(instruction);
            if (mapType == MapType.TryToCatch)
            {
                return TryOffsetToCatchHandlers[offset].Item1[0]
                                                       .ExceptionHandler
                                                       .TryEnd
                                                       .Previous
                                                       .Offset;
            }
            else if (mapType == MapType.CatchToCatch)
            {
                return CatchOffsetToCatchHandler[offset].Item1
                                                        .ExceptionHandler
                                                        .HandlerEnd
                                                        .Previous
                                                        .Offset;
            }
            else if (mapType == MapType.TryToFinally)
            {
                return TryOffsetToFinallyHandler[offset].Item1.TryEnd.Previous.Offset;
            }

            else if (mapType == MapType.FinallyToFinally)
            {
                return FinallyOffsetToFinallyHandler[offset].Item1.HandlerEnd.Previous.Offset;
            }
            return DefaultHandlerEndOffset;
        }

        private static Dictionary<int, (T, int)> ConvertBoundsToOffsets<T>(
            Dictionary<(Instruction, Instruction), T> boundsToObject)
        {
            var converted = new Dictionary<int, (T, int)>();
            foreach ((var start, var end) in boundsToObject.Keys)
            {
                // We track the narrowest of all of the intervals of this type containing the
                // offset.
                for (int i = start.Offset; i <= end.Offset; i++)
                {
                    if (!converted.ContainsKey(i) ||
                            end.Offset - start.Offset < converted[i].Item2)
                    {
                        converted[i] = (boundsToObject[(start, end)], end.Offset - start.Offset);
                    }
                }
            }
            return converted;
        }
    }
}
