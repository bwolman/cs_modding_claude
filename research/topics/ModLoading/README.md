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
| Modification1 | First modification pass |
| Modification2 | Second modification pass |
| Modification3 | Third modification pass |
| Modification4 | Fourth modification pass |
| Modification4B | Extended fourth pass (post-Modification4) |
| Modification5 | Fifth modification pass |
| ModificationEnd | Final modification cleanup |
| PreSimulation | Before simulation tick |
| GameSimulation | Main simulation tick |
| PostSimulation | After simulation tick |
| Rendering | Render frame |
| Raycast | Raycast processing |
| PreTool | Before tool update |
| ToolUpdate | Tool system update |
| PostTool | After tool update |
| ApplyTool | Tool result application |
| ClearTool | Tool state cleanup |
| UIUpdate | UI data updates |
| UITooltip | Tooltip updates |
| PrefabUpdate | Prefab system updates |
| Serialize | Save game serialization |
| Deserialize | Load game deserialization |
| DebugGizmos | Debug visualization rendering |
| Cleanup | End-of-frame cleanup |

The two-type-parameter variants `UpdateBefore<T, TOther>` and `UpdateAfter<T, TOther>` order your system relative to a specific vanilla system within the same phase. Both systems must be in the same phase for the ordering to take effect:

```csharp
// TSystem runs before TOther within Modification4
updateSystem.UpdateBefore<CustomLaneSystem, Game.Net.LaneSystem>(
    SystemUpdatePhase.Modification4);

// TSystem runs after TOther within Rendering
updateSystem.UpdateAfter<CustomOverlaySystem, Game.Rendering.OverlayRenderSystem>(
    SystemUpdatePhase.Rendering);
```

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

### Full System Replacement: ResidentAISystem Example

Complex vanilla systems like `ResidentAISystem` use `NativeQueue` producer/consumer patterns internally. When replacing these, the replacement must replicate the queue interface so that other systems interacting with those queues continue to function:

```csharp
public class Mod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        // 1. Disable the vanilla system
        var vanillaSystem = World.DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<Game.Simulation.ResidentAISystem>();
        vanillaSystem.Enabled = false;

        // 2. Register replacement with explicit ordering
        updateSystem.UpdateAfter<CustomResidentAISystem,
            Game.Simulation.ResidentAISystem>(
            SystemUpdatePhase.GameSimulation);
    }
}

public partial class CustomResidentAISystem : GameSystemBase
{
    // Replicate the NativeQueue producer/consumer interface
    // that other systems (e.g., HouseholdBehaviorSystem) write to
    private NativeQueue<SetupQueueItem> m_SetupQueue;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_SetupQueue = new NativeQueue<SetupQueueItem>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        m_SetupQueue.Dispose();
        base.OnDestroy();
    }

    // Expose the queue writer so producer systems can enqueue work
    public NativeQueue<SetupQueueItem>.ParallelWriter GetSetupQueueWriter()
        => m_SetupQueue.AsParallelWriter();

    protected override void OnUpdate()
    {
        // Drain the queue and process items
        while (m_SetupQueue.TryDequeue(out var item))
        {
            // Custom processing logic replacing vanilla behavior
        }
    }
}
```

### Disabling with Enabled=false in OnCreate

An alternative to disabling vanilla systems in `IMod.OnLoad()` is to disable them from within the replacement system's `OnCreate()`. This keeps the disable logic co-located with the replacement:

```csharp
public partial class CustomZoneSpawnSystem : GameSystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        // Disable the vanilla system from within the replacement
        var vanillaSystem = World.DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<Game.Simulation.ZoneSpawnSystem>();
        vanillaSystem.Enabled = false;
    }

    protected override void OnUpdate()
    {
        // Replacement logic
    }
}

// In IMod.OnLoad -- register with UpdateAfter to maintain ordering
updateSystem.UpdateAfter<CustomZoneSpawnSystem,
    Game.Simulation.ZoneSpawnSystem>(SystemUpdatePhase.GameSimulation);
```

### Handling Inner Systems

Some vanilla systems contain inner system classes that must also be disabled. Check the decompiled source for nested `GameSystemBase` subclasses:

```csharp
protected override void OnCreate()
{
    base.OnCreate();

    // Disable main system
    var mainSystem = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<Game.Simulation.SomeSystem>();
    mainSystem.Enabled = false;

    // Also disable inner systems that the main system delegates to
    var innerSystem = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<Game.Simulation.SomeSystem.InnerUpdateSystem>();
    innerSystem.Enabled = false;
}
```

### When NOT to Use Enabled=false Replacement

The full system replacement pattern is not always appropriate:

- **Simple calculation tweaks**: If you only need to change one formula or constant, a Harmony patch is simpler than reimplementing the entire system
- **UI-only modifications**: Patching a UI method is lower risk than replacing the whole UI system
- **Systems with many dependents**: If dozens of other systems read from the vanilla system's public properties, replicating the entire interface is error-prone
- **Frequently updated systems**: Vanilla systems that change every game patch require constant maintenance of the full replacement

### Harmony Prefix as System Replacement Alternative

Instead of disabling vanilla systems in `OnLoad` or `OnCreate`, a Harmony prefix on `OnUpdate` can both disable the system and skip execution in a single patch:

```csharp
[HarmonyPatch(typeof(Game.Simulation.SomeVanillaSystem), "OnUpdate")]
public static class SomeVanillaSystemPatch
{
    public static bool Prefix(Game.Simulation.SomeVanillaSystem __instance)
    {
        // Disable the system so it won't be scheduled again
        __instance.Enabled = false;
        // Return false to skip the original OnUpdate execution
        return false;
    }
}
```

**Trade-offs vs Enabled=false in OnLoad**:

| Aspect | Harmony Prefix | Enabled=false in OnLoad |
|--------|---------------|------------------------|
| Timing | Runs on first OnUpdate tick | Runs before any simulation |
| First frame | Vanilla system runs once before patch fires | Never runs |
| Dependencies | Requires Harmony library | No extra dependencies |
| Discoverability | Patch is in a separate class | Disable logic is in OnLoad |
| Reversibility | Can unpatch cleanly | Must re-enable manually |

Use the Harmony prefix approach when you need the vanilla system to complete its first `OnCreate`/`OnUpdate` cycle before being replaced, or when you want to conditionally disable based on runtime state inspected during the first tick.

### Caveats

- The disabled vanilla system's `OnCreate` still runs (it was created before your mod loaded). Only `OnUpdate` is skipped.
- If the vanilla system implements `IDefaultSerializable` or `ISerializable`, disabling it does NOT disable its serialization. The serialization libraries call `Serialize`/`Deserialize` directly, bypassing the `Enabled` flag. Your replacement system should implement the same interfaces if it needs to persist state.
- Burst-compiled jobs inside the vanilla system are not affected by `.Enabled = false` -- they simply never get scheduled because `OnUpdate` is skipped.

### No-Harmony Architecture

Some of the most complex CS2 mods use **zero Harmony patches**, achieving all functionality through pure ECS patterns. The Traffic mod (krzychu124) demonstrates this at scale with a complete lane system replacement, custom tools, custom rendering, and custom serializable components — all without a single Harmony patch.

**How it works:**
1. **Full system replacement**: Disable vanilla systems with `Enabled = false` and register custom replacements
2. **Custom ToolBaseSystem subclasses**: The game's `ToolBaseSystem` API supports fully custom tools without patching
3. **Custom serializable components**: `ISerializable` provides clean data persistence without intercepting vanilla serialization
4. **Custom rendering overlays**: Register systems in the `Rendering` phase for overlay rendering

**Trade-offs vs Harmony patching:**

| Aspect | Pure ECS | Harmony Patching |
|--------|----------|-----------------|
| Mod conflicts | Minimal (each mod owns its systems) | High (multiple mods patch same method) |
| Game update resilience | Medium (system interfaces may change) | Low (method signatures change frequently) |
| Code complexity | Higher (must reimplement full systems) | Lower (small targeted patches) |
| Burst compatibility | Full (can use Burst-compiled jobs) | None (Harmony cannot patch Burst jobs) |
| Debugging | Standard C# debugging | Complex (injected IL hard to trace) |

**When to use pure ECS**: Complex mods that modify core simulation logic (traffic, pathfinding, demand), mods needing Burst-compiled jobs for performance, or mods where entire system behavior needs to change.

**When to use Harmony**: Small targeted tweaks (adjusting a single calculation), UI rendering modifications, or intercepting specific method calls without reimplementing the full system.

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

### Example 6: Find Mod Assembly Path via SearchFilter (Alternative)

An alternative to `TryGetExecutableAsset` for finding the mod's assembly path. Uses `SearchFilter<ExecutableAsset>` to query the asset database directly. This is useful when you don't have a reference to the `ModManager` or need to search for other mods' assets.

```csharp
using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;

public class MyMod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        // SearchFilter<ExecutableAsset> queries the AssetDatabase for
        // mod DLLs. Each result is an ExecutableAsset with path, name,
        // and metadata.
        foreach (var asset in AssetDatabase.global.GetAsset<ExecutableAsset>(
            SearchFilter<ExecutableAsset>.ByCondition(
                a => a.isLoaded && a.isMod)))
        {
            // Find our own mod's asset by assembly name match
            if (asset.assembly == typeof(MyMod).Assembly)
            {
                string modPath = asset.path;
                string modDir = System.IO.Path.GetDirectoryName(modPath);
                Log.Info($"Mod directory: {modDir}");
                break;
            }
        }

        // You can also search for a specific mod by name:
        // AssetDatabase.global.GetAsset<ExecutableAsset>(
        //     SearchFilter<ExecutableAsset>.ByCondition(
        //         a => a.name == "OtherModName"))
    }

    public void OnDispose() { }
}
```

**When to use which approach:**
- **`TryGetExecutableAsset`** (Example 5): Simplest way to find your own mod's asset. Requires access to `GameManager.instance.modManager` and passes `this` (the IMod instance).
- **`SearchFilter<ExecutableAsset>`** (Example 6): More flexible -- can search for any mod's assets by arbitrary conditions. Useful for inter-mod discovery or when you need to enumerate all loaded mods.

### Example 7: Detect Installed Mods via ModManager Iteration

Simple mod presence detection using `GameManager.instance.modManager` iteration. Best for basic boolean compatibility flags.

```csharp
public void OnLoad(UpdateSystem updateSystem)
{
    foreach (ModManager.ModInfo modInfo in GameManager.instance.modManager)
    {
        if (modInfo.asset.name.Equals("RWH"))
        {
            Log.Info("Found Realistic Workplaces and Households mod");
            // Adjust behavior accordingly
        }
    }
}
```

### Example 8: Detect Installed Mods via ListModsEnabled

Lightweight mod presence detection using `ModManager.ListModsEnabled()`. Returns assembly-qualified names, so use `StartsWith` for matching.

```csharp
// Cache with lazy evaluation
private static bool? isRoadBuilderEnabled;
public static bool IsRoadBuilderEnabled => isRoadBuilderEnabled ??=
    GameManager.instance.modManager.ListModsEnabled()
        .Any(x => x.StartsWith("RoadBuilder, "));

// ListModsEnabled returns strings like:
// "RoadBuilder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
```

**When to use which mod detection pattern:**
- **ModManager iteration** (Example 7): Simple loop, good for checking `modInfo.asset.name` (short name)
- **ListModsEnabled** (Example 8): Returns assembly-qualified names, good for `StartsWith` checks with caching
- **SearchFilter + AssetDatabase** (Example 6): Most flexible, access to mod assembly for reflection

### Example 9: Enabled=false Safety Pattern for One-Shot Systems

Systems that perform destructive operations should start disabled and re-disable after each execution. This prevents accidental runs.

```csharp
public partial class DestroyAllVegetationSystem : GameSystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        // Safety: system must be explicitly enabled by UI or other trigger
        Enabled = false;
    }

    protected override void OnUpdate()
    {
        // Perform destructive operation...
        DestroyAllTrees();

        // Re-disable immediately after execution
        Enabled = false;
    }

    // Called from settings UI or button handler
    public void TriggerDestruction() => Enabled = true;
}
```

**`Enabled = false` vs `RequireForUpdate`**: `RequireForUpdate(query)` disables based on entity query emptiness — the system auto-enables when matching entities exist. `Enabled = false` is explicit control — the system only runs when code explicitly sets `Enabled = true`. Use `Enabled = false` for dangerous one-shot operations; use `RequireForUpdate` for data-driven activation.

### Example 10: Conditional Attribute for Debug Logging

The `[Conditional]` attribute strips method calls entirely from Release builds at compile time. This provides zero-cost debug logging without `#if DEBUG` blocks around every call site:

```csharp
using System.Diagnostics;

public static class ModLogger
{
    // These calls are completely stripped from Release builds
    // when the symbol is not defined — zero runtime cost.

    [Conditional("DEBUG_TOOL")]
    public static void DebugTool(string message)
    {
        Log.Info($"[Tool] {message}");
    }

    [Conditional("DEBUG_CONNECTIONS")]
    public static void DebugConnections(string message)
    {
        Log.Info($"[Connections] {message}");
    }

    // Always included in all builds
    public static void Info(string message) => Log.Info(message);
}

// Usage — calls to DebugTool are omitted by the compiler in Release:
ModLogger.DebugTool($"Processing node {entity}"); // zero-cost in Release
ModLogger.Info("Mod loaded"); // always present
```

Define symbols in your `.csproj` per configuration:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>DEBUG;DEBUG_TOOL;DEBUG_CONNECTIONS</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants></DefineConstants>
</PropertyGroup>
```

### Example 11: onSettingsApplied Callback for Runtime Settings Sync

`ModSetting.onSettingsApplied` fires whenever settings change at runtime (user clicks Apply in the options menu). Use it to update cached values, UI bindings, or overlay parameters without restarting:

```csharp
public class ModUISystem : GameSystemBase
{
    private ModSettings m_Settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_Settings = ModSettings.Instance;
        m_Settings.onSettingsApplied += OnSettingsApplied;
    }

    private void OnSettingsApplied(Setting setting)
    {
        if (setting is ModSettings modSettings)
        {
            // Update keybinding display strings
            UpdateKeybindingLabels(modSettings);
            // Refresh overlay rendering parameters
            UpdateOverlayColors(modSettings);
        }
    }

    protected override void OnDestroy()
    {
        m_Settings.onSettingsApplied -= OnSettingsApplied;
        base.OnDestroy();
    }

    protected override void OnUpdate() { }
}
```

### Example 12: OnGameLoadingComplete for Game Mode-Aware Initialization

Override `OnGameLoadingComplete(Purpose, GameMode)` to enable/disable features based on whether the player is in Game mode, Editor mode, or the Main Menu. This fires after the game finishes loading a save or entering a mode:

```csharp
public class ModUISystem : GameSystemBase
{
    private ProxyAction m_ToggleAction;

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);

        bool isGameOrEditor = mode == GameMode.Game || mode == GameMode.Editor;
        m_ToggleAction.shouldBeEnabled = isGameOrEditor;

        // Enable/disable UI elements based on mode
        if (mode == GameMode.Game)
            InitializeGameModeUI();
        else if (mode == GameMode.Editor)
            InitializeEditorModeUI();
    }

    protected override void OnUpdate() { }
}
```

**`OnGameLoadingComplete` vs `OnGameLoaded`**: `OnGameLoaded` fires during the serialization context (for restoring system state). `OnGameLoadingComplete` fires after the full load is done — use it for enabling input actions, UI elements, and mode-dependent features.

### Example 13: GameManager.RegisterUpdater for Deferred Initialization

`GameManager.instance.RegisterUpdater()` defers work to the next update cycle on the main thread. Use it when system references are not yet available during `OnCreate`, when you need to show UI dialogs after initialization, or to kick off async work after the game loop is running:

```csharp
public class MySystem : GameSystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        // Defer to next frame — some systems aren't ready during OnCreate
        GameManager.instance.RegisterUpdater(() =>
        {
            var otherSystem = World.GetExistingSystemManaged<SomeOtherSystem>();
            // Now safe to access otherSystem
            return Task.CompletedTask;
        });
    }

    protected override void OnUpdate() { }
}

// In IMod.OnLoad — kick off async work after game loop is running:
public void OnLoad(UpdateSystem updateSystem)
{
    GameManager.instance.RegisterUpdater(async () =>
    {
        await Task.Run(() => LoadExternalResources());
    });

    // Show a dialog after initialization:
    GameManager.instance.RegisterUpdater(() =>
    {
        GameManager.instance.userInterface.appBindings
            .ShowConfirmationDialog(
                new ConfirmationDialog("My Mod", "Setup complete!", "OK"),
                _ => { });
        return Task.CompletedTask;
    });
}
```

## Deserialization Cleanup Pattern

When a mod creates transient entities at runtime (e.g., temporary connections, editing state, overlay markers) that should not persist across save/load, register a cleanup system in the `Deserialize` phase. This ensures stale entities from a previous session are destroyed when a new game loads:

```csharp
public partial class ModDataClearSystem : GameSystemBase
{
    private EntityQuery m_TransientQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TransientQuery = GetEntityQuery(
            ComponentType.ReadOnly<MyTransientComponent>(),
            ComponentType.Exclude<FakePrefabData>()); // Don't destroy prefab entities
    }

    protected override void OnUpdate()
    {
        EntityManager.DestroyEntity(m_TransientQuery);
    }
}

// Register in Deserialize phase from IMod.OnLoad:
updateSystem.UpdateAt<ModDataClearSystem>(SystemUpdatePhase.Deserialize);
```

**Key details:**
- Exclude `FakePrefabData` to avoid destroying permanent prefab entities
- The `Deserialize` phase runs when loading a save or starting a new game
- This is separate from `ISerializable` — serialized components survive save/load by design; this pattern is for runtime-only transient entities that should be cleaned up

## Build Configuration Patterns

### WITH_BURST Conditional Compilation

Complex mods can use conditional Burst compilation: develop with standard C# debugging, release with Burst performance. Define `WITH_BURST` only in Release configuration:

```xml
<!-- In .csproj -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants>WITH_BURST</DefineConstants>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>TRACE;DEBUG;DEBUG_TOOL</DefineConstants>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

Then wrap Burst attributes conditionally:

```csharp
#if WITH_BURST
[Unity.Burst.BurstCompile]
#endif
public partial struct MyExpensiveJob : IJobChunk
{
    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex,
        bool useEnabledMask, in v128 chunkEnabledMask)
    {
        // Heavy computation — Burst-compiled in Release, debuggable in Debug
    }
}
```

**Benefits**: Standard debugger works in Debug builds (Burst-compiled code cannot be debugged with a regular .NET debugger). Release builds get full Burst optimization. `AllowUnsafeBlocks` is required in both configurations for Burst-compatible job structs.

## GetUpdateInterval: Controlling System Tick Frequency

Custom systems do not need to run every frame. `GameSystemBase` provides a virtual method `GetUpdateInterval(SystemUpdatePhase)` that controls how many frames elapse between each `OnUpdate` call:

```csharp
// Decompiled from Game.GameSystemBase
public virtual int GetUpdateInterval(SystemUpdatePhase phase)
{
    return 1; // Default: run every frame
}
```

Override this to reduce how often your system ticks. For example, the vanilla `TreeGrowthSystem` and community mods like Tree_Controller use an interval of 512:

```csharp
public override int GetUpdateInterval(SystemUpdatePhase phase)
{
    return 512; // Run every 512 frames (= 32 updates per day)
}
```

### SimulationUtils.GetUpdateFrame: Work Distribution

When a system runs at a reduced interval, it typically processes only a subset of entities per tick using the `UpdateFrame` shared component filter. The `SimulationUtils.GetUpdateFrame` method determines which group to process:

```csharp
// Decompiled from Game.Simulation.SimulationUtils
public static uint GetUpdateFrame(uint frame, int updatesPerDay, int groupCount)
{
    return (uint)((frame / (262144 / (updatesPerDay * groupCount)))
                  & (groupCount - 1));
}
```

- `frame` -- the current `SimulationSystem.frameIndex`
- `updatesPerDay` -- how many times per day each entity should be processed (e.g., 32)
- `groupCount` -- how many groups entities are divided into (e.g., 16)

The method divides the day (262144 frames) into `updatesPerDay * groupCount` buckets and returns the current bucket index masked to `groupCount - 1`.

### Applying the UpdateFrame Filter

Use `SetSharedComponentFilter` on your `EntityQuery` to process only the current group:

```csharp
private SimulationSystem m_SimulationSystem;
private EntityQuery m_TreeQuery;

protected override void OnUpdate()
{
    // 32 updates/day, 16 groups -> each group processed every 512 frames
    uint updateFrame = SimulationUtils.GetUpdateFrame(
        m_SimulationSystem.frameIndex, 32, 16);
    m_TreeQuery.ResetFilter();
    m_TreeQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));

    // Now m_TreeQuery only contains entities in the current group
    // Process them...
}
```

### Relationship: Interval vs Updates-Per-Day

| Interval | Updates/Day | Groups | Entities per tick |
|----------|------------|--------|-------------------|
| 512 | 32 | 16 | 1/16 of total |
| 256 | 64 | 16 | 1/16 of total |
| 128 | 128 | 16 | 1/16 of total |
| 16 | 1024 | 16 | 1/16 of total |

The interval determines how often your `OnUpdate` runs. The `groupCount` determines what fraction of entities are processed per tick. Together they control total throughput: with interval=512 and groups=16, each entity is processed 32 times per day (262144 / 512 * 16 / 16 = 32).

### When to Match Vanilla Intervals

Custom systems that read data produced by vanilla systems should match or be a multiple of the vanilla system's interval. For example, a mod system that reads tree growth data should use interval 512 (same as `TreeGrowthSystem`) to ensure synchronized data. Running at a faster interval wastes CPU cycles processing stale data.

### GetUpdateOffset: Staggering System Execution

`GameSystemBase` also provides `GetUpdateOffset(SystemUpdatePhase)` (default: -1, meaning auto-assigned) to stagger when systems run within the same interval. The game distributes systems across frames to avoid spikes. Override this only if your system must run on a specific frame offset.

## Open Questions

- [ ] **Load order guarantees**: The order mods are loaded depends on the order they appear in the AssetDatabase scan. There is no explicit load-order declaration. Mods that depend on other mods should use assembly references to ensure correct ordering.
- [ ] **Hot-reloading**: ModManager.RequireRestart() exists but actual hot-reloading (unload + reload without game restart) is not supported for code mods. UI modules can be added/removed dynamically.
- [x] **Inter-mod API pattern**: No official API. Mods use: (1) `GameManager.instance.modManager` iteration for presence detection, (2) `ModManager.ListModsEnabled()` for assembly-qualified name checks, (3) `Assembly.Load()` + `GetType()` for runtime type resolution, (4) public static classes for direct reference when assembly dependency exists.
- [ ] **PDX Mods dependency declaration**: How dependencies are declared in the PDX Mods platform metadata (separate from assembly references) needs testing with the Paradox launcher.

## Custom Buffer Components on Vanilla Entities

Mods can add custom `IBufferElementData` to vanilla entities (e.g., Nodes, Edges) to store mod-specific data alongside game data. The pattern uses a two-level ownership model:

```csharp
// 1. Tag component on vanilla Node entity (zero-data, IEmptySerializable)
public struct ModifiedConnections : IComponentData, IEmptySerializable { }

// 2. Buffer on vanilla Node entity referencing external data entities
public struct ModifiedLaneConnections : IBufferElementData, ISerializable
{
    public Entity m_DataEntity;  // Points to separate entity with full data
    public int m_LaneIndex;
    // ISerializable for save/load persistence
}

// 3. Separate entity with the actual data buffer
public struct GeneratedConnection : IBufferElementData, ISerializable
{
    public Entity m_SourceEntity;
    public Entity m_TargetEntity;
    public PathMethod m_Method;
}

// 4. Back-reference component on data entity
public struct DataOwner : IComponentData, ISerializable
{
    public Entity m_Owner;  // Back to the vanilla Node entity
}
```

**Why two levels?**: Vanilla entities have fixed archetypes. Adding a large buffer directly would disrupt chunk layout. The two-level model keeps the vanilla entity lightweight (just a tag + small buffer of entity refs) and stores heavy data in separate entities.

**Cascade cleanup** (#249): When vanilla entities with mod data are deleted, the mod's data entities must be cleaned up too. Register a cleanup system at `SystemUpdatePhase.Modification4B`:

```csharp
// Query: vanilla entities that have mod data AND are being deleted
EntityQuery deletedWithModData = SystemAPI.QueryBuilder()
    .WithAll<Game.Net.Node, ModifiedLaneConnections, Deleted>()
    .Build();

// In OnUpdate: destroy referenced data entities
var chunks = deletedWithModData.ToArchetypeChunkArray(Allocator.Temp);
foreach (var chunk in chunks)
{
    var buffers = chunk.GetBufferAccessor(ref modLaneConnectionsHandle);
    for (int i = 0; i < buffers.Length; i++)
        foreach (var conn in buffers[i])
            EntityManager.DestroyEntity(conn.m_DataEntity);
}
```

## Camera Navigation to Entity

`CameraUpdateSystem.orbitCameraController` supports "Jump to Entity" navigation:

```csharp
var cameraSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
cameraSystem.orbitCameraController.followedEntity = targetEntity;
cameraSystem.orbitCameraController.TryMatchPosition(cameraSystem.activeCameraController);
cameraSystem.activeCameraController = cameraSystem.orbitCameraController;
```

This smoothly transitions the camera to follow a specific entity, useful for "Find and Navigate" features.

## Cross-Mod Interop via Reflection

### Mod Presence Detection

The simplest form of cross-mod interop is checking whether another mod is loaded. Use `GameManager.instance.modManager` to iterate loaded mods:

```csharp
// Simple presence check via ModManager iteration
bool isOtherModPresent = false;
foreach (ModManager.ModInfo modInfo in GameManager.instance.modManager)
{
    if (modInfo.asset.name.Equals("OtherModName"))
    {
        isOtherModPresent = true;
        break;
    }
}
```

### Assembly Scanning with AppDomain

For discovering types and reading static fields from other mods without a direct assembly reference, scan `AppDomain.CurrentDomain.GetAssemblies()`:

```csharp
private static Lazy<Type> s_OtherModApi = new Lazy<Type>(() =>
{
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        if (assembly.GetName().Name != "OtherModAssembly")
            continue;

        try
        {
            return assembly.GetType("OtherMod.PublicApi");
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types may fail to load if their dependencies
            // are missing. Log and continue with partial results.
            Log.Warn($"Partial type load from {assembly.GetName().Name}: "
                + $"{ex.LoaderExceptions.Length} failures");
            return ex.Types.FirstOrDefault(t =>
                t?.FullName == "OtherMod.PublicApi");
        }
    }
    return null;
});

// Read a static field from the discovered type
public static int GetOtherModValue()
{
    var apiType = s_OtherModApi.Value;
    if (apiType == null)
        return 100; // Fallback to neutral/default value

    var field = apiType.GetField("SomeStaticValue",
        BindingFlags.Static | BindingFlags.Public);
    return field != null ? (int)field.GetValue(null) : 100;
}
```

### Discovering API Methods via ModManager

Pattern for discovering public static API methods from other mods by scanning all loaded mod assemblies:

```csharp
foreach (var item in GameManager.instance.modManager)
{
    var modType = item.asset.assembly?.GetTypesDerivedFrom<IMod>().FirstOrDefault();
    if (modType == null) continue;

    // Discover API methods by convention (name + signature)
    var apiMethod = modType.GetMethod("GetSearchMethod",
        BindingFlags.Static | BindingFlags.Public);
    if (apiMethod != null)
    {
        // Validate return type and parameters before invoking
        if (apiMethod.ReturnType == typeof(Func<string, bool>))
        {
            var searchFunc = (Func<string, bool>)apiMethod.Invoke(null, null);
            // Use the discovered API
        }
    }
}
```

**Key considerations**:
- Handle `ReflectionTypeLoadException` when scanning assemblies -- some types may fail to load if their dependencies are missing
- Use `Lazy<T>` initialization to cache discovered types and avoid repeated assembly scanning
- Always provide fallback/neutral values when the other mod is not present
- Use `GetTypesDerivedFrom<IMod>()` on `item.asset.assembly` to find the mod's main type
- Validate method signatures before invoking (return type + parameters)
- Defer registration via `GameManager.instance.RegisterUpdater()` if timing matters
- This is fragile -- API methods may change between mod versions

## PDX Mods Metadata Access

Access PDX Mods platform details via `PdxSdkPlatform` (requires reflection for private `m_SDKContext`):

```csharp
var pdxPlatform = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");
var context = typeof(PdxSdkPlatform)
    .GetField("m_SDKContext", BindingFlags.NonPublic | BindingFlags.Instance)
    .GetValue(pdxPlatform) as IContext; // PDX.SDK.Contracts.IContext

// Get local mod details by platform ID
var details = await context.Mods.GetLocalModDetails(platformID);
string folderPath = details.Mod.LocalData.FolderAbsolutePath;
DateTime lastUpdate = details.Mod.LatestUpdate;
```

**Simpler checks**: `prefab.asset?.database == AssetDatabase<ParadoxMods>.instance` for PDX Mods detection, `prefab.asset.GetMeta().platformID` for the mod's platform identifier.

## Burst Native Library Loading

### BurstRuntime.LoadAdditionalLibrary

Mods that include Burst-compiled jobs can ship platform-specific native libraries. Use `BurstRuntime.LoadAdditionalLibrary(string path)` to load them at runtime:

```csharp
public void OnLoad(UpdateSystem updateSystem)
{
    string modDir = GetModDirectory();
    string libraryPath;

    switch (Application.platform)
    {
        case RuntimePlatform.WindowsPlayer:
            libraryPath = Path.Combine(modDir, "lib_burst_generated.dll");
            break;
        case RuntimePlatform.OSXPlayer:
            libraryPath = Path.Combine(modDir, "lib_burst_generated.bundle");
            break;
        case RuntimePlatform.LinuxPlayer:
            libraryPath = Path.Combine(modDir, "lib_burst_generated.so");
            break;
        default:
            Log.Warn($"Unsupported platform: {Application.platform}");
            return;
    }

    if (File.Exists(libraryPath))
    {
        BurstRuntime.LoadAdditionalLibrary(libraryPath);
        Log.Info($"Loaded Burst library: {libraryPath}");
    }
    else
    {
        Log.Warn($"Burst library not found: {libraryPath}");
    }
}
```

**Relationship to `ExecutableAsset.isBursted`**: The `isBursted` property on `ExecutableAsset` indicates whether the mod DLL was compiled with Burst. When `isBursted` is true, the game expects a companion native library. `LoadAdditionalLibrary` is the mechanism for loading that library when it ships alongside the mod rather than in the default search path.

**Platform file extensions**:
- Windows: `.dll`
- macOS: `.bundle`
- Linux: `.so`

## GameSystemBase.OnGameLoaded Callback

### Purpose

`OnGameLoaded(Context context)` is a virtual callback on `GameSystemBase` that fires after save deserialization completes but before the first simulation frame runs. It provides a safe point to initialize system state that depends on loaded game data:

```csharp
public partial class MyModSystem : GameSystemBase
{
    private bool m_Initialized;

    protected override void OnGameLoaded(Context context)
    {
        base.OnGameLoaded(context);

        // Safe to query entities here -- deserialization is complete
        var query = GetEntityQuery(ComponentType.ReadOnly<MyComponent>());
        if (!query.IsEmpty)
        {
            InitializeFromExistingData(query);
        }

        m_Initialized = true;
    }

    protected override void OnUpdate()
    {
        // Guard against running before game data is loaded
        if (!m_Initialized)
            return;

        // Normal update logic
    }
}
```

### Frame-Skip Pattern for Race Conditions

Some systems need to wait an additional frame after `OnGameLoaded` to ensure all other systems have processed the loaded data. Use a frame-skip counter:

```csharp
public partial class DeferredInitSystem : GameSystemBase
{
    private int m_SkipFrames;

    protected override void OnGameLoaded(Context context)
    {
        base.OnGameLoaded(context);
        // Skip 2 frames to let dependent systems process loaded data
        m_SkipFrames = 2;
    }

    protected override void OnUpdate()
    {
        if (m_SkipFrames > 0)
        {
            m_SkipFrames--;
            return;
        }

        // All systems have had time to process loaded data
        ProcessData();
    }
}
```

### Init Flag Pattern

For systems that require both `OnGameLoaded` and `OnCreate` initialization, use an explicit flag to track readiness:

```csharp
public partial class TwoPhaseInitSystem : GameSystemBase
{
    private bool m_SystemReady;
    private NativeArray<float> m_CachedData;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CachedData = new NativeArray<float>(1024, Allocator.Persistent);
        // System exists but is not ready until game data loads
    }

    protected override void OnGameLoaded(Context context)
    {
        base.OnGameLoaded(context);
        // Populate cached data from loaded entities
        PopulateCacheFromEntities();
        m_SystemReady = true;
    }

    protected override void OnUpdate()
    {
        if (!m_SystemReady) return;
        // Use m_CachedData safely
    }
}
```

**`OnGameLoaded` vs `OnGameLoadingComplete`**: `OnGameLoaded` fires in the serialization context, after deserialization but before the first simulation frame. `OnGameLoadingComplete` fires later, after the full loading process including UI setup. Use `OnGameLoaded` for data initialization; use `OnGameLoadingComplete` for UI and input setup.

## Static Asset Deployment

### MSBuild AfterBuild Targets

Non-code files (SVGs, PNGs, JSON, localization files) can be automatically deployed alongside the mod DLL using MSBuild `AfterBuild` targets. These files are then accessible at runtime via `coui://` host locations:

```xml
<!-- In .csproj -->
<PropertyGroup>
  <DeployDir>$(CSII_TOOLPATH)\Mods\$(AssemblyName)</DeployDir>
</PropertyGroup>

<ItemGroup>
  <!-- Include all files from the Resources folder -->
  <ModAssets Include="Resources\**\*.*" />
</ItemGroup>

<Target Name="DeployAssets" AfterTargets="Build">
  <MakeDir Directories="$(DeployDir)" />
  <Copy SourceFiles="@(ModAssets)"
        DestinationFolder="$(DeployDir)\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>
```

### Runtime Access via coui:// Protocol

Deployed assets are available at runtime through the `coui://` protocol. The host location maps to the mod's deployment directory:

```csharp
// In your UI module or system:
// SVG icons deployed to Mods/MyMod/icons/
string iconUrl = "coui://ui-mods/icons/my-icon.svg";

// JSON configuration deployed to Mods/MyMod/config/
string configUrl = "coui://ui-mods/config/defaults.json";
```

**Key details**:
- The `coui://` host location is registered by the game's `InitializeUIModules()` phase during mod loading
- Assets must be in the mod's deployment directory to be accessible
- SVG files are commonly used for toolbar icons and UI elements
- The `SkipUnchangedFiles="true"` attribute prevents unnecessary copies during incremental builds

## Shared Resource Library Pattern

### Purpose

A mod can exist solely to provide shared assets (icons, localization, utilities) to other mods. This avoids duplicating common resources across multiple mods:

```
SharedResources/
├── SharedResources.csproj
├── Resources/
│   ├── icons/           # SVG icons shared by multiple mods
│   ├── localization/    # Shared localization strings
│   └── styles/          # Shared CSS/styling
└── PublishConfiguration.xml
```

### Cross-Mod Dependency Declaration

Mods that depend on the shared resource library declare it in their `PublishConfiguration.xml`:

```xml
<!-- In the consuming mod's PublishConfiguration.xml -->
<PublishConfiguration>
  <ModId value="12345" />
  <DisplayName value="My Feature Mod" />
  <Dependency id="67890" displayName="Shared Resources" />
</PublishConfiguration>
```

The `Dependency` element ensures the shared resource mod is installed before the consuming mod loads. The `id` value is the PDX Mods platform ID of the dependency.

### How It Works

1. The shared resource mod is published to PDX Mods as a regular mod (it may or may not have an `IMod` implementation)
2. Its `ExecutableAsset` has `isReference = true` if it contains no `IMod` class (pure library)
3. Consuming mods reference the shared assembly and declare the PDX Mods dependency
4. The game resolves the dependency during `ExecutableAsset.ResolveModAssets()` and loads the shared library first
5. Assets deployed by the shared mod are available via `coui://` to all consuming mods

## PublishConfiguration.xml Schema

### Full Schema

The `PublishConfiguration.xml` file controls mod metadata for PDX Mods publishing:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PublishConfiguration>
  <!-- PDX Mods platform ID. Empty or -1 for unpublished mods.
       Auto-populated on first publish. -->
  <ModId value="12345" />

  <!-- Display name on PDX Mods -->
  <DisplayName value="My Mod Name" />

  <!-- Short description -->
  <ShortDescription value="Brief description of the mod" />

  <!-- Long description (supports markdown) -->
  <LongDescription value="Detailed description..." />

  <!-- Game version compatibility. Supports wildcards:
       "1.2.*" matches any patch version of 1.2 -->
  <GameVersion value="1.2.*" />

  <!-- Thumbnail image path (relative to project root) -->
  <Thumbnail value="Properties/Thumbnail.png" />

  <!-- Screenshots -->
  <Screenshot value="Properties/Screenshot1.png" />
  <Screenshot value="Properties/Screenshot2.png" />

  <!-- Tags for PDX Mods categorization -->
  <Tag value="Code Mod" />

  <!-- Mod dependencies by PDX Mods platform ID -->
  <Dependency id="67890" displayName="Required Mod" />
  <Dependency id="11111" displayName="Another Dependency" />

  <!-- External links -->
  <ExternalLink url="https://github.com/user/repo"
                displayName="Source Code" type="github" />
  <ExternalLink url="https://discord.gg/invite"
                displayName="Discord" type="discord" />

  <!-- Changelog for current version -->
  <ChangeLog value="- Fixed bug X\n- Added feature Y" />

  <!-- Forum topic ID (auto-populated) -->
  <ForumLink value="https://forum.paradoxplaza.com/..." />
</PublishConfiguration>
```

### GameVersion Wildcards

The `GameVersion` element supports wildcard patterns:
- `"1.2.3"` -- exact version match only
- `"1.2.*"` -- any patch version of 1.2
- `"1.*"` -- any version in the 1.x series
- Omitting `GameVersion` means the mod is compatible with any version (not recommended)

### XmlPoke Auto-Population Pattern

Use MSBuild `XmlPoke` tasks to auto-populate `PublishConfiguration.xml` during build, keeping version numbers and metadata in sync with the project:

```xml
<!-- In .csproj -->
<Target Name="UpdatePublishConfig" BeforeTargets="Build">
  <XmlPoke XmlInputPath="PublishConfiguration.xml"
           Query="/PublishConfiguration/GameVersion/@value"
           Value="$(GameVersion)" />
  <XmlPoke XmlInputPath="PublishConfiguration.xml"
           Query="/PublishConfiguration/ShortDescription/@value"
           Value="$(Description)" />
</Target>
```

This keeps the publish configuration in sync with `.csproj` properties, avoiding manual duplication.

## Build Infrastructure: CSII_TOOLPATH and Mod.props/Mod.targets

### CSII_TOOLPATH Environment Variable

The `CSII_TOOLPATH` environment variable points to the Cities: Skylines II installation directory. This is the standard way to locate game assemblies for building and to determine the mod deployment directory:

```xml
<!-- Typical value -->
<!-- Windows: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II -->
<!-- macOS: /Volumes/steamapps/common/Cities Skylines II -->
<!-- Linux: ~/.steam/steam/steamapps/common/Cities Skylines II -->
```

### Mod.props and Mod.targets

Community-maintained build infrastructure uses shared MSBuild files that standardize the build process across mods:

**Mod.props** -- Defines common properties:

```xml
<!-- Mod.props -->
<Project>
  <PropertyGroup>
    <GamePath>$(CSII_TOOLPATH)</GamePath>
    <ManagedPath>$(GamePath)/Cities2_Data/Managed</ManagedPath>
    <DeployDir>$(LOCALAPPDATA)/Colossal Order/Cities Skylines II/Mods/$(AssemblyName)</DeployDir>
  </PropertyGroup>
</Project>
```

**References.csproj** -- Centralizes game assembly references with `Private="false"` (Copy Local = No):

```xml
<!-- References.csproj or shared props file -->
<ItemGroup>
  <Reference Include="Game">
    <HintPath>$(ManagedPath)/Game.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="Colossal.Core">
    <HintPath>$(ManagedPath)/Colossal.Core.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="Unity.Entities">
    <HintPath>$(ManagedPath)/Unity.Entities.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <!-- Additional game assemblies as needed -->
</ItemGroup>
```

**Mod.targets** -- Defines build targets including deployment and cleanup:

```xml
<!-- Mod.targets -->
<Project>
  <!-- Deploy mod DLL and assets to game's Mods folder -->
  <Target Name="DeployMod" AfterTargets="Build">
    <MakeDir Directories="$(DeployDir)" />
    <Copy SourceFiles="$(TargetPath)"
          DestinationFolder="$(DeployDir)"
          SkipUnchangedFiles="true" />
  </Target>

  <!-- Clean deployed files -->
  <Target Name="CleanupMod" AfterTargets="Clean">
    <RemoveDir Directories="$(DeployDir)" />
  </Target>
</Project>
```

### Usage in a Mod .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="$(SolutionDir)Mod.props" />

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>MyMod</AssemblyName>
  </PropertyGroup>

  <Import Project="$(SolutionDir)References.csproj" />
  <Import Project="$(SolutionDir)Mod.targets" />
</Project>
```

The `DeployDir` property automatically places the built mod DLL into the game's local mods directory, enabling the build-deploy-test loop described in the project setup.

## BepInEx Plugin Pattern

### Alternative to IMod

BepInEx is an alternative mod loading framework used by some CS2 mods. Instead of implementing `IMod`, mods extend `BaseUnityPlugin`:

```csharp
using BepInEx;
using HarmonyLib;

[BepInPlugin("com.myname.mymod", "My Mod", "1.0.0")]
public class MyPlugin : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("com.myname.mymod");
        _harmony.PatchAll(typeof(MyPlugin).Assembly);

        Logger.LogInfo("My BepInEx mod loaded");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchAll("com.myname.mymod");
    }
}
```

### System Injection via SystemOrder.Initialize

BepInEx plugins cannot use `UpdateSystem` directly (it is passed only to `IMod.OnLoad`). Instead, they inject systems using a Harmony postfix on `SystemOrder.Initialize`:

```csharp
[HarmonyPatch(typeof(SystemOrder), "Initialize")]
public static class SystemOrderPatch
{
    public static void Postfix(UpdateSystem updateSystem)
    {
        // Now we have access to the UpdateSystem instance
        updateSystem.UpdateAt<MyCustomSystem>(SystemUpdatePhase.GameSimulation);
    }
}
```

### OnCreateWorld vs OnLoad Distinction

| Aspect | IMod.OnLoad | BepInEx Awake + SystemOrder Postfix |
|--------|------------|-------------------------------------|
| When it runs | After ModManager initializes mods | Awake: early in Unity lifecycle; Postfix: when SystemOrder initializes |
| UpdateSystem access | Direct parameter | Via Harmony postfix on SystemOrder.Initialize |
| Mod settings | Full ModSetting integration | Must implement custom settings handling |
| PDX Mods support | Native | Not natively supported |
| Game lifecycle | Managed by ModManager | Managed by BepInEx chain loader |

**When to use BepInEx**: When you need very early initialization (before the mod manager runs), when porting from other Unity games that use BepInEx, or when using BepInEx-specific features like configuration files and plugin dependencies.

**When to use IMod**: For standard CS2 mods distributed via PDX Mods. The `IMod` pattern is the officially supported approach and provides full integration with mod settings, the options UI, and the mod manager lifecycle.

## EntityManager.CreateSingleton for Mod-Global ECS State

### Purpose

`EntityManager.CreateSingleton<T>()` creates an entity with a single component that serves as mod-global state accessible from any ECS system. This is the ECS-idiomatic way to share configuration or state across multiple systems without static fields:

```csharp
// Define the singleton component
public struct MyModConfig : IComponentData
{
    public float SpeedMultiplier;
    public int MaxEntities;
    public bool FeatureEnabled;
}

// Create the singleton in OnCreate of a primary system
public partial class MyModConfigSystem : GameSystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        EntityManager.CreateSingleton(new MyModConfig
        {
            SpeedMultiplier = 1.0f,
            MaxEntities = 100,
            FeatureEnabled = true
        });
    }

    protected override void OnUpdate() { }
}
```

### Accessing from Other Systems

Any system can read or write the singleton using `SystemAPI.GetSingleton<T>()` and `SystemAPI.SetSingleton<T>()`:

```csharp
public partial class MySimulationSystem : GameSystemBase
{
    protected override void OnUpdate()
    {
        // Read the singleton
        var config = SystemAPI.GetSingleton<MyModConfig>();
        if (!config.FeatureEnabled) return;

        float speed = config.SpeedMultiplier;
        // Use speed in simulation logic...
    }
}

public partial class MySettingsSyncSystem : GameSystemBase
{
    protected override void OnUpdate()
    {
        // Write to the singleton (e.g., when mod settings change)
        var config = SystemAPI.GetSingleton<MyModConfig>();
        config.SpeedMultiplier = ModSettings.Instance.SpeedMultiplier;
        SystemAPI.SetSingleton(config);
    }
}
```

### Key Considerations

- **One entity per singleton type**: `CreateSingleton<T>` creates exactly one entity. Calling it again with the same type will throw an exception.
- **Burst-compatible**: Singleton components are plain `IComponentData` structs, fully compatible with Burst-compiled jobs via `GetSingleton`/`SetSingleton` in job code.
- **Not for Harmony patches**: Singletons are ECS-only. Harmony patches operate outside the ECS world and should use static fields instead.
- **Lifecycle**: The singleton entity persists until explicitly destroyed or the World is disposed. It does not survive save/load unless the component implements `ISerializable`.

## Sources

- Decompiled from: Game.dll -- Game.Modding.IMod, Game.Modding.ModManager, Game.Modding.ModSetting, Game.UpdateSystem, Game.SystemUpdatePhase
- Asset system: Colossal.IO.AssetDatabase.dll -- Colossal.IO.AssetDatabase.ExecutableAsset
- CS2 Code Modding Dev Diary: https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding
