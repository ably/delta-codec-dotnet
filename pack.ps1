param (
    [Parameter(Mandatory=$true)][string]$version
)

if (!(Get-Command -Name 'git' -ErrorAction SilentlyContinue)) {
  "git not found in PATH"
  Exit -1
}

if (!(Get-Command -Name 'dotnet' -ErrorAction SilentlyContinue)) {
  "dotnet not found in PATH"
  Exit -2
}

git tag $version
dotnet restore
dotnet pack --configuration Release -p:Version=$version