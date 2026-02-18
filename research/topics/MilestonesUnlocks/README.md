# Research: Milestones & Unlocks

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 tracks city XP, triggers milestone level-ups, manages the development tree point economy, and unlocks prefabs (services, zones, buildings, policies, features) via dependency chains.

**Why**: A mod may need to: programmatically unlock/lock items, modify XP accumulation rates, add custom unlock requirements, skip milestones, award dev tree points, or create entirely new progression gating.

**Boundaries**: Out of scope -- the achievement/trophy system (`AchievementTriggerSystem`), population victory conditions, and specific Chirper messages triggered by milestones. Map tile purchase mechanics are documented separately in `research/topics/MapTilePurchase/`.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's There |
|----------|-----------|-------------|
| Game.dll | Game.City | XP, MilestoneLevel, DevTreePoints, MilestoneReachedEvent, XPRewardFlags |
| Game.dll | Game.Simulation | MilestoneSystem, XPSystem, XPAccumulationSystem, XPBuiltSystem, NetXPSystem, XPGain, XPMessage, XPReason, IMilestoneSystem, IXPSystem |
| Game.dll | Game.Prefabs | MilestoneData, MilestonePrefab, DevTreeNodeData, DevTreeNodePrefab, DevTreeNodeRequirement, DevTreeNodeAutoUnlock, UnlockRequirement, UnlockFlags, Unlock, Locked, Unlockable, ManualUnlockable, UnlockSystem, UnlockAllSystem, UnlockOnBuildData, UnlockRequirementData, ForceUnlockRequirementData, PrefabUnlockedRequirement, XPParameterData, XPParametersPrefab, UnlockFilterData, ForceUIGroupUnlockData, ObjectBuiltRequirementSystem, PrefabUnlockedRequirementSystem |
| Game.dll | Game.Prefabs.Modes | MilestonesMode, UnlockAtStartMode, XPParametersMode |
| Game.dll | Game.UI.InGame | MilestoneUISystem, DevTreeUISystem, ProgressionPanel |
| Game.dll | Game.Serialization | ResetUnlockRequirementSystem |

## Component Map

### `XP` (Game.City)

City-level singleton tracking total accumulated XP.

| Field | Type | Description |
|-------|------|-------------|
| m_XP | int | Total accumulated XP for the city |
| m_MaximumPopulation | int | Highest population ever reached (for one-time XP bonuses) |
| m_MaximumIncome | int | Highest income ever reached |
| m_XPRewardRecord | XPRewardFlags | Bitfield tracking one-time XP bonuses already awarded |

*Source: `Game.dll` -> `Game.City.XP`*

### `MilestoneLevel` (Game.City)

City-level singleton tracking the current milestone progression.

| Field | Type | Description |
|-------|------|-------------|
| m_AchievedMilestone | int | Index of the highest achieved milestone (0 = none) |

*Source: `Game.dll` -> `Game.City.MilestoneLevel`*

### `DevTreePoints` (Game.City)

City-level singleton tracking available development tree points.

| Field | Type | Description |
|-------|------|-------------|
| m_Points | int | Currently available (unspent) dev tree points |

*Source: `Game.dll` -> `Game.City.DevTreePoints`*

### `MilestoneReachedEvent` (Game.City)

Transient event component created when a milestone is reached.

| Field | Type | Description |
|-------|------|-------------|
| m_Milestone | Entity | The milestone prefab entity that was reached |
| m_Index | int | The milestone index |

*Source: `Game.dll` -> `Game.City.MilestoneReachedEvent`*

### `MilestoneData` (Game.Prefabs)

Per-milestone prefab component defining thresholds and rewards.

| Field | Type | Description |
|-------|------|-------------|
| m_Index | int | Milestone number (1-20, sequential) |
| m_Reward | int | Money awarded when milestone is reached |
| m_DevTreePoints | int | Dev tree points awarded |
| m_MapTiles | int | Map tiles unlocked for purchase |
| m_LoanLimit | int | Additional creditworthiness (loan capacity) |
| m_XpRequried | int | Cumulative XP threshold to reach this milestone |
| m_Major | bool | Whether this is a major milestone (UI styling) |
| m_IsVictory | bool | Whether reaching this triggers the victory condition |

*Source: `Game.dll` -> `Game.Prefabs.MilestoneData`*

### `DevTreeNodeData` (Game.Prefabs)

Per-node component on development tree nodes.

| Field | Type | Description |
|-------|------|-------------|
| m_Cost | int | Dev tree points cost to purchase (0 = auto-unlock) |
| m_Service | Entity | Service prefab this node belongs to |

*Source: `Game.dll` -> `Game.Prefabs.DevTreeNodeData`*

### `DevTreeNodeRequirement` (Game.Prefabs) -- buffer

Prerequisite nodes that must be unlocked before this node can be purchased.

| Field | Type | Description |
|-------|------|-------------|
| m_Node | Entity | Required dev tree node entity (any one must be unlocked) |

*Source: `Game.dll` -> `Game.Prefabs.DevTreeNodeRequirement`*

### `DevTreeNodeAutoUnlock` (Game.Prefabs)

Empty tag component added to dev tree nodes with cost=0, indicating they auto-unlock when their service is unlocked.

*Source: `Game.dll` -> `Game.Prefabs.DevTreeNodeAutoUnlock`*

### `UnlockRequirement` (Game.Prefabs) -- buffer

Dependency list on any lockable prefab, defining what must be unlocked first.

| Field | Type | Description |
|-------|------|-------------|
| m_Prefab | Entity | Prefab entity that must be unlocked |
| m_Flags | UnlockFlags | RequireAll (0x1) or RequireAny (0x2) |

*Source: `Game.dll` -> `Game.Prefabs.UnlockRequirement`*

### `Locked` (Game.Prefabs)

Enableable tag component. When enabled, the prefab is locked. The unlock system disables this component (not removes it) to unlock a prefab.

*Source: `Game.dll` -> `Game.Prefabs.Locked`*

### `Unlock` (Game.Prefabs)

Event component carrying an unlock event for a specific prefab.

| Field | Type | Description |
|-------|------|-------------|
| m_Prefab | Entity | The prefab entity to unlock |

*Source: `Game.dll` -> `Game.Prefabs.Unlock`*

### `XPParameterData` (Game.Prefabs)

Singleton prefab controlling XP rates from population and happiness.

| Field | Type | Description |
|-------|------|-------------|
| m_XPPerPopulation | float | XP per new population milestone (per update tick) |
| m_XPPerHappiness | float | XP per happiness unit (per update tick) |

*Source: `Game.dll` -> `Game.Prefabs.XPParameterData`*

### `XPGain` (Game.Simulation)

Queue element representing a pending XP addition.

| Field | Type | Description |
|-------|------|-------------|
| entity | Entity | Source entity (Entity.Null for global sources) |
| amount | int | XP amount to add |
| reason | XPReason | Category of XP gain |

*Source: `Game.dll` -> `Game.Simulation.XPGain`*

### `UnlockOnBuildData` (Game.Prefabs) -- buffer

Links a buildable prefab to entities that should be unlocked when it is placed.

| Field | Type | Description |
|-------|------|-------------|
| m_Entity | Entity | Entity to unlock when this prefab is built |

*Source: `Game.dll` -> `Game.Prefabs.UnlockOnBuildData`*

### `UnlockRequirementData` (Game.Prefabs)

Tracks progress toward build-count-based unlock requirements.

| Field | Type | Description |
|-------|------|-------------|
| m_Progress | int | Current count of built objects toward the requirement |

*Source: `Game.dll` -> `Game.Prefabs.UnlockRequirementData`*

## Enums

### `XPReason` (Game.Simulation)

| Value | Name | Description |
|-------|------|-------------|
| 0 | Unknown | Default/unknown |
| 1 | ServiceBuilding | Placing a service building |
| 2 | ServiceUpgrade | Upgrading a service building |
| 3 | ElectricityNetwork | One-time bonus for first electricity grid |
| 4 | Population | Ongoing XP from population growth |
| 5 | Happiness | Ongoing XP from average citizen happiness |
| 6 | Income | (Unused in current code) |
| 7 | Road | Building road segments |
| 8 | TrainTrack | Building train tracks |
| 9 | TramTrack | Building tram tracks |
| 10 | SubwayTrack | Building subway tracks |
| 11 | Waterway | Building waterways |
| 12 | Pipe | Building water/sewage pipes |
| 13 | PowerLine | Building power lines |

### `UnlockFlags` (Game.Prefabs)

| Value | Name | Description |
|-------|------|-------------|
| 0x1 | RequireAll | This requirement must be satisfied (AND logic) |
| 0x2 | RequireAny | At least one RequireAny must be satisfied (OR logic) |

### `XPRewardFlags` (Game.City)

| Value | Name | Description |
|-------|------|-------------|
| 0x1 | ElectricityGridBuilt | One-time electricity grid bonus already awarded |

## System Map

### `XPSystem` (Game.Simulation)

Central XP processing hub. All XP sources enqueue `XPGain` items into a shared `NativeQueue`; this system drains the queue and adds the totals to the city's `XP.m_XP`.

- **Base class**: GameSystemBase, IXPSystem
- **Update phase**: Simulation (every frame)
- **Reads**: XPGain queue, CitySystem.City
- **Writes**: XP.m_XP on the city entity
- **Key methods**:
  - `GetQueue(out JobHandle)` -- producers call this to get write access to the XP queue
  - `AddQueueWriter(JobHandle)` -- producers register their job handle
  - `TransferMessages(IXPMessageHandler)` -- drains the message queue (used by MilestoneUISystem)

### `XPAccumulationSystem` (Game.Simulation)

Periodic XP source from population and happiness. Runs 32 times per game day.

- **Base class**: GameSystemBase
- **Update interval**: `262144 / 32 = 8192` frames
- **Reads**: Population, XPParameterData, CityStatistics (taxable income)
- **Writes**: XP queue (population and happiness gains), XP.m_MaximumPopulation, XP.m_MaximumIncome
- **Key logic**:
  - Requires population >= 10 to produce XP
  - Population XP = `XPPerPopulation * (population - maxPopulation) / updatesPerDay`
  - Happiness XP = `XPPerHappiness * averageHappiness / updatesPerDay`

### `XPBuiltSystem` (Game.Simulation)

Awards XP when service buildings or upgrades are placed.

- **Base class**: GameSystemBase
- **Update phase**: Runs when entities with `Created` + `PrefabRef` exist
- **Reads**: PlaceableObjectData.m_XPReward, ServiceUpgradeData.m_XPReward, ElectricityConsumer
- **Writes**: XP queue (ServiceBuilding and ServiceUpgrade reasons), XP.m_XPRewardRecord
- **Key logic**:
  - One-time electricity grid bonus of 25 XP when first consumer is fulfilled
  - Tracks signature buildings via PlacedSignatureBuildingData

### `NetXPSystem` (Game.Simulation)

Awards XP when network segments (roads, tracks, pipes, power lines) are created.

- **Base class**: GameSystemBase
- **Update phase**: Runs when net edges with `Created` tag exist
- **Reads**: PlaceableNetData.m_XPReward, Curve.m_Length, Edge, road/track/pipeline/powerline data
- **Writes**: XP queue (Road, TrainTrack, TramTrack, SubwayTrack, Waterway, Pipe, PowerLine)
- **Key logic**:
  - XP = `(xpReward + elevationBonus) * curveLength / 112.0`
  - Elevated roads get +1.0 bonus per segment
  - Deleted segments subtract XP (only the highest-value network type is reported)

### `MilestoneSystem` (Game.Simulation)

Checks whether the city's XP has crossed the next milestone threshold, and triggers milestone rewards.

- **Base class**: GameSystemBase, IMilestoneSystem
- **Update phase**: Simulation (every frame)
- **Reads**: XP (via CitySystem.XP), MilestoneData, MilestoneLevel
- **Writes**: MilestoneLevel.m_AchievedMilestone, PlayerMoney, Creditworthiness
- **Key methods**:
  - `OnUpdate()` -- compares city XP to next milestone threshold; increments m_AchievedMilestone
  - `NextMilestone(int)` -- creates MilestoneReachedEvent and Unlock events; awards money and loan limit
  - `TryGetMilestone(int, out Entity, out MilestoneData)` -- finds milestone by index
  - `UnlockAllMilestones()` -- unlocks every milestone, awards all money/loans

### `DevTreeSystem` (Game.City)

Manages development tree points and node purchasing.

- **Base class**: GameSystemBase
- **Update phase**: Runs when MilestoneReachedEvent entities exist
- **Reads**: MilestoneReachedEvent, MilestoneData.m_DevTreePoints, DevTreeNodeData, Locked
- **Writes**: DevTreePoints.m_Points
- **Key methods**:
  - `OnUpdate()` -- adds dev tree points from milestone rewards
  - `Purchase(Entity node)` -- spends points to unlock a dev tree node (validates cost, service unlock, requirements)
  - `points` property -- get/set available points directly

### `UnlockSystem` (Game.Prefabs)

Core unlock resolver. Evaluates `UnlockRequirement` buffers on locked prefabs and unlocks them when requirements are met.

- **Base class**: GameSystemBase
- **Update phase**: Runs when Unlock events exist, or on game load, or when Updated entities exist
- **Reads**: Locked, UnlockRequirement buffer
- **Writes**: Locked (disables to unlock)
- **Key methods**:
  - `ProcessEvents()` -- handles Unlock event entities
  - `CheckUnlockRequirementsJob` -- parallel job checking requirement satisfaction
  - `UnlockPrefab(Entity, bool)` -- sets `Locked` enabled=false, optionally creates cascading Unlock event
  - `IsLocked(PrefabBase)` -- public API to check lock status
- **Unlock logic**: Runs in a loop until no more prefabs are unlockable (cascading unlocks)

### `UnlockAllSystem` (Game.Prefabs)

Debug/cheat system that unlocks everything. Disabled by default; enabled via `CityConfigurationSystem.unlockAll`.

- **Base class**: GameSystemBase
- **Update phase**: One-shot (disables itself after running)
- **Key methods**:
  - `UnlockAllImpl()` -- creates Unlock events for all Locked entities, calls `MilestoneSystem.UnlockAllMilestones()`

### `ObjectBuiltRequirementSystem` (Game.Prefabs)

Tracks build-count requirements. When a building with `UnlockOnBuildData` is placed, increments progress on linked requirements.

- **Base class**: GameSystemBase
- **Reads**: PrefabRef, Created/Deleted, UnlockOnBuildData buffer, ObjectBuiltRequirementData
- **Writes**: UnlockRequirementData.m_Progress, creates Unlock events when threshold reached
- **Key logic**: Decrement on delete, increment on create; unlock when `m_Progress >= m_MinimumCount`

### `PrefabUnlockedRequirementSystem` (Game.Prefabs)

Watches for Unlock events and cascades unlocks to entities with `PrefabUnlockedRequirement` buffers.

- **Base class**: GameSystemBase
- **Reads**: Unlock events, PrefabUnlockedRequirement buffer, Locked
- **Writes**: Creates cascading Unlock events

### `MilestoneUISystem` (Game.UI.InGame)

Exposes milestone data to the UI via bindings.

- **Base class**: UISystemBase, IXPMessageHandler, ISerializable
- **UI bindings** (group: "milestone"):
  - `achievedMilestone` -- current milestone index
  - `maxMilestoneReached` -- whether all milestones are complete
  - `achievedMilestoneXP` / `nextMilestoneXP` / `totalXP` -- XP progress
  - `milestones` -- sorted list of all milestones
  - `milestoneDetails` / `milestoneUnlocks` / `unlockDetails` -- detail panels
  - `xpMessageAdded` -- event fired when XP gain occurs
  - `unlockAll` -- whether unlock-all mode is active

### `DevTreeUISystem` (Game.UI.InGame)

Exposes development tree data to the UI.

- **Base class**: UISystemBase
- **UI bindings** (group: "devTree"):
  - `points` -- available dev tree points
  - `services` -- list of service categories
  - `nodes` / `nodeDetails` -- per-service node listing
  - `serviceDetails` -- service detail panels

## Data Flow

```
XP ACCUMULATION PIPELINE
========================
[Population + Happiness]          [Buildings Placed]          [Networks Built]
        |                                |                          |
        v                                v                          v
XPAccumulationSystem              XPBuiltSystem               NetXPSystem
 (every 8192 frames)         (on Created entities)        (on Created edges)
        |                                |                          |
        +----------> NativeQueue<XPGain> <--------------------------+
                            |
                            v
                      XPSystem.OnUpdate()
                      Drains queue -> XP.m_XP += gain.amount
                      Queues XPMessage for UI
                            |
                            v
MILESTONE CHECK (every frame)
==============================
                      MilestoneSystem.OnUpdate()
                      If XP >= nextMilestone.m_XpRequried:
                        1. MilestoneLevel.m_AchievedMilestone++
                        2. Create MilestoneReachedEvent entity
                        3. Create Unlock event for milestone prefab
                        4. PlayerMoney += milestone.m_Reward
                        5. Creditworthiness += milestone.m_LoanLimit
                            |
                            v
DEV TREE POINTS
===============
                      DevTreeSystem.OnUpdate()
                      Reads MilestoneReachedEvent entities
                      DevTreePoints.m_Points += milestone.m_DevTreePoints
                            |
                            v
                      DevTreeSystem.Purchase(node)
                      If DevTreePoints >= node.m_Cost AND service unlocked
                         AND requirements met:
                        1. DevTreePoints -= node.m_Cost
                        2. Create Unlock event for node
                            |
                            v
UNLOCK CASCADE
==============
                      UnlockSystem.OnUpdate()
                      1. ProcessEvents(): for each Unlock event entity,
                         disable Locked on target prefab
                      2. CheckUnlockRequirementsJob: scan all Locked+UnlockRequirement
                         entities; if requirements met, enqueue for unlock
                      3. Loop until no more unlockable (cascading chain)
                            |
                            v
                      [Prefab now available in game UI]
                      Buildings, zones, policies, features
                      become placeable/activatable
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| XP per population | XPParameterData.m_XPPerPopulation | Singleton prefab (XPParametersPrefab) |
| XP per happiness | XPParameterData.m_XPPerHappiness | Singleton prefab (XPParametersPrefab) |
| Milestone XP thresholds | MilestoneData.m_XpRequried | Per-milestone prefab (MilestonePrefab) |
| Milestone money reward | MilestoneData.m_Reward | Per-milestone prefab |
| Milestone dev tree points | MilestoneData.m_DevTreePoints | Per-milestone prefab |
| Milestone map tiles | MilestoneData.m_MapTiles | Per-milestone prefab |
| Milestone loan limit | MilestoneData.m_LoanLimit | Per-milestone prefab |
| Dev tree node cost | DevTreeNodeData.m_Cost | Per-node prefab (DevTreeNodePrefab) |
| Building XP reward | PlaceableObjectData.m_XPReward | Per-building prefab |
| Upgrade XP reward | ServiceUpgradeData.m_XPReward | Per-upgrade prefab |
| Network XP reward | PlaceableNetData.m_XPReward | Per-network prefab |
| Network XP length divisor | NetXPSystem.kXPRewardLength | Hardcoded: 112.0f |
| Electricity grid bonus | XPBuiltSystem.kElectricityGridXPBonus | Hardcoded: 25 |
| XP accumulation frequency | XPAccumulationSystem.kUpdatesPerDay | Hardcoded: 32 per game day |
| Default dev tree points formula | DevTreeSystem.GetDefaultPoints(level) | `(level+1)/2 + 1` (levels 1-18), 10 (level >= 19) |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.MilestoneSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify milestone thresholds at runtime; skip milestone requirements; trigger custom rewards on milestone reach
- **Risk level**: Low -- straightforward logic, no complex job scheduling
- **Side effects**: Postfix can read the new `m_AchievedMilestone` to know if a level-up just happened

### Candidate 2: `Game.Simulation.XPSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix
- **What it enables**: Multiply all XP gains (by modifying queue contents), cap XP, or add bonus XP
- **Risk level**: Medium -- operates on NativeQueue shared with multiple job writers
- **Side effects**: Must ensure job dependencies are complete before reading the queue

### Candidate 3: `Game.City.DevTreeSystem.Purchase`

- **Signature**: `public void Purchase(Entity node)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override cost checks, auto-purchase nodes, award free points, track purchases
- **Risk level**: Low -- called from UI binding, simple synchronous method
- **Side effects**: None beyond the intended unlock cascade

### Candidate 4: `Game.Prefabs.UnlockSystem.UnlockPrefab`

- **Signature**: `private void UnlockPrefab(Entity unlock, bool createEvent)`
- **Patch type**: Prefix
- **What it enables**: Prevent specific prefabs from unlocking; add custom logic on unlock; log unlock events
- **Risk level**: Low -- single-entity operation
- **Side effects**: Blocking unlock here prevents cascade for downstream dependencies

### Candidate 5: `Game.Simulation.XPAccumulationSystem.XPAccumulateJob.Execute`

- **Signature**: `public void Execute()` (BurstCompile)
- **Patch type**: Cannot patch directly (Burst-compiled). Use Prefix on `OnUpdate()` instead to modify `XPParameterData`
- **Risk level**: N/A -- must modify the singleton prefab data instead
- **Side effects**: Changing XPParameterData affects all future accumulation ticks

## Mod Blueprint

- **Systems to create**: Custom `GameSystemBase` systems for XP multiplier management, custom milestone rewards, or forced unlock/lock behavior
- **Components to add**: Optional custom tag components for tracking mod-managed unlocks
- **Patches needed**: Prefix on `MilestoneSystem.OnUpdate` to modify XP thresholds; Postfix on `DevTreeSystem.Purchase` for custom purchase tracking
- **Settings**: XP multiplier, auto-unlock toggles, custom milestone thresholds
- **UI changes**: Bind to "milestone" and "devTree" UI groups for custom panels; override `achievedMilestoneXP`/`nextMilestoneXP` for display

## Examples

### Example 1: Unlock All Milestones Programmatically

Use the built-in `MilestoneSystem.UnlockAllMilestones()` method. This awards all money, loan limits, and creates Unlock events for every milestone prefab.

```csharp
public partial class UnlockAllMilestonesSystem : GameSystemBase
{
    private MilestoneSystem m_MilestoneSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_MilestoneSystem = World.GetOrCreateSystemManaged<MilestoneSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Call this once to unlock every milestone and award all rewards.
    /// </summary>
    public void UnlockAll()
    {
        m_MilestoneSystem.UnlockAllMilestones();
    }
}
```

### Example 2: Check Whether a Prefab Is Locked

Query the `Locked` enableable component to determine if a specific prefab is currently locked.

```csharp
public partial class UnlockCheckerSystem : GameSystemBase
{
    private PrefabSystem m_PrefabSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Returns true if the given prefab entity is currently locked.
    /// </summary>
    public bool IsPrefabLocked(Entity prefabEntity)
    {
        return EntityManager.HasEnabledComponent<Locked>(prefabEntity);
    }

    /// <summary>
    /// Returns true if the given PrefabBase is currently locked.
    /// Uses PrefabSystem to resolve the entity.
    /// </summary>
    public bool IsPrefabLocked(PrefabBase prefab)
    {
        Entity entity = m_PrefabSystem.GetEntity(prefab);
        return EntityManager.HasEnabledComponent<Locked>(entity);
    }
}
```

### Example 3: Award Bonus XP

Inject XP directly into the XP queue to add arbitrary amounts. Works the same way the game's own systems add XP.

```csharp
public partial class BonusXPSystem : GameSystemBase
{
    private XPSystem m_XPSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_XPSystem = World.GetOrCreateSystemManaged<XPSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Add XP to the city. Processes on the next XPSystem update tick.
    /// </summary>
    public void AwardXP(int amount, XPReason reason = XPReason.Unknown)
    {
        JobHandle deps;
        NativeQueue<XPGain> queue = m_XPSystem.GetQueue(out deps);
        deps.Complete();

        queue.Enqueue(new XPGain
        {
            amount = amount,
            entity = Entity.Null,
            reason = reason
        });
    }
}
```

### Example 4: Force-Unlock a Specific Prefab

Create an `Unlock` event entity to unlock a specific prefab. The `UnlockSystem` will process it and cascade to any dependents.

```csharp
public partial class ForceUnlockSystem : GameSystemBase
{
    private PrefabSystem m_PrefabSystem;
    private EntityArchetype m_UnlockEventArchetype;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        m_UnlockEventArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<Game.Common.Event>(),
            ComponentType.ReadWrite<Unlock>()
        );
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Unlock a specific prefab entity. The UnlockSystem will cascade
    /// to any prefabs that depend on this one via UnlockRequirement.
    /// </summary>
    public void UnlockPrefab(Entity prefabEntity)
    {
        if (!EntityManager.HasEnabledComponent<Locked>(prefabEntity))
            return; // Already unlocked

        Entity unlockEvent = EntityManager.CreateEntity(m_UnlockEventArchetype);
        EntityManager.SetComponentData(unlockEvent, new Unlock(prefabEntity));
    }

    /// <summary>
    /// Unlock by PrefabBase reference.
    /// </summary>
    public void UnlockPrefab(PrefabBase prefab)
    {
        Entity entity = m_PrefabSystem.GetEntity(prefab);
        UnlockPrefab(entity);
    }
}
```

### Example 5: XP Multiplier via Harmony Patch

Modify the `XPParameterData` singleton to change XP accumulation rates. This affects population and happiness XP gains.

```csharp
[HarmonyPatch(typeof(Game.Simulation.XPAccumulationSystem), "OnUpdate")]
public static class XPMultiplierPatch
{
    private static float s_Multiplier = 2.0f;

    /// <summary>
    /// Prefix: temporarily modify the XP parameters before the
    /// accumulation job reads them. The singleton prefab data is
    /// shared, so this affects all future ticks until reverted.
    /// </summary>
    public static void Prefix(XPAccumulationSystem __instance)
    {
        // Access the singleton query via reflection or stored reference
        var query = __instance.GetType()
            .GetField("m_XPSettingsQuery",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)
            ?.GetValue(__instance);

        if (query is EntityQuery eq && !eq.IsEmptyIgnoreFilter)
        {
            var data = eq.GetSingleton<XPParameterData>();
            data.m_XPPerPopulation *= s_Multiplier;
            data.m_XPPerHappiness *= s_Multiplier;
            eq.SetSingleton(data);
        }
    }

    public static void SetMultiplier(float multiplier)
    {
        s_Multiplier = multiplier;
    }
}
```

## Open Questions

- [ ] What are the actual default XP threshold values for each of the ~20 milestones? These are defined in MilestonePrefab assets, not in code. The code only stores the structure.
- [ ] How does the `MilestonesMode` mode prefab override milestone values for different game modes (easy/hard)? The mechanism is clear but the actual override values are in prefab data.
- [ ] What is the exact interaction between `UnlockAtStartMode` and the progression system in sandbox/editor modes? The code clears UnlockRequirement buffers, but the full mode initialization flow involves multiple mode prefabs.
- [ ] The `ForceUIGroupUnlockData` buffer -- which UI groups does it affect and when is it populated? Used for forcing UI categories to appear even when content is locked.
- [ ] Does `PrefabUnlockedRequirementSystem` handle all cascading unlock scenarios, or are there edge cases where the cascade chain breaks?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Decompiled types: MilestoneSystem, MilestoneData, MilestonePrefab, XP, MilestoneLevel, DevTreePoints, MilestoneReachedEvent, DevTreeSystem, DevTreeNodeData, DevTreeNodePrefab, DevTreeNodeRequirement, DevTreeNodeAutoUnlock, UnlockSystem, UnlockAllSystem, UnlockRequirement, UnlockFlags, Unlock, Locked, Unlockable, ManualUnlockable, XPSystem, XPAccumulationSystem, XPBuiltSystem, NetXPSystem, XPGain, XPMessage, XPReason, XPRewardFlags, XPParameterData, XPParametersPrefab, UnlockOnBuildData, UnlockRequirementData, ForceUnlockRequirementData, PrefabUnlockedRequirement, PrefabUnlockedRequirementSystem, ObjectBuiltRequirementSystem, MilestonesMode, UnlockAtStartMode, ResetUnlockRequirementSystem, MilestoneUISystem, DevTreeUISystem, IMilestoneSystem, IXPSystem, UnlockFilterData, ForceUIGroupUnlockData
- Tool: ilspycmd v9.1 (.NET 8.0)
