dotnet restore
$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:ASPNETCORE_URLS = "http://*:8096"
dotnet run
