param (
    [Parameter(Mandatory=$true)][string]$version
)

$msbuild = Binaries\vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe -version '[15.1,)' | select-object -first 1
if (!$msbuild) {
  "Failed to find MSBuild 15.1+"
  Exit -1
}

if (!(Get-Command -Name 'git' -ErrorAction SilentlyContinue)) {
  "git not found in PATH"
  Exit -2
}

git tag $version
.\Binaries\nuget restore
& $msbuild --% /t:Pack /p:Configuration=Release