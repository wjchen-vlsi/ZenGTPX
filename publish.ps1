param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$project = Join-Path $root "src\ZenGTPX\ZenGTPX.csproj"
$dist = Join-Path $root "dist"
$scoopDotnet = Join-Path $env:USERPROFILE "scoop\apps\dotnet8-sdk\current\dotnet.exe"
$dotnet = if (Test-Path $scoopDotnet) { $scoopDotnet } else { "dotnet" }

& $dotnet publish $project `
    -c $Configuration `
    -p:Platform=x86 `
    -r win-x86 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:PublishDir="$dist\"

Copy-Item -LiteralPath (Join-Path $root "config\zen7.cfg") -Destination (Join-Path $dist "zen7.cfg") -Force
Copy-Item -LiteralPath (Join-Path $root "config\zen7_zh-TW.cfg") -Destination (Join-Path $dist "zen7_zh-TW.cfg") -Force

Remove-Item -LiteralPath (Join-Path $dist "ZenGTPX.pdb") -Force -ErrorAction SilentlyContinue
