<#
Rebuilds runtimes/win-x64/native/miniaudio.dll from native/src/miniaudio.h.

Requires the "Desktop development with C++" workload (MSVC) for Visual Studio.
Run this manually whenever miniaudio.h is upgraded; the produced .dll is checked
into the repo under runtimes/win-x64/native so consumers don't need a C toolchain
to build Panther.Core.Audio.
#>
$ErrorActionPreference = 'Stop'

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vsPath = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) {
    throw "Could not find a Visual Studio installation with the C++ (VC.Tools.x86.x64) workload."
}
$vcvarsall = Join-Path $vsPath 'VC\Auxiliary\Build\vcvarsall.bat'

$root = Split-Path -Parent $PSScriptRoot
$srcFile = Join-Path $root 'native\src\miniaudio_impl.c'
$outDir = Join-Path $root 'runtimes\win-x64\native'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outDll = Join-Path $outDir 'miniaudio.dll'

$cmd = "`"$vcvarsall`" x64 && cl /LD /O2 /DNDEBUG /nologo /Fe:`"$outDll`" `"$srcFile`" /link Ole32.lib"
& cmd /c $cmd
if ($LASTEXITCODE -ne 0) {
    throw "Native build failed with exit code $LASTEXITCODE"
}

Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $outDir 'miniaudio.exp'), (Join-Path $outDir 'miniaudio.lib'), (Join-Path $root 'native\src\miniaudio_impl.obj')

Write-Host "Built $outDll"
