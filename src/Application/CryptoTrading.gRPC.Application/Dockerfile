FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Application/CryptoTrading.gRPC.Application/CryptoTrading.gRPC.Application.csproj", "CryptoTrading.gRPC.Application/"]
RUN dotnet restore "src/Application/CryptoTrading.gRPC.Application/CryptoTrading.gRPC.Application.csproj"
COPY . .
WORKDIR "/src/CryptoTrading.gRPC.Application"
RUN dotnet build "CryptoTrading.gRPC.Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CryptoTrading.gRPC.Application.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CryptoTrading.gRPC.Application.dll"]
