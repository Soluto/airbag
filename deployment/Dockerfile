FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY src/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine
RUN addgroup dotnet && adduser -D -G dotnet -h /home/dotnet dotnet
USER dotnet
WORKDIR /home/dotnet/app
COPY --from=build-env /app/out .
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "airbag.dll"]