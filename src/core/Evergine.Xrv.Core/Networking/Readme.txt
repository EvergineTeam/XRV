To make your app discoverable, you need to disable isolation in UWP to make it work
https://docs.microsoft.com/en-us/windows/uwp/communication/interprocess-communication

We provide necessary commands below, but if you want to have a different package working, read the following.
To get package family name you need to execute the following in classic PowerShell console. 
Important: can't execute this in PS Core console, as it seems that Appx is bugged there
https://github.com/PowerShell/PowerShell/issues/13138

Import-Module Appx
Get-AppxPackage | Where-Object { $_.Name.Contains("Xrv") } | Select PackageFamilyName

checknetisolation loopbackexempt -a -n="XrvSamples.UWP_7z9n4gweseebp"

# this last command needs to be run as admin, and remain open to enable loopback exempt
checknetisolation loopbackexempt -is -n="XrvSamples.UWP_7z9n4gweseebp"