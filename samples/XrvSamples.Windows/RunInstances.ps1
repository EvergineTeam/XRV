<#
.SYNOPSIS
	Runs an indeterminate number of instances of Windows sample, for testing purposes, (c) 2023 Evergine
.DESCRIPTION
	It creates an indeterminate number of instances of Windows sample, for testing purposes. Please, note that
    you should build project separately.
.EXAMPLE

.LINK
	https://evergine.com/
#>

param (
	[Parameter(mandatory=$false)]
    [int]$numberOfInstances = 1,
    
    [Parameter(mandatory=$false)]
    [int]$initialPort = 12360,

    [Parameter(mandatory=$false)]
    [string]$configuration = "Release"
)

for (;$numberOfInstances -gt 0;$numberOfInstances--) {
    $cmd = "& ./bin/$configuration/net8.0-windows/XrvSamples.Windows.exe -Port=$($initialPort + $numberOfInstances - 1)"
    Invoke-Expression $cmd &
}