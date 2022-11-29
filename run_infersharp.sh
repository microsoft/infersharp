#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

set -e

# Check if we have enough arguments.
if [ "$#" -lt 1 ]; then
    echo "run_infersharp.sh <dll_folder_path> <options - see https://fbinfer.com/docs/man-infer-run#OPTIONS>"
	exit
fi

infer_args=""

if [ "$#" -gt 1 ]; then
    i=2
    while [ $i -le $# ]
    do 		
        if [ ${!i} == "--output-folder" ]; then
            ((i++))
            output_folder=${!i}        
		else
			infer_args+="${!i} "
		fi
        ((i++))
    done
fi

echo "Processing {$1}"
# Preparation
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"
if [ -d infer-staging ]; then rm -Rf infer-staging; fi
mkdir infer-staging

echo -e "Copying binaries to a staging folder...\n"
shopt -s globstar

# Find all .dlls with matching .pdbs within the same folder and copy them in pairs
for f in "$1"/**; do
	if [ "${f##*.}" == "dll" ]; then
		if [ -f "${f%.*}.pdb" ]; then
			cp -n "$f" "${f%.*}.pdb" infer-staging
		fi
	fi
done

# Iterate again to copy all .dlls that have no matching .pdbs to cover the scenario where .pdb is embedded in .dll
for f in "$1"/**; do
	if [ "${f##*.}" == "dll" ]; then
		cp -n "$f" infer-staging
	fi
done

# Run InferSharp analysis.
echo -e "Code translation started..."
./Cilsil/Cilsil translate infer-staging --outcfg infer-staging/cfg.json --outtenv infer-staging/tenv.json --cfgtxt infer-staging/cfg.txt --extprogress
echo -e "Code translation completed. Analyzing...\n"
infer run $infer_args --cfg-json infer-staging/cfg.json --tenv-json infer-staging/tenv.json

if [ "$output_folder" != "" ]; then
    if [ ! -d "$output_folder" ]; then
        mkdir "$output_folder"
    fi

    cp infer-out/report.sarif infer-out/report.txt $output_folder/
    echo -e "\nFull reports available at '$output_folder'\n"
fi
