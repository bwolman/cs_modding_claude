// Source: RoadBuilder-CSII/RoadBuilder/UI/src/index.tsx
// Annotated example of the ModRegistrar pattern.
//
// This is the entry point for a mod's TypeScript UI module.
// The default export must be a ModRegistrar function that receives moduleRegistry.
//
// Key patterns demonstrated:
// 1. moduleRegistry.append("GameTopLeft", ...) — inject toolbar button
// 2. moduleRegistry.append("Game", ...)        — inject gameplay panel
// 3. moduleRegistry.append("Editor", ...)      — inject editor panel
// 4. moduleRegistry.extend(path, name, HOC)    — wrap a vanilla component

import { ModRegistrar } from "cs2/modding";
import { ModView } from "mods/ModView/ModView";
import ModIconButton from "mods/Components/ModIconButton/ModIconButton";
import { VanillaComponentResolver } from "vanillacomponentresolver";
import { RemoveVanillaRightToolbar } from "mods/Components/RemoveVanillaRightToolbar";
import mod from "../mod.json";

const register: ModRegistrar = (moduleRegistry) => {
    console.log(mod.id + " UI module registering...");

    // Store registry reference for resolving vanilla components elsewhere
    VanillaComponentResolver.setRegistry(moduleRegistry);

    // Inject a toolbar button into the top-left toolbar area
    // This appears alongside vanilla tool buttons (roads, zones, etc.)
    moduleRegistry.append("GameTopLeft", ModIconButton);

    // Inject the main mod panel into the gameplay view
    // The component controls its own visibility based on tool mode
    moduleRegistry.append("Game", () => ModView(false));

    // Same panel for the editor view
    moduleRegistry.append("Editor", () => ModView(true));

    // Wrap/extend a vanilla component — here, removing the right toolbar
    // The extend method takes: (component path, export name, wrapper HOC)
    moduleRegistry.extend(
        "game-ui/game/components/right-menu/right-menu.tsx",
        "RightMenu",
        RemoveVanillaRightToolbar
    );

    console.log(mod.id + " UI module registrations completed.");
};

export default register;
