FROM mcr.microsoft.com/azure-functions/node:3.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
     AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
     FUNCTIONS_V2_COMPATIBILITY_MODE=true \     
     AzureWebJobsStorage='' \
     AZURE_FUNCTIONS_ENVIRONMENT=Development \
     WEBSITE_HOSTNAME=localhost \
     WEBSITE_SITE_NAME=sqlarcmila

COPY ./bin/Debug/netcoreapp3.1 /home/site/wwwroot