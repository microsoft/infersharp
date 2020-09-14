// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Utils;
using System;

namespace Cilsil.Services
{
    /// <summary>
    /// Executions thrown by different services composing the CILSIL pipeline.
    /// </summary>
    /// <seealso cref="Exception" />
    public class ServiceExecutionException : Exception
    {
        /// <summary>
        /// The program state; used to provide state information for the exception message.
        /// </summary>
        public ProgramState ProgramState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceExecutionException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="state">The state.</param>
        public ServiceExecutionException(string message, ProgramState state = null) : base(message)
        {
            ProgramState = state;
        }
    }
}
