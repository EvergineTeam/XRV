<#
.SYNOPSIS
	Evergine XRV add-ons generator script, (c) 2023 Evergine
.DESCRIPTION
	This script generates XRV add-on packages
.EXAMPLE

.LINK
	https://evergine.com/
#>

param (
	[Parameter(mandatory=$true)]
    [string]$version,
    
    [Parameter(mandatory=$true)]
    [ValidateSet('core', 'modules', 'all')]
    [string]$mode,

    [Parameter(mandatory=$true)]
    [string]$outputPath,

    [Parameter(mandatory=$false)]
    [string]$configuration = "Release",
    
    [Parameter(mandatory=$false)]
    [bool]$prepareEnvironment = $true,
    
    [Parameter(mandatory=$false)]
    [bool]$forceAllModules = $false
)

function UpdateAddOnSpecFile([string]$assetsProjectPath, [string]$version) {
    $assetsProjectFolder = (Get-Item $assetsProjectPath).Directory.FullName
    Get-ChildItem -Path $assetsProjectFolder -Filter '*.wespec' |
        ForEach-Object {
            Write-Host "Reading contents for $($_.FullName)"
            $contents = Get-Content -Raw -Path $_.FullName | ConvertFrom-Yaml -Ordered
            $contents.Nugets = $contents.Nugets -replace "2023.0.0.0-preview$", $version

            Write-Host "Updating contents for $($_.FullName)"
            ConvertTo-Yaml -Data $contents | Out-File -FilePath $_.FullName
            Get-Content $_.FullName
        }
}

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
$outputPath = CreateDirectory($outputPath)
Write-Host "Output directory is $outputPath"

# Create add-on packages
$createCorePackage = $mode -eq 'core' -or $mode -eq 'all'
$createModulePackages = $mode -eq 'modules' -or $mode -eq 'all'
$whiteListedModules = (Get-Content -Path .\packaging\whitelist.txt)

Get-ChildItem -Path src -Recurse -Filter '*.Assets.csproj' | 
    Foreach-Object { 
        if(-not ($_.Name -match "^Evergine.Xrv.([^.]+).Assets.csproj$")) {
            Write-Host "Skip: project name with incorrect format $($_.Name)"
            return;
        }

        $moduleName = $matches[1];
        $isCore = $moduleName -eq 'Core'

        if ($isCore -and -not $createCorePackage) {
            Write-Host "Skip: Core add-on package"
            return;
        }
        
        if (-not $isCore -and -not $createModulePackages) {
            Write-Host "Skip: $moduleName add-on package"
            return;
        }
    
        if (($isCore -and $createCorePackage) `
            -or (-not $isCore -and $createModulePackages -and $forceAllModules) `
            -or (-not $isCore -and $createModulePackages -and $whiteListedModules.Contains($moduleName))) {
            Write-Host "Update $moduleName spec file"
            UpdateAddOnSpecFile $_.FullName $version
            
            Write-Host "Create add-on package for $moduleName"
            MsBuildRestoreProject `
                -projectFilePath $_.FullName `
                -useMRTKNuget $true
                
            msbuild $_.FullName /p:version=$version
        }
        else {
          Write-Host "Skip: $($_.Name) module is not white-listed"
        }
    }

# Collect generated packages
Get-ChildItem -Path ".\*.wepkg" -Recurse | Move-Item -Destination $outputPath -Force