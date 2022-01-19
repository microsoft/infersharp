FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS base

FROM base AS backend

COPY . .
RUN cd /
RUN chmod +x build_csharp_models.sh && ./build_csharp_models.sh
RUN cp /infer-out/models.sql /infer-release/usr/local/lib/infer/infer/lib/models.sql
RUN dotnet publish -c Release Cilsil/Cilsil.csproj -r ubuntu.16.10-x64
RUN dotnet build Examples/Examples/Examples.csproj

FROM debian:bullseye-slim AS release
RUN apt-get update && apt-get install --yes --no-install-recommends curl ca-certificates
WORKDIR infersharp
COPY --from=backend /infer-release/usr/local /infersharp/infer
ENV PATH /infersharp/infer/bin:${PATH}
COPY --from=backend /Examples/Examples/bin/Debug/net5.0/ /infersharp/Examples/
COPY --from=backend /Cilsil/bin/Release/net5.0/ubuntu.16.10-x64/publish/ /infersharp/Cilsil/
COPY --from=backend run_infersharp.sh /infersharp/
COPY --from=backend /.build/NOTICE.txt /
COPY --from=backend LICENSE /