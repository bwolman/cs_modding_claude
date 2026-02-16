// Extracted from Game.dll → Game.Input.InputManager
// Shows how the global mask blocks all maps (including mod maps)

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
        All = Keyboard | Mouse | Gamepad
    }

    private bool m_OverlayActive;
    private bool m_HasInputFieldFocus;
    private DeviceType m_BlockedControlTypes;
    private Dictionary<string, ProxyActionMap> m_Maps;

    // --- mask property ---
    // Pushes to ALL maps, including mod maps
    public DeviceType mask
    {
        set
        {
            foreach (var (_, proxyActionMap) in m_Maps)
                proxyActionMap.mask = value;
        }
    }

    // --- GetMaskForControlScheme ---
    // Calculates per-frame mask. Returns 0 when overlay is active.
    private int GetMaskForControlScheme(ControlScheme scheme)
    {
        return scheme switch
        {
            ControlScheme.KeyboardAndMouse =>
                (!m_OverlayActive)
                    ? (m_HasInputFieldFocus ? 2 : 3)   // Mouse(2) or Kb+Mouse(3)
                    : 0,                                 // Nothing
            ControlScheme.Gamepad =>
                (!m_OverlayActive) ? 4 : 0,
            _ => 0
        } & (int)(~m_BlockedControlTypes);
    }

    // --- CreateOverlayBarrier ---
    // Blocks ALL maps except Engagement and Splash screen
    public InputBarrier CreateOverlayBarrier(string barrierName)
    {
        var maps = m_Maps.Values
            .Where(m => m.name != "Engagement" && m.name != "Splash screen")
            .ToArray();
        return new InputBarrier(barrierName, maps, DeviceType.All, blocked: true);
    }

    // --- CreateMapBarrier ---
    // Blocks only the specified named map (e.g., "Tool")
    public InputBarrier CreateMapBarrier(string mapName, string barrierName)
    {
        var map = m_Maps[mapName];
        return new InputBarrier(barrierName, new[] { map }, DeviceType.All, blocked: false);
    }
}
