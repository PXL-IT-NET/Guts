cd C:\Workspace\PXL-IT-NET\Guts\Backend\Guts.Api
dotnet restore
dotnet publish -c Release
gcloud config set project guts-api
gcloud app deploy .\bin\Release\netcoreapp2.1\publish\app.yaml
Y
gcloud app browse

