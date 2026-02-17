# Research: Save/Load & Custom Data Persistence

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 serializes game state to save files and how mods can persist custom data (custom ECS components, system-level state) across save/load cycles.

**Why**: Any mod that tracks per-entity or per-system state (custom building settings, custom component values, mod-specific counters) needs that data to survive save/load. This is the most commonly asked question in the CS2 modding community.

**Boundaries**: Out of scope -- save file format on disk (compression, headers), the asset database system, and the map editor's separate serialization flow.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Colossal.Core.dll | Colossal.Serialization.Entities | All serialization interfaces (ISerializable, IWriter, IReader), ComponentSerializer infrastructure, SystemSerializer infrastructure, Context, Purpose, FormerlySerializedAsAttribute |
| Game.dll | Game.Serialization | SaveGameSystem, LoadGameSystem, WriteSystem, ReadSystem, SerializerSystem, ClearSystem, all post-load fix-up systems |
| Game.dll | Game (root) | AutoSaveSystem |
| Game.dll | Game.SceneFlow | SaveHelpers (utility for save paths) |

## Component Map

### Serialization Interfaces

These interfaces control whether a type's data survives save/load. Implement them on your `IComponentData`, `IBufferElementData`, `ISharedComponentData`, or system class.

### `ISerializable` (Colossal.Serialization.Entities)

The core serialization contract. Any ECS component or system implementing this interface will have its data persisted in save files.

```csharp
public interface ISerializable
{
    void Serialize<TWriter>(TWriter writer) where TWriter : IWriter;
    void Deserialize<TReader>(TReader reader) where TReader : IReader;
}
```

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.ISerializable`*

### `IEmptySerializable` (Colossal.Serialization.Entities)

Marker interface for tag components (zero-size components with no data). The serialization system preserves the component's **presence** on entities across save/load without writing any data bytes.

```csharp
public interface IEmptySerializable { }
```

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.IEmptySerializable`*

### `IDefaultSerializable` (Colossal.Serialization.Entities)

Extends `ISerializable` for systems. Provides a `SetDefaults()` method called during new game creation when no save data exists.

```csharp
public interface IDefaultSerializable : ISerializable
{
    void SetDefaults(Context context);
}
```

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.IDefaultSerializable`*

### `IJobSerializable` (Colossal.Serialization.Entities)

For systems that serialize using Burst-compiled jobs. Uses `JobHandle`-based async serialization.

```csharp
public interface IJobSerializable
{
    JobHandle Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps) where TWriter : struct, IWriter;
    JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps) where TReader : struct, IReader;
    JobHandle SetDefaults(Context context, JobHandle inputDeps);
}
```

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.IJobSerializable`*

### `ISerializeAsEnabled` (Colossal.Serialization.Entities)

Marker interface that controls how enableable components are serialized. When an enableable component implements `ISerializeAsEnabled`, its enabled/disabled state is serialized alongside its data. Without this, enableable components use special serializers that track the enabled bit separately.

```csharp
public interface ISerializeAsEnabled { }
```

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.ISerializeAsEnabled`*

### `IWriter` (Colossal.Serialization.Entities)

Binary writer interface used in `Serialize<TWriter>()`. Supports all Unity math types, primitives, entities, strings, and nested `ISerializable` types.

Key methods:
- `Write(int value)`, `Write(float value)`, `Write(bool value)`, etc. -- primitives
- `Write(Entity value)` -- writes an entity reference (remapped during deserialization)
- `Write(string value)` -- strings
- `Write<TSerializable>(TSerializable value)` -- nested serializable types
- `Begin()` / `End(block)` -- size-prefixed blocks for forward compatibility

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.IWriter`*

### `IReader` (Colossal.Serialization.Entities)

Binary reader interface used in `Deserialize<TReader>()`. Mirror of `IWriter` with `Read(out T value)` overloads.

Key methods:
- `Read(out int value)`, `Read(out float value)`, etc. -- primitives
- `Read(out Entity value)` -- reads a remapped entity reference
- `Read(out string value)` -- strings
- `Begin()` / `End(block)` -- matched size-prefixed blocks
- `Skip(int size)` -- skip bytes (for forward-compatible deserialization)

The `context` property provides version information for backward-compatible deserialization.

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.IReader`*

### `Context` (Colossal.Serialization.Entities)

Carries metadata about the current save/load operation.

| Field | Type | Description |
|-------|------|-------------|
| purpose | Purpose | SaveGame, LoadGame, NewGame, etc. |
| version | Version | Save format version for backward compatibility |
| format | ContextFormat | Format tags for feature detection |

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.Context`*

### `Purpose` (Colossal.Serialization.Entities)

| Value | Description |
|-------|-------------|
| SaveGame | Saving a game |
| NewGame | Starting a new game |
| LoadGame | Loading a saved game |
| SaveMap | Saving a map in the editor |
| NewMap | Creating a new map |
| LoadMap | Loading a map |
| Cleanup | Cleanup/reset phase |

### `FormerlySerializedAsAttribute` (Colossal.Serialization.Entities)

Enables renaming types without breaking existing save files. The serialization system uses this to resolve old type names to their new types during deserialization.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class FormerlySerializedAsAttribute : Attribute
{
    public string oldName { get; }
    public FormerlySerializedAsAttribute(string oldName);
}
```

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.FormerlySerializedAsAttribute`*

## System Map

### Discovery: How the Serialization System Finds Your Types

The game uses two libraries to discover serializable types at startup:

#### `ComponentSerializerLibrary` (Colossal.Serialization.Entities)

- **Purpose**: Discovers all ECS component types that implement `ISerializable` or `IEmptySerializable` and creates the appropriate serializer wrapper.
- **Discovery logic** (in `Initialize()`):
  1. Iterates ALL registered ECS types via `TypeManager.GetTypeCount()`
  2. For each type, checks if it implements `IEmptySerializable` or `ISerializable`
  3. Creates the appropriate generic serializer:
     - `IEmptySerializable` -> `EmptyComponentSerializer` (preserves component presence, no data)
     - `ISerializable` + `IComponentData` -> `ComponentDataSerializer<T>`
     - `ISerializable` + `IBufferElementData` -> `BufferElementDataSerializer<T>`
     - `ISerializable` + `ISharedComponentData` -> `SharedComponentDataSerializer<T>`
     - Enableable variants use `EnableableComponentDataSerializer<T>` / `EnableableBufferElementDataSerializer<T>`
  4. Stores a mapping from `ComponentType` -> serializer index
  5. Reads `[FormerlySerializedAs]` attributes for type rename support

**Key insight for modding**: This discovery is **automatic**. Any `IComponentData` struct that implements `ISerializable` in your mod assembly will be discovered and serialized. You do NOT need to register it anywhere.

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.ComponentSerializerLibrary`*

#### `SystemSerializerLibrary` (Colossal.Serialization.Entities)

- **Purpose**: Discovers all ECS systems that implement serialization interfaces and creates serializer wrappers.
- **Discovery logic** (in `Initialize(World)`):
  1. Iterates all systems in the ECS World
  2. For each system, checks interfaces in priority order:
     - `IJobSerializable` -> `JobSystemSerializer<T>`
     - `IDefaultSerializable` -> `DefaultSystemSerializer<T>`
     - `ISerializable` -> `ComponentSystemSerializer<T>` (with a logged error -- game prefers IDefaultSerializable)
  3. Stores a mapping from `Type` -> serializer index
  4. Reads `[FormerlySerializedAs]` attributes for type rename support

**Key insight for modding**: Systems implementing `IDefaultSerializable` will automatically have their `Serialize`/`Deserialize` methods called during save/load, and `SetDefaults` called on new game. Use this for mod-wide singleton state.

*Source: `Colossal.Core.dll` -> `Colossal.Serialization.Entities.SystemSerializerLibrary`*

### `SaveGameSystem` (Game.Serialization)

- **Base class**: GameSystemBase
- **Purpose**: Orchestrates saving. Enables itself, runs `SystemUpdatePhase.Serialize` via `UpdateSystem`, waits for `WriteSystem` to finish writing, then disables itself.
- **Key flow**:
  1. `RunOnce()` is called (async) to start save
  2. First `OnUpdate()`: triggers `m_UpdateSystem.Update(SystemUpdatePhase.Serialize)` which runs all serialization systems
  3. Second `OnUpdate()`: checks if `WriteSystem.writeDependency.IsCompleted`, completes the Task

*Source: `Game.dll` -> `Game.Serialization.SaveGameSystem`*

### `LoadGameSystem` (Game.Serialization)

- **Base class**: GameSystemBase
- **Purpose**: Orchestrates loading. Triggers `SystemUpdatePhase.Deserialize` then invokes `onOnSaveGameLoaded` event.
- **Key flow**:
  1. `RunOnce()` is called (async) to start load
  2. `OnUpdate()`: triggers `m_UpdateSystem.Update(SystemUpdatePhase.Deserialize)`
  3. Fires `onOnSaveGameLoaded` delegate with the deserialization context

*Source: `Game.dll` -> `Game.Serialization.LoadGameSystem`*

### `WriteSystem` (Game.Serialization)

- **Base class**: GameSystemBase, implements `IWriteBufferProvider<WriteBuffer>`
- **Purpose**: Manages the binary write pipeline. Buffers are written sequentially (raw or compressed) to the save stream.
- **Key method**: `AddBuffer(BufferFormat format)` returns a `WriteBuffer` for a serializer to write into.

*Source: `Game.dll` -> `Game.Serialization.WriteSystem`*

### `ReadSystem` (Game.Serialization)

- **Base class**: GameSystemBase, implements `IReadBufferProvider<ReadBuffer>`
- **Purpose**: Manages the binary read pipeline. Reads buffers (raw or compressed) from the save file stream.
- **Key method**: `GetBuffer(BufferFormat format)` returns a `ReadBuffer` for a deserializer to read from.

*Source: `Game.dll` -> `Game.Serialization.ReadSystem`*

### `AutoSaveSystem` (Game)

- **Base class**: GameSystemBase
- **Purpose**: Triggers auto-saves on a timer. Saves to a rolling set of auto-save slots.
- **Key behavior**: Checks `GeneralSettings.autoSave` and `AutoSaveSettings.interval`, invokes `GameManager.instance.SaveGameAsync()`.

*Source: `Game.dll` -> `Game.AutoSaveSystem`*

## Data Flow

```
[SAVE]
GameManager.SaveGameAsync()
    |
    v
SaveGameSystem.RunOnce()
    |-- Sets context (Purpose.SaveGame, current Version)
    |-- Enables system
    |
    v
SaveGameSystem.OnUpdate() [first call]
    |-- UpdateSystem.Update(SystemUpdatePhase.Serialize)
    |       |
    |       v
    |   BeginPrefabSerializationSystem: maps prefabs to indices
    |       |
    |       v
    |   SerializerSystem: orchestrates all serializers
    |       |-- ComponentSerializerLibrary: for each registered component type
    |       |       |-- ComponentSerializer.SerializeData<TWriter>()
    |       |       |       calls ISerializable.Serialize() on each entity's component
    |       |
    |       |-- SystemSerializerLibrary: for each registered system
    |       |       |-- SystemSerializer.SerializeSystem<TWriter>()
    |       |       |       calls ISerializable.Serialize() or IJobSerializable.Serialize()
    |       |
    |       v
    |   WriteSystem.OnUpdate(): flushes all buffers to stream
    |
    v
SaveGameSystem.OnUpdate() [second call]
    |-- WriteSystem.writeDependency.IsCompleted -> true
    |-- Disables self, completes Task
    |
    v
Save file written to disk

[LOAD]
GameManager.LoadGameAsync()
    |
    v
LoadGameSystem.RunOnce()
    |-- Sets context (Purpose.LoadGame, save's Version)
    |
    v
LoadGameSystem.OnUpdate()
    |-- UpdateSystem.Update(SystemUpdatePhase.Deserialize)
    |       |
    |       v
    |   ReadSystem: reads buffers from save file
    |       |
    |       v
    |   SerializerSystem: orchestrates all deserializers
    |       |-- ComponentSerializerLibrary: for each saved component type
    |       |       |-- Resolves type name (including FormerlySerializedAs fallback)
    |       |       |-- ComponentSerializer.DeserializeData<TReader>()
    |       |       |       calls ISerializable.Deserialize() on each entity's component
    |       |       |-- If type not found: logs warning, skips data (ObsoleteComponentSerializer)
    |       |
    |       |-- SystemSerializerLibrary: for each saved system type
    |       |       |-- Resolves type name (including FormerlySerializedAs fallback)
    |       |       |-- SystemSerializer.DeserializeSystem<TReader>()
    |       |       |-- If system not found: logs warning, skips data (ObsoleteSystemSerializer)
    |       |
    |       v
    |   Post-deserialization fix-up systems (Game.Serialization namespace)
    |       |-- PrimaryPrefabReferencesSystem, SecondaryPrefabReferencesSystem
    |       |-- Various DataMigration systems
    |
    v
LoadGameSystem fires onOnSaveGameLoaded(context)
    |
    v
Game resumes with loaded state

[NEW GAME]
    |
    v
SystemSerializerLibrary: for each system implementing IDefaultSerializable
    |-- Calls SetDefaults(context) where context.purpose == Purpose.NewGame
    |
    v
Game starts with default state
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Save format version | Colossal.Version static fields | Colossal.Core.dll -- Version struct has named static versions like `saveOptimizations`, `penaltyCounter`, `snow`, etc. |
| Auto-save interval | GeneralSettings.autoSave | Game settings, default varies |
| Save file location | EnvPath.kUserDataPath | `%AppData%/LocalLow/Colossal Order/Cities Skylines II/Saves/` |
| Mod settings location | EnvPath.kUserDataPath + "ModsSettings/" | Per-mod settings directory |
| Buffer compression | BufferFormat enum | Raw or ZStd-compressed chunks |

## Harmony Patch Points

### Candidate 1: `LoadGameSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Run logic after a game is loaded (e.g., validate custom data, apply migrations)
- **Risk level**: Low
- **Side effects**: Runs once per load, after all deserialization is complete

### Candidate 2: `SaveGameSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix
- **What it enables**: Modify state just before save (e.g., clean up temporary data that shouldn't be persisted)
- **Risk level**: Low
- **Side effects**: Called twice per save (once to serialize, once to check completion)

### Candidate 3: `LoadGameSystem.onOnSaveGameLoaded`

- **Signature**: `public EventGameLoaded onOnSaveGameLoaded` (delegate)
- **Patch type**: Not a patch -- subscribe directly
- **What it enables**: Register a callback that fires after every load completes, receiving the deserialization `Context`
- **Risk level**: Low
- **Side effects**: None -- purely additive

## Mod Blueprint

### Approach 1: Persisting Custom ECS Components (Per-Entity Data)

For data attached to individual entities (e.g., per-building settings, per-citizen stats), implement `ISerializable` on your `IComponentData` struct.

**Systems to create**: None required for serialization itself -- it's automatic.

**Components to add**:
```csharp
public struct MyModData : IComponentData, ISerializable, IDefaultSerializable
{
    public int m_CustomValue;
    public float m_CustomRate;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_CustomValue);
        writer.Write(m_CustomRate);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_CustomValue);
        reader.Read(out m_CustomRate);
    }

    public void SetDefaults(Context context)
    {
        m_CustomValue = 0;
        m_CustomRate = 1.0f;
    }
}
```

### Approach 2: Persisting System-Level State (Singleton/Global Data)

For mod-wide state that isn't per-entity (e.g., global counters, configuration state, history data), implement `IDefaultSerializable` on your `GameSystemBase` subclass.

**Systems to create**:
```csharp
public partial class MyModStateSystem : GameSystemBase, IDefaultSerializable
{
    private int m_TotalEventsProcessed;
    private float m_GlobalMultiplier;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_TotalEventsProcessed);
        writer.Write(m_GlobalMultiplier);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_TotalEventsProcessed);
        reader.Read(out m_GlobalMultiplier);
    }

    public void SetDefaults(Context context)
    {
        m_TotalEventsProcessed = 0;
        m_GlobalMultiplier = 1.0f;
    }

    protected override void OnUpdate() { /* ... */ }
}
```

### Approach 3: Tag Components (Presence-Only)

For components that carry no data but whose presence on an entity matters (e.g., "this building has been marked by my mod"), implement `IEmptySerializable`.

**Components to add**:
```csharp
public struct ModMarker : IComponentData, IEmptySerializable { }
```

### Patches needed
- None required for basic persistence -- the framework is automatic
- Optional: subscribe to `LoadGameSystem.onOnSaveGameLoaded` for post-load validation

### Settings
- Not required for persistence itself
- Mod settings (UI options) are persisted separately via the ModOptionsUI system (see `ModOptionsUI` research)

### UI changes
- None required for persistence

## Examples

### Example 1: Basic Persistent Component

A custom component that stores per-building mod data. This data automatically survives save/load because it implements `ISerializable`.

```csharp
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace MyMod.Components
{
    /// <summary>
    /// Custom per-building data that persists across save/load.
    /// The serialization system auto-discovers this via ISerializable.
    /// </summary>
    public struct BuildingModSettings : IComponentData, ISerializable
    {
        public float m_EfficiencyMultiplier;
        public int m_CustomLevel;
        public bool m_IsEnabled;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_EfficiencyMultiplier);
            writer.Write(m_CustomLevel);
            writer.Write(m_IsEnabled);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_EfficiencyMultiplier);
            reader.Read(out m_CustomLevel);
            reader.Read(out m_IsEnabled);
        }
    }
}
```

### Example 2: Version-Aware Deserialization (Adding Fields Over Time)

When you add fields to a component in a mod update, older saves won't have them. Use `reader.context.version` or a custom version byte to handle this gracefully.

```csharp
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace MyMod.Components
{
    /// <summary>
    /// Component with versioned serialization. New fields added in v2
    /// get default values when loading older saves.
    /// </summary>
    public struct VersionedData : IComponentData, ISerializable
    {
        // v1 fields
        public int m_BaseValue;

        // v2 fields (added in mod update)
        public float m_NewMultiplier;
        public bool m_NewFlag;

        // Internal version for our mod's serialization format
        private const byte kCurrentVersion = 2;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            // Write our mod version first
            writer.Write(kCurrentVersion);
            // v1 fields
            writer.Write(m_BaseValue);
            // v2 fields
            writer.Write(m_NewMultiplier);
            writer.Write(m_NewFlag);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            // v1 fields (always present)
            reader.Read(out m_BaseValue);

            if (version >= 2)
            {
                // v2 fields
                reader.Read(out m_NewMultiplier);
                reader.Read(out m_NewFlag);
            }
            else
            {
                // Defaults for old saves
                m_NewMultiplier = 1.0f;
                m_NewFlag = false;
            }
        }
    }
}
```

### Example 3: Persistent System State (Global Mod Data)

A system that tracks global mod state across save/load cycles. Implements `IDefaultSerializable` to provide defaults for new games.

```csharp
using Colossal.Serialization.Entities;
using Game;

namespace MyMod.Systems
{
    /// <summary>
    /// Tracks mod-global state that persists in save files.
    /// IDefaultSerializable provides SetDefaults for new games.
    /// </summary>
    public partial class ModGlobalStateSystem : GameSystemBase, IDefaultSerializable
    {
        public int TotalBuildingsModified { get; private set; }
        public float AccumulatedBonus { get; private set; }
        private bool m_HasBeenInitialized;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(TotalBuildingsModified);
            writer.Write(AccumulatedBonus);
            writer.Write(m_HasBeenInitialized);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int total);
            TotalBuildingsModified = total;
            reader.Read(out float bonus);
            AccumulatedBonus = bonus;
            reader.Read(out m_HasBeenInitialized);
        }

        public void SetDefaults(Context context)
        {
            TotalBuildingsModified = 0;
            AccumulatedBonus = 0f;
            m_HasBeenInitialized = true;
        }

        protected override void OnUpdate()
        {
            // System logic here
        }
    }
}
```

### Example 4: Persistent Buffer Element Data

Custom buffer element data (multiple values per entity) that persists. Useful for tracking history, lists, or variable-length data.

```csharp
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace MyMod.Components
{
    /// <summary>
    /// Buffer element storing event history per entity.
    /// Each entity can have multiple of these.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct EventHistoryEntry : IBufferElementData, ISerializable
    {
        public uint m_Frame;
        public int m_EventType;
        public float m_Value;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Frame);
            writer.Write(m_EventType);
            writer.Write(m_Value);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Frame);
            reader.Read(out m_EventType);
            reader.Read(out m_Value);
        }
    }
}
```

### Example 5: Tag Component with IEmptySerializable

A zero-data marker component that persists across save/load. The serialization system only tracks whether the component exists on each entity -- no data is written.

```csharp
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace MyMod.Components
{
    /// <summary>
    /// Marks a building as managed by this mod.
    /// IEmptySerializable persists the component's presence without data.
    /// </summary>
    public struct ManagedByMod : IComponentData, IEmptySerializable { }
}
```

### Example 6: Renaming a Type Without Breaking Saves

When refactoring your mod, use `[FormerlySerializedAs]` so that save files with the old type name can still load.

```csharp
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace MyMod.Components
{
    /// <summary>
    /// Renamed from "MyMod.Components.OldName, MyModAssembly"
    /// Saves with the old name will still load correctly.
    /// </summary>
    [FormerlySerializedAs("MyMod.Components.OldName, MyModAssembly")]
    public struct NewBetterName : IComponentData, ISerializable
    {
        public int m_Value;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Value);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Value);
        }
    }
}
```

### Example 7: Post-Load Validation via Event Subscription

Subscribe to the `onOnSaveGameLoaded` event to validate or fix up custom data after a game loads.

```csharp
using Colossal.Serialization.Entities;
using Game;
using Game.Serialization;

namespace MyMod.Systems
{
    /// <summary>
    /// Validates mod data integrity after every game load.
    /// </summary>
    public partial class PostLoadValidationSystem : GameSystemBase
    {
        private LoadGameSystem m_LoadGameSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_LoadGameSystem = World.GetOrCreateSystemManaged<LoadGameSystem>();
            m_LoadGameSystem.onOnSaveGameLoaded += OnGameLoaded;
        }

        protected override void OnDestroy()
        {
            m_LoadGameSystem.onOnSaveGameLoaded -= OnGameLoaded;
            base.OnDestroy();
        }

        private void OnGameLoaded(Context context)
        {
            Mod.Log.Info($"Game loaded (purpose={context.purpose}, version={context.version})");
            // Validate/fix up custom component data here
            // For example: remove orphaned components, set defaults for new fields
        }

        protected override void OnUpdate() { }
    }
}
```

## Open Questions

- [ ] How does the serialization handle entity references in custom components when the referenced entity doesn't exist in an older save? (Likely remapped to Entity.Null)
- [ ] Is there a size limit on custom component data per entity?
- [ ] Does uninstalling a mod with serialized components cause errors on load, or does the `ObsoleteComponentSerializer` skip the data cleanly?
- [ ] Can `ISharedComponentData` with `ISerializable` be used by mods, or is this restricted to game-internal types?

## Sources

- Decompiled from: Colossal.Core.dll -- Colossal.Serialization.Entities (ISerializable, IWriter, IReader, IDefaultSerializable, IEmptySerializable, IJobSerializable, ISerializeAsEnabled, ComponentSerializer, ComponentSerializerLibrary, SystemSerializerLibrary, SystemSerializer, FormerlySerializedAsAttribute, Context, Purpose)
- Decompiled from: Game.dll -- Game.Serialization (SaveGameSystem, LoadGameSystem, WriteSystem, ReadSystem, ClearSystem, BeginPrefabSerializationSystem)
- Decompiled from: Game.dll -- Game (AutoSaveSystem), Game.SceneFlow (SaveHelpers)
- Decompiled from: Game.dll -- Game.Citizens.Citizen (example of vanilla ISerializable component with version-aware deserialization)
- Decompiled snippets saved to: `research/topics/SaveLoadPersistence/snippets/`
