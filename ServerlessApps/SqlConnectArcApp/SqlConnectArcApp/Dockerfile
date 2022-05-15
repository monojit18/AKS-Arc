FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS base

COPY . /src/sqlconnectarcapp
RUN cd /src/sqlconnectarcapp && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:3.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV AzureWebJobsStorage=""

COPY --from=base ["/home/site/wwwroot", "/home/site/wwwroot"]