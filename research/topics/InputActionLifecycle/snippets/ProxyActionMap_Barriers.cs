// Extracted from Game.dll → Game.Input.ProxyActionMap
// Shows how InputBarrier controls map-level enabling/disabling

namespace Game.Input;

public class ProxyActionMap
{
    private List<InputBarrier> m_Barriers = new();
    private bool m_Enabled = true;
    private DeviceType m_Mask;
    private List<ProxyAction> m_Actions = new();

    public bool enabled => m_Enabled;

    public DeviceType mask
    {
        get => m_Mask;
        set
        {
            m_Mask = value;
            UpdateState();
        }
    }

    // --- UpdateState ---
    // If ANY barrier is blocked, the entire map is disabled.
    // This disables ALL actions in the map, including mod actions.
    internal void UpdateState()
    {
        bool flag = m_Barriers.All(b => !b.blocked);
        if (flag == m_Enabled) return;
        m_Enabled = flag;

        // Propagate to all actions in this map
        foreach (ProxyAction action in m_Actions)
            action.UpdateState();
    }
}

// --- InputBarrier ---
// IDisposable. blocked = true from construction for overlay barriers.
public class InputBarrier : IDisposable
{
    private ProxyActionMap[] m_Maps;
    public bool blocked;

    public InputBarrier(string name, ProxyActionMap[] maps, DeviceType mask, bool blocked)
    {
        m_Maps = maps;
        this.blocked = blocked;
        foreach (var map in maps)
            map.m_Barriers.Add(this);
        // triggers UpdateState
    }

    public void Dispose()
    {
        foreach (var map in m_Maps)
            map.m_Barriers.Remove(this);
        // triggers UpdateState → map may re-enable
    }
}
