#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function section {
    printf '#%.0s' {1..35}; echo
    echo $1
    printf '#%.0s' {1..35}; echo
}

section "Build base Infer"
# cd infer; ./build-infer.sh java; sudo make install; cd ..
cd infer && \
eval $(opam env) && \
chmod +x ./build-infer.sh ./autogen.sh && \
./build-infer.sh java && \
./autogen.sh && \
sudo make install && \    
cd ..

./build_csharp_models.sh