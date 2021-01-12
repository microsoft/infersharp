FROM mcr.microsoft.com/dotnet/core/sdk:5.0 AS base

FROM base AS buildbackend
WORKDIR /app
COPY infer /app/infer/
# mkdir the man/man1 directory due to Debian bug #863199
RUN apt-get update && \
    mkdir -p /usr/local/lib/infer/infer && \
    apt-get install --yes --no-install-recommends \
    autoconf \
    automake \
    bubblewrap \
    bzip2 \
    cmake \
    curl \
    g++ \
    gcc \
    git \
    libc6-dev \
    libgmp-dev \
    libmpfr-dev \
    libsqlite3-dev \
    make \
    openjdk-8-jdk-headless \
    patch \
    pkg-config \
    python2.7 \
    unzip \
    sudo \
    xz-utils \
    zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

# Install node.js
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash -
RUN apt-get install -y nodejs

# Some scripts in facebook-clang-plugins assume "python" is available
RUN cd /usr/local/bin && ln -s /usr/bin/python2.7 python

# Install opam 2
RUN curl -sL https://github.com/ocaml/opam/releases/download/2.0.2/opam-2.0.2-x86_64-linux > /usr/bin/opam && \
    chmod +x /usr/bin/opam

# Disable sandboxing
# Without this opam fails to compile OCaml for some reason. We don't need sandboxing inside a Docker container anyway.
RUN opam init --reinit --bare --disable-sandboxing

# build in non-optimized mode by default to speed up build times
ENV BUILD_MODE=default

# prevent exiting by compulsively hitting Control-D
ENV IGNOREEOF=9

# export `opam env`
ENV OPAM_SWITCH_PREFIX=/root/.opam/4.07.1
ENV CAML_LD_LIBRARY_PATH=/root/.opam/4.07.1/lib/stublibs:/root/.opam/4.07.1/lib/ocaml/stublibs:/root/.opam/4.07.1/lib/ocaml
ENV OCAML_TOPLEVEL_PATH=/root/.opam/4.07.1/lib/toplevel
ENV MANPATH=$MANPATH:/root/.opam/4.07.1/man
ENV PATH=/root/.opam/4.07.1/bin:$PATH

# Build Infer
RUN cd infer && \
    eval $(opam env) && \
    chmod +x ./build-infer.sh ./autogen.sh && \
    ./build-infer.sh java && \
    ./autogen.sh && \
    sudo make install && \    
    cd .. && \
    rm -r infer

FROM buildbackend AS buildfrontend
WORKDIR /app
COPY . .
RUN chmod +x build_csharp_models.sh && ./build_csharp_models.sh
RUN dotnet test Cilsil.Test/Cilsil.Test.csproj
RUN dotnet publish -c Release Cilsil/Cilsil.csproj -r ubuntu.16.10-x64
RUN dotnet publish -c Release AnalysisResultParser/AnalysisResultParser/AnalysisResultParser.csproj -r ubuntu.16.10-x64
RUN dotnet build Examples/Examples/Examples.csproj

FROM buildbackend AS release
WORKDIR /app
COPY --from=buildfrontend /app/AnalysisResultParser/AnalysisResultParser/bin/Release/net5.0/ubuntu.16.10-x64/publish/ /app/AnalysisResultParser/
COPY --from=buildfrontend /app/Examples/Examples/bin/Debug/net5.0/ /app/Examples/
COPY --from=buildfrontend /app/Cilsil/bin/Release/net5.0/ubuntu.16.10-x64/publish/ /app/Cilsil/
COPY --from=buildfrontend /usr/local/lib/infer/infer/lib/specs/ /usr/local/lib/infer/infer/lib/specs/
COPY --from=buildfrontend /app/run_infersharp.sh /app/
COPY --from=buildfrontend /app/.build/NOTICE.txt /app/
COPY --from=buildfrontend /app/LICENSE /app/
