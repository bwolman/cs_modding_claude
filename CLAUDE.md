# Cities: Skylines II Modding — Claude Instructions

## Project Structure

Every mod repo follows this layout:

```
ModName/
├── ModName.sln
├── CLAUDE.md
├── .gitignore
├── .editorconfig
├── README.md
├── LICENSE                  # MIT
├── CHANGELOG.md
├── src/
│   └── ModName/
│       ├── ModName.csproj   # .NET Standard 2.1
│       ├── Mod.cs           # Entry point (implements IMod)
│       ├── Settings/        # Mod settings classes
│       ├── Systems/         # ECS game systems (one per file)
│       ├── UI/              # UI components
│       ├── Patches/         # Harmony patches
│       └── Resources/       # Localization, embedded assets
├── docs/
└── tools/
```

## Source Code Rules

- **One mod per repo**, trunk-based branching (main + short-lived feature branches)
- **SemVer** for releases (Major.Minor.Patch)
- **Never commit** game DLLs, build output, or local config
- Build with `dotnet build` — post-build step auto-deploys to game's Mods/ folder

## C# Conventions

- **Microsoft C# naming**: PascalCase for types/methods/properties, camelCase for locals/params, `_camelCase` for private fields
- **One ECS system per file**, named after the system class
- **Soft limit ~300 lines per file** — split large classes
- **Comprehensive XML doc comments** (`///`) on all members
- **Defensive error handling** — try/catch around risky operations, log all exceptions
- **Logging** via CS2's `LogManager` (`ILog`)

## CS2-Specific Patterns

- Entry point: class implementing `IMod` with `OnLoad`/`OnDispose`
- Target: **.NET Standard 2.1** (game uses Unity 2022.3.7f1 Mono runtime)
- Reference game assemblies from `Cities2_Data/Managed/` with Copy Local = No
- Use ECS systems (`GameSystemBase`) for simulation logic
- Use Harmony (`HarmonyLib`) for patching existing game methods
- Store mod settings in `{EnvPath.kUserDataPath}/ModsSettings/YourMod/`
- Distribute via **PDX Mods**

## Logging Pattern

```csharp
internal static ILog Log { get; } = LogManager.GetLogger(nameof(ModName)).SetShowsErrorsInUI(true);
```

## Testing

- Build → auto-deploy → launch game → check logs → iterate
- No unit tests — verify behavior in-game with log output
- No standard test save required

## Documentation

Every mod includes:
- **README.md** — standardized template (see `docs/project-setup.md`)
- **CHANGELOG.md** — simple list of changes per version
- **LICENSE** — MIT

## Research

Before writing mod code, research the relevant game systems by decompiling game DLLs and tracing ECS components/systems. Follow the standards and use the template:

- `research/RESEARCH_STANDARDS.md` — phases, decompilation workflow, completeness criteria
- `research/TEMPLATE.md` — template for documenting research findings
- `research/decompiled/` — indexed decompiled source (not committed to git)
- `research/topics/` — completed research per topic

**Tools**: `ilspycmd` (requires .NET 8.0 runtime) for decompiling game assemblies from `Cities2_Data/Managed/`.

## Reference Docs

- `docs/coding-standards.md` — detailed C# and CS2 patterns
- `docs/project-setup.md` — setup guide and templates
- `docs/community-tools.md` — curated tools and resources
