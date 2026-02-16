# Research: [Topic Name]

> **Status**: Draft | In Progress | Complete
> **Date started**: YYYY-MM-DD
> **Last updated**: YYYY-MM-DD

## Scope

**What we're investigating**: [One sentence describing the game behavior]

**Why**: [What mod needs this research — what will we build or change?]

**Boundaries**: [What's explicitly out of scope]

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.X.Y  | Description  |

## Component Map

ECS components involved in this system. Document every field.

### `ComponentName` (Game.X.Y)

| Field | Type | Description |
|-------|------|-------------|
| m_Field | float | What it represents |

*Source: `Game.dll` → `Game.X.Y.ComponentName`*

<!-- Repeat for each component -->

## System Map

Systems that read/write the components above.

### `SystemName` (Game.X.Y)

- **Base class**: GameSystemBase
- **Update phase**: Simulation / UI / Rendering
- **Queries**:
  - EntityQuery requiring: [ComponentA, ComponentB]
- **Reads**: ComponentA.m_Field
- **Writes**: ComponentB.m_Value
- **Key methods**:
  - `OnUpdate()` — [what it does]
  - `MethodName()` — [what it does]

<!-- Repeat for each system -->

## Data Flow

Text-based diagram showing how data moves through the system.

```
[Source/Input]
    │
    ▼
[System A] reads Component X, writes Component Y
    │
    ▼
[System B] reads Component Y, writes Component Z
    │
    ▼
[Output/Effect in game]
```

## Prefab & Configuration

Where initial/default values come from.

| Value | Source | Location |
|-------|--------|----------|
| Max health | PrefabName.m_MaxHealth | Game.Prefabs.X |

## Harmony Patch Points

Methods suitable for modding via Harmony patches.

### Candidate 1: `Namespace.Class.Method`

- **Signature**: `ReturnType Method(ParamType param)`
- **Patch type**: Prefix / Postfix / Transpiler
- **What it enables**: [What a mod could do by patching this]
- **Risk level**: Low / Medium / High
- **Side effects**: [Known risks of patching here]

<!-- Repeat for each candidate -->

## Mod Blueprint

How a mod would use these findings.

- **Systems to create**: [Custom systems extending GameSystemBase]
- **Components to add**: [New ECS components if needed]
- **Patches needed**: [Harmony patches from the list above]
- **Settings**: [User-configurable values]
- **UI changes**: [If applicable]

## Examples

Working C# code examples demonstrating practical usage. Each example should be a complete, self-contained snippet a modder can adapt.

### Example 1: [Short Description]

[Brief explanation of what this example demonstrates and when you'd use it.]

```csharp
// Working code here
```

### Example 2: [Short Description]

[Brief explanation.]

```csharp
// Working code here
```

<!-- Include 3-5 examples covering common use cases -->

## Open Questions

- [ ] Question that still needs answering
- [ ] Another unknown

## Sources

- Decompiled from: [Assembly name, version if known]
- Game version tested: [CS2 version]
