# Community Tools & Resources

## Modding Frameworks

| Tool | Purpose | Link |
|------|---------|------|
| BepInEx 6 (Mono) | Plugin loader framework | [GitHub](https://github.com/BepInEx/BepInEx) |
| HarmonyX | Runtime method patching | [GitHub](https://github.com/BepInEx/HarmonyX) |
| PDXModsBridge | Use IMod interface on Thunderstore | [GitHub](https://github.com/Cities2Modding/PDXModsBridge) |

## References & Guides

| Resource | Description |
|----------|-------------|
| [Cities2Modding](https://github.com/optimus-code/Cities2Modding) | Community info dump and guides |
| [CS2 Modding Guide](https://github.com/ps1ke/Cities-Skylines-2-Modding-Guide) | Beginner walkthrough |
| [Paradox Wiki - Modding](https://cs2.paradoxwikis.com/Modding) | Official modding wiki |
| [PDX Mods](https://mods.paradoxplaza.com/games/cities_skylines_2) | Official mod distribution |

## Game Internals

- **Unity version**: 2022.3.7f1
- **Runtime**: Mono / .NET Standard 2.1
- **Key assemblies**: `Game.dll`, Colossal Order libraries in `Cities2_Data/Managed/`
- **ECS**: Unity DOTS-style Entity Component System
- **UI**: Coherent/Cohtml-based UI framework

## Key Namespaces

| Namespace | Contains |
|-----------|----------|
| `Game` | Core game systems |
| `Game.Simulation` | Simulation logic (economy, traffic, etc.) |
| `Game.UI` | UI systems and bindings |
| `Game.Net` | Network/road systems |
| `Game.Zones` | Zoning systems |
| `Game.Buildings` | Building systems |
| `Game.Common` | Shared components and utilities |
| `Game.Debug` | Logging and debug tools |
| `Colossal.IO` | IO utilities |
| `Colossal.Mathematics` | Math helpers |

## File Locations

| Path | Contents |
|------|----------|
| `Cities2_Data/Managed/` | Game assemblies (reference these) |
| `AppData/LocalLow/Colossal Order/Cities Skylines II/Mods/` | Local mod deployment |
| `AppData/LocalLow/Colossal Order/Cities Skylines II/Logs/` | Mod log output |
| `AppData/LocalLow/Colossal Order/Cities Skylines II/ModsSettings/` | Mod settings storage |
