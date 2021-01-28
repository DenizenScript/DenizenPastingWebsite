#!/bin/bash
dotnet restore
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://*:8096 dotnet run
