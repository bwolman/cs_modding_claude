// Key polling and event methods from Game.Input.ProxyAction
// (Extracted from full decompiled source — see README for full type documentation)

namespace Game.Input;

// ProxyAction wraps a Unity InputAction, providing these key methods:

public class ProxyAction : IProxyAction
{
    // --- Polling methods (call from OnUpdate) ---

    public bool IsPressed()
    {
        return m_SourceAction.IsPressed();
    }

    public bool WasPressedThisFrame()
    {
        return m_SourceAction.WasPressedThisFrame();
    }

    public bool WasReleasedThisFrame()
    {
        return m_SourceAction.WasReleasedThisFrame();
    }

    public bool WasPerformedThisFrame()
    {
        return m_SourceAction.WasPerformedThisFrame();
    }

    public bool IsInProgress()
    {
        return m_SourceAction.IsInProgress();
    }

    public T ReadValue<T>() where T : struct { /* reads from InputActionState */ }

    public float GetMagnitude() { /* reads magnitude from action state */ }

    // --- Event subscription ---

    public event Action<ProxyAction, InputActionPhase> onInteraction
    {
        add
        {
            // First subscriber hooks Unity callbacks:
            //   m_SourceAction.started += SourceOnStarted;
            //   m_SourceAction.performed += SourceOnPerformed;
            //   m_SourceAction.canceled += SourceOnCanceled;
            m_OnInteraction += value;
        }
        remove
        {
            m_OnInteraction -= value;
            // Last unsubscribe unhooks Unity callbacks
        }
    }

    // --- Enable/Disable ---

    public bool shouldBeEnabled
    {
        get { return m_DefaultActivator?.enabled ?? false; }
        set
        {
            if (isBuiltIn) throw new Exception("Built-in actions can not be enabled directly");
            // Creates or updates default activator
        }
    }

    // --- Lookup ---

    public string name => m_SourceAction.name;
    public string mapName => m_Map.name;
    public bool enabled => m_SourceAction.enabled;

    // --- Barrier/Activator creation ---

    public InputBarrier CreateBarrier(string barrierName = null, InputManager.DeviceType barrierMask = InputManager.DeviceType.All);
    public InputActivator CreateActivator(string activatorName = null, InputManager.DeviceType activatorMask = InputManager.DeviceType.All);
}
