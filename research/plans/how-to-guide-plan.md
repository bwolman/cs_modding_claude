# Plan: Transform CS2 Modding Research into a Practical How-To Guide

> **Date**: 2026-02-17
> **Status**: Proposed

---

## 1. Current State Assessment

### What the Site Does Well

The site is an exceptional deep-reference resource. Specific strengths:

- **Depth and accuracy**: 51 HTML pages covering virtually every game system, from fire ignition to cargo transport, each traced from decompiled source. Component fields are documented with types and descriptions. System update intervals, query patterns, and flag values are all present.
- **Working code examples**: Every page includes 3-7 C# code snippets that demonstrate practical ECS patterns (e.g., the police dispatch page has 7 progressively complex examples from "set a flag" to "complete dispatch-and-guard system").
- **Honest about pitfalls**: Pages like police-dispatch.html include detailed "why this approach fails" sections and warning boxes. The site does not just show the happy path -- it shows what breaks and why.
- **ASCII data flow diagrams**: Each page includes text-based pipeline diagrams that trace data from input through systems to output. These are genuinely useful for understanding multi-system interactions.
- **Consistent structure**: Every page follows the same Scope / How It Works / Key Components / Data Flow / Examples / Configuration / Open Questions / Sources pattern, making it predictable to navigate.
- **Cross-references**: Pages link to related topics (e.g., crime-trigger links to police-dispatch, emergency-dispatch links to individual service pages).

### What Is Missing for a How-To Guide

- **No "Getting Started" content**: A new modder landing on the site has no idea where to begin. There is no setup guide, no "your first mod" tutorial, no explanation of what tools they need or how to install them. The site assumes you already have a working mod project.
- **No progressive learning path**: All 51 pages are at the same depth level. There is no "start here, then read this, then try that" progression. A reader who wants to learn modding cannot distinguish between a foundational concept (mod loading, ECS basics) and an advanced topic (harmony transpilers, pathfinding cost models).
- **No concept explainers**: The site assumes familiarity with Unity ECS, Harmony, the cohtml UI system, and CS2's prefab model. None of these are explained from scratch. A modder coming from other games or from general C# development would be lost.
- **No task-oriented content**: Every page answers "how does game system X work internally?" but none answer "how do I accomplish task Y in my mod?" For example, there is no page for "how to add a custom toolbar button" that walks through the complete process -- the mod-ui-buttons page explains the binding system in depth but does not provide a start-to-finish walkthrough.
- **No troubleshooting content**: No page covers common errors, debugging techniques, log file locations, or how to diagnose why a mod is not loading.
- **No difficulty indicators**: A reader cannot tell that mod-loading-dependencies is foundational while harmony-transpilers is advanced. All pages appear equal in the sidebar.
- **Sidebar is overwhelming**: 51 entries in 8 categories, all visible at once. No visual hierarchy. No "start here" indicator. The sidebar is organized by game system domain (Events, Infrastructure, Economy) rather than by modding task or learning level.
- **Landing page is a wall of cards**: The index.html lists all 51 topics as cards with technical descriptions. There is no welcome message, no "if you are new start here" guidance, no explanation of what the site is or who it is for.
- **No build/deploy workflow**: The CLAUDE.md and docs/ folder contain project setup info, but none of this is exposed on the site. A visitor to the GitHub Pages site cannot find how to set up their development environment.

### Audience Gap

- **Current audience**: Experienced CS2 modders or Unity ECS developers who need deep reference material on specific game systems. They already know how to create a mod project, build, deploy, and test. They arrive via search for a specific system name.
- **Potential audience**: Anyone interested in CS2 modding, including those who have never modded a game before, developers familiar with C# but not Unity ECS, modders from CS1 transitioning to CS2, and experienced modders looking for cookbook solutions to common tasks.

---

## 2. Proposed New Content Types

### A. Getting Started Guide (new section)

A multi-page guide for new modders, covering:

1. **Prerequisites** -- what you need installed (.NET SDK, IDE, game copy, ilspycmd)
2. **Project setup** -- creating a .csproj, referencing game assemblies, setting CSII_TOOLPATH
3. **Your first mod** -- a minimal IMod implementation that logs a message
4. **Build, deploy, test cycle** -- building with dotnet, finding the Mods folder, checking logs
5. **Understanding the game architecture** -- brief ECS primer, what GameSystemBase is, how systems are registered

This content partially exists in `docs/project-setup.md` and `docs/coding-standards.md` but is not on the site.

### B. Tutorial Series (new section)

Progressive tutorials that build on each other. Each tutorial produces a working mod or mod feature:

1. "Your First ECS System" -- a system that counts entities and logs the count
2. "Reading Game Data" -- querying entities with EntityQuery, reading components
3. "Modifying Game Data" -- writing to components, creating entities
4. "Adding a Settings Panel" -- using ModSetting and the options UI
5. "Adding a Toolbar Button" -- TriggerBinding + ValueBinding to create a clickable UI element
6. "Patching Game Methods with Harmony" -- prefix, postfix, and when to use them
7. "Replacing a Vanilla System" -- disabling a system and registering a replacement
8. "Persisting Custom Data" -- ISerializable components that survive save/load
9. "Adding Custom Hotkeys" -- ProxyAction, input manager, key combos
10. "Building a Complete Mod" -- combining multiple techniques into a real mod

### C. Recipe / Cookbook Pages (new section)

Short, task-focused pages that answer "How do I...?" questions:

- "How do I find all entities of a specific type?"
- "How do I read a prefab's configuration data?"
- "How do I dispatch an emergency vehicle to a location?"
- "How do I add a custom overlay to an info view?"
- "How do I trigger a game event programmatically?"
- "How do I detect when the player enters/exits game mode?"
- "How do I schedule my system to run less frequently?"
- "How do I share data between two of my systems?"
- "How do I detect if another mod is installed?"
- "How do I add localized strings for my mod?"

These would be 1-2 page answers with copy-paste code. Many of these answers already exist buried inside the 51 reference pages but are hard to discover.

### D. Concept Explainers (new section)

Background articles for modders who need to understand a technology before they can use the reference pages:

- "Unity ECS for CS2 Modders" -- entities, components, systems, queries, archetypes
- "How CS2 Uses Harmony" -- what Harmony is, prefix/postfix/transpiler, when to use which
- "The cohtml UI Model" -- how C# bindings map to TypeScript/React, the coui:// protocol
- "CS2's Prefab System Explained" -- two-entity model, managed vs ECS, why prefabs matter
- "Understanding Game Update Phases" -- what each SystemUpdatePhase means, how to pick one
- "NativeContainers and Burst" -- why CS2 uses NativeArray/NativeList, Burst compilation, safety

### E. Troubleshooting Guide (new section)

- Common error messages and their fixes
- How to find and read game logs
- Debugging a mod that does not load (MissedDependenciesError, LoadAssemblyError)
- Debugging a system that does not run (RequireForUpdate, wrong phase, Enabled=false)
- Debugging Harmony patches that do not apply
- Performance issues: system running too frequently, entity query too broad

---

## 3. Site Structure Improvements

### Navigation Redesign

The current sidebar has one flat list of 51 pages organized by game domain. The proposed redesign adds a two-tier navigation with content-type sections above the reference material.

**Proposed sidebar structure:**

```
GETTING STARTED
  Setup Your Environment
  Your First Mod
  Build & Deploy
  Architecture Overview

TUTORIALS
  Your First System
  Reading Game Data
  Modifying Game Data
  Settings Panel
  Toolbar Button
  Harmony Patching
  System Replacement
  Data Persistence
  Custom Hotkeys
  Complete Mod Walkthrough

COOKBOOK
  Common Tasks (index)
  Entity Queries
  Emergency Dispatch
  Custom Overlays
  Game Events
  System Scheduling
  Cross-Mod Detection
  Localization

CONCEPTS
  Unity ECS Primer
  Harmony Overview
  cohtml UI Model
  Prefab System
  Update Phases
  NativeContainers & Burst

TROUBLESHOOTING
  Common Errors
  Debugging Guide

---  (visual separator)

REFERENCE: Events & Simulation
  (existing 8 pages)

REFERENCE: Infrastructure
  (existing 6 pages)

REFERENCE: Economy & Population
  (existing 10 pages)

REFERENCE: City Services
  (existing 7 pages)

REFERENCE: Governance
  (existing 2 pages)

REFERENCE: Environment
  (existing 3 pages)

REFERENCE: Vehicles & Transport
  (existing 1 page)

REFERENCE: Tools & Input
  (existing 5 pages)

REFERENCE: UI & Settings
  (existing 4 pages)

REFERENCE: Modding Framework
  (existing 5 pages)
```

The sidebar sections above the separator (Getting Started, Tutorials, Cookbook, Concepts, Troubleshooting) would be collapsible. The Reference sections would default to collapsed, expanding on click.

### Landing Page Redesign

Replace the current wall-of-cards index.html with a welcome page that serves different audiences:

```
CS2 Modding Guide & Reference

[Welcome paragraph: what this site is, who it's for]

NEW TO CS2 MODDING?
  Start here: [Setup Your Environment] -> [Your First Mod] -> [Build & Deploy]

LOOKING FOR HOW-TO ANSWERS?
  Browse the [Cookbook] for common tasks, or follow the [Tutorial Series]

NEED DEEP SYSTEM REFERENCE?
  Jump to [Reference Topics] organized by game domain

RECENT ADDITIONS
  [3-4 most recently updated pages]
```

Below the hero section, keep a condensed version of the reference cards but organize them as a secondary section.

### Cross-Linking Strategy

Every reference page should gain:
- A "Prerequisites" note at the top linking to relevant concept explainers
- A "Quick Start" box at the top with links to related cookbook/tutorial entries
- A "Related Tutorials" section at the bottom

Every tutorial/cookbook page should link to:
- The reference page(s) that provide deep background
- The concept explainer(s) for the technologies used

### Difficulty Indicators

Add visual tags to sidebar entries:

- **Beginner** (green dot) -- Getting Started, first tutorials
- **Intermediate** (yellow dot) -- most tutorials, cookbook entries
- **Advanced** (red dot) -- Harmony transpilers, system replacement, Burst
- **Reference** (blue dot) -- existing deep-dive pages

---

## 4. Per-Page Improvements to Existing Content

### Add "Quick Start" Boxes to Reference Pages

Every existing reference page currently opens with a "Scope" box that describes what the page covers. Above or within this, add a "Quick Start" or "Common Tasks" box:

```
QUICK START -- What can I do with this?
- Trigger a crime at a building: [jump to Example 1]
- Change crime probability: [jump to Example 5]
- Listen for crime events: [jump to Example 4]
Related tutorial: [Triggering Game Events]
```

This lets a task-oriented reader jump directly to what they need without reading the full 800-line reference page.

### Make Code Examples More Copy-Paste Friendly

Current code examples are good but could be improved:
- Add `using` statements at the top of longer examples (some already do this, others do not)
- Add a comment at the top of each example stating what game assemblies must be referenced
- For examples that extend GameSystemBase, include a comment showing how to register the system in OnLoad
- Consider adding a "copy" button via JavaScript

### Add "When Would I Use This?" Context

Each reference page explains HOW a game system works but not WHEN a modder would care. Add a brief section near the top:

```
WHEN WOULD I USE THIS?
- Building a "Crime Wave" mod that triggers crimes on demand
- Adjusting crime difficulty for a rebalance mod
- Creating a crime statistics dashboard
- Making a mod that prevents crime in specific districts
```

### Page Length and Readability

Some reference pages are very long (mod-loading-dependencies.html is 2029 lines). Consider:
- Adding a floating table of contents on the right side for long pages
- Using `<details>`/`<summary>` elements to collapse advanced sections
- Breaking the longest pages into sub-pages linked from a topic hub

---

## 5. Prioritized Roadmap

### Phase 1: Quick Wins (1-2 weeks)

Minimal effort, high impact changes to the existing site:

1. **Redesign the landing page** -- Replace the card wall with a welcome section that directs different audiences. Keep the reference cards as a secondary section. (1 page change)
2. **Add "Quick Start" boxes to the 10 most-visited reference pages** -- mod-loading-dependencies, mod-ui-buttons, mod-options-ui, harmony-transpilers, save-load-persistence, localization, prefab-system, fire-ignition, crime-trigger, police-dispatch. These are the pages most likely to be found via search. (10 page edits)
3. **Add difficulty indicators to sidebar entries** -- A CSS class per difficulty level with a colored dot. (CSS + all pages sidebar update)
4. **Make the sidebar collapsible by section** -- A small JavaScript addition so reference sections can be collapsed. (1 JS file + CSS)
5. **Add a "Concepts" landing page** -- A single page that briefly explains ECS, Harmony, cohtml, and prefabs with links to external resources and to relevant reference pages. (1 new page)

**Estimated scope**: 10-15 page edits, 1-2 new pages, CSS/JS additions.

### Phase 2: New Tutorial Content (3-6 weeks)

Create the tutorial and getting-started content:

1. **Getting Started sequence** (4 new pages)
   - Setup Your Environment
   - Your First Mod (IMod + log message)
   - Build & Deploy
   - Architecture Overview (ECS concepts for CS2)

2. **Core tutorials** (5-6 new pages)
   - Your First System
   - Reading Game Data
   - Adding a Settings Panel
   - Adding a Toolbar Button
   - Patching with Harmony (prefix/postfix)
   - Replacing a Vanilla System

3. **Cookbook index + first 5-8 recipe pages**
   - Entity Queries
   - Emergency Dispatch
   - Game Events
   - System Scheduling
   - Cross-Mod Detection

4. **Concept explainers** (3 new pages)
   - Unity ECS for CS2 Modders
   - Harmony Overview
   - cohtml UI Model

**Estimated scope**: 15-20 new pages. Each tutorial page is shorter than a reference page (target 200-400 lines of HTML).

### Phase 3: Full Restructure (2-4 weeks after Phase 2)

Complete the transformation:

1. **Remaining cookbook recipes** (5-7 more pages)
2. **Remaining tutorials** (4 more pages: data persistence, hotkeys, Burst, complete mod walkthrough)
3. **Troubleshooting guide** (2 pages)
4. **Remaining concept explainers** (2-3 more pages)
5. **Floating table of contents** for long reference pages
6. **Collapsible sections** on long reference pages using `<details>`
7. **Full sidebar navigation overhaul** -- implement the two-tier design from Section 3
8. **Search functionality** -- client-side search using lunr.js or similar
9. **Cross-linking pass** -- go through all 51 reference pages and add links to tutorials/cookbook/concepts
10. **Add "Related tutorials" sections** to all existing reference pages

**Estimated scope**: 15-20 new pages, significant edits to all 51 existing pages, JS additions for search and navigation.

---

## 6. Specific Page Ideas

### Getting Started (4 pages)

1. **"Setting Up Your CS2 Modding Environment"** -- Install .NET SDK 8.0 (for ilspycmd), Visual Studio or Rider, set CSII_TOOLPATH, create Mod.props/Mod.targets. End state: ready to build.

2. **"Creating Your First CS2 Mod Step by Step"** -- Create .csproj, implement IMod, log a message in OnLoad, build with dotnet build, deploy to Mods folder, verify in game logs. The "hello world" of CS2 modding.

3. **"The Build-Deploy-Test Loop"** -- How the build pipeline works (Mod.targets copies DLL), where logs are, how to iterate quickly, common build errors and their fixes.

4. **"CS2 Architecture for Modders"** -- Brief explanation of ECS (entities, components, systems), how CS2 organizes game logic into systems, what GameSystemBase provides, what update phases mean. NOT a full ECS tutorial -- just enough to start reading the reference pages.

### Tutorials (10 pages)

5. **"How to Create a Custom ECS System"** -- Write a system that counts all residential buildings every 256 frames and logs the count. Covers GetEntityQuery, OnUpdate, GetUpdateInterval.

6. **"How to Read and Modify Game Data"** -- Read citizen wellbeing values, modify a building's crime rate. Covers EntityManager.GetComponentData, SetComponentData, HasComponent.

7. **"How to Add a Settings Panel to Your Mod"** -- Create a ModSetting subclass with a bool toggle and a float slider, register in OnLoad, persist to disk. End-to-end from code to visible settings page.

8. **"How to Add a Custom Toolbar Button"** -- Create a UISystemBase with TriggerBinding and ValueBinding, deploy a TypeScript UI module, wire the button to toggle a system. Full stack C# + TS.

9. **"How to Patch Game Methods with Harmony"** -- Apply a postfix patch that logs every fire ignition, apply a prefix patch that prevents crime on specific buildings. Covers patch lifecycle and cleanup.

10. **"How to Replace a Vanilla Game System"** -- Disable ResidentialDemandSystem and register a custom replacement. Covers Enabled=false, matching public interfaces, inter-system dependencies.

11. **"How to Save Custom Data with Your Mod"** -- Create an ISerializable component, add it to entities, verify it survives save/load. Covers IDefaultSerializable, version handling.

12. **"How to Register Custom Hotkeys"** -- Create a ProxyAction, bind to a key combo, respond to input, enable/disable based on GameMode. Covers the full input lifecycle.

13. **"How to Add a Custom Info View Overlay"** -- Create a custom overlay that colors buildings by a mod-computed value. Covers InfoviewPrefab, overlay rendering, color schemes.

14. **"Building a Complete Mod: Crime Wave Simulator"** -- Combine event triggering, UI buttons, settings, and logging into a polished mod. Ties together skills from earlier tutorials.

### Cookbook Recipes (8 pages)

15. **"How to Find Entities by Component Type"** -- EntityQuery patterns, filters, SharedComponentFilter for UpdateFrame, ToEntityArray with allocator management.

16. **"How to Dispatch Emergency Vehicles"** -- Condensed recipe from police-dispatch and emergency-dispatch reference pages. Three approaches in order of simplicity.

17. **"How to Trigger Game Events (Fire, Crime, Sickness)"** -- The event entity archetype pattern applied to the three most common event types. Copy-paste code for each.

18. **"How to Detect Game Mode and Loading State"** -- OnGameLoadingComplete, OnGameLoaded, GameMode checks, RegisterUpdater for deferred init.

19. **"How to Control System Update Frequency"** -- GetUpdateInterval, SimulationUtils.GetUpdateFrame, UpdateFrame filter, matching vanilla intervals.

20. **"How to Detect Other Mods and Interoperate"** -- ModManager iteration, ListModsEnabled, AppDomain assembly scanning, reflection-based API discovery.

21. **"How to Add Localized Strings for Multiple Languages"** -- MemorySource, LocalizedString, the localization key convention, adding translations.

22. **"How to Deploy Static Assets (Icons, Config Files)"** -- MSBuild AfterBuild targets, the coui:// protocol, accessing deployed files at runtime.

---

## 7. Technical Implementation Notes

### No Build System Change Needed

The site is plain HTML + CSS with no build system. New pages follow the same pattern. This is actually an advantage -- no tooling overhead for adding content.

### Sidebar Duplication Problem

Currently, the sidebar HTML is duplicated in every page. Adding new sections will require updating all 51+ pages each time. Consider:
- A simple JavaScript include that loads sidebar.html
- A lightweight templating step (e.g., a shell script that injects sidebar.html into each page)
- Keeping the manual approach but being aware of the maintenance cost

### CSS Additions Needed

- Difficulty indicator dots (3 colors)
- Collapsible sidebar sections (JS + CSS)
- "Quick Start" box styling (distinct from the existing "scope" box)
- Floating table of contents styling
- Tutorial step numbering/progress indicators

### Content Source

Much of the tutorial and cookbook content can be extracted from existing reference pages. For example:
- The "Getting Started" guide content is partially in docs/project-setup.md and docs/coding-standards.md
- The "mod loading" tutorial is partially in mod-loading-dependencies.html (the examples section)
- The "settings panel" tutorial is partially in mod-options-ui.html
- Cookbook recipes can be distilled from the Examples sections of reference pages

This means the writing effort is partially about reorganizing and simplifying existing content, not creating everything from scratch.
