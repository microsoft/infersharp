#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check if we have enough arguments.
if [ "$#" -ne 2 ] && [ "$#" -ne 3 ] ; then
    echo "run_infersharp.sh <dll_files_path> <output_path> <report_On_file1,report_on_file2,...> -- requires 2 arguments (dll_files_path & output_path), 1 optional (report_on_files)"
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
sudo infer capture 
sudo mkdir infer-out/captured 
sudo infer analyzejson --debug --cfg-json $1/cfg.json --tenv-json $1/tenv.json
reportonfiles=$(echo $3 | sed 's/ /,/g')
sudo dotnet /app/AnalysisResultParser/AnalysisResultParser.dll infer-out/bugs.txt infer-out/filtered_bugs.txt $reportonfiles
mkdir -p $2
sudo cp infer-out/filtered_bugs.txt $2
echo "Bug report is ready at $2/"
