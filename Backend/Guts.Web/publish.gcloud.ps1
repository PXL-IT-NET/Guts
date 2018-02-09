cd C:\Workspace\PXL-IT-NET\Guts\Backend\Guts.Web
dotnet restore
dotnet publish -c Release
gcloud config set project guts-web
gcloud app deploy .\bin\Release\netcoreapp2.0\publish\app.yaml
gcloud app browse
