#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function section {
    printf '#%.0s' {1..35}; echo
    echo $1
    printf '#%.0s' {1..35}; echo
}

section "Build Infer# and models"
dotnet clean Infersharp.sln
dotnet build Infersharp.sln

mkdir -p models_out

section "Extract CFGs from models"
dotnet \
Cilsil/bin/Debug/netcoreapp2.2/Cilsil.dll translate \
System/bin/Debug/netcoreapp2.2/System.dll \
--outcfg models_out/cfg.json \
--outtenv models_out/tenv.json \
--cfgtxt models_out/cfg.txt

section "Analyze model CFGs using Infer#"
cd infer
rm -rf infer-out
infer capture
mkdir infer-out/captured
infer analyzejson --debug \
--cfg-json ../models_out/cfg.json \
--tenv-json ../models_out/tenv.json

section "Move model specs to lib"
sudo cp -r infer-out/specs/. /usr/local/lib/infer/infer/lib/specs/

cd ..
