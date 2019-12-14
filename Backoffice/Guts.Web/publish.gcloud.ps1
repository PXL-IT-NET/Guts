cd C:\Workspace\PXL-IT-NET\Guts\Backend\Guts.Web
dotnet restore
dotnet publish -c Release
gcloud config set project guts-web
gcloud app deploy .\bin\Release\netcoreapp2.1\publish\app.yaml
Y
gcloud app browse
