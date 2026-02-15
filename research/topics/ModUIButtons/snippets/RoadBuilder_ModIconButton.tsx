// Source: RoadBuilder-CSII/RoadBuilder/UI/src/mods/Components/ModIconButton/ModIconButton.tsx
// Annotated example of a toolbar button component injected via moduleRegistry.append("GameTopLeft", ...).
//
// Key patterns demonstrated:
// 1. useValue(binding$) — subscribe to a C# ValueBinding
// 2. trigger(group, name) — invoke a C# TriggerBinding (via pre-bound function)
// 3. Button + Tooltip from cs2/ui — game-style React components
// 4. CSS modules with game CSS variables (--accentColorNormal, etc.)
// 5. SVG icon via mask-image for dynamic coloring

import classNames from "classnames";
import { Button, Tooltip } from "cs2/ui";           // Game-style React components
import { tool } from "cs2/bindings";                 // Vanilla binding observables
import { bindValue, trigger, useValue } from "cs2/api"; // Binding API
import mod from "mod.json";                           // { id: "RoadBuilder", ... }
import styles from "./ModIconButton.module.scss";     // CSS modules
import trafficIcon from "images/RB_ModIcon.svg";
import trafficIconActive from "images/RB_ModIconActive.svg";
import { RoadBuilderToolModeEnum } from "domain/RoadBuilderToolMode";
import { roadBuilderToolMode$, toggleTool } from "mods/bindings";

export default () => {
    // Subscribe to C# ValueBinding<RoadBuilderToolModeEnum> — re-renders on change
    const roadBuilderToolMode = useValue(roadBuilderToolMode$);

    return (
        // Tooltip wraps the button with hover text
        <Tooltip tooltip="Road Builder">
            <Button
                variant="floating"    // Floating style = toolbar button
                className={classNames(
                    { [styles.selected]: roadBuilderToolMode !== RoadBuilderToolModeEnum.None },
                    styles.toggle
                )}
                onSelect={toggleTool}  // Calls trigger("RoadBuilder", "ToggleTool")
            >
                {/* SVG icon via CSS mask-image for theme-aware coloring */}
                <img style={{
                    maskImage: `url(${
                        roadBuilderToolMode !== RoadBuilderToolModeEnum.None
                            ? trafficIconActive
                            : trafficIcon
                    })`
                }} />
            </Button>
        </Tooltip>
    );
};

// === Companion SCSS (ModIconButton.module.scss) ===
//
// .toggle {
//     background-color: var(--accentColorNormal);
//     &:hover { background-color: var(--accentColorNormal-hover); }
//     &:active { background-color: var(--accentColorNormal-pressed); }
//     &.selected { background-color: white; }
//     img {
//         width: 100%; height: 100%;
//         background-color: white;
//         mask-size: contain;
//         mask-position: 50% 50%;
//     }
// }
