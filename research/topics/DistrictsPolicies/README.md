# Research: Districts & Policies

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 defines districts as painted area entities, applies policies (toggle and slider) to districts/buildings/routes/cities, and how those policies produce runtime modifiers that affect simulation values like speed limits, parking fees, garbage production, and traffic restrictions.

**Why**: Mods need to create custom policies, apply modifiers to districts, read which district an entity belongs to, and react to policy changes.

**Boundaries**: This research covers all four policy scopes handled by `ModifiedSystem`: District, Building, Route (transit lines), and City. Building-level and city-wide modifiers share the same architecture but their specific modifier types are not exhaustively listed here.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Areas | District, DistrictModifier, CurrentDistrict, BorderDistrict, ServiceDistrict, DistrictModifierType, DistrictOption, CurrentDistrictSystem, ServiceDistrictSystem |
| Game.dll | Game.Policies | Policy, PolicyFlags, Modify, ModifiedSystem, DistrictModifierInitializeSystem |
| Game.dll | Game.Prefabs | DistrictPrefab, PolicyPrefab, PolicyTogglePrefab, PolicySliderPrefab, PolicyData, PolicySliderData, DistrictData, DistrictOptionData, DistrictModifierData, DistrictModifiers, DistrictOptions, DefaultPolicyData, DefaultPolicyInfo, PolicyCategory, PolicyVisibility, ModifierValueMode, PolicySliderUnit |
| Game.dll | Game.Pathfind | LanePoliciesSystem |
| Game.dll | Game.UI.InGame | DistrictsSection, PoliciesUISystem |

## Component Map

### `District` (Game.Areas)

| Field | Type | Description |
|-------|------|-------------|
| m_OptionMask | uint | Bitmask of active DistrictOption flags (e.g. PaidParking, ForbidHeavyTraffic) |

*Source: `Game.dll` -> `Game.Areas.District`*

### `DistrictModifier` (Game.Areas) [Buffer]

| Field | Type | Description |
|-------|------|-------------|
| m_Delta | float2 | x = absolute delta, y = relative delta. Index in buffer corresponds to DistrictModifierType enum value |

*Source: `Game.dll` -> `Game.Areas.DistrictModifier`*

### `CurrentDistrict` (Game.Areas)

| Field | Type | Description |
|-------|------|-------------|
| m_District | Entity | The district entity this object/building is currently inside |

*Source: `Game.dll` -> `Game.Areas.CurrentDistrict`*

### `BorderDistrict` (Game.Areas)

| Field | Type | Description |
|-------|------|-------------|
| m_Left | Entity | District entity on left side of a road edge |
| m_Right | Entity | District entity on right side of a road edge |

*Source: `Game.dll` -> `Game.Areas.BorderDistrict`*

### `ServiceDistrict` (Game.Areas) [Buffer]

| Field | Type | Description |
|-------|------|-------------|
| m_District | Entity | A district this service building is assigned to serve |

*Source: `Game.dll` -> `Game.Areas.ServiceDistrict`*

### `Policy` (Game.Policies) [Buffer]

| Field | Type | Description |
|-------|------|-------------|
| m_Policy | Entity | The policy prefab entity |
| m_Flags | PolicyFlags | Active (1) or inactive (0) |
| m_Adjustment | float | Slider value for PolicySliderPrefab; unused for toggles |

*Source: `Game.dll` -> `Game.Policies.Policy`*

### `Modify` (Game.Policies)

| Field | Type | Description |
|-------|------|-------------|
| m_Entity | Entity | Target entity (district, building, city, or route) |
| m_Policy | Entity | Policy prefab entity being toggled/adjusted |
| m_Flags | PolicyFlags | Desired active state |
| m_Adjustment | float | Desired slider value |

*Source: `Game.dll` -> `Game.Policies.Modify` -- event component on event entities*

### `PolicyData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_Visibility | int | 0 = Default (visible), 1 = HideFromPolicyList |

*Source: `Game.dll` -> `Game.Prefabs.PolicyData`*

### `PolicySliderData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_Range | Bounds1 | Min/max slider range |
| m_Default | float | Default slider value |
| m_Step | float | Slider step increment |
| m_Unit | int | 0=money, 1=percentage, 2=integer (PolicySliderUnit) |

*Source: `Game.dll` -> `Game.Prefabs.PolicySliderData`*

### `DistrictOptionData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_OptionMask | uint | Bitmask of DistrictOption values this policy sets |

*Source: `Game.dll` -> `Game.Prefabs.DistrictOptionData`*

### `DistrictModifierData` (Game.Prefabs) [Buffer]

| Field | Type | Description |
|-------|------|-------------|
| m_Type | DistrictModifierType | Which simulation value to modify |
| m_Mode | ModifierValueMode | Relative, Absolute, or InverseRelative |
| m_Range | Bounds1 | Min/max effect range (interpolated by slider position) |

*Source: `Game.dll` -> `Game.Prefabs.DistrictModifierData`*

### `DefaultPolicyData` (Game.Prefabs) [Buffer]

| Field | Type | Description |
|-------|------|-------------|
| m_Policy | Entity | Policy prefab entity that is active by default on new districts |

*Source: `Game.dll` -> `Game.Prefabs.DefaultPolicyData`*

## Enums

### `DistrictModifierType` (Game.Areas)

| Value | Name | Description |
|-------|------|-------------|
| 0 | GarbageProduction | Modifies garbage output |
| 1 | ProductConsumption | Modifies commercial product consumption |
| 2 | ParkingFee | Modifies parking costs |
| 3 | BuildingFireHazard | Modifies fire risk |
| 4 | BuildingFireResponseTime | Modifies fire response |
| 5 | BuildingUpkeep | Modifies building maintenance costs |
| 6 | LowCommercialTax | Modifies commercial tax rate |
| 7 | Wellbeing | Modifies citizen wellbeing |
| 8 | CrimeAccumulation | Modifies crime rate |
| 9 | StreetSpeedLimit | Modifies road speed limits |
| 10 | StreetTrafficSafety | Modifies traffic safety |
| 11 | EnergyConsumptionAwareness | Modifies energy usage |
| 12 | CarReserveProbability | Modifies chance citizens reserve cars |

### `DistrictOption` (Game.Areas)

| Value | Name | Description |
|-------|------|-------------|
| 0 | PaidParking | Enables paid parking in district |
| 1 | ForbidCombustionEngines | Bans combustion engine vehicles |
| 2 | ForbidTransitTraffic | Bans through-traffic |
| 3 | ForbidHeavyTraffic | Bans heavy vehicles |
| 4 | ForbidBicycles | Bans bicycles |

### `PolicyFlags` (Game.Policies)

| Value | Name | Description |
|-------|------|-------------|
| 1 | Active | Policy is currently active |

### `PolicyCategory` (Game.Prefabs)

| Value | Name |
|-------|------|
| 0 | None |
| 1 | CityPlanning |
| 2 | Budget |
| 3 | Traffic |
| 4 | Culture |
| 5 | Services |

### `ModifierValueMode` (Game.Prefabs)

| Value | Name | Effect |
|-------|------|--------|
| 0 | Relative | `delta.y = delta.y * (1 + value) + value` |
| 1 | Absolute | `delta.x += value` |
| 2 | InverseRelative | `value = 1/max(0.001, 1+value) - 1`, then applied as Relative |

## System Map

### `CurrentDistrictSystem` (Game.Areas)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**:
  - Updated entities with CurrentDistrict or BorderDistrict
  - Also runs when `UpdateCollectSystem.districtsUpdated` is true
- **Reads**: Transform, EdgeGeometry, District, area quad tree (Node, Triangle)
- **Writes**: CurrentDistrict.m_District, BorderDistrict.m_Left/m_Right
- **Key methods**:
  - `OnUpdate()` -- Uses area search tree to find which district polygon each entity is inside via point-in-triangle tests
  - `FindDistrictParallelJob` -- For existing entities when district boundaries change, iterates object/net quad trees to find affected items
  - `FindDistrictChunkJob` -- For newly created/updated entities, assigns their current district

### `ServiceDistrictSystem` (Game.Areas)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**:
  - Deleted District entities
  - Entities with ServiceDistrict buffer
- **Reads**: Deleted districts
- **Writes**: ServiceDistrict buffer (removes references to deleted districts)
- **Key methods**:
  - `OnUpdate()` -- Cleans up ServiceDistrict buffer entries when districts are deleted

### `DistrictModifierInitializeSystem` (Game.Policies)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**:
  - Created District entities (with Policy buffer, excluding Temp)
- **Reads**: Policy buffer, PolicySliderData, DistrictOptionData, DistrictModifierData
- **Writes**: District.m_OptionMask, DistrictModifier buffer
- **Key methods**:
  - `RefreshDistrictOptions()` -- Iterates active policies, OR's each policy's DistrictOptionData.m_OptionMask into District.m_OptionMask
  - `RefreshDistrictModifiers()` -- Clears modifier buffer, iterates active policies, interpolates slider value to modifier delta, calls AddModifier
  - `AddModifier()` -- Applies modifier value based on ModifierValueMode (Relative, Absolute, InverseRelative) to the DistrictModifier at the index matching DistrictModifierType

### `ModifiedSystem` (Game.Policies)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (responds to Modify events)
- **Queries**:
  - Event entities with Modify component
- **Reads**: Modify, Owner, ServiceUpgrade, CoverageElement, various refresh data
- **Writes**: Policy buffer, District, DistrictModifier, BuildingModifier, RouteModifier, CityModifier
- **Key methods**:
  - `ModifyPolicyJob.Execute()` -- Processes Modify events: adds/removes/updates Policy entries in the target entity's Policy buffer, then calls RefreshEffects
  - `RefreshEffects()` -- Delegates to the appropriate refresh system (district, building, route, or city) to recalculate modifiers
  - `GetPolicyRange()` -- Determines scope by checking component presence on the target entity: District component -> District, Owner+ServiceUpgrade -> Building, Route component -> Route, otherwise -> City

**Inner enum `ModifiedSystem.PolicyRange`**:

| Value | Name | Description |
|-------|------|-------------|
| 0 | None | No policy range determined |
| 1 | District | Policy targets a district area entity |
| 2 | Building | Policy targets a building entity (with Owner+ServiceUpgrade) |
| 3 | Route | Policy targets a transit route entity |
| 4 | City | Policy targets the city-wide singleton |

**Inner struct `ModifiedSystem.PolicyEventInfo`**:

| Field | Type | Description |
|-------|------|-------------|
| m_Activated | bool | True if the policy was activated, false if deactivated |
| m_Entity | Entity | The target entity the policy was applied to |
| m_PolicyRange | PolicyRange | The determined scope of the policy event |

`PolicyEventInfo` is produced by `ModifyPolicyJob` after processing a `Modify` event, encapsulating the result for downstream refresh logic.

### Policy Scope: All Four Targets

`ModifiedSystem.GetPolicyRange()` determines which scope a policy targets. The same `Policy` buffer and `Modify` event pattern is used for all four:

| Scope | Target Entity | Modifier Buffer | Example Policies |
|-------|---------------|-----------------|------------------|
| District | Area entity with `District` component | `DistrictModifier` | Speed limits, parking fees, traffic bans |
| Building | Building entity with service upgrades | `BuildingModifier` | Service building efficiency adjustments |
| Route | Transit route entity (bus/tram/train line) | `RouteModifier` | Ticket price, vehicle frequency adjustments |
| City | City-wide singleton entity | `CityModifier` | City-wide tax rates, global service adjustments |

Route modifiers work identically to district modifiers: `ModifiedSystem` processes the `Modify` event, updates the `Policy` buffer on the route entity, and calls `RefreshEffects()` which delegates to the route-specific refresh logic to rebuild the `RouteModifier` buffer.

### `PoliciesUISystem` (Game.UI.InGame)

- **Base class**: UISystemBase
- **Update phase**: UI
- **Key methods**:
  - `SetPolicy(Entity entity, Entity policy, bool active, float adjustment)` -- Canonical UI-level API for setting policies programmatically. Creates a `Modify` event entity targeting the given entity with the specified policy, active state, and slider adjustment value. This is the same path the game UI uses when the player toggles or adjusts a policy.

**Usage**: Call `SetPolicy` from mod code to activate or deactivate policies without manually constructing `Modify` event entities. This is the preferred approach for programmatic policy changes from UI-layer code.

```csharp
// Get the PoliciesUISystem instance
PoliciesUISystem policiesUI = World.GetOrCreateSystemManaged<PoliciesUISystem>();

// Activate a policy on a district
policiesUI.SetPolicy(districtEntity, policyPrefabEntity, true, 0f);

// Set a slider policy to a specific value
policiesUI.SetPolicy(districtEntity, sliderPolicyPrefab, true, 0.5f);

// Deactivate a policy
policiesUI.SetPolicy(districtEntity, policyPrefabEntity, false, 0f);
```

### `LanePoliciesSystem` (Game.Pathfind)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (responds to Modify events)
- **Queries**:
  - Modify events
  - BorderDistrict edges with SubLane
- **Reads**: Modify, DistrictOptionData, DistrictModifierData, BorderDistrict
- **Writes**: PathfindUpdated on affected lanes
- **Key methods**:
  - `OnUpdate()` -- When district policies change (parking, speed, traffic bans), marks affected lanes for pathfinding recalculation

## Data Flow

```
DISTRICT CREATION
  Player paints district area
    -> DistrictPrefab.GetArchetypeComponents creates entity with:
       District, Geometry, DistrictModifier (buffer), Policy (buffer)
    -> DistrictModifierInitializeSystem processes Created entities
       Applies default policies to District.m_OptionMask and DistrictModifier buffer
          |
          v
ENTITY-TO-DISTRICT MAPPING
  CurrentDistrictSystem
    For each entity with CurrentDistrict component:
      Uses area quad tree to do point-in-triangle test
      Writes district entity to CurrentDistrict.m_District
    For each road edge with BorderDistrict component:
      Tests left/right sides of road against district polygons
      Writes BorderDistrict.m_Left and m_Right
          |
          v
POLICY ACTIVATION (User toggles/adjusts a policy)
  UI creates Event entity with Modify component
    m_Entity = district entity
    m_Policy = policy prefab entity
    m_Flags = Active or 0
    m_Adjustment = slider value
          |
          v
  ModifiedSystem.ModifyPolicyJob
    Finds Policy buffer on target entity
    Adds, updates, or removes the Policy entry
    Calls RefreshEffects -> RefreshDistrictOptions + RefreshDistrictModifiers
      -> District.m_OptionMask updated (OR of all active option masks)
      -> DistrictModifier buffer rebuilt (modifiers from all active policies)
          |
          v
  LanePoliciesSystem
    Checks Modify events for lane-affecting policies
    Marks affected lanes with PathfindUpdated
    (speed limits, parking, traffic bans trigger pathfind recalculation)
          |
          v
MODIFIER CONSUMPTION (Game systems read modifiers)
  Simulation systems read DistrictModifier buffer and District.m_OptionMask
  e.g. speed limit modifier affects CarLane pathfinding costs
  e.g. PaidParking option affects ParkingLane availability
  e.g. garbage production modifier scales building garbage output
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Policy visibility | PolicyPrefab.m_Visibility | Game.Prefabs.PolicyPrefab |
| Policy category | PolicyPrefab.m_Category | Game.Prefabs.PolicyPrefab |
| Slider range/default/step | PolicySliderPrefab fields | Game.Prefabs.PolicySliderPrefab |
| District modifier type/mode/range | DistrictModifierInfo | Game.Prefabs.DistrictModifierInfo (array on DistrictModifiers component) |
| District options | DistrictOptions.m_Options | Game.Prefabs.DistrictOptions (array of DistrictOption) |
| Default policies | DefaultPolicyInfo.m_Policy | Game.Prefabs.DefaultPolicyInfo |
| Name colors | DistrictPrefab.m_NameColor | Game.Prefabs.DistrictPrefab |

## Harmony Patch Points

### Candidate 1: `Game.Policies.ModifiedSystem.ModifyPolicyJob.RefreshEffects`

- **Signature**: `void RefreshEffects(Entity entity, Entity policy, DynamicBuffer<Policy> policies)`
- **Patch type**: Postfix
- **What it enables**: React to any policy change on any entity, add custom side effects
- **Risk level**: Medium
- **Side effects**: Called from a job; patching requires careful thread safety

### Candidate 2: `Game.Policies.DistrictModifierInitializeSystem.DistrictModifierRefreshData.RefreshDistrictModifiers`

- **Signature**: `void RefreshDistrictModifiers(DynamicBuffer<DistrictModifier> modifiers, DynamicBuffer<Policy> policies)`
- **Patch type**: Postfix
- **What it enables**: Inject custom modifiers or adjust modifier values after standard calculation
- **Risk level**: Low
- **Side effects**: Modifiers are recalculated from scratch each time, so postfix additions are safe

### Candidate 3: `Game.Policies.DistrictModifierInitializeSystem.DistrictModifierRefreshData.AddModifier`

- **Signature**: `static void AddModifier(DynamicBuffer<DistrictModifier> modifiers, DistrictModifierData modifierData, float delta)`
- **Patch type**: Prefix (to override) or Postfix (to adjust)
- **What it enables**: Change how individual modifier values are calculated (e.g. scale effects)
- **Risk level**: Low
- **Side effects**: Static method, straightforward to patch

## Mod Blueprint

- **Systems to create**: Custom system that reads DistrictModifier buffer and CurrentDistrict to apply custom game effects
- **Components to add**: None required for reading; new DistrictModifierType values would need custom handling
- **Patches needed**: Postfix on RefreshDistrictModifiers to inject additional modifiers; or create Modify events programmatically to toggle policies
- **Settings**: User-configurable modifier multipliers
- **UI changes**: Custom policy entries would need UI binding to appear in the district policy panel

## Examples

### Example 1: Read a Building's Current District

Check which district a building belongs to and what modifiers apply there.

```csharp
public void LogBuildingDistrict(EntityManager em, Entity building)
{
    if (!em.HasComponent<CurrentDistrict>(building)) return;

    CurrentDistrict cd = em.GetComponentData<CurrentDistrict>(building);
    if (cd.m_District == Entity.Null)
    {
        Log.Info($"Building {building} is not in any district.");
        return;
    }

    Log.Info($"Building {building} is in district {cd.m_District}");

    // Read the district's option mask
    if (em.HasComponent<District>(cd.m_District))
    {
        District district = em.GetComponentData<District>(cd.m_District);
        bool paidParking = (district.m_OptionMask & (1u << (int)DistrictOption.PaidParking)) != 0;
        bool noCombustion = (district.m_OptionMask & (1u << (int)DistrictOption.ForbidCombustionEngines)) != 0;
        Log.Info($"  Paid parking: {paidParking}");
        Log.Info($"  No combustion: {noCombustion}");
    }

    // Read modifiers
    if (em.HasBuffer<DistrictModifier>(cd.m_District))
    {
        DynamicBuffer<DistrictModifier> mods = em.GetBuffer<DistrictModifier>(cd.m_District);
        if (mods.Length > (int)DistrictModifierType.StreetSpeedLimit)
        {
            DistrictModifier speedMod = mods[(int)DistrictModifierType.StreetSpeedLimit];
            Log.Info($"  Speed limit modifier: absolute={speedMod.m_Delta.x}, relative={speedMod.m_Delta.y}");
        }
    }
}
```

### Example 2: Programmatically Toggle a District Policy

Create a Modify event to activate or deactivate a policy on a district.

```csharp
public void ToggleDistrictPolicy(
    EntityManager em,
    Entity districtEntity,
    Entity policyPrefab,
    bool activate)
{
    Entity eventEntity = em.CreateEntity();
    em.AddComponent<Game.Common.Event>(eventEntity);
    em.AddComponentData(eventEntity, new Modify(
        entity: districtEntity,
        policy: policyPrefab,
        active: activate,
        adjustment: 0f
    ));
}
```

### Example 3: Adjust a Slider Policy Value

Set a slider policy (e.g. speed limit) to a specific value on a district.

```csharp
public void SetSliderPolicy(
    EntityManager em,
    Entity districtEntity,
    Entity sliderPolicyPrefab,
    float sliderValue)
{
    Entity eventEntity = em.CreateEntity();
    em.AddComponent<Game.Common.Event>(eventEntity);
    em.AddComponentData(eventEntity, new Modify(
        entity: districtEntity,
        policy: sliderPolicyPrefab,
        active: true,
        adjustment: sliderValue
    ));
}
```

### Example 4: Custom System Reading District Modifiers

A system that monitors garbage production modifiers across all districts.

```csharp
public partial class DistrictGarbageMonitorSystem : GameSystemBase
{
    private EntityQuery _districtQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _districtQuery = GetEntityQuery(
            ComponentType.ReadOnly<District>(),
            ComponentType.ReadOnly<DistrictModifier>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities =
            _districtQuery.ToEntityArray(Allocator.Temp);
        BufferLookup<DistrictModifier> modLookup =
            GetBufferLookup<DistrictModifier>(true);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity district = entities[i];
            DynamicBuffer<DistrictModifier> mods = modLookup[district];
            if (mods.Length > (int)DistrictModifierType.GarbageProduction)
            {
                DistrictModifier garbageMod = mods[(int)DistrictModifierType.GarbageProduction];
                float effectiveMultiplier = (1f + garbageMod.m_Delta.y);
                if (effectiveMultiplier != 1f)
                {
                    Log.Info($"District {district}: garbage multiplier = {effectiveMultiplier:F2} (absolute offset = {garbageMod.m_Delta.x:F2})");
                }
            }
        }
        entities.Dispose();
    }
}
```

### Example 5: List All Active Policies on a District

Enumerate the Policy buffer to see which policies are active and their slider values.

```csharp
public void ListDistrictPolicies(EntityManager em, PrefabSystem prefabSystem, Entity districtEntity)
{
    if (!em.HasBuffer<Policy>(districtEntity))
    {
        Log.Info("Entity has no Policy buffer.");
        return;
    }

    DynamicBuffer<Policy> policies = em.GetBuffer<Policy>(districtEntity);
    Log.Info($"District {districtEntity} has {policies.Length} policies:");

    for (int i = 0; i < policies.Length; i++)
    {
        Policy policy = policies[i];
        bool active = (policy.m_Flags & PolicyFlags.Active) != 0;
        string name = prefabSystem.GetPrefab<PolicyPrefab>(policy.m_Policy)?.name ?? "Unknown";
        bool isSlider = em.HasComponent<PolicySliderData>(policy.m_Policy);

        if (isSlider)
        {
            PolicySliderData sliderData = em.GetComponentData<PolicySliderData>(policy.m_Policy);
            Log.Info($"  [{i}] {name}: active={active}, value={policy.m_Adjustment} (range {sliderData.m_Range.min}-{sliderData.m_Range.max})");
        }
        else
        {
            Log.Info($"  [{i}] {name}: active={active}");
        }
    }
}
```

## Open Questions

- [ ] How are default policies assigned to newly painted districts? The DefaultPolicyData buffer exists on some prefab but the exact initialization path was not fully traced.
- [ ] CityModifierUpdateSystem handles city-wide modifiers with an effect provider pattern that aggregates from buildings -- the full aggregation logic is complex and not documented here.
- [ ] The DistrictModifier buffer is indexed by DistrictModifierType enum value. If a mod adds new enum values beyond the existing 13, the buffer must be extended accordingly and consumers must know about the new indices.

## Sources

- Decompiled from: Game.dll -- Game.Areas.CurrentDistrictSystem, Game.Areas.ServiceDistrictSystem, Game.Areas.District, Game.Areas.DistrictModifier, Game.Areas.CurrentDistrict, Game.Areas.BorderDistrict, Game.Areas.ServiceDistrict, Game.Areas.DistrictModifierType, Game.Areas.DistrictOption
- Policy types: Game.Policies.Policy, Game.Policies.PolicyFlags, Game.Policies.Modify, Game.Policies.ModifiedSystem, Game.Policies.ModifiedSystem.PolicyEventInfo, Game.Policies.ModifiedSystem.PolicyRange, Game.Policies.DistrictModifierInitializeSystem
- Prefab types: Game.Prefabs.DistrictPrefab, Game.Prefabs.PolicyPrefab, Game.Prefabs.PolicySliderPrefab, Game.Prefabs.PolicyTogglePrefab, Game.Prefabs.PolicyData, Game.Prefabs.PolicySliderData, Game.Prefabs.DistrictOptionData, Game.Prefabs.DistrictModifierData, Game.Prefabs.DistrictModifiers, Game.Prefabs.DistrictOptions, Game.Prefabs.DefaultPolicyData
- UI systems: Game.UI.InGame.PoliciesUISystem
- Pathfinding integration: Game.Pathfind.LanePoliciesSystem
