# XRV

## Prerequisites

- Follow the installation instructions at https://learn.microsoft.com/en-us/windows/mixed-reality/develop/install-the-tools
- Install the matching Windows 10 SDK: look for the tag `<TargetPlatformVersion>[...]</TargetPlatformVersion>` at src/core/Evergine.Xrv.Core/Evergine.Xrv.Core.csproj, close to UWP constant definition
  - You may not find such in Visual Studio Installer, but Microsoft's website

## How to update to newer Engine version

1. Ensure MRTK is updated to new Evergine version.
2. Update Engine references in XRV. You can use Evergine Launcher for this (it will automatically update all references).
3. Update packages targets in _Directory.Build.props_ file.
```
	<ItemGroup Condition="'$(IsAddOnProject)'=='true'">
		<PackageReference Include="Evergine.Packages.Targets" Version="2023.9.21.1-nightly" />
	</ItemGroup>
```
4. **(If you want to create XRV public packages)** Remember to update _Directory.Build.props_ file to point to public MRTK version to be used.
```
    <ItemGroup Condition="'$(UseMRTKNuget)'=='true' And '$(ReferenceMRTK)'=='true'">
		<PackageReference Include="Evergine.MRTK" Version="2023.9.25.3-nightly" />
    </ItemGroup>
```