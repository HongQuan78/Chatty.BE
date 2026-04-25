#!/bin/bash

ACTION=${1:-up}
COMPOSE_FILE="docker-compose.dev.yml"

case $ACTION in
  "up")
    docker-compose -f $COMPOSE_FILE up -d
    ;;
  "down")
    docker-compose -f $COMPOSE_FILE down
    ;;
  "build")
    docker-compose -f $COMPOSE_FILE build
    ;;
  "migrate")
    echo "Running migrations in Docker to bypass WDAC restrictions and adhere to Clean Architecture..."
    MSYS_NO_PATHCONV=1 docker run --rm -v "${PWD}:/src" -w /src --network chattybe_default mcr.microsoft.com/dotnet/sdk:10.0 bash -c 'dotnet tool install -g dotnet-ef && export PATH="$PATH:/root/.dotnet/tools" && dotnet restore Chatty.BE.sln && dotnet ef database update --project Chatty.BE.Infrastructure --startup-project Chatty.BE.Infrastructure'
    ;;
  "logs")
    docker-compose -f $COMPOSE_FILE logs -f
    ;;
  "ps")
    docker-compose -f $COMPOSE_FILE ps
    ;;
  "test")
    echo "Running tests in Docker to bypass WDAC restrictions..."
    docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test Chatty.BE.sln
    ;;
  *)
    echo "Usage: ./dev.sh {up|down|build|migrate|test|logs|ps}"
    exit 1
    ;;
esac
