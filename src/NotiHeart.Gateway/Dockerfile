FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/NotiHeart.Gateway/NotiHeart.Gateway.csproj"
RUN dotnet publish "src/NotiHeart.Gateway/NotiHeart.Gateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "NotiHeart.Gateway.dll"]
