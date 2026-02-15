# Coding Standards

## Naming

Follow [Microsoft C# conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

| Element | Style | Example |
|---------|-------|---------|
| Classes, structs, enums | PascalCase | `TrafficManager` |
| Methods, properties | PascalCase | `GetVehicleCount()` |
| Local variables, parameters | camelCase | `vehicleCount` |
| Private fields | _camelCase | `_vehicleCount` |
| Constants | PascalCase | `MaxVehicles` |
| Interfaces | IPascalCase | `ITrafficHandler` |

## File Organization

- **One ECS system per file** — `TrafficMonitorSystem` goes in `TrafficMonitorSystem.cs`
- **One Harmony patch class per file** — `ZoningPatch` goes in `ZoningPatch.cs`
- **Soft limit of ~300 lines per file** — if a class is growing, split it
- Group `using` directives: System → Unity → Game → Mod namespaces

## XML Documentation

Document all members with `///` comments:

```csharp
/// <summary>
/// Monitors traffic flow and adjusts signal timing.
/// </summary>
/// <remarks>
/// Runs every simulation tick. Reads from TrafficFlowData
/// and writes to SignalTimingData components.
/// </remarks>
public partial class TrafficMonitorSystem : GameSystemBase
```

## Error Handling

Wrap risky operations in try/catch and log exceptions:

```csharp
try
{
    ProcessVehicles(query);
}
catch (Exception ex)
{
    Mod.Log.Error(ex, "Failed to process vehicles");
}
```

Where to be defensive:
- System `OnUpdate` methods
- Harmony patch methods (prefix/postfix)
- Settings load/save
- Any interaction with game data that could be null or in unexpected state

## ECS System Pattern

```csharp
/// <summary>
/// Brief description of what this system does.
/// </summary>
public partial class MySystem : GameSystemBase
{
    private EntityQuery _myQuery;

    /// <inheritdoc/>
    protected override void OnCreate()
    {
        base.OnCreate();
        _myQuery = GetEntityQuery(ComponentType.ReadOnly<MyComponent>());
        RequireForUpdate(_myQuery);
    }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        // System logic here
    }
}
```

Register systems in your `Mod.OnLoad`:

```csharp
updateSystem.UpdateAt<MySystem>(SystemUpdatePhase.GameSimulation);
```

## Harmony Patch Pattern

```csharp
/// <summary>
/// Patches ZoneSystem to modify zone behavior.
/// </summary>
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SomeMethod))]
public static class ZoneSystemPatch
{
    /// <summary>
    /// Runs before the original method.
    /// </summary>
    public static bool Prefix(/* params */)
    {
        // Return false to skip original, true to continue
        return true;
    }
}
```
