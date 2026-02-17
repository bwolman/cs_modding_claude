# Research: Mod Loading & Dependencies

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 discovers, loads, and initializes mods at startup. The IMod interface lifecycle, dependency resolution, PDX Mods integration, and inter-mod communication patterns.

**Why**: To understand the exact mod lifecycle so modders can properly initialize systems, declare dependencies, handle load order, and safely interact with other mods.

**Boundaries**: Mod settings UI is covered in Mod Options UI research. Harmony patching mechanics are covered in Harmony Transpilers research. This focuses on the loading pipeline itself.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Modding | IMod, ModManager, ModSetting |
| Game.dll | Game | UpdateSystem, SystemUpdatePhase, GameSystemBase |
| Game.dll | Game.SceneFlow | GameManager (entry point orchestrator) |
| Colossal.IO.AssetDatabase.dll | Colossal.IO.AssetDatabase | ExecutableAsset, AssetDatabase (mod discovery) |
| Colossal.PSI.PdxSdk.dll | Colossal.PSI.PdxSdk | PdxSdkPlatform (PDX Mods integration) |

## Key Concepts

### IMod Interface

The single entry point for all CS2 code mods:

```csharp
public interface IMod
{
    void OnLoad(UpdateSystem updateSystem);
    void OnDispose();
}
```

- `OnLoad` is called once when the mod is loaded. The `UpdateSystem` parameter is used to register custom ECS systems.
- `OnDispose` is called when the mod is unloaded (game exit or mod disabled).

### ModManager

Central class that orchestrates all mod loading. It is owned by `GameManager` and processes mods during game initialization.

**Load sequence**:
1. `RegisterMods()` -- discovers all ExecutableAsset entries from AssetDatabase
2. `ExecutableAsset.ResolveModAssets()` -- resolves dependencies between mods
3. `InitializeMods()` -- loads each mod's assembly and calls IMod.OnLoad
4. `InitializeUIModules()` -- registers UI module assets

### ModInfo States

Each mod goes through a state machine during loading:

| State | Description |
|-------|-------------|
| Unknown | Initial state, not yet processed |
| Loaded | Successfully loaded and initialized |
| Disposed | Successfully disposed |
| IsNotModWarning | Assembly does not contain an IMod implementation |
| IsNotUniqueWarning | Duplicate mod detected |
| GeneralError | Unspecified error during loading |
| MissedDependenciesError | Required dependencies not found |
| LoadAssemblyError | Failed to load the mod's DLL |
| LoadAssemblyReferenceError | Failed to load a referenced DLL |

### ExecutableAsset

Represents a mod's DLL in the asset database. Key properties:
- `isMod` -- whether this asset contains an IMod implementation
- `isReference` -- whether this is a shared library (not a mod itself)
- `isUnique` -- no duplicate exists
- `canBeLoaded` -- all dependencies are resolved
- `isLoaded` -- assembly has been loaded
- `isBursted` -- has Burst-compiled code
- `references` -- dictionary of assembly references (name -> resolved asset or null)

### UpdateSystem

Passed to `IMod.OnLoad()`, allows mods to register custom ECS systems into the game's update loop.

**Registration methods**:

| Method | Description |
|--------|-------------|
| `UpdateAt<T>(phase)` | Register system at a specific update phase |
| `UpdateBefore<T>(phase)` | Register to update before other systems in the phase |
| `UpdateAfter<T>(phase)` | Register to update after other systems in the phase |
| `UpdateBefore<T, Other>(phase)` | Register to update before a specific other system |
| `UpdateAfter<T, Other>(phase)` | Register to update after a specific other system |

### SystemUpdatePhase

The game's update loop is divided into ordered phases:

| Phase | Description |
|-------|-------------|
| MainLoop | Main loop entry |
| Modification1-5 | Sequential modification passes |
| PreSimulation | Before simulation tick |
| GameSimulation | Main simulation tick |
| PostSimulation | After simulation tick |
| Rendering | Render frame |
| PreTool / ToolUpdate / PostTool / ApplyTool / ClearTool | Tool system phases |
| UIUpdate | UI data updates |
| UITooltip | Tooltip updates |
| PrefabUpdate | Prefab system updates |
| Serialize / Deserialize | Save/load phases |
| Cleanup | End-of-frame cleanup |

## Component Map

### `ModSetting` (Game.Modding)

Abstract base class for mod settings. Extends the game's `Setting` class.

| Property | Type | Description |
|----------|------|-------------|
| mod | IMod | Reference to the owning mod instance |
| id | string | Unique ID: `{AssemblyName}.{Namespace}.{ClassName}` |
| name | string | Setting class name |
| keyBindingRegistered | bool | Whether key bindings have been registered |

Key methods:
- `RegisterInOptionsUI()` -- adds settings to the game's options menu
- `UnregisterInOptionsUI()` -- removes settings from options menu

## System Map

### `ModManager` (Game.Modding)

- **Type**: Regular class (not ECS system), implements IDisposable
- **Owner**: GameManager.instance.modManager
- **Key responsibilities**:
  1. Discover mod DLLs via AssetDatabase
  2. Resolve assembly dependencies between mods
  3. Load assemblies and find IMod implementations
  4. Call IMod.OnLoad on each mod
  5. Handle errors and display notifications
  6. Register UI modules from UIModuleAsset entries

### `UpdateSystem` (Game)

- **Base class**: GameSystemBase
- **Key responsibility**: Manages the ordered execution of all game systems across update phases
- **Used by mods**: Passed to IMod.OnLoad to register custom systems

## Data Flow

```
GAME STARTUP
  GameManager creates ModManager
        |
        v
  ModManager.Initialize(updateSystem)
        |
        v
  PHASE 1: RegisterMods()
    AssetDatabase.global scans for ExecutableAsset entries
    Each .dll with IMod implementation gets a ModInfo
    Maps by Identifier (mod ID)
        |
        v
  PHASE 2: ExecutableAsset.ResolveModAssets()
    For each mod, resolve assembly references
    Check: isMod, isUnique, canBeLoaded
    Mark missing dependencies
        |
        v
  PHASE 3: InitializeMods()
    For each ModInfo in order:
      Skip if not required or already processed
      Check: isMod? isUnique? canBeLoaded?
      Load assembly via ExecutableAsset.LoadAssembly()
        -> TypeManager.InitializeAdditionalTypes(assembly)
        -> SerializerSystem.SetDirty()
      Find all types implementing IMod via reflection
      Create instances (FormatterServices.GetUninitializedObject)
      Call IMod.OnLoad(updateSystem) for each instance
      State -> Loaded
        |
        v
  PHASE 4: InitializeUIModules()
    Scan for UIModuleAsset entries
    Register UI host locations
    Add active UI mod locations to AppBindings
        |
        v
  MOD IS ACTIVE
    Custom systems run in their registered phases
    Harmony patches are active
    Settings are registered
        |
        v
  GAME SHUTDOWN
    ModManager.Dispose()
      For each ModInfo:
        Call IMod.OnDispose()
        State -> Disposed
```

## Prefab & Configuration

### Mod Discovery Path

Mods are discovered from the AssetDatabase, which scans:
- PDX Mods subscription directory (managed by Paradox launcher)
- Local mods directory: `{EnvPath.kUserDataPath}/Mods/`

### Mod Assembly Structure

A valid mod requires:
- A .NET Standard 2.1 DLL
- At least one class implementing `IMod`
- Proper assembly references (game assemblies with Copy Local = No)

### Dependency Declaration

Dependencies are declared through standard .NET assembly references. The `ExecutableAsset.ResolveModAssets()` method:
1. Reads each mod's assembly references via Cecil
2. Attempts to resolve each reference against other registered mods and game assemblies
3. If any reference cannot be resolved, sets `canBeLoaded = false` and state = `MissedDependenciesError`

## System Replacement Pattern

A common community modding pattern is to replace vanilla ECS systems entirely with custom implementations. This is more reliable than Harmony-patching complex systems with Burst-compiled jobs, since the jobs themselves cannot be patched.

### Disabling Vanilla Systems

Any `GameSystemBase` can be disabled at runtime by setting `.Enabled = false`. The game's update loop skips disabled systems. This is done in `IMod.OnLoad()`:

```csharp
public void OnLoad(UpdateSystem updateSystem)
{
    // Disable the vanilla system
    var vanillaSystem = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<Game.Simulation.ResidentialDemandSystem>();
    vanillaSystem.Enabled = false;

    // Register your replacement system in the same phase
    updateSystem.UpdateAt<CustomResidentialDemandSystem>(
        SystemUpdatePhase.GameSimulation);
}
```

### Registering Replacement Systems with UpdateAt

The replacement system must be registered in the same `SystemUpdatePhase` as the vanilla system it replaces. Use `UpdateAfter` or `UpdateBefore` if ordering relative to other systems matters:

```csharp
// Replace ZoneSpawnSystem with a custom version
updateSystem.UpdateBefore<CustomZoneSpawnSystem,
    Game.Simulation.ZoneSpawnSystem>(
    SystemUpdatePhase.GameSimulation);
```

The replacement system should implement the same public interface (properties, methods) that other systems depend on. For example, if replacing `ResidentialDemandSystem`, the replacement must expose `householdDemand`, `buildingDemand`, and the `GetLowDensityDemandFactors()` method, because `ZoneSpawnSystem` and UI systems read these.

### Inter-Mod Detection

When multiple mods might replace the same system, mods should detect each other to avoid conflicts. Common patterns:

1. **Check if system is already disabled**: Before disabling a vanilla system, check if another mod already did:
   ```csharp
   var vanillaSystem = World.DefaultGameObjectInjectionWorld
       .GetOrCreateSystemManaged<Game.Simulation.SomeSystem>();
   if (vanillaSystem.Enabled)
   {
       vanillaSystem.Enabled = false;
       // Register replacement
   }
   else
   {
       Log.Warn("SomeSystem already disabled by another mod, skipping replacement");
   }
   ```

2. **Assembly reference check**: Reference the other mod's assembly and check for its presence:
   ```csharp
   bool otherModLoaded = AppDomain.CurrentDomain.GetAssemblies()
       .Any(a => a.GetName().Name == "OtherModAssembly");
   ```

3. **Shared marker component**: Define a tag component that replacement systems add to a well-known entity, allowing other mods to detect which systems have been replaced.

### Caveats

- The disabled vanilla system's `OnCreate` still runs (it was created before your mod loaded). Only `OnUpdate` is skipped.
- If the vanilla system implements `IDefaultSerializable` or `ISerializable`, disabling it does NOT disable its serialization. The serialization libraries call `Serialize`/`Deserialize` directly, bypassing the `Enabled` flag. Your replacement system should implement the same interfaces if it needs to persist state.
- Burst-compiled jobs inside the vanilla system are not affected by `.Enabled = false` -- they simply never get scheduled because `OnUpdate` is skipped.

## Harmony Patch Points

### Candidate 1: `Game.Modding.ModManager.InitializeMods`

- **Signature**: `private void InitializeMods(UpdateSystem updateSystem)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept mod loading, modify load order, add custom initialization
- **Risk level**: High (core mod infrastructure)

### Candidate 2: `Game.Modding.ModManager.RegisterMods`

- **Signature**: `private void RegisterMods()`
- **Patch type**: Postfix
- **What it enables**: Inject additional mods, modify mod discovery
- **Risk level**: High

## Mod Blueprint

- **Entry point**: Implement `IMod` interface with `OnLoad`/`OnDispose`
- **System registration**: Use `UpdateSystem.UpdateAt<T>(phase)` in OnLoad
- **Settings**: Extend `ModSetting`, call `RegisterInOptionsUI()` in OnLoad
- **Harmony**: Create `Harmony` instance, call `PatchAll()` in OnLoad, `UnpatchAll()` in OnDispose
- **Logging**: Use `LogManager.GetLogger(nameof(YourMod))` for game-integrated logging

## Examples

### Example 1: Basic Mod Entry Point

The minimal IMod implementation that registers a custom system.

```csharp
using Game;
using Game.Modding;
using Colossal.Logging;

public class MyMod : IMod
{
    internal static ILog Log = LogManager.GetLogger(nameof(MyMod))
        .SetShowsErrorsInUI(true);

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info("MyMod loaded");

        // Register a custom system in the simulation phase
        updateSystem.UpdateAt<MyCustomSystem>(SystemUpdatePhase.GameSimulation);
    }

    public void OnDispose()
    {
        Log.Info("MyMod disposed");
    }
}
```

### Example 2: Mod with Harmony Patches

Initialize Harmony in OnLoad and clean up in OnDispose.

```csharp
using Game;
using Game.Modding;
using HarmonyLib;

public class MyHarmonyMod : IMod
{
    private Harmony _harmony;

    public void OnLoad(UpdateSystem updateSystem)
    {
        _harmony = new Harmony("com.myname.mymod");
        _harmony.PatchAll(typeof(MyHarmonyMod).Assembly);

        updateSystem.UpdateAt<MySystem>(SystemUpdatePhase.GameSimulation);
    }

    public void OnDispose()
    {
        _harmony?.UnpatchAll("com.myname.mymod");
    }
}
```

### Example 3: Mod with Settings

Register settings that appear in the game's options menu.

```csharp
public class MyMod : IMod
{
    private MyModSettings _settings;

    public void OnLoad(UpdateSystem updateSystem)
    {
        _settings = new MyModSettings(this);
        _settings.RegisterInOptionsUI();

        // Register localization for settings labels
        GameManager.instance.localizationManager.AddSource(
            "en-US", new MyModLocale());
    }

    public void OnDispose()
    {
        _settings?.UnregisterInOptionsUI();
    }
}

public class MyModSettings : ModSetting
{
    [SettingsUISection("General")]
    public bool EnableFeature { get; set; } = true;

    [SettingsUISection("General")]
    [SettingsUISlider(min = 0.1f, max = 5.0f, step = 0.1f)]
    public float Multiplier { get; set; } = 1.0f;

    public MyModSettings(IMod mod) : base(mod) { }
}
```

### Example 4: System Registration with Ordering

Register systems with explicit update ordering relative to game systems.

```csharp
public class MyMod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        // Run before the simulation tick
        updateSystem.UpdateAt<PreProcessSystem>(SystemUpdatePhase.PreSimulation);

        // Run during simulation, after a specific game system
        updateSystem.UpdateAfter<MySimSystem, Game.Simulation.StorageCompanySystem>(
            SystemUpdatePhase.GameSimulation);

        // Run during UI update phase
        updateSystem.UpdateAt<MyUISystem>(SystemUpdatePhase.UIUpdate);

        // Run during tool update
        updateSystem.UpdateAt<MyToolSystem>(SystemUpdatePhase.ToolUpdate);
    }

    public void OnDispose() { }
}
```

### Example 5: Get Mod's Own Asset Information

Access the ExecutableAsset to find the mod's installation path and metadata.

```csharp
public class MyMod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        var modManager = GameManager.instance.modManager;
        if (modManager.TryGetExecutableAsset(this, out var asset))
        {
            Log.Info($"Mod name: {asset.fullName}");
            Log.Info($"Mod path: {asset.path}");
            Log.Info($"Is local: {asset.isLocal}");
            Log.Info($"Is Bursted: {asset.isBursted}");

            // Use path for loading additional resources
            string modDirectory = Path.GetDirectoryName(asset.path);
            string configPath = Path.Combine(modDirectory, "config.json");
        }
    }

    public void OnDispose() { }
}
```

## Open Questions

- [ ] **Load order guarantees**: The order mods are loaded depends on the order they appear in the AssetDatabase scan. There is no explicit load-order declaration. Mods that depend on other mods should use assembly references to ensure correct ordering.
- [ ] **Hot-reloading**: ModManager.RequireRestart() exists but actual hot-reloading (unload + reload without game restart) is not supported for code mods. UI modules can be added/removed dynamically.
- [ ] **Inter-mod API pattern**: There is no official inter-mod communication API. Mods typically expose public static classes/methods and reference each other via assembly references.
- [ ] **PDX Mods dependency declaration**: How dependencies are declared in the PDX Mods platform metadata (separate from assembly references) needs testing with the Paradox launcher.

## Sources

- Decompiled from: Game.dll — Game.Modding.IMod, Game.Modding.ModManager, Game.Modding.ModSetting, Game.UpdateSystem, Game.SystemUpdatePhase
- Asset system: Colossal.IO.AssetDatabase.dll — Colossal.IO.AssetDatabase.ExecutableAsset
- CS2 Code Modding Dev Diary: https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding
