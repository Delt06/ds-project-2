# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app
RUN mkdir /output
EXPOSE 55555-55556:55555-55556

#copy csproj and restore as distinct layers
COPY . /app

WORKDIR /app/Client
RUN dotnet publish -c Release -o /output

# final stage/image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build /output .
ENTRYPOINT ["dotnet", "Client.dll"]