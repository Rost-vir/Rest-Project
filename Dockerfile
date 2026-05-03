FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AuctionSystem.API.csproj .
RUN dotnet restore AuctionSystem.API.csproj

COPY . .

RUN dotnet publish AuctionSystem.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/auction.db"

EXPOSE 5000

ENTRYPOINT ["dotnet", "AuctionSystem.API.dll"]
