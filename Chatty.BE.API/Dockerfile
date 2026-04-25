FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["Chatty.BE.sln", "./"]
COPY ["global.json", "./"]
COPY ["Chatty.BE.API/Chatty.BE.API.csproj", "Chatty.BE.API/"]
COPY ["Chatty.BE.Infrastructure/Chatty.BE.Infrastructure.csproj", "Chatty.BE.Infrastructure/"]
COPY ["Chatty.BE.Application/Chatty.BE.Application.csproj", "Chatty.BE.Application/"]
COPY ["Chatty.BE.Domain/Chatty.BE.Domain.csproj", "Chatty.BE.Domain/"]

RUN dotnet restore "Chatty.BE.API/Chatty.BE.API.csproj"

FROM restore AS build
COPY . .
WORKDIR /src/Chatty.BE.API
RUN dotnet build "Chatty.BE.API.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "Chatty.BE.API.csproj" -c Release -o /app/publish --no-build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chatty.BE.API.dll"]
