﻿cd C:\Workspace\PXL-IT-NET\Guts\Backend\Guts.Api
dotnet restore
dotnet publish -c Release
gcloud config set project guts-api
gcloud app deploy .\bin\Release\netcoreapp2.0\publish\app.yaml
gcloud app browse