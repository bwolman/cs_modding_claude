# Research: Chirper / Social Media Notifications

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2's in-game "Chirper" social media system creates, displays, and manages chirp notifications -- the Twitter/X-like messages that appear from citizens, brands, and city services.

**Why**: Modders want to create custom chirps (triggered by mod events, milestones, or gameplay conditions), modify chirp behavior (like counts, frequency, sender selection), and potentially add new chirper accounts or notification categories.

**Boundaries**: Life path events that produce chirps are out of scope (they share the chirp creation pipeline but have separate trigger logic). The localization system for chirp message text is covered in the Localization topic.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Triggers | Chirp, ChirpEntity, ChirpLink, ChirpCreationData, ChirpFlags, CreateChirpSystem, ChirpLikeCountSystem |
| Game.dll | Game.Prefabs | ChirpData, ChirpDataFlags, ChirperAccountData, TriggerChirpData, ServiceChirpData, BrandChirpData, RandomLikeCountData |
| Game.dll | Game.UI.InGame | ChirperUISystem, ChirpLinkSystem |

## Component Map

### `Chirp` (Game.Triggers)

Core component on every chirp entity. Implements `ISerializable` for save/load persistence.

| Field | Type | Description |
|-------|------|-------------|
| m_Sender | Entity | The citizen, brand, or chirper account that "posted" this chirp |
| m_CreationFrame | uint | Simulation frame when the chirp was created |
| m_Likes | uint | Current like count (animated upward over time) |
| m_TargetLikes | uint | Final target like count (calculated at creation from population) |
| m_InactiveFrame | uint | Frame when like accumulation stops |
| m_ViralFactor | int | Controls the shape of the like accumulation curve (5-100) |
| m_ContinuousFactor | float | Probability of skipping a like update tick (default 0.2) |
| m_Flags | ChirpFlags | Bitfield: `Liked` (1) = player has liked this chirp |

*Source: `Game.dll` -> `Game.Triggers.Chirp`*

### `ChirpEntity` (Game.Triggers)

Buffer element (capacity 2) linking a chirp to referenced entities (e.g., the citizen sender, a mentioned building, a partner in a "had a baby" chirp).

| Field | Type | Description |
|-------|------|-------------|
| m_Entity | Entity | Referenced entity (citizen, building, etc.) |

*Source: `Game.dll` -> `Game.Triggers.ChirpEntity`*

### `ChirpLink` (Game.Triggers)

Buffer element linking entities back to chirps they are mentioned in.

| Field | Type | Description |
|-------|------|-------------|
| m_Chirp | Entity | The chirp entity that references this entity |

*Source: `Game.dll` -> `Game.Triggers.ChirpLink`*

### `ChirpCreationData` (Game.Triggers)

Struct enqueued into `CreateChirpSystem`'s `NativeQueue` by other systems to request a new chirp.

| Field | Type | Description |
|-------|------|-------------|
| m_TriggerPrefab | Entity | The trigger/event prefab that caused this chirp |
| m_Sender | Entity | Suggested sender (household, company, or specific citizen) |
| m_Target | Entity | Optional target entity to link in the chirp |

*Source: `Game.dll` -> `Game.Triggers.ChirpCreationData`*

### `ChirpFlags` (Game.Triggers)

| Flag | Value | Meaning |
|------|-------|---------|
| Liked | 1 | The player has liked this chirp |

*Source: `Game.dll` -> `Game.Triggers.ChirpFlags`*

### `ChirpData` (Game.Prefabs)

Prefab component defining a chirp type's archetype and behavior.

| Field | Type | Description |
|-------|------|-------------|
| m_Archetype | EntityArchetype | The ECS archetype used when creating chirp entities of this type |
| m_Flags | ChirpDataFlags | Who can send this chirp type |
| m_ChirperAccount | Entity | Default chirper account entity (for service chirps) |

*Source: `Game.dll` -> `Game.Prefabs.ChirpData`*

### `ChirpDataFlags` (Game.Prefabs)

| Flag | Value | Meaning |
|------|-------|---------|
| None | 0 | No special flags |
| CitizensCanSend | 1 | Citizens can be selected as sender |
| BrandsCanSend | 2 | Company brands can be selected as sender |
| ServiceCanSend | 4 | City service accounts can send |

*Source: `Game.dll` -> `Game.Prefabs.ChirpDataFlags`*

### `ChirperAccountData` (Game.Prefabs)

Empty marker component on entities that represent chirper accounts (service accounts like "City Water Department"). The presence of this component changes how the UI resolves the sender's avatar and link.

*Source: `Game.dll` -> `Game.Prefabs.ChirperAccountData`*

### `TriggerChirpData` (Game.Prefabs)

Buffer element on trigger/event prefabs linking them to possible chirp prefabs.

| Field | Type | Description |
|-------|------|-------------|
| m_Chirp | Entity | A chirp prefab entity that can be created for this trigger |

*Source: `Game.dll` -> `Game.Prefabs.TriggerChirpData`*

### `ServiceChirpData` (Game.Prefabs)

Component on chirp prefabs sent by city service accounts.

| Field | Type | Description |
|-------|------|-------------|
| m_Account | Entity | The chirper account entity to use as sender |

*Source: `Game.dll` -> `Game.Prefabs.ServiceChirpData`*

### `BrandChirpData` (Game.Prefabs)

Empty marker component on chirp prefabs sent by company brands. The sender entity is used directly (the company/brand entity itself).

*Source: `Game.dll` -> `Game.Prefabs.BrandChirpData`*

### `RandomLikeCountData` (Game.Prefabs)

Prefab component that overrides the default like count calculation for specific chirp types. Allows fine-tuning engagement per chirp category.

| Field | Type | Description |
|-------|------|-------------|
| m_EducatedPercentage | float | Fraction of educated population that contributes to likes |
| m_UneducatedPercentage | float | Fraction of uneducated population that contributes to likes |
| m_RandomAmountFactor | float2 | Min/max range for random like percentage of population |
| m_ActiveDays | float2 | Min/max range for how many in-game days the chirp stays active |
| m_ContinuousFactor | float | Override for Chirp.m_ContinuousFactor |
| m_GoViralFactor | int2 | Min/max range for the viral factor (curve shape) |

*Source: `Game.dll` -> `Game.Prefabs.RandomLikeCountData`*

## System Map

### `CreateChirpSystem` (Game.Triggers)

The core system that processes chirp creation requests and produces chirp entities.

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Enabled**: Only when `GameMode.IsGame()` is true
- **Input**: `NativeQueue<ChirpCreationData>` -- other systems enqueue requests via `GetQueue()`
- **Queries**:
  - `m_PrefabQuery`: All entities with `ChirpData` (chirp prefabs)
  - `m_ChirpQuery`: All entities with `Chirp + PrefabRef`, excluding `Temp/Deleted` (existing chirps)
  - `m_CitizenQuery`: All entities with `Citizen + HouseholdMember`, excluding `Deleted` (for random sender selection)
- **Key jobs**:
  - `CollectRecentChirpsJob` (IJobChunk, Burst) -- Scans existing chirps and builds a hashmap of chirps created within the last 18,000 frames (~5 minutes). This prevents duplicate chirps of the same type.
  - `CreateChirpJob` (IJob, Burst) -- Dequeues `ChirpCreationData`, resolves chirp prefab, finds a sender, calculates like parameters, and creates the chirp entity.
- **Sender resolution** (`FindSender`):
  1. **Service chirps**: Uses `ServiceChirpData.m_Account` (a chirper account entity)
  2. **Brand chirps**: Uses the sender directly (the company/brand entity)
  3. **Citizen chirps**: Tries employees of the sender company, then household citizens of the sender household, then falls back to a random adult citizen from the population
- **Like calculation**:
  - Base population = `educatedPopulation + uneducatedPopulation`
  - Random amount factor = `random(0.001, 0.03)` (or from `RandomLikeCountData`)
  - Target likes = `population * amountFactor`
  - Active duration = `random(0.2, 1.0) * 262144 frames` (~0.2 to 1.0 in-game days)
  - Viral factor = `random(5, 100)` (controls curve steepness)

*Source: `Game.dll` -> `Game.Triggers.CreateChirpSystem`*

### `ChirpLikeCountSystem` (Game.Triggers)

Animates the like count on active chirps over time.

- **Base class**: GameSystemBase
- **Update interval**: Every 64 frames
- **Queries**:
  - All entities with `Chirp`, excluding `Temp/Deleted`
- **Job**: `LikeCountUpdateJob` (IJobChunk, Burst)
  - For each chirp where `m_InactiveFrame > m_CreationFrame` and current frame <= `m_InactiveFrame`:
  - Calculates progress: `t = (currentFrame - creationFrame) / (inactiveFrame - creationFrame)`
  - Applies viral curve: `likes = targetLikes * (1 - (1 - t)^viralFactor)`
  - Uses `m_ContinuousFactor` as probability to skip a tick (adds randomness)
  - Only increases likes (uses `math.max` to prevent decreases)

*Source: `Game.dll` -> `Game.Triggers.ChirpLikeCountSystem`*

### `ChirperUISystem` (Game.UI.InGame)

The UI system that exposes chirp data to the frontend via Colossal UI bindings.

- **Base class**: UISystemBase
- **Group**: "chirper"
- **Bindings**:
  - `chirper.chirps` (RawValueBinding) -- JSON array of all chirps, sorted newest first
  - `chirper.chirpAdded` (RawEventBinding) -- Fired when new chirps are created
  - `chirper.addLike` (TriggerBinding<Entity>) -- Player likes a chirp
  - `chirper.removeLike` (TriggerBinding<Entity>) -- Player unlikes a chirp
  - `chirper.selectLink` (TriggerBinding<string>) -- Player clicks a link in a chirp
- **Chirp JSON structure** (per chirp):
  - `entity` -- Chirp entity reference
  - `sender` -- Sender info (entity, link with name/target)
  - `date` -- Creation tick (frame offset from first frame)
  - `messageId` -- Localization key (from `RandomLocalization` component)
  - `links` -- Array of linked entities (citizens, buildings)
  - `likes` -- Current like count
  - `liked` -- Whether player has liked it
- **Link resolution**: Chirp links can target entities, infoviews, or nothing. Company links redirect to the company's property (building). ChirperAccount links redirect to their associated infoview.
- **Avatar resolution**: Checks sender entity for an icon, then checks ChirperAccount for infoview icon, then checks BrandPrefab for thumbnail URL.

*Source: `Game.dll` -> `Game.UI.InGame.ChirperUISystem`*

### `ChirpLinkSystem` (Game.UI.InGame)

Caches entity names for chirp senders and links, so chirps remain readable even after the referenced entity is deleted (e.g., a citizen who died).

- **Base class**: GameSystemBase (implements ISerializable)
- **Purpose**: When an entity referenced by a chirp is deleted, the cached name is preserved so the chirp text still displays properly.
- **Data structure**: `Dictionary<Entity, CachedChirpData>` mapping chirp entities to cached sender/link names.
- **Serialized**: Yes -- cached names survive save/load.

*Source: `Game.dll` -> `Game.UI.InGame.ChirpLinkSystem`*

## Data Flow

```
[Game Event / Trigger System]
  Enqueues ChirpCreationData into CreateChirpSystem.GetQueue()
  (e.g., milestone reached, citizen had baby, crime event, service issue)
        |
        v
[CreateChirpSystem] (every frame when queue non-empty)
  CollectRecentChirpsJob:
    Scans existing chirps for duplicates (18,000-frame cooldown)
    Builds NativeParallelHashMap<prefab, chirpEntity>
        |
        v
  CreateChirpJob:
    Dequeues ChirpCreationData
    GetChirpPrefab: resolves trigger -> chirp prefab (random selection if multiple)
    Checks dedup map -> skip if recent chirp of same type exists
    FindSender: Service -> account, Brand -> direct, Citizen -> random from household/company/population
    Calculates target likes from population + random factors
    Creates chirp entity with Chirp component + ChirpEntity buffer
        |
        v
[ChirpLikeCountSystem] (every 64 frames)
  LikeCountUpdateJob:
    For each active chirp (currentFrame <= inactiveFrame):
      progress = (frame - creation) / (inactive - creation)
      likes = max(likes, targetLikes * (1 - (1-progress)^viralFactor))
      Random skip via continuousFactor
        |
        v
[ChirpLinkSystem] (on chirp creation/deletion)
  Caches sender and link entity names
  Preserves names when referenced entities are deleted
        |
        v
[ChirperUISystem] (on chirp change)
  Binds chirp data to UI as JSON
  Sorts chirps by creation frame (newest first)
  Resolves avatars, links, localization keys
  Handles player like/unlike actions
        |
        v
[UI Frontend - Chirper Panel]
  Displays chirp feed to player
  Shows sender name, avatar, message, like count
  Clickable links to entities/infoviews
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Chirp archetype | ChirpData.m_Archetype | Game.Prefabs.ChirpData |
| Sender type flags | ChirpData.m_Flags | Game.Prefabs.ChirpData |
| Service account | ServiceChirpData.m_Account | Game.Prefabs.ServiceChirpData |
| Trigger-to-chirp mapping | TriggerChirpData buffer | Game.Prefabs.TriggerChirpData |
| Like count tuning | RandomLikeCountData | Game.Prefabs.RandomLikeCountData |
| Dedup cooldown | Hardcoded 18,000 frames | CreateChirpSystem.CollectRecentChirpsJob |
| Like update interval | Hardcoded 64 frames | ChirpLikeCountSystem.GetUpdateInterval |
| Active duration base | 262,144 frames (~1 in-game day) | CreateChirpSystem.CreateChirpJob |
| Default viral factor | Random 5-100 | CreateChirpSystem.CreateChirpJob |
| Default amount factor | Random 0.001-0.03 | CreateChirpSystem.CreateChirpJob |

## Harmony Patch Points

### Candidate 1: `Game.Triggers.CreateChirpSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix
- **What it enables**: Intercept chirp creation entirely -- filter out unwanted chirps, modify the queue before processing, or inject custom chirps
- **Risk level**: Medium
- **Side effects**: Blocking chirps may cause the queue to grow indefinitely if not drained

### Candidate 2: `Game.Triggers.CreateChirpSystem+CreateChirpJob.FindSender`

- **Signature**: `Entity FindSender(Entity sender, Entity target, Entity prefab, ref Random random)`
- **Patch type**: Postfix
- **What it enables**: Override sender selection -- assign specific citizens or custom accounts as chirp senders
- **Risk level**: Low
- **Side effects**: Must return a valid, living citizen entity or the chirp won't display properly

### Candidate 3: `Game.UI.InGame.ChirperUISystem.BindChirp`

- **Signature**: `void BindChirp(IJsonWriter binder, Entity chirpEntity, bool newChirp)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify chirp display data before it reaches the UI -- change text, inject custom links, modify like counts
- **Risk level**: Low
- **Side effects**: Must write valid JSON structure or UI will break

### Candidate 4: `Game.Triggers.ChirpLikeCountSystem+LikeCountUpdateJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Prefix (skip original) or Transpiler
- **What it enables**: Change the like accumulation curve, speed up/slow down engagement, add "dislike" mechanics
- **Risk level**: Medium
- **Side effects**: Burst-compiled job -- Harmony patches on Burst jobs require careful handling

## Mod Blueprint

- **Systems to create**:
  - `CustomChirpTriggerSystem` -- monitors game state and enqueues `ChirpCreationData` for custom events
  - `ChirpFilterSystem` -- optionally filters/modifies the chirp queue before `CreateChirpSystem` processes it
- **Components to add**:
  - Custom chirp prefab entities with `ChirpData`, `TriggerChirpData`, and optionally `RandomLikeCountData`
  - Custom `ChirperAccountData` entities for new service accounts
- **Patches needed**:
  - Prefix on `CreateChirpSystem.OnUpdate` if filtering chirps
  - Postfix on `ChirperUISystem.BindChirp` if modifying display
- **Settings**:
  - Chirp frequency (cooldown override)
  - Like count multiplier
  - Enable/disable specific chirp categories
- **UI changes**:
  - Custom chirp messages via localization keys
  - Custom chirper account avatars via icon paths

## Examples

### Example 1: Enqueue a Custom Chirp

Enqueue a chirp creation request from a mod system. The `CreateChirpSystem` will process it on its next update.

```csharp
public partial class CustomChirpTriggerSystem : GameSystemBase
{
    private CreateChirpSystem _createChirpSystem;
    private EntityQuery _triggerPrefabQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _createChirpSystem = World.GetOrCreateSystemManaged<CreateChirpSystem>();
    }

    protected override void OnUpdate()
    {
        if (!ShouldTriggerChirp()) return;

        JobHandle deps;
        NativeQueue<ChirpCreationData> queue = _createChirpSystem.GetQueue(out deps);
        deps.Complete();

        queue.Enqueue(new ChirpCreationData
        {
            m_TriggerPrefab = myTriggerPrefabEntity,  // Must have TriggerChirpData buffer
            m_Sender = senderHouseholdOrCompany,       // Household/company for citizen selection
            m_Target = Entity.Null                     // Optional linked entity
        });

        _createChirpSystem.AddQueueWriter(Dependency);
    }

    private bool ShouldTriggerChirp() { /* your logic */ return false; }
}
```

### Example 2: Read All Active Chirps

Query and log all current chirps in the game.

```csharp
public void LogAllChirps(EntityManager em)
{
    EntityQuery chirpQuery = em.CreateEntityQuery(
        ComponentType.ReadOnly<Chirp>(),
        ComponentType.ReadOnly<PrefabRef>(),
        ComponentType.Exclude<Deleted>()
    );

    NativeArray<Entity> entities = chirpQuery.ToEntityArray(Allocator.Temp);
    NativeArray<Chirp> chirps = chirpQuery.ToComponentDataArray<Chirp>(Allocator.Temp);

    for (int i = 0; i < chirps.Length; i++)
    {
        Chirp chirp = chirps[i];
        Log.Info($"Chirp {entities[i]}: sender={chirp.m_Sender}, " +
                 $"likes={chirp.m_Likes}/{chirp.m_TargetLikes}, " +
                 $"viral={chirp.m_ViralFactor}, " +
                 $"liked={((chirp.m_Flags & ChirpFlags.Liked) != 0)}");
    }

    entities.Dispose();
    chirps.Dispose();
    chirpQuery.Dispose();
}
```

### Example 3: Modify Like Count Curve via Harmony Prefix

Patch the like count system to apply a multiplier to all chirp engagement.

```csharp
[HarmonyPatch(typeof(ChirpLikeCountSystem), "OnUpdate")]
public static class ChirpLikeMultiplierPatch
{
    private static float _likeMultiplier = 2.0f;

    public static void Postfix(ChirpLikeCountSystem __instance)
    {
        // After the system updates, scale all chirp likes
        EntityManager em = __instance.EntityManager;
        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadWrite<Chirp>(),
            ComponentType.Exclude<Deleted>()
        );

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Chirp chirp = em.GetComponentData<Chirp>(entities[i]);
            chirp.m_TargetLikes = (uint)(chirp.m_TargetLikes * _likeMultiplier);
            em.SetComponentData(entities[i], chirp);
        }

        entities.Dispose();
        query.Dispose();
    }
}
```

### Example 4: Filter Chirps by Type

Prevent specific chirp types from being created by patching the queue processing.

```csharp
[HarmonyPatch(typeof(CreateChirpSystem), "OnUpdate")]
public static class ChirpFilterPatch
{
    private static HashSet<string> _blockedPrefabs = new HashSet<string>();

    public static void Prefix(CreateChirpSystem __instance)
    {
        // Access the queue and drain blocked items
        // Note: Direct queue manipulation requires reflection since GetQueue
        // returns a NativeQueue that must be carefully synchronized
        // A safer approach is to remove chirp entities after creation
    }
}

// Safer alternative: remove unwanted chirps after creation
public partial class ChirpCleanupSystem : GameSystemBase
{
    private EntityQuery _newChirpQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _newChirpQuery = GetEntityQuery(
            ComponentType.ReadOnly<Chirp>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.ReadOnly<Created>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        NativeArray<Entity> entities = _newChirpQuery.ToEntityArray(Allocator.Temp);
        NativeArray<PrefabRef> prefabs = _newChirpQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (ShouldBlock(prefabs[i].m_Prefab))
            {
                em.AddComponent<Deleted>(entities[i]);
            }
        }

        entities.Dispose();
        prefabs.Dispose();
    }

    private bool ShouldBlock(Entity prefab) { /* your filter logic */ return false; }
}
```

### Example 5: Create a Custom Chirper Account

Register a new chirper account entity that can be used as a sender for custom chirps.

```csharp
public Entity CreateCustomChirperAccount(EntityManager em, PrefabSystem prefabSystem)
{
    // Create an entity with ChirperAccountData to act as a custom account
    Entity account = em.CreateEntity();
    em.AddComponent<ChirperAccountData>(account);

    // The ChirperUISystem will use this entity's name and icon
    // for display. You'll need a PrefabData component and a
    // corresponding ChirperAccount prefab for full integration.

    // For a service chirp referencing this account:
    // Set ServiceChirpData.m_Account = account on your chirp prefab

    return account;
}
```

## Open Questions

- [ ] How are chirp messages localized? The `RandomLocalizationIndex` component selects a variant, but the full localization key construction path (through `RandomLocalization.m_LocalizationID`) needs more investigation.
- [ ] What triggers enqueue `ChirpCreationData`? The trigger systems (milestone reached, population threshold, service issues) that produce chirps are spread across many systems and not fully catalogued.
- [ ] How does the `ChirperPanel` UI component (frontend) render the chirp feed? The C# side binds JSON, but the React/UI Toolkit frontend rendering is in game UI assemblies not decompiled here.
- [ ] Can modders add entirely new chirp prefabs at runtime, or must they be present in the prefab system at load time? The `TriggerChirpData` buffer approach suggests load-time registration.
- [ ] The `Telemetry.Chirp()` call in `ChirperUISystem.BindChirp` suggests chirps are tracked for analytics -- does this affect mod chirps?

## Sources

- Decompiled from: Game.dll
  - `Game.Triggers.Chirp`
  - `Game.Triggers.ChirpEntity`
  - `Game.Triggers.ChirpLink`
  - `Game.Triggers.ChirpCreationData`
  - `Game.Triggers.ChirpFlags`
  - `Game.Triggers.CreateChirpSystem`
  - `Game.Triggers.ChirpLikeCountSystem`
  - `Game.Prefabs.ChirpData`
  - `Game.Prefabs.ChirpDataFlags`
  - `Game.Prefabs.ChirperAccountData`
  - `Game.Prefabs.TriggerChirpData`
  - `Game.Prefabs.ServiceChirpData`
  - `Game.Prefabs.BrandChirpData`
  - `Game.Prefabs.RandomLikeCountData`
  - `Game.UI.InGame.ChirperUISystem`
  - `Game.UI.InGame.ChirpLinkSystem`
