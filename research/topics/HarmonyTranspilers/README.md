# Research: Harmony Transpiler Patterns

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: Advanced Harmony patching beyond prefix/postfix -- IL-level code rewriting using transpilers, CodeMatcher, and CodeInstruction. How to safely modify game method bodies at the IL level for CS2 modding.

**Why**: Many mod scenarios require changing logic *inside* a method rather than just intercepting entry/exit. Transpilers allow inserting, removing, or replacing IL instructions within the original method body, enabling modifications that prefix/postfix cannot achieve.

**Boundaries**: This covers Harmony 2.x transpiler APIs only. Prefix/postfix basics, reverse patches, and finalizers are out of scope. CS2-specific Burst compilation limitations are noted but the focus is on the Harmony API itself.

## Relevant Libraries

| Library | Namespace | What's there |
|---------|-----------|-------------|
| 0Harmony.dll | HarmonyLib | Harmony, HarmonyPatch, CodeInstruction, CodeMatcher, CodeMatch, AccessTools, Transpiler attribute |
| System.Reflection.Emit | System.Reflection.Emit | OpCodes, ILGenerator, Label, LocalBuilder |

## Key Concepts

### What is a Transpiler?

A transpiler is a method that receives the IL instructions of an original method as `IEnumerable<CodeInstruction>` and returns a modified `IEnumerable<CodeInstruction>`. Unlike prefix/postfix patches which run at method boundaries, transpilers rewrite the method body itself at the IL level.

Transpilers run **once** at patch time (not per call), making them zero-overhead at runtime. However, they are significantly harder to write, debug, and maintain.

### Transpiler Signature

```csharp
[HarmonyTranspiler]
static IEnumerable<CodeInstruction> Transpiler(
    IEnumerable<CodeInstruction> instructions,  // Required: original IL
    ILGenerator generator,                       // Optional: for labels/locals
    MethodBase original                          // Optional: original MethodInfo
)
```

All three parameters are injected by Harmony. Only `instructions` is required.

### CodeInstruction

Represents a single IL instruction with:
- `opcode` -- the IL operation (from `System.Reflection.Emit.OpCodes`)
- `operand` -- the argument (MethodInfo, FieldInfo, Label, int, string, etc.)
- `labels` -- list of `Label` objects (branch targets pointing here)
- `blocks` -- exception block boundaries

**Factory methods** (cleaner than manual construction):

| Method | IL Opcode(s) | Purpose |
|--------|-------------|---------|
| `CodeInstruction.Call(type, name, params, generics)` | `Call` | Call a method |
| `CodeInstruction.LoadField(type, name, useAddress)` | `Ldfld`/`Ldsfld` | Load field value |
| `CodeInstruction.StoreField(type, name)` | `Stfld`/`Stsfld` | Store field value |
| `CodeInstruction.LoadLocal(index, useAddress)` | `Ldloc`/`Ldloc_N` | Load local variable |
| `CodeInstruction.StoreLocal(index)` | `Stloc`/`Stloc_N` | Store local variable |
| `CodeInstruction.LoadArgument(index, useAddress)` | `Ldarg`/`Ldarg_N` | Load method argument |
| `CodeInstruction.StoreArgument(index)` | `Starg` | Store method argument |

### CodeMatcher

A cursor-based API for navigating and modifying IL instruction sequences. Far more maintainable than manual index-based iteration.

**Key methods**:

| Method | Description |
|--------|-------------|
| `MatchStartForward(params CodeMatch[])` | Find pattern forward, position at start of match |
| `MatchEndForward(params CodeMatch[])` | Find pattern forward, position at end of match |
| `MatchStartBackward(params CodeMatch[])` | Find pattern backward, position at start |
| `Insert(params CodeInstruction[])` | Insert before current position |
| `InsertAndAdvance(params CodeInstruction[])` | Insert and move cursor past inserted code |
| `RemoveInstruction()` | Remove instruction at current position |
| `RemoveInstructions(int)` | Remove N instructions from current position |
| `SetInstruction(CodeInstruction)` | Replace instruction at current position |
| `SetOperandAndAdvance(object)` | Change operand of current instruction |
| `Advance(int)` | Move cursor by offset |
| `Repeat(Action<CodeMatcher>)` | Repeat match for all occurrences |
| `ThrowIfInvalid(string)` | Throw if last match failed |
| `ThrowIfNotMatch(string)` | Throw with message if no match |
| `IsValid` | True if last match succeeded |
| `Instructions()` | Return the modified instruction list |

### CodeMatch

Pattern matching for IL instructions used with CodeMatcher.

```csharp
// Match by opcode
new CodeMatch(OpCodes.Ldarg_0)

// Match by opcode and operand
new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Foo), "Bar"))

// Match by opcode with predicate
new CodeMatch(OpCodes.Ldfld, null, "myFieldName")

// Match using expression (type-safe)
CodeMatch.Calls(() => default(DamageHandler).Kill(default))

// Match any instruction (wildcard)
new CodeMatch()
```

### Labels and Branching

Labels are Harmony's abstraction for jump targets. When the game's IL has branch instructions (`Br`, `Brfalse`, `Brtrue`, etc.), they use `Label` objects as operands.

**Creating new branches**:
1. Get `ILGenerator` from transpiler parameters
2. Call `generator.DefineLabel()` to create a new `Label`
3. Add the label to the target instruction's `labels` list
4. Use the label as the operand for a branch instruction

**Critical rule**: When removing instructions that have labels, always move those labels to another instruction. Orphaned labels cause `InvalidProgramException`.

### Local Variables

To create new local variables in a transpiler:
1. Call `generator.DeclareLocal(typeof(T))` to get a `LocalBuilder`
2. Use `Stloc` to store and `Ldloc` to load via the `LocalBuilder`

**Warning**: Never use `ILGenerator.Emit()` -- Harmony manages emission internally. Only use ILGenerator for `DefineLabel()` and `DeclareLocal()`.

## CS2-Specific Considerations

### Burst Compilation Limitation

CS2 uses Unity's Burst compiler to compile performance-critical ECS jobs (IJob, IJobChunk, etc.) into native code. **Harmony cannot patch Burst-compiled methods** because they are no longer managed .NET code.

What CAN be patched:
- Managed system classes (`GameSystemBase.OnUpdate`, `OnCreate`, etc.)
- UI systems (`UISystemBase` subclasses)
- Prefab initialization (`PrefabBase.Initialize`)
- Any non-Burst managed method

What CANNOT be patched:
- Burst-compiled job `Execute()` methods
- Any method marked with `[BurstCompile]`
- Native code generated by Burst

### Practical Implication

For CS2 modding, transpilers are most useful for:
- Modifying managed system logic (OnUpdate, OnCreate)
- Changing how prefab data is initialized
- Altering UI binding behavior
- Intercepting tool system methods

For Burst-compiled simulation logic, use the ECS approach instead: create custom systems that run before/after the target system and modify components directly.

## Pattern Catalog

### Pattern 1: Replace a Method Call

Find a specific method call and replace it with your own.

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
static class ReplaceCallPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(OriginalClass), "OriginalMethod"))
        )
        .ThrowIfInvalid("Could not find OriginalClass.OriginalMethod call")
        .SetInstruction(new CodeInstruction(OpCodes.Call,
            AccessTools.Method(typeof(ReplaceCallPatch), nameof(MyReplacement))));

        return matcher.Instructions();
    }

    static ReturnType MyReplacement(/* same parameters as original */)
    {
        // Custom logic here
    }
}
```

### Pattern 2: Insert Code Before/After a Specific Point

Inject a static method call at a precise location.

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
static class InsertCodePatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(SomeType), "SomeMethod"))
        )
        .ThrowIfInvalid("Could not find SomeType.SomeMethod")
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),  // load 'this'
            CodeInstruction.Call(typeof(InsertCodePatch), nameof(BeforeCallback))
        );

        return matcher.Instructions();
    }

    static void BeforeCallback(TargetClass instance)
    {
        // Runs just before SomeMethod is called
    }
}
```

### Pattern 3: Conditional Skip (Add an If-Check)

Wrap existing logic in a conditional branch so it can be skipped.

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
static class ConditionalSkipPatch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var skipLabel = generator.DefineLabel();
        var matcher = new CodeMatcher(instructions);

        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(TargetClass), "ExpensiveOperation"))
        )
        .ThrowIfInvalid("Could not find ExpensiveOperation call")
        .InsertAndAdvance(
            CodeInstruction.Call(typeof(ConditionalSkipPatch), nameof(ShouldSkip)),
            new CodeInstruction(OpCodes.Brtrue, skipLabel)
        );

        // Find the instruction after ExpensiveOperation and add the skip label
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(TargetClass), "ExpensiveOperation"))
        )
        .Advance(1)
        .AddLabels(new[] { skipLabel });

        return matcher.Instructions();
    }

    static bool ShouldSkip()
    {
        return MyModSettings.SkipExpensiveOperation;
    }
}
```

### Pattern 4: Replace a Constant Value

Change a hardcoded numeric constant in the original method.

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
static class ReplaceConstantPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // Find: ldc.i4 128 (e.g., a hardcoded update interval)
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldc_I4, 128)
        )
        .ThrowIfInvalid("Could not find constant 128")
        .SetOperandAndAdvance(64);  // Change to 64

        return matcher.Instructions();
    }
}
```

### Pattern 5: Repeat Replacement for All Occurrences

Replace every occurrence of a pattern, not just the first.

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
static class RepeatReplacePatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(OldHelper), "OldMethod"))
        )
        .Repeat(cm =>
        {
            cm.SetInstruction(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(RepeatReplacePatch), nameof(NewMethod))));
        });

        return matcher.Instructions();
    }

    static void NewMethod(/* same signature */) { /* ... */ }
}
```

### Pattern 6: Surgical Job Removal from System OnUpdate

Remove individual jobs from a system's `OnUpdate` while keeping the rest intact. The mod then runs replacement jobs in a custom system. This keeps the vanilla system's data pipeline (queues, queries) operational.

```csharp
[HarmonyPatch(typeof(BuildingUpkeepSystem), "OnUpdate")]
static class RemoveJobsPatch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        // 1. Identify job types by their nested class names
        Type levelUpJobType = Type.GetType(
            "Game.Simulation.BuildingUpkeepSystem+LevelupJob,Game", true);
        Type levelDownJobType = Type.GetType(
            "Game.Simulation.BuildingUpkeepSystem+LeveldownJob,Game", true);
        MethodInfo dependencySetter = AccessTools.PropertySetter(
            typeof(SystemBase), "Dependency");

        // 2. Find local variable indices from MethodBody
        int levelUpIndex = -1, levelDownIndex = -1;
        foreach (var info in original.GetMethodBody().LocalVariables)
        {
            if (info.LocalType == levelUpJobType) levelUpIndex = info.LocalIndex;
            if (info.LocalType == levelDownJobType) levelDownIndex = info.LocalIndex;
        }

        // 3. Use CodeMatcher to find and NOP out the job scheduling blocks
        // Pattern: ldloca.s <jobIndex> ... call Dependency.set (stloc for result)
        // Replace the entire sequence with NOPs
        var matcher = new CodeMatcher(instructions);
        // ... match and remove job scheduling instructions for each target job
        return matcher.Instructions();
    }
}
```

Key techniques:
1. Use `Type.GetType("Namespace.Outer+Inner,Assembly")` to resolve nested job types
2. Use `MethodBase.GetMethodBody().LocalVariables` to find job local variable indices
3. Use `CodeMatcher` to find and NOP the scheduling block (from `ldloca.s <index>` to `Dependency.set`)
4. Pair with the NativeQueue reflection pattern (Example 6) to consume the vanilla system's output queues

## Harmony Patch Points for CS2

### Suitable Targets (Managed Code)

| System | Method | Why Transpile? |
|--------|--------|---------------|
| ToolSystem | SetInfoview, UpdateInfoviewColors | Modify info view behavior mid-method |
| Various UI systems | OnUpdate, PerformUpdate | Change UI data computation logic |
| PrefabInitializeSystem | OnUpdate | Alter prefab initialization values |
| Any non-Burst GameSystemBase | OnCreate, OnUpdate | Modify system logic at specific points |

### Unsuitable Targets (Burst-Compiled)

| System | Method | Why NOT? |
|--------|--------|---------|
| WaterPipeFlowJob | Execute | Burst-compiled native code |
| Any IJob/IJobChunk | Execute | Burst-compiled |
| TrafficLaneSignalJob | Execute | Burst-compiled |

## Debugging Transpilers

### Enable Harmony Debug Logging

```csharp
// In your mod's OnLoad:
Harmony.DEBUG = true;
// Or use the attribute:
[HarmonyDebug]
[HarmonyPatch(typeof(Target), "Method")]
static class MyPatch { ... }
```

This outputs detailed IL instruction listings before and after patching to the BepInEx/Harmony log.

### Common Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `InvalidProgramException` | Orphaned labels (removed instruction had labels) | Move labels to adjacent instruction |
| `InvalidProgramException` | Duplicate labels (copied instruction kept labels) | Clear labels on copies |
| `NullReferenceException` in CodeMatcher | Missing ILGenerator parameter | Add `ILGenerator generator` to transpiler |
| Patch not applying | Target method is Burst-compiled | Cannot transpile; use ECS approach instead |
| `ThrowIfInvalid` throws | IL pattern changed between game versions | Update match pattern or use wider match |

### Inspecting IL Code

Use these tools to see IL instructions before writing transpilers:
- **dnSpy** -- GUI decompiler with IL view
- **ILSpy** -- GUI/CLI decompiler (ilspycmd for CLI)
- **Harmony.DEBUG = true** -- logs IL before/after patching
- **SharpLab** -- online C# to IL compiler for testing patterns

## Examples

### Example 1: Replace a Method Call with CodeMatcher

Replace the game's call to a damage calculation with a custom one.

```csharp
[HarmonyPatch(typeof(Game.Simulation.FireIgnitionSystem), "OnUpdate")]
public static class FireDamageTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        var originalMethod = AccessTools.Method(typeof(Game.Simulation.FireUtils), "CalculateDamage");
        var replacementMethod = AccessTools.Method(typeof(FireDamageTranspiler), nameof(CustomDamage));

        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call, originalMethod)
        )
        .ThrowIfInvalid("Could not find FireUtils.CalculateDamage call")
        .SetInstruction(new CodeInstruction(OpCodes.Call, replacementMethod));

        return matcher.Instructions();
    }

    public static float CustomDamage(/* same params as original */)
    {
        // Reduce all fire damage by 50%
        return originalResult * 0.5f;
    }
}
```

### Example 2: Inject a Callback Using ILGenerator

Insert a debug logging call before a specific operation, preserving the stack.

```csharp
[HarmonyPatch(typeof(Game.Tools.ToolSystem), "SetInfoview")]
public static class InfoviewDebugTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Find the first stfld to m_CurrentInfoview
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Stfld &&
                codes[i].operand is FieldInfo field &&
                field.Name == "m_CurrentInfoview")
            {
                // Insert before the store: dup the value and log it
                codes.Insert(i, new CodeInstruction(OpCodes.Dup));
                codes.Insert(i + 1, CodeInstruction.Call(
                    typeof(InfoviewDebugTranspiler), nameof(LogInfoview)));
                break;
            }
        }
        return codes;
    }

    public static void LogInfoview(object prefab)
    {
        Mod.Log.Info($"Infoview changing to: {prefab}");
    }
}
```

### Example 3: Add a Conditional Branch

Skip an entire code block based on a mod setting.

```csharp
[HarmonyPatch(typeof(Game.Simulation.SomeSystem), "OnUpdate")]
public static class ConditionalSkipTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var skipLabel = generator.DefineLabel();
        var matcher = new CodeMatcher(instructions, generator);

        // Find the start of the block we want to skip
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.Method(
                typeof(Game.Simulation.SomeSystem), "ProcessExpensiveLogic"))
        )
        .ThrowIfInvalid("Could not find ProcessExpensiveLogic")
        .InsertAndAdvance(
            // Call our check method
            CodeInstruction.Call(typeof(ConditionalSkipTranspiler), nameof(ShouldSkip)),
            // If true, jump past the block
            new CodeInstruction(OpCodes.Brtrue, skipLabel)
        );

        // Find the instruction after ProcessExpensiveLogic and mark it
        matcher.MatchEndForward(
            new CodeMatch(OpCodes.Call, AccessTools.Method(
                typeof(Game.Simulation.SomeSystem), "ProcessExpensiveLogic"))
        )
        .Advance(1);
        matcher.Instruction.labels.Add(skipLabel);

        return matcher.Instructions();
    }

    public static bool ShouldSkip()
    {
        return ModSettings.Instance.DisableExpensiveLogic;
    }
}
```

### Example 4: Replace All Occurrences with Repeat

Replace every call to a utility method throughout a target method.

```csharp
[HarmonyPatch(typeof(Game.UI.InGame.SomeUISystem), "PerformUpdate")]
public static class ReplaceAllCallsTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = AccessTools.Method(typeof(Game.UI.InGame.InfoviewsUIUtils), "GetDefaultLabel");
        var replacement = AccessTools.Method(typeof(ReplaceAllCallsTranspiler), nameof(GetCustomLabel));

        var matcher = new CodeMatcher(instructions);
        matcher.MatchStartForward(new CodeMatch(OpCodes.Call, original))
            .Repeat(cm =>
            {
                cm.SetInstruction(new CodeInstruction(OpCodes.Call, replacement));
            });

        return matcher.Instructions();
    }

    public static string GetCustomLabel(/* same params */)
    {
        return "Custom Label";
    }
}
```

### Example 5: Manual Iteration with Label Preservation

When CodeMatcher is not flexible enough, iterate manually while preserving labels.

```csharp
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
public static class ManualTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool patched = false;

        for (int i = 0; i < codes.Count - 1; i++)
        {
            // Find: ldc.r4 100f followed by mul
            if (codes[i].opcode == OpCodes.Ldc_R4 &&
                codes[i].operand is float f && Math.Abs(f - 100f) < 0.01f &&
                codes[i + 1].opcode == OpCodes.Mul)
            {
                // Replace the constant, preserving labels
                var replacement = new CodeInstruction(OpCodes.Ldc_R4, 200f);
                replacement.labels = codes[i].labels;  // Move labels
                replacement.blocks = codes[i].blocks;   // Move exception blocks
                codes[i] = replacement;
                patched = true;
                break;
            }
        }

        if (!patched)
            Mod.Log.Warn("ManualTranspiler: Could not find target pattern");

        return codes;
    }
}
```

### Example 6: NativeQueue Field Reflection for System-to-System Data Sharing

When a transpiler removes jobs from a vanilla system (e.g., removing `LevelupJob` from `BuildingUpkeepSystem`), the custom replacement system needs access to the private `NativeQueue` fields that feed those jobs. Use `AccessTools.Field` to reflect private fields from one system into another:

```csharp
protected override void OnCreate()
{
    base.OnCreate();

    // Reflect the level-up queue from BuildingUpkeepSystem
    FieldInfo levelupField = AccessTools.Field(
        typeof(BuildingUpkeepSystem), "m_LevelupQueue");

    if (levelupField is null)
    {
        Mod.Instance.Log.Error("Unable to get LevelupQueue FieldInfo");
        Enabled = false;  // Graceful degradation
        return;
    }

    _levelupQueue = (NativeQueue<Entity>)levelupField.GetValue(
        World.GetOrCreateSystemManaged<BuildingUpkeepSystem>());

    // Same for level-down queue
    FieldInfo leveldownField = AccessTools.Field(
        typeof(BuildingUpkeepSystem), "m_LeveldownQueue");
    _leveldownQueue = (NativeQueue<Entity>)leveldownField.GetValue(
        World.GetOrCreateSystemManaged<BuildingUpkeepSystem>());
}
```

Key techniques:
1. `AccessTools.Field` (from HarmonyLib) gets `FieldInfo` for private fields
2. `GetValue` with system instance from `World.GetOrCreateSystemManaged`
3. `NativeQueue` cast — the reflected value is cast directly to the native container type
4. **Graceful degradation**: If reflection fails (e.g., game update renames field), disable the system instead of crashing

This pattern is commonly paired with a transpiler that removes the original jobs, creating a "job replacement" architecture where the vanilla system still populates the queues but a custom system processes them.

## Open Questions

- [ ] **HarmonyX vs Harmony 2.x in CS2**: CS2 may use HarmonyX (BepInEx fork) rather than vanilla Harmony 2.x. The API is nearly identical but there may be minor differences in transpiler behavior.
- [ ] **Burst deoptimization**: Whether it is possible to disable Burst compilation for specific jobs at runtime to make them patchable (via `[BurstCompile(CompileSynchronously = false)]` or environment variables).
- [ ] **Multi-transpiler compatibility**: When multiple mods transpile the same method, Harmony chains them. However, each transpiler sees the output of the previous one, which can cause match failures if patterns shift. No standard solution exists beyond defensive matching.
- [ ] **IL verification**: There is no built-in way to validate transpiler output before it executes. Bad IL causes runtime crashes rather than compile-time errors.

## Sources

- Harmony official documentation: https://harmony.pardeike.net/articles/patching-transpiler.html
- Harmony CodeMatcher documentation: https://harmony.pardeike.net/articles/patching-transpiler-matcher.html
- Harmony CodeInstruction API: https://harmony.pardeike.net/api/HarmonyLib.CodeInstruction.html
- Harmony CodeMatcher API: https://harmony.pardeike.net/api/HarmonyLib.CodeMatcher.html
- Harmony AccessTools API: https://harmony.pardeike.net/api/HarmonyLib.AccessTools.html
- Simple Transpiler Tutorial by pardeike: https://gist.github.com/pardeike/c02e29f9e030e6a016422ca8a89eefc9
- Another Transpiler Tutorial by JavidPack: https://gist.github.com/JavidPack/454477b67db8b017cb101371a8c49a1c
- CS2 Code Modding Dev Diary: https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding
- Harmony GitHub: https://github.com/pardeike/Harmony
