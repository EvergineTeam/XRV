<#
.SYNOPSIS
	Evergine XRV packaging helper script, (c) 2023 Evergine
.DESCRIPTION
	This script contains some helper functions needed for the package generation process.
.EXAMPLE

.LINK
	https://evergine.com/
#>

# Utility functions
function CreateDirectory([string]$directoryPath)
{
	$_ = New-Item -ItemType Directory -Force -Path $directoryPath
	Resolve-Path $directoryPath
}

function ClearDirectory([string]$directoryPath) {
	Remove-Item "$directoryPath\*" -Recurse -Force
}

function PrepareEnvironment()
{
	if (Get-Command "msbuild.exe" -ErrorAction SilentlyContinue) {
		Write-Host "Skip: msbuild is already in PATH"
		return;
	}

	# Create temp folder
	$tempFolder = "temp"
	New-Item -ItemType Directory -Force -Path $tempFolder
	
	# Add to path
	$toolsPath = Resolve-Path $tempFolder
	$env:Path = "$toolsPath;" + $env:Path

	# Download vswhere
	Write-Host "Downloading VSWhere tool"
	$vsWherePath = Join-Path -Path $tempFolder -ChildPath "vswhere.exe"
	Invoke-WebRequest "https://github.com/microsoft/vswhere/releases/download/3.1.1/vswhere.exe" -OutFile $vsWherePath

	# Invoke vswhere
	Write-Host "Determining MSBuild path"
	$msBuildPath = vswhere -prerelease -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1
	if (-Not $?) { exit $lastexitcode }

	# Update PATH to refer msbuild
	$msBuildDirPath = Resolve-Path $msBuildPath | Split-Path
	Write-Host "MSBuild is located at $msBuildDirPath"
	$env:Path = "$msBuildDirPath;" + $env:Path

	# Clean up
	Remove-Item $toolsPath -Recurse
}

function CheckEnvironmentVariable([string]$variable)
{
	Test-Path $variable
}

function CheckIfThisIsGitHub {
	CheckEnvironmentVariable("env:GITHUB_ENV")
}

function GetVersionNumber([string]$revision, [string]$suffix) {
	if ($suffix) {
		$suffix = "-$suffix";
	}
	
	$version = "$(Get-Date -Format "yyyy.M.dd").$revision$suffix"
	return $version;
}

function MsBuildRestoreProject([string]$projectFilePath, [bool]$useMRTKNuget) {
	msbuild $projectFilePath /t:restore /p:UseMRTKNuget=$useMRTKNuget
	CheckLastExitCode
}

function MsBuildProject([string]$projectFilePath, [string]$configuration, [bool]$useMRTKNuget) {
	msbuild $projectFilePath /p:Configuration=$configuration /p:UseMRTKNuget=$useMRTKNuget
	CheckLastExitCode
}

function MsBuildPackProject([string]$projectFilePath, [string]$configuration, [string]$outputPath, [string]$version, [bool]$useMRTKNuget) {
	msbuild $projectFilePath /t:pack /p:Configuration=$configuration /p:UseMRTKNuget=$useMRTKNuget /p:PackageOutputPath=$outputPath /p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg /p:version=$version
	CheckLastExitCode
}

function CheckLastExitCode {
	if (-Not $?) {
		Write-Host "Detected exit code $LastExitCode"
		throw "CheckLastExitCode: Error"
	}
}