FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS base

FROM base AS backend
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
      opam \
      openjdk-11-jdk-headless \
      patch \
      patchelf \
      pkg-config \
      python3.9 \
      python3-distutils \
      sqlite3 \
      unzip \
      zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

# Disable sandboxing
# Without this opam fails to compile OCaml for some reason. We don't need sandboxing inside a Docker container anyway.
RUN opam init --reinit --bare --disable-sandboxing

# Download the latest Infer main
RUN cd / && \
    git clone https://github.com/facebook/infer.git

# build in non-optimized mode by default to speed up build times
ENV BUILD_MODE=dev

# prevent exiting by compulsively hitting Control-D
ENV IGNOREEOF=9

RUN cd /infer && \
    chmod +x ./build-infer.sh && \
    chmod +x ./autogen.sh

RUN cd /infer && ./build-infer.sh --only-setup-opam
RUN cd /infer && \
    eval $(opam env) && \
    ./autogen.sh && \
    ./configure --disable-c-analyzers --disable-erlang-analyzers

# Generate a release
RUN cd /infer && \
    make install-with-libs \
    BUILD_MODE=opt \
    PATCHELF=patchelf \
    DESTDIR="/infer-release" \
    libdir_relative_to_bindir="../lib"
    
ENV PATH /infer-release/usr/local/bin:${PATH}

COPY . .
RUN cd /
#RUN dotnet test Cilsil.Test/Cilsil.Test.csproj
RUN dotnet publish -c Release Cilsil/Cilsil.csproj -r linux-x64
RUN dotnet build Examples/proj/proj.csproj

FROM debian:bullseye-slim AS release
RUN apt-get update && apt-get install --yes --no-install-recommends curl ca-certificates
WORKDIR infersharp
COPY --from=backend /infer-release/usr/local /infersharp/infer
RUN ln -s /infersharp/infer/bin/infer /usr/local/bin/infer
ENV PATH /infersharp/infer/bin:${PATH}
COPY --from=backend /Examples/proj/bin/Debug/net6.0/  /infersharp/Examples/
COPY --from=backend /Cilsil/bin/Release/net6.0/linux-x64/publish/ /infersharp/Cilsil/
COPY --from=backend .inferconfig /infersharp/
COPY --from=backend run_infersharp.sh /infersharp/
COPY --from=backend /.build/NOTICE.txt /
COPY --from=backend LICENSE /
RUN chmod -R 777 .
