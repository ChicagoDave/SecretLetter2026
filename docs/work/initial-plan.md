# Phase 1: OpenSilver Project Setup

## Context

The Silverlight 3 source code for *Jack Toresal and The Secret Letter* lives in three directories (`SecretLetter/`, `Textfyre.UI/`, `Textfyre.VM/`) with legacy `.csproj` files. We need to create a modern OpenSilver solution that can compile this source in-place — no file copying needed.

## Approach: In-Place Project Replacement

Instead of creating separate directories and copying source files (as the overview suggested), we'll create new SDK-style `.csproj` files **in the same directories** as the existing source. This eliminates Phase 2 entirely since the source files are already there.

- Back up old `.csproj` files by renaming to `.csproj.silverlight`
- Create new SDK-style `.csproj` files with OpenSilver references
- Create two new directories: `SecretLetter.Browser/` (Blazor WASM host) and `SecretLetter.Simulator/` (WPF simulator)
- Create a solution file at the root

## Steps

### 1. Check latest OpenSilver NuGet version
Run `dotnet package search OpenSilver --take 5` to confirm the latest version.

### 2. Back up legacy .csproj files
Rename:
- `Textfyre.VM/Textfyre.VM.csproj` → `Textfyre.VM/Textfyre.VM.csproj.silverlight`
- `Textfyre.UI/Textfyre.UI.csproj` → `Textfyre.UI/Textfyre.UI.csproj.silverlight`
- `SecretLetter/SecretLetter.csproj` → `SecretLetter/SecretLetter.csproj.silverlight`

### 3. Create `Textfyre.VM/Textfyre.VM.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Textfyre.VM</RootNamespace>
    <AssemblyName>Textfyre.VM</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="OpenSilver" Version="3.1.0" />
  </ItemGroup>
  <ItemGroup>
    <!-- Exclude legacy project files -->
    <None Remove="**/*.csproj.silverlight" />
    <Compile Remove="Properties/AssemblyInfo.cs" />
  </ItemGroup>
</Project>
```

### 4. Create `Textfyre.UI/Textfyre.UI.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Textfyre.UI</RootNamespace>
    <AssemblyName>Textfyre.UI</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="OpenSilver" Version="3.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Textfyre.VM\Textfyre.VM.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Exclude legacy/unwanted files -->
    <Compile Remove="Properties/AssemblyInfo.cs" />
    <Compile Remove="Service References/**" />
    <None Remove="Service References/**" />
    <None Remove="ServiceReferences.ClientConfig" />
  </ItemGroup>
</Project>
```

### 5. Create `SecretLetter/SecretLetter.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>SecretLetter</RootNamespace>
    <AssemblyName>SecretLetter</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="OpenSilver" Version="3.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Textfyre.UI\Textfyre.UI.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Exclude legacy/unwanted files -->
    <Compile Remove="Properties/AssemblyInfo.cs" />
    <Compile Remove="Service References/**" />
    <Compile Remove="GameFiles/GameFile.Designer.cs" />
    <None Remove="Service References/**" />
    <None Remove="ServiceReferences.ClientConfig" />
    <!-- Game assets as embedded resources -->
    <EmbeddedResource Include="GameFiles/**/*.ulx" />
    <EmbeddedResource Include="GameFiles/**/*.xml" />
    <Content Include="Images/**/*" />
    <Content Include="Fonts/**/*" />
  </ItemGroup>
</Project>
```

### 6. Create `SecretLetter.Browser/` (Blazor WASM host)

**`SecretLetter.Browser/SecretLetter.Browser.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>SecretLetter.Browser</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="8.0.*" PrivateAssets="all" />
    <PackageReference Include="OpenSilver.Browser" Version="3.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SecretLetter\SecretLetter.csproj" />
  </ItemGroup>
</Project>
```

**`SecretLetter.Browser/wwwroot/index.html`** — Standard OpenSilver Blazor host page
**`SecretLetter.Browser/Program.cs`** — Blazor entry point calling OpenSilver
**`SecretLetter.Browser/App.razor`** — Blazor root component
**`SecretLetter.Browser/Pages/Index.razor`** — Page hosting the Silverlight content

### 7. Create `SecretLetter.Simulator/` (WPF desktop simulator)

**`SecretLetter.Simulator/SecretLetter.Simulator.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWPF>true</UseWPF>
    <RootNamespace>SecretLetter.Simulator</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="OpenSilver.Simulator" Version="3.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SecretLetter\SecretLetter.csproj" />
  </ItemGroup>
</Project>
```

**`SecretLetter.Simulator/Startup.cs`** — WPF entry point launching the simulator

### 8. Create `SecretLetter.sln` at root
Solution containing all 5 projects with correct dependency references.

## Verification
1. Run `dotnet restore SecretLetter.sln` — should restore all NuGet packages
2. Run `dotnet build Textfyre.VM/Textfyre.VM.csproj` — expect compilation errors (Phase 3 work), but project structure should be valid
3. Verify solution loads all 5 projects

## Files Created/Modified
| Action | File |
|--------|------|
| Rename | `Textfyre.VM/Textfyre.VM.csproj` → `.csproj.silverlight` |
| Rename | `Textfyre.UI/Textfyre.UI.csproj` → `.csproj.silverlight` |
| Rename | `SecretLetter/SecretLetter.csproj` → `.csproj.silverlight` |
| Create | `Textfyre.VM/Textfyre.VM.csproj` (new SDK-style) |
| Create | `Textfyre.UI/Textfyre.UI.csproj` (new SDK-style) |
| Create | `SecretLetter/SecretLetter.csproj` (new SDK-style) |
| Create | `SecretLetter.Browser/SecretLetter.Browser.csproj` |
| Create | `SecretLetter.Browser/wwwroot/index.html` |
| Create | `SecretLetter.Browser/Program.cs` |
| Create | `SecretLetter.Browser/App.razor` |
| Create | `SecretLetter.Browser/Pages/Index.razor` |
| Create | `SecretLetter.Simulator/SecretLetter.Simulator.csproj` |
| Create | `SecretLetter.Simulator/Startup.cs` |
| Create | `SecretLetter.sln` |
