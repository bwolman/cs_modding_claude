// Extracted from Game.dll → Game.Input.InputManager
// Shows how the global mask is computed and propagated to all maps (including mod maps)

namespace Game.Input;

public class InputManager
{
    [Flags]
    public enum DeviceType
    {
        None = 0,
        Keyboard = 1,
        Mouse = 2,
        Gamepad = 4,
        All = 7
    }

    private bool m_OverlayActive;
    private bool m_HasInputFieldFocus;
    private DeviceType m_BlockedControlTypes;

    // --- mask property ---
    // Setting this pushes the mask to ALL maps, including mod maps.
    public DeviceType mask
    {
        set
        {
            foreach (var (_, proxyActionMap) in m_Maps)
                proxyActionMap.mask = value;  // all maps, including mod maps
        }
    }

    // --- GetMaskForControlScheme ---
    // Determines what device types are allowed based on current context.
    // When overlayActive → mask = 0 (nothing works)
    // When hasInputFieldFocus → mask = Mouse only (keyboard blocked)
    // Otherwise → full device mask
    private int GetMaskForControlScheme(ControlScheme controlScheme)
    {
        int result = controlScheme switch
        {
            ControlScheme.KeyboardAndMouse =>
                (!m_OverlayActive) ? (m_HasInputFieldFocus ? 2 : 3) : 0,
            //    no overlay:         text focus → Mouse(2)  normal → Kb+Mouse(3)   overlay → None(0)
            ControlScheme.Gamepad =>
                (!m_OverlayActive) ? 4 : 0,
            //    no overlay → Gamepad(4)   overlay → None(0)
            _ => 0
        };
        return result & (int)(~m_BlockedControlTypes);  // subtract blocked types (key rebinding)
    }

    // --- CreateOverlayBarrier ---
    // Creates a barrier covering ALL maps except Engagement/Splash screen.
    // Includes mod maps. Used by loading screens, splash screens, etc.
    public InputBarrier CreateOverlayBarrier(string barrierName)
    {
        var maps = m_Maps.Values
            .Where(m => m.name != "Engagement" && m.name != "Splash screen")
            .ToArray();
        return new InputBarrier(barrierName, maps, DeviceType.All, blocked: true);
    }

    // --- CreateMapBarrier ---
    // Creates a barrier for a SPECIFIC map by name.
    // ToolSystem uses this for "Tool" map only — does NOT affect mod maps.
    public InputBarrier CreateMapBarrier(string mapName, string barrierName)
    {
        ProxyActionMap map = GetOrCreateMap(mapName);
        return new InputBarrier(barrierName, new[] { map }, DeviceType.All);
    }

    // --- hasInputFieldFocus ---
    // Set by the UI when a text input field gains/loses focus.
    // Triggers global mask recalculation → keyboard blocked for all maps.
    public bool hasInputFieldFocus
    {
        get => m_HasInputFieldFocus;
        set
        {
            if (m_HasInputFieldFocus != value)
            {
                m_HasInputFieldFocus = value;
                RefreshActiveControl();  // recalculates mask
            }
        }
    }
}
