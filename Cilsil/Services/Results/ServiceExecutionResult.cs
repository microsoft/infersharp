// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Cilsil.Services
{
    internal abstract class ServiceExecutionResult
    {
        public ServiceExecutionResult PreviousResult { get; set; }

        public ServiceExecutionResult ThenExecute(IService service)
        {
            var result = service.ExecuteFromResult(this);
            result.PreviousResult = this;
            return result;
        }

        /// <summary>
        /// Get the latest result of a given type
        /// </summary>
        /// <typeparam name="T">Result type, must be subtype of 
        /// <see cref="ServiceExecutionResult"/></typeparam>
        /// <returns>Latest result of a given type if exists, null otherwise</returns>
        public T GetResult<T>() where T : ServiceExecutionResult
        {
            if (this is T)
            {
                return (T)this;
            }

            return PreviousResult?.GetResult<T>();
        }
    }
}
