param (
    [Parameter(Mandatory=$false)]
    [ValidateSet("up", "down", "build", "migrate", "logs", "ps")]
    [string]$Action = "up"
)

$ComposeFile = "docker-compose.dev.yml"

switch ($Action) {
    "up" {
        docker-compose -f $ComposeFile up -d
    }
    "down" {
        docker-compose -f $ComposeFile down
    }
    "build" {
        docker-compose -f $ComposeFile build
    }
    "migrate" {
        docker run --rm -v "${PWD}:/src" -w /src --network chattybe_default mcr.microsoft.com/dotnet/sdk:10.0 bash -c 'dotnet tool install -g dotnet-ef && export PATH="$PATH:/root/.dotnet/tools" && dotnet restore Chatty.BE.sln && dotnet ef database update --project Chatty.BE.Infrastructure --startup-project Chatty.BE.Infrastructure'
    }
    "logs" {
        docker-compose -f $ComposeFile logs -f
    }
    "ps" {
        docker-compose -f $ComposeFile ps
    }
}
