// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.IO;

namespace Examples
{
    public class ResourceLeak
    {
        public void ResourceLeakExample()
        {
            var streamReader = new StreamReader("somefile.txt");
            streamReader.ReadToEnd();
            // FIXME: Should do streamReader.Close(), otherwise resource leak.
        }
    }
}