#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["xElectricityPriceApi/xElectricityPriceApi.csproj", "xElectricityPriceApi/"]
COPY ["xElectricityPriceApiShared/xElectricityPriceApiShared.csproj", "xElectricityPriceApiShared/"]
RUN dotnet restore "xElectricityPriceApi/xElectricityPriceApi.csproj"
COPY . .
WORKDIR "/src/xElectricityPriceApi"
RUN dotnet build "xElectricityPriceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "xElectricityPriceApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "xElectricityPriceApi.dll"]