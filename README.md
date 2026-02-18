# Cities: Skylines II Modding Reference

A comprehensive, decompilation-backed reference for modding Cities: Skylines II. Every system, component, and data flow has been traced through the game's actual code — not guessed from observation.

**[Browse the site](https://skywolf.net/cs_modding_claude/site/)**

## What's Inside

### Research (51 topics)

Each topic follows a rigorous 7-phase research process: decompile the game DLL, trace every ECS component field, map system update logic, diagram data flows, identify Harmony patch points, and provide working C# code examples.

| Category | Topics |
|----------|--------|
| **City Services** | Police Dispatch, Fire Ignition, Healthcare, Education, Deathcare, Garbage Collection, Emergency Dispatch, Parks & Recreation |
| **Economy & Population** | Economy & Budget, Demand Systems, Citizens & Households, Company Simulation, Resource Production, Workplace & Labor, Land Value & Property, Trade & Connections, Tourism Economy |
| **Infrastructure** | Water & Sewage Pipes, Water System, Electricity Grid, Road Network, Pathfinding, Traffic Flow & Lane Control, Public Transit, Pollution, Cargo Transport |
| **Events & Simulation** | Crime Trigger, Disaster Simulation, Vehicle Out of Control, Citizen Sickness, Weather & Climate, Chirper System, Vehicle Spawning |
| **Governance** | Districts & Policies, Zoning, Map Tile Purchase, Terrain & Resources |
| **Mod Framework** | Mod Loading & Dependencies, Mod Options UI, Mod UI Buttons, Localization, Harmony Transpilers, Save/Load Persistence, Tool Activation, Tool Raycast, Object Tool System, Prefab System, Mod Hotkey Input, Input Action Lifecycle, Info Views & Overlays, Event Entity Archetype |

### Site (52 HTML pages)

A static GitHub Pages site with searchable, interlinked documentation. Each page includes:

- **Scope box** — what the topic covers at a glance
- **Component map** — every ECS component with every field documented
- **System map** — which systems read/write those components, their update rates, and job logic
- **Data flow diagram** — ASCII diagrams tracing data end-to-end
- **Code examples** — 3-5 working C# snippets per topic, copy-paste ready
- **Harmony patch points** — exact method signatures for patching
- **Configuration table** — prefab parameters with defaults
- **Open questions** — what's still unknown

### Decompiled Snippets

Over 200 decompiled source files from `Game.dll` stored alongside each topic in `research/topics/{Topic}/snippets/`. These are the primary sources — every claim in the documentation traces back to decompiled code.

## How It Was Generated

This entire repository was created by [Claude Code](https://claude.com/claude-code) (Anthropic's CLI agent) working autonomously:

1. **Decompilation** — Claude used `ilspycmd` to decompile types from the game's `Game.dll` assembly, saving raw C# output as snippet files
2. **Analysis** — Each decompiled system was traced to understand data flows, component relationships, and update pipelines
3. **Documentation** — READMEs were written following a standardized template (`research/TEMPLATE.md`) with quality criteria from `research/RESEARCH_STANDARDS.md`
4. **HTML generation** — Each README was rendered into a styled HTML page with consistent navigation
5. **Issue tracking** — Work was organized via GitHub Issues (300+ issues created, triaged, and closed)
6. **Community mod analysis** — Popular open-source mods were analyzed to validate findings and discover undocumented patterns

The human's role was directing what to research and reviewing output. All decompilation, analysis, writing, and HTML generation was performed by Claude.

## Project Structure

```
cs_modding_claude/
├── research/
│   ├── RESEARCH_STANDARDS.md    # 7-phase research methodology
│   ├── TEMPLATE.md              # README template for new topics
│   ├── popular_mods.csv         # Survey of analyzed community mods
│   └── topics/                  # 51 research topics
│       └── {TopicName}/
│           ├── README.md        # Full research documentation
│           └── snippets/        # Decompiled source files
├── site/
│   ├── index.html               # Landing page with topic cards
│   └── {topic-name}.html        # 52 documentation pages
├── docs/
│   ├── coding-standards.md      # C# conventions for CS2 mods
│   ├── project-setup.md         # Mod project setup guide
│   ├── community-tools.md       # Curated tools and resources
│   └── workflow.md              # GitHub Issues workflow
└── CLAUDE.md                    # Instructions for Claude Code
```

## Tech Stack

- **Game**: Cities: Skylines II (Unity 2022.3.7f1, .NET Standard 2.1)
- **Decompiler**: [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`)
- **Patching**: [Harmony](https://github.com/pardeike/Harmony) 2.x
- **ECS**: Unity DOTS-inspired custom ECS (`GameSystemBase`, `IComponentData`, `IBufferElementData`)
- **UI**: Cohtml (HTML/CSS/JS rendering in Unity) with React-based game UI

## Contributing

This is primarily a Claude Code-generated reference. If you spot inaccuracies or want to suggest new topics, open an issue.

## License

MIT
