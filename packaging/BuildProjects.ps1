<#
.SYNOPSIS
	Evergine XRV NuGet packages generator script, (c) 2023 Evergine
.DESCRIPTION
	This script generates XRV nuget packages
.EXAMPLE

.LINK
	https://evergine.com/
#>

param (
	[Parameter(mandatory=$false)]
    [string]$version,
    
    [Parameter(mandatory=$true)]
    [ValidateSet('core', 'modules', 'all')]
    [string]$mode,

	[Parameter(mandatory=$false)]
    [bool]$createPackages = $false,

    [Parameter(mandatory=$false)]
    [string]$outputPath,

    [Parameter(mandatory=$false)]
    [string]$configuration = "Release",
    
    [Parameter(mandatory=$false)]
    [bool]$prepareEnvironment = $true,
    
    [Parameter(mandatory=$false)]
    [bool]$forceAllModules = $false
)

# Source helper functions
. "$PSScriptRoot/Helpers.ps1"

# Locate build tools and enter build environment
if ($prepareEnvironment) {
    Write-Host "Preparing build environment"
    PrepareEnvironment
}
else {
    Write-Host "Skip: build environment preparation"
}

# Ensure output directory exists
if ($createPackages) {
    $outputPath = CreateDirectory($outputPath)
    Write-Host "Output directory is $outputPath"
}
else {
    Write-Host "Skip: Output directory creation"
}

# Create NuGets for core library
if ($mode -eq 'core' -or $mode -eq 'all') {
    $coreProjectPath = Get-ChildItem Evergine.Xrv.Core.csproj -Recurse | Select-Object -ExpandProperty FullName
    Write-Host "Core project file path: $coreProjectPath"

    Write-Host "Restoring $coreProjectPath dependencies"
    MsBuildRestoreProject `
        -projectFilePath $coreProjectPath `
        -useMRTKNuget $createPackages

    if ($createPackages) {
        Write-Host "Create $coreProjectPath NuGet package"
        MsBuildPackProject `
            -projectFilePath $coreProjectPath `
            -configuration $configuration `
            -outputPath $outputPath `
            -version $version `
            -useMRTKNuget $true
    } else {
        Write-Host "Build $coreProjectPath"
        MsBuildProject `
            -projectFilePath $coreProjectPath `
            -configuration $configuration `
            -useMRTKNuget $false
    }
}
else {
    Write-Host "Skip: NuGet creation for core library"
}

# Create NuGets for modules library
if ($mode -eq 'modules' -or $mode -eq 'all') {
    $whiteListedModules = (Get-Content -Path .\packaging\whitelist.txt)

    Get-ChildItem -Path src/modules -Recurse -Filter '*.csproj' | 
    Where-Object { $_.Directory.Name -match "Evergine.Xrv.[^.]+$" } | 
    ForEach-Object { 
      if(-not ($_.Name -match "^Evergine.Xrv.([^.]+).csproj$")) {
        Write-Host "Skip: project name with incorrect format $($_.Name)"
        return;
      }

      $moduleName = $matches[1];

      if ($forceAllModules -or $whiteListedModules.Contains($moduleName)) {
        Write-Host "Restoring $moduleName dependencies"
        MsBuildRestoreProject `
            -projectFilePath $_.FullName `
            -useMRTKNuget $createPackages
        
        if ($createPackages) {
            Write-Host "Create $moduleName NuGet package"
            MsBuildPackProject `
                -projectFilePath $_.FullName `
                -configuration $configuration `
                -outputPath $outputPath `
                -version $version `
                -useMRTKNuget $true
        }
        else {
            Write-Host "Build $($_.FullName)"
            MsBuildProject `
                -projectFilePath $_.FullName `
                -configuration $configuration `
                -useMRTKNuget $false
        }
      }
      else {
        Write-Host "Skip: $($_.Name) module is not white-listed"
      }
    }
}
else {
    Write-Host "Skip: NuGet creation for modules"
}
