param (
    [Parameter(Mandatory=$true)][string]$version,
    [Parameter(Mandatory=$true)][string]$apiKey
)

if (!(Get-Command -Name 'git' -ErrorAction SilentlyContinue)) {
  "git not found in PATH"
  Exit -1
}

git push origin $version
.\Binaries\nuget push MiscUtil.Compression.Vcdiff\bin\Release\MiscUtil.Compression.Vcdiff.$version.nupkg -Source https://api.nuget.org/v3/index.json -ApiKey $apiKey