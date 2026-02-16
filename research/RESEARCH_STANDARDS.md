# CS2 Game Research Standards

Standards Claude follows when investigating any game system before writing mod code.

## Why Research First

CS2 has no official API docs for internals. Coding against wrong assumptions means painful in-game debugging cycles. Every mod task starts with research — decompile, trace, understand, then code.

## Tools

- **ilspycmd** — CLI decompiler for .NET assemblies
  - Location: `~/.dotnet/tools/ilspycmd`
  - Requires: .NET 8.0 runtime
- **Game DLLs** — `/Volumes/steamapps/common/Cities Skylines II/Cities2_Data/Managed/`
- **Local lib copies** — `lib/` (key DLLs copied locally for reference)
- **Key assemblies**:
  - `Game.dll` — core game systems, ECS components, simulation logic
  - `Colossal.Core.dll` — engine-level utilities, base classes
  - `Colossal.UI.Binding.dll` — UI data binding framework
  - `Colossal.IO.AssetDatabase.dll` — asset loading
  - `Colossal.Mathematics.dll` — math utilities
  - `Colossal.Collections.dll` — custom collection types
  - `Colossal.Localization.dll` — localization system

## Research Phases

Every research task follows these phases in order. Do not skip phases.

### Phase 1: Scope Definition

Before touching any DLL, write down:
- What game behavior are we investigating?
- What would a mod need to change or read?
- What are our initial guesses about which systems are involved?

**Self-prompt**: *"What exactly do I need to understand to build this mod? What's the minimum scope?"*

### Phase 2: Namespace Discovery

List namespaces in relevant assemblies to find where the game organizes this feature.

```bash
ilspycmd -l <assembly.dll>
```

This lists all types. Filter for relevant keywords. Save the filtered list.

**Self-prompt**: *"Which namespaces contain types related to [topic]? Are there any I didn't expect?"*

### Phase 3: Component Identification (ECS)

CS2 uses Unity ECS. Find the **components** (data containers) related to the topic.

- Search for `struct` types implementing `IComponentData`, `IBufferElementData`, `ICleanupComponentData`
- Components are the data — they tell you what the game tracks
- Document every field and its type

**Self-prompt**: *"Have I found ALL components related to this topic? Check adjacent namespaces."*

### Phase 4: System Tracing

Find the **systems** (logic) that read/write those components.

- Search for classes extending `GameSystemBase`, `SystemBase`, or similar
- Look at `OnCreate`, `OnUpdate`, `OnDestroy` methods
- Identify `EntityQuery` definitions — these show which components each system operates on
- Trace the full update loop: which systems run, in what order

**Self-prompt**: *"For each component I found, which systems read it? Which systems write it? Is the data flow complete?"*

### Phase 5: Prefab & Configuration Tracing

Many game values come from prefabs, not runtime components.

- Search for types extending `PrefabBase`, `ComponentBase`
- Look for `Initialize` methods that set default values
- Check `PrefabSystem` usage for how prefabs are loaded

**Self-prompt**: *"Where do the initial values come from? Are they hardcoded, from prefabs, or from config files?"*

### Phase 6: Patch Point Identification

Identify methods suitable for Harmony patching.

- Look for `public` or `protected` methods with clear single responsibilities
- Prefer methods that are called per-frame or per-tick (high leverage)
- Note method signatures exactly (return type, parameters, generic constraints)
- Flag methods that are too complex or coupled for safe patching

**Self-prompt**: *"If I patch this method, what side effects could occur? Is there a safer patch point?"*

### Phase 7: Synthesis & Documentation

Write up findings using `TEMPLATE.md`. This is the deliverable.

**Self-prompt**: *"Could another developer read this and start coding immediately? What's missing?"*

## Decompilation Workflow

### Decompile a full assembly to a folder

```bash
ilspycmd -p -o research/decompiled/<AssemblyName>/ "<path-to-dll>"
```

- `-p` — decompile as project (preserves file structure)
- `-o` — output directory
- Creates one `.cs` file per type, organized by namespace

### Decompile a single type

```bash
ilspycmd -t <FullTypeName> "<path-to-dll>"
```

Outputs to stdout. Use this for quick lookups.

### List types in an assembly

```bash
ilspycmd -l c "<path-to-dll>"    # classes
ilspycmd -l s "<path-to-dll>"    # structs (ECS components are structs)
ilspycmd -l i "<path-to-dll>"    # interfaces
ilspycmd -l e "<path-to-dll>"    # enums
```

The `-l` flag requires an entity type filter: `c`(lass), `s`(truct), `i`(nterface), `d`(elegate), `e`(num).

### After decompilation

1. Create an index file at `research/decompiled/<AssemblyName>/INDEX.md` listing:
   - Assembly name and version
   - Date decompiled
   - Key namespaces discovered
   - Notable types for the current research topic

2. Do NOT commit decompiled source to git (add to `.gitignore`)

## Output Format

All research findings go in `research/topics/<TopicName>/` using the template.

### Naming conventions

- Use the game's internal naming for folders: `CitizenHealth`, `ZoneSystem`, `TransportLine` — match the namespace or system name from the game code
- Lowercase with hyphens only if there's no clear game name

### File structure per topic

```
research/topics/<TopicName>/
├── README.md          # Filled-in TEMPLATE.md (includes Examples section)
├── components.md      # Detailed component documentation (if large)
├── systems.md         # Detailed system documentation (if large)
└── snippets/          # Key code snippets from decompilation
```

### GitHub Pages (required)

Each research topic must also have an HTML page in `site/`:

```
site/<topic-slug>.html   # e.g. site/fire-ignition.html
```

The HTML page must:
- Link to `style.css` (shared stylesheet)
- Include nav bar: `Home` link → `index.html`, separator, page name
- Be human-readable — guide the reader to understanding, not just dump data
- Use `<div class="diagram"><pre>` for ASCII art diagrams
- Use `<pre><code>` for code examples
- HTML-escape `<` and `>` in code blocks
- No emojis
- Structure: Scope → How It Works → Key Components → Data Flow → Examples → Configuration → Open Questions → Sources

After creating the HTML page, add a card for it in `site/index.html`.

## Completeness Criteria

Research is **done** when:

1. All relevant ECS components are documented with every field
2. All systems that read/write those components are identified
3. The data flow is traceable: input → processing → output
4. Prefab/configuration sources for initial values are found
5. At least one viable Harmony patch point is identified (if patching is needed)
6. Open questions are explicitly listed (not hidden)
7. Another developer could start coding from the research alone
8. **Examples section** with working C# code snippets demonstrating practical usage (3-5 examples per topic)
9. **GitHub Pages HTML page** created in `site/` — human-readable, guides the reader to understanding

Research is **NOT done** if:

- You found systems but didn't check what components they query
- You found components but didn't trace which systems use them
- You skipped prefab tracing
- You have unnamed "TODO: investigate" items
- There are no working code examples in the README
- There is no HTML page in `site/` for the topic
