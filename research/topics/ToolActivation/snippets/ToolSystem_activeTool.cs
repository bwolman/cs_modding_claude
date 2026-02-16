// Extracted from Game.dll → Game.Tools.ToolSystem
// Key methods related to tool activation safety
// Full decompilation at: research/topics/ToolRaycast/snippets/ToolSystem.cs

namespace Game.Tools;

public class ToolSystem : GameSystemBase
{
    // --- Fields ---
    private ToolBaseSystem m_ActiveTool;
    private ToolBaseSystem m_LastTool;
    private bool m_FullUpdateRequired;
    private bool m_IsUpdating;

    // --- activeTool property ---
    // Setting this from ANY context (TriggerBinding, input callback, etc.) is safe.
    // The setter writes m_ActiveTool immediately, sets the update flag, and fires
    // EventToolChanged synchronously. ToolUpdate() detects the change by comparing
    // activeTool != m_LastTool on the next frame.
    public ToolBaseSystem activeTool
    {
        get { return m_ActiveTool; }
        set
        {
            if (value != m_ActiveTool)
            {
                m_ActiveTool = value;
                RequireFullUpdate();
                EventToolChanged?.Invoke(value);
            }
        }
    }

    // --- RequireFullUpdate ---
    // Branches on m_IsUpdating:
    //   Inside OnUpdate  → defers to m_FullUpdateRequired (flushed at end of OnUpdate)
    //   Outside OnUpdate → writes fullUpdateRequired directly
    // Both paths produce the same result: fullUpdateRequired = true before next ToolUpdate.
    public void RequireFullUpdate()
    {
        if (m_IsUpdating)
        {
            m_FullUpdateRequired = true;
        }
        else
        {
            fullUpdateRequired = true;
        }
    }

    // --- OnUpdate ---
    // m_IsUpdating is true ONLY during this method. TriggerBinding callbacks fire
    // outside this scope (during cohtml UI processing on the main thread).
    protected override void OnUpdate()
    {
        m_ToolActionBarrier.blocked = !GameManager.instance.gameMode.IsGameOrEditor()
                                      || GameManager.instance.isGameLoading;
        m_IsUpdating = true;
        m_UpdateSystem.Update(SystemUpdatePhase.PreTool);
        ToolUpdate();
        m_UpdateSystem.Update(SystemUpdatePhase.PostTool);
        fullUpdateRequired = m_FullUpdateRequired;
        m_FullUpdateRequired = false;
        m_IsUpdating = false;
    }

    // --- ToolUpdate ---
    // Does NOT use fullUpdateRequired to detect tool changes.
    // Compares activeTool != m_LastTool directly every frame.
    private void ToolUpdate()
    {
        m_InfoviewTimer += UnityEngine.Time.deltaTime;
        m_InfoviewTimer %= 60f;
        if (activeTool != m_LastTool)
        {
            if (m_LastTool != null)
            {
                m_LastTool.Enabled = false;
                m_LastTool.Update();
            }
            m_LastTool = activeTool;
        }
        // ...
        if (m_LastTool != null)
        {
            m_LastTool.Enabled = true;
        }
        m_UpdateSystem.Update(SystemUpdatePhase.ToolUpdate);
        // ...
    }

    // --- ActivatePrefabTool ---
    // Vanilla precedent: sets activeTool from outside ToolUpdate.
    public bool ActivatePrefabTool(PrefabBase prefab)
    {
        if (prefab != null)
        {
            foreach (ToolBaseSystem tool in tools)
            {
                if (tool.TrySetPrefab(prefab))
                {
                    activeTool = tool;  // same setter, called from outside ToolUpdate
                    return true;
                }
            }
        }
        activeTool = m_DefaultToolSystem;
        return false;
    }
}
