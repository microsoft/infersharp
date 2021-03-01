FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS base

FROM base AS buildbackend
WORKDIR /app
# mkdir the man/man1 directory due to Debian bug #863199
RUN apt-get update && \
    mkdir -p /usr/share/man/man1 && \
    apt-get install --yes --no-install-recommends \
      autoconf \
      automake \
      bzip2 \
      cmake \
      curl \
      gcc \
      git \
      libc6-dev \
      libgmp-dev \
      libmpfr-dev \
      libsqlite3-dev \
      make \
      openjdk-11-jdk-headless \
      patch \
      pkg-config \
      python3.7 \
      python3-distutils \
      sqlite3 \
      sudo \
      unzip \
      zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

# Install opam 2
RUN curl -sL https://github.com/ocaml/opam/releases/download/2.0.6/opam-2.0.6-x86_64-linux > /usr/bin/opam && \
    chmod +x /usr/bin/opam

# Disable sandboxing
# Without this opam fails to compile OCaml for some reason. We don't need sandboxing inside a Docker container anyway.
RUN opam init --reinit --bare --disable-sandboxing

# Download the latest Infer master
RUN cd / && \
    git clone https://github.com/facebook/infer.git && \
    cd infer && \
    git reset --hard 285ddb4a98f337a40d61e73b7a0867e44fa4f042

# build in non-optimized mode by default to speed up build times
ENV BUILD_MODE=dev

# prevent exiting by compulsively hitting Control-D
ENV IGNOREEOF=9

# export `opam env`
ENV OPAM_SWITCH_PREFIX=/root/.opam/4.08.1
ENV CAML_LD_LIBRARY_PATH=/root/.opam/4.08.1/lib/stublibs:/root/.opam/4.08.1/lib/ocaml/stublibs:/root/.opam/4.08.1/lib/ocaml
ENV OCAML_TOPLEVEL_PATH=/root/.opam/4.08.1/lib/toplevel
ENV MANPATH=$MANPATH:/root/.opam/4.08.1/man
ENV PATH=/root/.opam/4.08.1/bin:$PATH

RUN cd / && \
    cd infer && \
    eval $(opam env) && \
    chmod +x ./build-infer.sh && \
    ./build-infer.sh java && \
    sudo make install && \
    cd .. && \
    rm -r infer

FROM buildbackend AS buildfrontend
WORKDIR /app
COPY . .
RUN chmod +x build_csharp_models.sh && ./build_csharp_models.sh
RUN dotnet test Cilsil.Test/Cilsil.Test.csproj
RUN dotnet publish -c Release Cilsil/Cilsil.csproj -r ubuntu.16.10-x64
RUN dotnet build Examples/Examples/Examples.csproj

FROM buildbackend AS release
WORKDIR /app
COPY --from=buildfrontend /app/Examples/Examples/bin/Debug/net5.0/ /app/Examples/
COPY --from=buildfrontend /app/Cilsil/bin/Release/net5.0/ubuntu.16.10-x64/publish/ /app/Cilsil/
COPY --from=buildfrontend /usr/local/lib/infer/infer/lib/models.sql /usr/local/lib/infer/infer/lib/models.sql
COPY --from=buildfrontend /app/run_infersharp.sh /app/
COPY --from=buildfrontend /app/.build/NOTICE.txt /app/
COPY --from=buildfrontend /app/LICENSE /app/