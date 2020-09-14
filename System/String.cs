// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using InferSharpModels;

namespace System
{
    public class String
    {
        public static bool IsNullOrWhiteSpace(string value)
        {
            var output = InferUndefined.bool_undefined();

            return (value == null) || output;
        }

        public static bool IsNullOrEmpty(string value)
        {
            var output = InferUndefined.bool_undefined();

            return (value == null) || output;
        }
    }
}
