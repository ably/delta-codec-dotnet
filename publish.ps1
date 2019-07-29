param (
    [Parameter(Mandatory=$true)][string]$version,
    [Parameter(Mandatory=$true)][string]$apiKey
)

if (!(Get-Command -Name 'git' -ErrorAction SilentlyContinue)) {
  "git not found in PATH"
  Exit -1
}

if (!(Get-Command -Name 'dotnet' -ErrorAction SilentlyContinue)) {
  "dotnet not found in PATH"
  Exit -2
}

git push origin $version
dotnet nuget push MiscUtil.Compression.Vcdiff\bin\Release\MiscUtil.Compression.Vcdiff.$version.nupkg --source https://api.nuget.org/v3/index.json --api-key $apiKey