FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet add package Datadog.Trace.ClrProfiler.Managed -v 1.11.0 \
    && dotnet add package Datadog.Trace -v 1.11.0 \
    && dotnet add package Datadog.Trace.AspNet -v 1.11.0 \
    && dotnet add package Datadog.Trace.OpenTracing -v 1.11.0
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.5-alpine
RUN addgroup dotnet && adduser -D -G dotnet -h /home/dotnet dotnet

# Add Datadog tracer
RUN mkdir -p /opt/datadog \
    && wget -O- https://github.com/DataDog/dd-trace-dotnet/releases/download/v1.11.0/datadog-dotnet-apm-1.11.0.tar.gz | tar xzf - -C /opt/datadog \
    && apk add libc6-compat
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json
ENV DD_DOTNET_TRACER_HOME=/opt/datadog

USER dotnet
WORKDIR /home/dotnet/app
COPY --from=build-env /app/out .
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "airbag.dll"]