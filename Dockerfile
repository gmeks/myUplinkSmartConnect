#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#docker build -t erlingsaeterdal/myuplinksmartconnect:1.0.0.1 .
#docker push erlingsaeterdal/myuplinksmartconnect:1.0.0.1
#docker tag erlingsaeterdal/myuplinksmartconnect:1.0.0.1 erlingsaeterdal/myuplinksmartconnect:latest
##docker push erlingsaeterdal/myuplinksmartconnect:latest


FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["myUplink/MyUplink-smartconnect.csproj", "myUplink/"]
RUN dotnet restore "myUplink/MyUplink-smartconnect.csproj"

COPY . .
WORKDIR "/src/myUplink"
RUN dotnet build "MyUplink-smartconnect.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyUplink-smartconnect.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyUplink-smartconnect.dll"]