// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;

namespace Cilsil.Services.Results
{
    internal class TenvParserResult : ServiceExecutionResult
    {
        public TypeEnvironment TypeEnvironment { get; }

        public TenvParserResult(TypeEnvironment typeEnvironment)
        {
            TypeEnvironment = typeEnvironment;
        }
    }
}
