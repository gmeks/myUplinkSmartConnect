param([parameter(Mandatory = $false,Position = 0)][string] $targetVersion)

if ([string]::IsNullOrWhitespace($targetVersion))
{
    Write-Host No target version defined, using default
    $targetVersion = "1.0.0.0"
}

Write-Host Builds should be tagged with $targetVersion

dotnet clean xElectricityPriceApi/xElectricityPriceApi.csproj

Write-Host Building docker.
docker build -t reg.thexsoft.com:443/xelectricitypriceapi/api:$targetVersion -f DockerfileElPriceApi .
docker push reg.thexsoft.com:443/xelectricitypriceapi/api:$targetVersion
#docker buildx build --push --tag erlingsaeterdal/myuplinksmartconnect:$targetVersion --platform linux/amd64,linux/arm/v7,linux/arm64 .

Write-Host Building debug files.
dotnet build --configuration release MyUplink-smartconnect.sln
sentry-cli debug-files upload --include-sources -p xelectricitypriceapi xElectricityPriceApi\bin\Release\net9.0