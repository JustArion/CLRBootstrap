# 2025 / github.com/JustArion
# CLRBootstrap Build Script

# idk another way to bind to the x86 CL and the x64 version on the fly
$cl = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC\14.42.34433\bin\Hostx86\x86\cl.exe'

$CXXFLAGS = '/EHs /Z7 /c /std:c++20'

$ErrorActionPreference = 'Stop'
$workingDirectory = Get-Location
function WriteInfo($str)
{
    Write-Host "[*] $str" -ForegroundColor Green
}
function NotifyExec($str)
{
    WriteInfo("Exec | $str")
    Invoke-Expression $str
}

function ResetWorkingDirectory
{
    cd $workingDirectory
}

try
{
    # Set the Working Directory to the Script directory
    NotifyExec("cd $PSScriptRoot")

    # We're building the .obj file first
    NotifyExec("cd ../src/Native")

    # Build the CLRBootstrap.obj to the nuget directory
    NotifyExec(".'$cl' $CXXFLAGS dllmain.cpp /Fo`"$(Join-Path $workingDirectory "runtimes\native\lib\CLRBootstrap.obj")`"")

    # We're building the C# library now
    NotifyExec("cd ../Managed")

    # Build the library to the nuget directory
    NotifyExec("dotnet publish Dawn.AOT.csproj --configuration Release --arch x86 --output bin")

    # Copy the library from the output directory to the nuget directory
    NotifyExec("cp bin/Dawn.AOT.dll '$(Join-Path $workingDirectory "\lib\net9.0-windows")'")

    # Copy the deps too
    NotifyExec("cp bin/Dawn.AOT.deps.json '$(Join-Path $workingDirectory "\lib\net9.0-windows")'")
}
finally
{
    ResetWorkingDirectory
}