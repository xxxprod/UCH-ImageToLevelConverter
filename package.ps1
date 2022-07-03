Remove-Item publish -Recurse -Force | Out-Null
dotnet publish -c RELEASE --output publish\bin

Copy-Item ./UCH-ImageToLevelConverter.cmd publish