#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check if we have enough arguments.
if [ "$#" -ne 1 ]; then
    echo "run_infersharp.sh <dll_files_path> -- requires 1 argument (dll_files_path)"
    exit
fi

echo "Processing {$1}"
# Preparation
rm -r infer-out
coreLibraryPath=/app/Cilsil/System.Private.CoreLib.dll
echo $coreLibraryPath
echo "Copy binaries to a staging folder..."      
sudo cp $coreLibraryPath $1

# Run InferSharp analysis.
sudo dotnet /app/Cilsil/Cilsil.dll translate $1 --outcfg $1/cfg.json --outtenv $1/tenv.json --cfgtxt $1/cfg.txt
echo -e "\e[1;33mYou may see 'Unable to parse instruction xxx' above. This is expected as we have not yet translated all the CIL instructions, which follow a long tail distribution. We are continuing to expand our .NET translation coverage. \e[0m\n"
echo -e "Translation completed. Analyzing...\n"
sudo infer capture 
sudo mkdir infer-out/captured 
sudo infer $(infer help --list-issue-types 2> /dev/null | grep ':true:' | cut -d ':' -f 1 | sed -e 's/^/--disable-issue-type /') --enable-issue-type NULL_DEREFERENCE --enable-issue-type DOTNET_RESOURCE_LEAK --enable-issue-type THREAD_SAFETY_VIOLATION analyzejson --debug --cfg-json $1/cfg.json --tenv-json $1/tenv.json
