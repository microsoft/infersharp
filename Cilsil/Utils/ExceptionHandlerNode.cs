using Mono.Cecil.Cil;

namespace Cilsil.Utils
{
    /// <summary>
    /// Wrapper around exception handlers to reflect the sequence of clauses in a catch partial
    /// function.
    /// </summary>
    public class ExceptionHandlerNode
    {
        /// <summary>
        /// The exception handler.
        /// </summary>
        public readonly ExceptionHandler ExceptionHandler;
        /// <summary>
        /// The next catch clause.
        /// </summary>
        public ExceptionHandlerNode NextCatchBlock;
        /// <summary>
        /// The finally block.
        /// </summary>
        public ExceptionHandler FinallyBlock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlerNode"/> class.
        /// </summary>
        /// <param name="exceptionHandler">The exception handler.</param>
        /// <param name="nextCatchBlock">The next catch clause.</param>
        /// <param name="finallyBlock">The finally block.</param>
        public ExceptionHandlerNode(ExceptionHandler exceptionHandler,  
                                    ExceptionHandlerNode nextCatchBlock = null, 
                                    ExceptionHandler finallyBlock = null)
        {
            ExceptionHandler = exceptionHandler;
            NextCatchBlock = nextCatchBlock;
            FinallyBlock = finallyBlock;
        }
    }

}
