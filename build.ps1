param([parameter(Mandatory = $false,Position = 0)][string] $targetVersion)

if ([string]::IsNullOrWhitespace($targetVersion))
{
    Write-Host No target version defined, using default
    $targetVersion = "1.0.0.0"
}

Write-Host Builds should be tagged with $targetVersion

if(Test-Path dist/ -PathType Container)
{
    Remove-Item dist/ -Recurse
}

dotnet clean myUplink/MyUplink-smartconnect.csproj

Write-Host Building windows,Linux and linux arm
dotnet publish myUplink/MyUplink-smartconnect.csproj -v m -c Release -r win-x64     -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -p:PublishTrimmed=true -p:ServerGarbageCollection=true  --output dist/Windows          /p:Version=$targetVersion /p:InformationalVersion=$targetVersion
dotnet publish myUplink/MyUplink-smartconnect.csproj -v m -c Release -r linux-x64   -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -p:PublishTrimmed=true -p:ServerGarbageCollection=true  --output dist/linux-x64    /p:Version=$targetVersion /p:InformationalVersion=$targetVersion
dotnet publish myUplink/MyUplink-smartconnect.csproj -v m -c Release -r linux-arm64 -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -p:PublishTrimmed=true -p:ServerGarbageCollection=true  --output dist/linux-arm64   /p:Version=$targetVersion /p:InformationalVersion=$targetVersion

Compress-Archive -Path dist/linux-x64/* -DestinationPath  dist/linux-x64.zip
Compress-Archive -Path dist/Windows/* -DestinationPath  dist/windows.zip
Compress-Archive -Path dist/linux-arm64/* -DestinationPath  dist/linux-arm64.zip

Remove-Item dist/linux-x64/  -Recurse
Remove-Item dist/linux-arm64/  -Recurse
Remove-Item dist/Windows/  -Recurse

Write-Host Building docker.
docker build -t erlingsaeterdal/myuplinksmartconnect:$targetVersion .
docker push erlingsaeterdal/myuplinksmartconnect:$targetVersion