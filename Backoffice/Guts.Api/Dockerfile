#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Guts.Api/Guts.Api.csproj", "Guts.Api/"]
COPY ["Guts.Bootstrapper/Guts.Bootstrapper.csproj", "Guts.Bootstrapper/"]
COPY ["Guts.Business/Guts.Business.csproj", "Guts.Business/"]
COPY ["Guts.Common/Guts.Common.csproj", "Guts.Common/"]
COPY ["Guts.Domain/Guts.Domain.csproj", "Guts.Domain/"]
COPY ["Guts.Infrastructure/Guts.Infrastructure.csproj", "Guts.Infrastructure/"]
RUN dotnet restore "Guts.Api/Guts.Api.csproj"
COPY . .
WORKDIR "/src/Guts.Api"
RUN dotnet build "Guts.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Guts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Guts.Api.dll"]