// Extracted from Game.dll → Game.Input.ProxyAction
// Key methods: shouldBeEnabled, UpdateState, ApplyState

namespace Game.Input;

public class ProxyAction
{
    private InputAction m_SourceAction;
    private InputActivator m_DefaultActivator;
    private List<InputActivator> m_Activators = new();
    private ProxyActionMap m_Map;

    // --- shouldBeEnabled ---
    // REQUIRED for mod actions. Without this, m_Activators is empty
    // and the action can never be enabled.
    public bool shouldBeEnabled
    {
        set
        {
            if (isBuiltIn) throw new Exception("Built-in actions can not be enabled directly");
            if (m_DefaultActivator != null)
                m_DefaultActivator.enabled = value;
            else if (value)
                m_DefaultActivator = new InputActivator(
                    ignoreIsBuiltIn: false,
                    "Default (" + name + ")",
                    this,
                    DeviceType.All,
                    enabled: true);
        }
    }

    // --- UpdateState ---
    // Called when map state changes. Computes preResolvedMask from activators.
    internal void UpdateState()
    {
        InputManager.DeviceType deviceType = InputManager.DeviceType.None;
        foreach (InputActivator activator in m_Activators)
        {
            if (activator.enabled)
                deviceType |= activator.mask & m_PreResolvedMask;
        }
        // If m_Activators is empty → deviceType = None → action never enabled

        m_PreResolvedMask = (m_Map.enabled
            ? (m_AvailableMask & m_Map.mask)
            : InputManager.DeviceType.None);
        // ...
    }

    // --- ApplyState ---
    // Actually enables or disables the underlying Unity InputAction
    internal void ApplyState(bool enabled, InputManager.DeviceType mask)
    {
        if (enabled)
            m_SourceAction.Enable();
        else
            m_SourceAction.Disable();
        // ...
    }

    // --- WasPressedThisFrame ---
    // Returns false when action is disabled (silently)
    public bool WasPressedThisFrame()
    {
        return m_SourceAction.WasPressedThisFrame();
    }
}
