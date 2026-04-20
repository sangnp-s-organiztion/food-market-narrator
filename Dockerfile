FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FoodMarketNarrator.Api/FoodMarketNarrator.Api.csproj FoodMarketNarrator.Api/
RUN dotnet restore FoodMarketNarrator.Api/FoodMarketNarrator.Api.csproj

COPY . .
RUN dotnet publish FoodMarketNarrator.Api/FoodMarketNarrator.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /src/FoodMarketNarrator.Maui/Resources/Images/ /app/wwwroot/maui-images/
COPY --from=build /src/FoodMarketNarrator.Maui/Resources/Narration/audio/ /app/wwwroot/maui-audios/

EXPOSE 10000
ENTRYPOINT ["dotnet", "food_market_narrator_api.dll"]
