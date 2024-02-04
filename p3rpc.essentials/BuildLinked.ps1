# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p3rpc.essentials/*" -Force -Recurse
dotnet publish "./p3rpc.essentials.csproj" -c Release -o "$env:RELOADEDIIMODS/p3rpc.essentials" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location