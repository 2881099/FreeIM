FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["imServer/imServer.csproj", "imServer/"]
COPY ["ImCore/ImCore.csproj", "ImCore/"]
RUN dotnet restore "imServer/imServer.csproj"
COPY . .
WORKDIR "/src/imServer"
RUN dotnet build "imServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "imServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "imServer.dll"]