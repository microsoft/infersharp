// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Examples
{
    public class NullDereference
    {
        public void NullDereferenceExample()
        {
            string testString = null;
            _ = testString.Length;
        }
    }
}