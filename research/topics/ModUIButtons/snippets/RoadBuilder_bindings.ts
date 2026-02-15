// Source: RoadBuilder-CSII/RoadBuilder/UI/src/mods/bindings.ts
// Annotated example of TypeScript binding declarations.
//
// Key patterns demonstrated:
// 1. bindValue<T>(group, name, default) — creates observable for C# ValueBinding
// 2. trigger.bind(null, group, name) — creates pre-bound no-arg trigger function
// 3. (args) => trigger(group, name, ...args) — creates trigger with arguments
// 4. mod.id as the binding group — matches C# side group parameter
//
// This file centralizes all binding declarations for the mod UI.
// Components import from here rather than calling bindValue/trigger directly.

import { bindValue, trigger } from "cs2/api";
import mod from "mod.json";  // { id: "RoadBuilder", ... }

// === Value Bindings (C# -> JS) ===
// Each matches a C# ValueBinding or GetterValueBinding with the same group+name.
// The third argument is the default value used before C# pushes the first update.

export const roadBuilderToolMode$ = bindValue(mod.id, "RoadBuilderToolMode", 0 /* None */);
export const roadLanes$ = bindValue<RoadLane[]>(mod.id, "GetRoadLanes", []);
export const getRoadName$ = bindValue<string>(mod.id, "GetRoadName");
export const isPaused$ = bindValue<boolean>(mod.id, "IsPaused");
export const IsCustomRoadSelected$ = bindValue<boolean>(mod.id, "IsCustomRoadSelected", false);

// Can also subscribe to vanilla game bindings:
export const fpsMeterLevel$ = bindValue<number>("app", "fpsMode");

// === Trigger Bindings (JS -> C#) ===

// No-arg triggers: use .bind() for a clean callable
export const toggleTool = trigger.bind(null, mod.id, "ToggleTool");
export const clearTool = trigger.bind(null, mod.id, "ClearTool");

// 1-arg triggers: wrap in arrow function
export const setRoadName = (name: string) => trigger(mod.id, "SetRoadName", name);
export const activateRoad = (id: string) => trigger(mod.id, "ActivateRoad", id);

// Multi-arg triggers: pass all args to trigger()
export const laneOptionClicked = (optionIndex: number, netSectionId: number, optionId: number, value: number) =>
    trigger(mod.id, "OptionClicked", optionIndex, netSectionId, optionId, value);

// Array arg: filter before sending
export const setRoadLanes = (lanes: RoadLane[]) => {
    trigger(mod.id, "SetRoadLanes", lanes.filter((x) => !x.IsEdgePlaceholder));
};
