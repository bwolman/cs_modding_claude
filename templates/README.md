# ModName

Brief one-line description of what the mod does.

## Features

- Feature 1
- Feature 2

## Installation

1. Subscribe on [PDX Mods](https://mods.paradoxplaza.com/games/cities_skylines_2), or
2. Download the latest release and copy the DLL to your Mods folder

## Compatibility

- Game version: X.X.X
- Known conflicts: None known

## Building from Source

1. Clone the repo
2. Create `Directory.Build.props` with your game path:
   ```xml
   <Project>
     <PropertyGroup>
       <GamePath>/path/to/Cities Skylines II</GamePath>
     </PropertyGroup>
   </Project>
   ```
3. Run `dotnet build`

The mod DLL will auto-deploy to your game's Mods folder.

## License

MIT
