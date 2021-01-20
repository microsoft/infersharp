// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Cilsil.Services
{
    internal interface IService
    {
        /// <summary>
        /// Execute the service
        /// </summary>
        /// <returns></returns>
        ServiceExecutionResult Execute();

        /// <summary>
        /// Execute the service following the result of a previous service execution
        /// </summary>
        ServiceExecutionResult ExecuteFromResult(ServiceExecutionResult result);
    }
}
