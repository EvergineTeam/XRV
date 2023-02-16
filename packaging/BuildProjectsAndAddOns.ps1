<#
.SYNOPSIS
	Evergine XRV packages generator script, (c) 2023 Evergine
.DESCRIPTION
	This script generates XRV nuget and add-on packages
.EXAMPLE

.LINK
	https://evergine.com/
#>

param (
	[Parameter(mandatory=$true)]
	[string]$version,
	
    [Parameter(mandatory=$false)]
    [string]$outputNugetsPath = "nugets",

    [Parameter(mandatory=$false)]
    [string]$outputAddOnsPath = "add-ons",

    [Parameter(mandatory=$false)]
    [string]$configuration = "Release",
    
    [Parameter(mandatory=$false)]
    [bool]$forceAllModules = $false
)

# Source helper functions
. "$PSScriptRoot/Helpers.ps1"

# Locate build tools and enter build environment
Write-Host "Preparing build environment"
PrepareEnvironment

# Creating NuGet packages
Write-Host "Creating NuGet packages for core and modules"
. "$PSScriptRoot/BuildProjects.ps1" `
	-version $version `
	-configuration $configuration `
	-mode all `
	-createPackages $true `
	-outputPath $outputNugetsPath `
	-prepareEnvironment $true `
	-forceAllModules $forceAllModules

# Creating add-on packages
Write-Host "Creating add-on packages for core and modules"
. "$PSScriptRoot/CreateAddOns.ps1" `
	-version $version `
	-configuration $configuration `
	-mode all `
	-outputPath $outputAddOnsPath `
	-prepareEnvironment $true `
	-forceAllModules $forceAllModules