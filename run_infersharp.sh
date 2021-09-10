#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check if we have enough arguments.
if [ "$#" -ne 1 ]; then
    echo "run_infersharp.sh <dll_folder_path> -- requires 1 argument (dll_folder_path)"
    exit
fi

echo "Processing {$1}"
# Preparation
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"
if [ -d infer-out ]; then rm -Rf infer-out; fi
if [ -d infer-staging ]; then rm -Rf infer-staging; fi
coreLibraryPath=Cilsil/System.Private.CoreLib.dll
echo "Copy binaries to a staging folder..."
mkdir infer-staging
cp -r $coreLibraryPath $1 infer-staging

# Run InferSharp analysis.
./Cilsil/Cilsil translate infer-staging --outcfg infer-staging/cfg.json --outtenv infer-staging/tenv.json --cfgtxt infer-staging/cfg.txt
echo -e "\e[1;33mYou may see 'Unable to parse instruction xxx' above. This is expected as we have not yet translated all the CIL instructions, which follow a long tail distribution. We are continuing to expand our .NET translation coverage. \e[0m\n"
echo -e "Translation completed. Analyzing...\n"
infer capture 
mkdir infer-out/captured 
infer $(infer help --list-issue-types 2> /dev/null | grep ':true:' | cut -d ':' -f 1 | sed -e 's/^/--disable-issue-type /') --enable-issue-type NULL_DEREFERENCE --enable-issue-type DOTNET_RESOURCE_LEAK --enable-issue-type THREAD_SAFETY_VIOLATION analyzejson --debug --cfg-json infer-staging/cfg.json --tenv-json infer-staging/tenv.json