# Project Setup

## Prerequisites

- .NET SDK (for `dotnet build`)
- Cities: Skylines II installed
- Any C# IDE or editor

## Creating a New Mod

1. Copy the files from `templates/` into a new repo
2. Rename all instances of `ModName` to your mod's name
3. Update the game assembly path in the `.csproj` to match your install location
4. Run `dotnet build` — the mod should compile and auto-deploy

## csproj Configuration

Key settings in your `.csproj`:

- **TargetFramework**: `netstandard2.1`
- **Game references**: Point to `Cities2_Data/Managed/`, set `Private="false"` (don't copy game DLLs)
- **Post-build**: Copy output DLL to the game's Mods folder

The game path should be configurable via a property so each developer can set their own:

```xml
<GamePath Condition="'$(GamePath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II</GamePath>
```

Override it locally without touching the csproj:

```xml
<!-- Directory.Build.props (git-ignored) -->
<Project>
  <PropertyGroup>
    <GamePath>/your/custom/path</GamePath>
  </PropertyGroup>
</Project>
```

## README Template

Every mod README should include these sections:

```markdown
# Mod Name

Brief one-line description of what the mod does.

## Features

- Feature 1
- Feature 2

## Installation

1. Subscribe on PDX Mods, or
2. Download the latest release and copy to your Mods folder

## Compatibility

- Game version: X.X.X
- Known conflicts: (list or "None known")

## Building from Source

1. Clone the repo
2. Set your game path in `Directory.Build.props`
3. Run `dotnet build`

## License

MIT
```

## CHANGELOG Format

Simple list per version:

```markdown
# Changelog

## 1.1.0
- Added feature X
- Fixed bug with Y

## 1.0.0
- Initial release
```

## Versioning

Use SemVer — `Major.Minor.Patch`:
- **Major**: Breaking changes or major feature overhauls
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes
