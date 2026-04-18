FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BerberApp.Api/BerberApp.Api.csproj", "BerberApp.Api/"]
COPY ["BerberApp.Application/BerberApp.Application.csproj", "BerberApp.Application/"]
COPY ["BerberApp.Domain/BerberApp.Domain.csproj", "BerberApp.Domain/"]
COPY ["BerberApp.Infrastructure/BerberApp.Infrastructure.csproj", "BerberApp.Infrastructure/"]

RUN dotnet restore "BerberApp.Api/BerberApp.Api.csproj"

COPY . .

WORKDIR "/src/BerberApp.Api"
RUN dotnet build "BerberApp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BerberApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN mkdir -p /app/logs
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BerberApp.Api.dll"]