# Research: Notifications & Advisor System

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 handles all forms of player notifications -- building problem icons floating above entities, banner notifications in the UI, Chirper social media messages, and the advisor/tutorial system.

**Why**: To enable mods to push custom notifications to the player (building problem icons, chirper messages, banner alerts), suppress or modify existing notifications, and interact with the advisor/tutorial system.

**Boundaries**: Not covering the rendering pipeline for icon sprites (GPU side), radio/audio event systems, or the internal tutorial trigger evaluation logic in detail. Focus is on the notification API surfaces and data flow relevant to modders.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Notifications | Icon, IconElement, IconCommandBuffer, IconCommandSystem, IconClusterSystem, IconAnimationSystem, IconDeletedSystem |
| Game.dll | Game.PSI | NotificationSystem (static facade for banner notifications) |
| Game.dll | Game.UI.Menu | NotificationUISystem (banner notification UI bindings) |
| Game.dll | Game.UI.InGame | ChirperUISystem, NotificationsPanel, NotificationsSection, Notification, TutorialsUISystem, GameTutorialsUISystem |
| Game.dll | Game.Triggers | Chirp, ChirpFlags, ChirpCreationData, CreateChirpSystem, ChirpLikeCountSystem, NotificationTriggerSystem |
| Game.dll | Game.Tutorials | TutorialSystem, TutorialActivationSystem, TutorialAutoActivationSystem, and many activation/deactivation systems |
| Game.dll | Game.Prefabs | NotificationIconPrefab, NotificationIconData, NotificationIconDisplayData, IconConfigurationPrefab, ChirperAccount, ChirpData, TutorialPrefab family |
| Game.dll | Game.Buildings | BuildingNotification |
| Game.dll | Game.Rendering | NotificationIconRenderSystem, NotificationIconLocationSystem, NotificationIconBufferSystem |
| Colossal.PSI.Common.dll | Colossal.PSI.Common | ProgressState |

## Component Map

### `Icon` (Game.Notifications)

The core ECS component on every notification icon entity. Created by `IconCommandSystem` when an `IconCommandBuffer.Add()` command is processed.

| Field | Type | Description |
|-------|------|-------------|
| m_Location | float3 | World-space position of the icon (auto-calculated from owner or custom) |
| m_Priority | IconPriority | Visual priority level (Info=10, Problem=50, Warning=100, MajorProblem=150, FatalProblem=250) |
| m_ClusterLayer | IconClusterLayer | Clustering layer (Default, Marker, Transaction) |
| m_Flags | IconFlags | Behavioral flags (Unique, IgnoreTarget, TargetLocation, OnTop, SecondaryLocation, CustomLocation) |
| m_ClusterIndex | int | Index within the icon cluster (managed by IconClusterSystem) |

*Source: `Game.dll` -> `Game.Notifications.Icon`*

### `IconElement` (Game.Notifications)

Buffer element on the **owner entity** (building, vehicle, etc.) that references all its icon entities. Each `IconElement` points to one Icon entity.

| Field | Type | Description |
|-------|------|-------------|
| m_Icon | Entity | Reference to the icon entity |

*Source: `Game.dll` -> `Game.Notifications.IconElement`*

### `Chirp` (Game.Triggers)

Component on chirp entities. Each chirp is an ECS entity with this component, a `PrefabRef`, and a `RandomLocalizationIndex`.

| Field | Type | Description |
|-------|------|-------------|
| m_Sender | Entity | The sender entity (citizen, company, ChirperAccount) |
| m_CreationFrame | uint | Simulation frame when the chirp was created |
| m_Likes | uint | Current like count |
| m_TargetLikes | uint | Target like count (for animation) |
| m_InactiveFrame | uint | Frame when chirp becomes inactive |
| m_ViralFactor | int | Multiplier for like accumulation |
| m_ContinuousFactor | float | Continuous like growth factor |
| m_Flags | ChirpFlags | Flags (Liked = 0x01) |

*Source: `Game.dll` -> `Game.Triggers.Chirp`*

### `ChirpCreationData` (Game.Triggers)

Struct passed to `CreateChirpSystem` to request chirp creation.

| Field | Type | Description |
|-------|------|-------------|
| m_TriggerPrefab | Entity | The trigger prefab entity that defines the chirp type |
| m_Sender | Entity | The sender entity |
| m_Target | Entity | Optional target entity for links |

*Source: `Game.dll` -> `Game.Triggers.ChirpCreationData`*

### `NotificationIconData` (Game.Prefabs)

Prefab component on `NotificationIconPrefab` entities. Stores the archetype used to create icon entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Archetype | EntityArchetype | Archetype for creating icon entities from this prefab |

*Source: `Game.dll` -> `Game.Prefabs.NotificationIconData`*

### `NotificationIconDisplayData` (Game.Prefabs)

Enableable prefab component that controls whether a notification icon type is visible. Disabling this hides all icons of that type.

| Field | Type | Description |
|-------|------|-------------|
| m_MinParams | float2 | Minimum display size parameters |
| m_MaxParams | float2 | Maximum display size parameters |
| m_IconIndex | int | Index in the icon texture atlas |
| m_CategoryMask | uint | Bitmask for icon category filtering |

*Source: `Game.dll` -> `Game.Prefabs.NotificationIconDisplayData`*

### `ChirpData` (Game.Prefabs)

Prefab component defining a chirp type's archetype and sender rules.

| Field | Type | Description |
|-------|------|-------------|
| m_Archetype | EntityArchetype | Entity archetype for chirp entities |
| m_Flags | ChirpDataFlags | Who can send: CitizensCanSend(1), BrandsCanSend(2), ServiceCanSend(4) |
| m_ChirperAccount | Entity | The ChirperAccount entity used as sender |

*Source: `Game.dll` -> `Game.Prefabs.ChirpData`*

## Enum Reference

### `IconPriority` (Game.Notifications)

| Value | Name | Description |
|-------|------|-------------|
| 0 | Min | Minimum priority |
| 10 | Info | Informational icons |
| 50 | Problem | Minor problems |
| 100 | Warning | Warnings |
| 150 | MajorProblem | Major problems |
| 200 | Error | Errors |
| 250 | FatalProblem | Fatal problems |
| 255 | Max | Maximum priority |

### `IconClusterLayer` (Game.Notifications)

| Value | Name | Description |
|-------|------|-------------|
| 0 | Default | Standard notification icons (building problems, etc.) |
| 1 | Marker | Marker icons |
| 2 | Transaction | Transaction icons (one-shot, no owner tracking) |

### `IconFlags` (Game.Notifications)

| Value | Name | Description |
|-------|------|-------------|
| 0x01 | Unique | Only one icon of this type per owner |
| 0x02 | IgnoreTarget | Don't match by target when deduplicating |
| 0x04 | TargetLocation | Use target entity's position instead of owner's |
| 0x08 | OnTop | Render on top of other icons |
| 0x10 | SecondaryLocation | Secondary location icon |
| 0x20 | CustomLocation | Use the custom float3 location from the Add command |

### `IconCategory` (Game.Prefabs)

| Value | Name |
|-------|------|
| 0 | Healthcare |
| 1 | FireRescue |
| 2 | Police |
| 3 | Water |
| 4 | Electricity |
| 5 | Garbage |
| 6 | Road |
| 7 | Track |
| 8 | Transport |
| 9 | Disaster |
| 10 | Zoning |
| 11 | AirPollution |
| 12 | NoisePollution |
| 13 | GroundPollution |
| 14 | LandValue |
| 15 | Level |

### `ProgressState` (Colossal.PSI.Common)

| Value | Name | Description |
|-------|------|-------------|
| 0 | None | No progress indicator |
| 1 | Progressing | Shows progress bar |
| 2 | Indeterminate | Spinning/loading indicator |
| 3 | Complete | Checkmark/success |
| 4 | Failed | Error/failure |
| 5 | Cancelled | Cancelled state |
| 6 | Warning | Warning indicator |

### `BuildingNotification` (Game.Buildings)

| Value | Name | Description |
|-------|------|-------------|
| 0 | None | No notification |
| 1 | AirPollution | Air pollution affecting building |
| 2 | NoisePollution | Noise pollution affecting building |
| 4 | GroundPollution | Ground pollution affecting building |

## System Map

### `IconCommandSystem` (Game.Notifications)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (via ModificationEndBarrier)
- **Purpose**: Processes batched icon add/remove/update commands from all systems
- **Key methods**:
  - `CreateCommandBuffer()` -- returns an `IconCommandBuffer` for systems to enqueue icon commands
  - `AddCommandBufferWriter(JobHandle)` -- registers job dependencies
  - `OnUpdate()` -- collects all commands from queues, sorts by owner, processes via `IconCommandPlaybackJob`
- **Data flow**: Systems call `CreateCommandBuffer()` -> enqueue Add/Remove/Update commands -> `OnUpdate()` processes commands -> creates/deletes/updates icon entities via EntityCommandBuffer

### `NotificationUISystem` (Game.UI.Menu)

- **Base class**: UISystemBase
- **Update phase**: UIUpdate
- **Purpose**: Manages banner notifications (top-of-screen popups)
- **Bindings**:
  - `ValueBinding<List<NotificationInfo>>("notification", "notifications")` -- current notification list
  - `TriggerBinding<string>("notification", "selectNotification")` -- click handler
- **Key methods**:
  - `AddOrUpdateNotification(identifier, title, text, thumbnail, progressState, progress, onClicked)` -- add/update a notification
  - `RemoveNotification(identifier, delay, ...)` -- remove with optional delay
  - `NotificationExists(identifier)` -- check existence
- **Note**: `Game.PSI.NotificationSystem` is a static facade that delegates to this system

### `ChirperUISystem` (Game.UI.InGame)

- **Base class**: UISystemBase
- **Update phase**: UIUpdate
- **Purpose**: Manages the Chirper social media feed
- **Bindings**:
  - `RawValueBinding("chirper", "chirps")` -- all chirps (polled when modified)
  - `RawEventBinding("chirper", "chirpAdded")` -- fires when new chirps are created
  - `TriggerBinding<Entity>("chirper", "addLike")` -- like a chirp
  - `TriggerBinding<Entity>("chirper", "removeLike")` -- unlike a chirp
  - `TriggerBinding<string>("chirper", "selectLink")` -- click chirp link (entity or infoview)
- **Queries**: Requires `Chirp + RandomLocalizationIndex + PrefabRef`, excludes `Deleted + Temp`

### `CreateChirpSystem` (Game.Triggers)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Purpose**: Creates chirp entities from `ChirpCreationData` queued by trigger actions
- **Key behavior**: Checks for recent chirps (within 18000 frames) to prevent duplicates of same prefab type
- **Queue**: `NativeQueue<ChirpCreationData>` -- systems enqueue chirp requests here

### `TutorialSystem` (Game.Tutorials)

- **Base class**: GameSystemBase, implements `ITutorialSystem`
- **Update phase**: Simulation
- **Purpose**: Manages tutorial/advisor state machine -- activation, phase progression, completion
- **Key properties**:
  - `activeTutorial` -- currently active tutorial entity
  - `activeTutorialList` -- active tutorial list entity
  - `tutorialEnabled` -- whether tutorials are enabled
  - `mode` -- TutorialMode (Default, Intro, etc.)
- **Key methods**:
  - `SetTutorial(tutorial, phase)` -- activate a tutorial
  - `ForceTutorial(tutorial, phase, advisorActivation)` -- force-activate a tutorial
  - `CompleteTutorial(tutorial)` -- mark tutorial completed
  - `CompleteCurrentTutorialPhase()` -- advance to next phase

### `TutorialsUISystem` (Game.UI.InGame)

- **Base class**: UISystemBase
- **Update phase**: UIUpdate
- **Purpose**: Bridges tutorial system state to the React frontend
- **Bindings**:
  - `RawValueBinding("tutorials", "activeList")` -- active tutorial list
  - `TriggerBinding<Entity>("tutorials", "activateTutorial")` -- activate a tutorial
  - `TriggerBinding<Entity, Entity>("tutorials", "activateTutorialPhase")` -- activate specific phase
  - `TriggerBinding<Entity, Entity, bool>("tutorials", "forceTutorial")` -- force tutorial with advisor flag
  - `TriggerBinding("tutorials", "completeActiveTutorialPhase")` -- complete current phase
  - `TriggerBinding("tutorials", "completeActiveTutorial")` -- complete current tutorial

### `NotificationTriggerSystem` (Game.Triggers)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Purpose**: Fires trigger actions when notification icons are created or resolved
- **Trigger types**: `TriggerType.NewNotification` (icon created), `TriggerType.NotificationResolved` (icon deleted)
- **Data**: Enqueues `TriggerAction` with the icon prefab, owner entity, target entity, and total count of that icon type

### `NotificationsSection` (Game.UI.InGame)

- **Base class**: InfoSectionBase
- **Purpose**: Displays notification icons in the selected entity info panel
- **Behavior**: Collects all `IconElement` entries from the selected entity and its related entities (employees, renters, household citizens, route waypoints)

## Data Flow

### Building Problem Icons

```
[Simulation System] detects problem on entity (e.g., no water, no electricity)
    |
    v
System gets IconCommandBuffer from IconCommandSystem.CreateCommandBuffer()
    |-- Calls buffer.Add(ownerEntity, iconPrefabEntity, priority, clusterLayer, flags)
    |
    v
IconCommandSystem.OnUpdate()
    |-- Collects all commands from all queues
    |-- Sorts by owner entity
    |-- IconCommandPlaybackJob processes commands:
        |-- Add: Creates icon entity from NotificationIconData.m_Archetype
        |       Sets Icon component (location, priority, layer, flags)
        |       Adds IconElement to owner's buffer
        |       Adds Owner component pointing back to owner
        |       Adds appear Animation
        |-- Remove: Plays resolve animation, then Deleted
        |-- Update: Updates icon location
    |
    v
IconClusterSystem
    |-- Groups nearby icons into clusters for rendering
    |
    v
NotificationIconRenderSystem
    |-- Renders icon sprites at world positions
    |
    v
[Player sees floating icon above building]
```

### Banner Notifications

```
[Any system or mod code]
    |-- Calls NotificationSystem.Push(identifier, title, text, progressState, progress)
    |
    v
Game.PSI.NotificationSystem (static facade)
    |-- Delegates to NotificationUISystem.AddOrUpdateNotification()
    |
    v
NotificationUISystem
    |-- Creates/updates NotificationInfo in m_NotificationsMap
    |-- Marks m_Dirty = true
    |-- OnUpdate() triggers ValueBinding update
    |
    v
React frontend receives "notification.notifications" binding update
    |-- Renders banner notifications in the UI
    |
    ~~ later ~~
    |
NotificationSystem.Pop(identifier, delay)
    |-- RemoveNotification with optional delay
    |-- After delay expires, notification removed from list
```

### Chirper Messages

```
[TriggerSystem creates trigger action with chirp data]
    |
    v
CreateChirpSystem
    |-- Dequeues ChirpCreationData from queue
    |-- Checks for duplicate (same prefab within 18000 frames)
    |-- Creates chirp entity:
        |   - Chirp component (sender, creation frame, likes)
        |   - PrefabRef pointing to chirp prefab
        |   - RandomLocalizationIndex for message variant
        |   - ChirpEntity buffer for linked entities
    |
    v
ChirperUISystem.OnUpdate()
    |-- Detects modified chirps via change filter
    |-- Updates "chirper.chirps" binding
    |-- Fires "chirper.chirpAdded" event for new chirps
    |
    v
React frontend
    |-- Renders chirp in Chirper panel
    |-- Player can like/unlike via "chirper.addLike"/"chirper.removeLike"
    |-- Player can click links via "chirper.selectLink" (navigates to entity or infoview)
```

### Tutorial/Advisor System

```
[Game event triggers tutorial activation condition]
    |
    v
TutorialActivationSystem (various subtypes)
    |-- Checks activation conditions (UI tag, fire event, health problem, etc.)
    |-- Adds TutorialActivated component to tutorial entity
    |
    v
TutorialSystem.OnUpdate()
    |-- Detects activated tutorials
    |-- Sets activeTutorial, advances phases
    |-- Manages TutorialActive, TutorialPhaseActive components
    |
    v
TutorialsUISystem.OnUpdate()
    |-- Monitors version numbers of TutorialActive, TutorialPhaseActive, etc.
    |-- Updates bindings when changes detected
    |
    v
React frontend
    |-- Renders advisor panel, tutorial cards, balloon tooltips
    |-- Player can complete phases via "tutorials.completeActiveTutorialPhase"
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Icon archetype | NotificationIconPrefab.LateInitialize() | Game.Prefabs.NotificationIconPrefab |
| Icon display size | NotificationIconPrefab.m_DisplaySize | Bounds1(3f, 3f) default |
| Icon pulsate amplitude | NotificationIconPrefab.m_PulsateAmplitude | Bounds1(0.01f, 0.1f) default |
| Icon enabled by default | NotificationIconPrefab.m_EnabledByDefault | true |
| Icon category mask | NotificationIconDisplayData.m_CategoryMask | Set by NotificationIconPrefabSystem |
| Animation durations | IconAnimationElement buffer on IconConfigurationPrefab | Per AnimationType |
| Chirp recent threshold | CreateChirpSystem.CollectRecentChirpsJob | 18000 frames (~5 minutes) |
| No-input notification | CompanyNotificationParameterPrefab.m_NoInputsNotificationPrefab | Prefab reference |
| No-customers notification | CompanyNotificationParameterPrefab.m_NoCustomersNotificationPrefab | Prefab reference |
| Tutorial activation delay | TutorialSystem.kActivationDelay | 3.0 seconds |
| Tutorial completion delay | TutorialSystem.kCompletionDelay | 1.5 seconds |

## Harmony Patch Points

### Candidate 1: `Game.PSI.NotificationSystem.Push`

- **Signature**: `static void Push(string identifier, LocalizedString? title, LocalizedString? text, string titleId, string textId, string thumbnail, ProgressState? progressState, int? progress, Action onClicked)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept, modify, or suppress banner notifications
- **Risk level**: Low -- static method with clear parameters
- **Side effects**: Prefix returning false suppresses the notification entirely

### Candidate 2: `Game.UI.Menu.NotificationUISystem.AddOrUpdateNotification`

- **Signature**: `NotificationInfo AddOrUpdateNotification(string identifier, LocalizedString? title, LocalizedString? text, string thumbnail, ProgressState? progressState, int? progress, Action onClicked)`
- **Patch type**: Prefix / Postfix
- **What it enables**: Intercept notification creation at the UI system level; modify content before display
- **Risk level**: Low -- instance method on UISystemBase subclass
- **Side effects**: None beyond the notification display

### Candidate 3: `Game.Triggers.CreateChirpSystem.OnUpdate` (or the internal CreateChirpJob)

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to suppress chirps) or Postfix (to add chirps)
- **What it enables**: Suppress specific chirp types or inject custom chirps
- **Risk level**: Medium -- involves ECS job scheduling
- **Side effects**: Must be careful with NativeContainer lifecycle

### Candidate 4: `Game.Tutorials.TutorialSystem.SetTutorial`

- **Signature**: `void SetTutorial(Entity tutorial, Entity phase)`
- **Patch type**: Prefix
- **What it enables**: Suppress or redirect tutorial activation
- **Risk level**: Low -- clean method on managed system
- **Side effects**: May break tutorial progression if not handled carefully

### Candidate 5: `Game.Notifications.IconCommandSystem.CreateCommandBuffer`

- **Signature**: `IconCommandBuffer CreateCommandBuffer()`
- **Patch type**: Postfix
- **What it enables**: Get a reference to the icon command buffer system for creating custom icon commands
- **Risk level**: Low -- read-only access point
- **Side effects**: None

## Examples

### Example 1: Push a Banner Notification

Display a non-blocking banner notification at the top of the screen using the static `NotificationSystem` facade. The identifier must be unique per notification -- reusing an identifier updates the existing notification instead of creating a new one.

```csharp
using Game.PSI;
using Game.UI.Localization;
using Colossal.PSI.Common;

/// <summary>
/// Push a banner notification to the player.
/// Call from any system or mod code running on the main thread.
/// </summary>
public void ShowModNotification()
{
    // Push a notification with a title, text, and progress state.
    // The identifier "my-mod.status" is the unique key.
    NotificationSystem.Push(
        "my-mod.status",
        title: LocalizedString.Value("My Mod"),
        text: LocalizedString.Value("Processing complete!"),
        progressState: ProgressState.Complete,
        progress: 100
    );

    // Remove the notification after 3 seconds.
    // The delay parameter keeps it visible before fading out.
    NotificationSystem.Pop(
        "my-mod.status",
        delay: 3f,
        text: LocalizedString.Value("Done!")
    );
}

/// <summary>
/// Show a progress notification that updates over time.
/// </summary>
public void ShowProgressNotification(int percent)
{
    NotificationSystem.Push(
        "my-mod.progress",
        title: LocalizedString.Value("My Mod"),
        text: LocalizedString.Value($"Working... {percent}%"),
        progressState: percent < 100
            ? ProgressState.Progressing
            : ProgressState.Complete,
        progress: percent
    );

    if (percent >= 100)
    {
        NotificationSystem.Pop("my-mod.progress", delay: 2f);
    }
}
```

### Example 2: Add a Building Problem Icon via IconCommandBuffer

Add a floating notification icon above a building entity using the `IconCommandBuffer`. This is how the game adds "no water", "no electricity", and similar icons. You need a reference to a `NotificationIconPrefab` entity.

```csharp
using Game.Notifications;
using Game.Prefabs;
using Unity.Entities;

/// <summary>
/// System that adds a custom notification icon to buildings.
/// Register at SystemUpdatePhase.Modification in Mod.OnLoad().
/// </summary>
public partial class CustomBuildingIconSystem : GameSystemBase
{
    private IconCommandSystem _iconCommandSystem;
    private PrefabSystem _prefabSystem;
    private Entity _iconPrefabEntity;

    protected override void OnCreate()
    {
        base.OnCreate();
        _iconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
        _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
    }

    protected override void OnUpdate()
    {
        // Resolve the icon prefab entity once (lazy init).
        // Use a vanilla icon name like "No Water" or your own registered prefab.
        if (_iconPrefabEntity == Entity.Null)
        {
            // Look up a vanilla notification icon by name
            if (_prefabSystem.TryGetPrefab(new PrefabID(
                    nameof(NotificationIconPrefab), "No Water"), out PrefabBase prefab))
            {
                _iconPrefabEntity = _prefabSystem.GetEntity(prefab);
            }
            else return;
        }

        // Get a command buffer from the icon system
        IconCommandBuffer buffer = _iconCommandSystem.CreateCommandBuffer();

        // Add icon to a building entity (replace with your actual query)
        Entity buildingEntity = GetSomeBuildingEntity();

        // Add an icon with Problem priority on the Default cluster layer
        buffer.Add(
            owner: buildingEntity,
            prefab: _iconPrefabEntity,
            priority: IconPriority.Problem,
            clusterLayer: IconClusterLayer.Default,
            flags: IconFlags.IgnoreTarget
        );

        // To remove an icon later:
        // buffer.Remove(buildingEntity, _iconPrefabEntity);
    }

    private Entity GetSomeBuildingEntity() => Entity.Null; // Placeholder
}
```

### Example 3: Suppress Specific Banner Notifications via Harmony

Use a Harmony prefix patch on `NotificationSystem.Push` to intercept and suppress specific notifications by identifier.

```csharp
using HarmonyLib;
using Game.PSI;

/// <summary>
/// Harmony patch to suppress specific banner notifications.
/// Apply during Mod.OnLoad() via Harmony.PatchAll().
/// </summary>
[HarmonyPatch(typeof(NotificationSystem), nameof(NotificationSystem.Push))]
public static class SuppressNotificationPatch
{
    /// <summary>
    /// Prefix that returns false to skip notifications matching a filter.
    /// </summary>
    public static bool Prefix(string identifier)
    {
        // Suppress all mod download notifications
        if (identifier.Contains("installation") || identifier.Contains("downloading"))
        {
            Mod.Log.Info($"Suppressed notification: {identifier}");
            return false; // Skip the original method
        }
        return true; // Allow all other notifications
    }
}
```

### Example 4: Listen for Notification Icon Events

Use the `NotificationTriggerSystem`'s trigger action pipeline to detect when notification icons are created or resolved on entities. This is useful for monitoring building problems.

```csharp
using Game.Notifications;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Monitors notification icon creation and deletion on entities.
/// Runs every frame and checks for new/removed icons.
/// </summary>
public partial class NotificationMonitorSystem : GameSystemBase
{
    private EntityQuery _iconQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        // Query for all icon entities
        _iconQuery = GetEntityQuery(
            ComponentType.ReadOnly<Icon>(),
            ComponentType.ReadOnly<Owner>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _iconQuery.ToEntityArray(Allocator.Temp);
        int problemCount = 0;
        int warningCount = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Icon icon = EntityManager.GetComponentData<Icon>(entities[i]);
            if (icon.m_Priority >= IconPriority.Problem &&
                icon.m_Priority < IconPriority.Warning)
                problemCount++;
            else if (icon.m_Priority >= IconPriority.Warning)
                warningCount++;
        }

        if (problemCount > 0 || warningCount > 0)
        {
            Mod.Log.Info($"Active notifications: {problemCount} problems, " +
                         $"{warningCount} warnings");
        }

        entities.Dispose();
    }
}
```

### Example 5: Toggle Notification Icon Visibility by Category

Disable or enable entire categories of notification icons by toggling the `NotificationIconDisplayData` enableable component on prefab entities. This is how the game's info view filtering works.

```csharp
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Toggles visibility of notification icons by category.
/// For example, hide all "Water" category icons.
/// </summary>
public partial class ToggleNotificationCategorySystem : GameSystemBase
{
    private EntityQuery _iconPrefabQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _iconPrefabQuery = GetEntityQuery(
            ComponentType.ReadOnly<NotificationIconData>(),
            ComponentType.ReadWrite<NotificationIconDisplayData>()
        );
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Toggle all notification icons of a specific category.
    /// </summary>
    /// <param name="category">The icon category to toggle</param>
    /// <param name="visible">Whether icons should be visible</param>
    public void SetCategoryVisible(IconCategory category, bool visible)
    {
        uint categoryBit = 1u << (int)category;
        var entities = _iconPrefabQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            var displayData = EntityManager
                .GetComponentData<NotificationIconDisplayData>(entities[i]);

            // Check if this prefab belongs to the target category
            if ((displayData.m_CategoryMask & categoryBit) != 0)
            {
                EntityManager.SetComponentEnabled<NotificationIconDisplayData>(
                    entities[i], visible);
            }
        }

        entities.Dispose();
    }
}
```

## Open Questions

- [ ] How to create a completely custom `NotificationIconPrefab` from mod code at runtime (with custom texture) without shipping a Unity asset
- [ ] Whether the `CreateChirpSystem` queue is accessible from mod code for injecting custom chirps, or whether a Harmony patch on `OnUpdate` is required
- [ ] Exact behavior of `IconClusterSystem` when many custom icons are added to the same area -- at what threshold do icons cluster?
- [ ] Whether `TutorialSystem.ForceTutorial()` can be used to show custom advisor-style panels or only vanilla tutorial content
- [ ] Thread safety of `NotificationSystem.Push/Pop` when called from background tasks

## Sources

- Decompiled from: `Game.dll`, `Colossal.PSI.Common.dll` (Cities: Skylines II)
- Key types: `Game.PSI.NotificationSystem`, `Game.UI.Menu.NotificationUISystem`, `Game.Notifications.IconCommandSystem`, `Game.Notifications.IconCommandBuffer`, `Game.Notifications.Icon`, `Game.Notifications.IconElement`, `Game.UI.InGame.ChirperUISystem`, `Game.Triggers.Chirp`, `Game.Triggers.CreateChirpSystem`, `Game.Tutorials.TutorialSystem`, `Game.UI.InGame.TutorialsUISystem`, `Game.Prefabs.NotificationIconPrefab`, `Game.Prefabs.NotificationIconData`, `Game.Prefabs.ChirpData`, `Game.Prefabs.ChirperAccount`
- Game version tested: Current Steam release
