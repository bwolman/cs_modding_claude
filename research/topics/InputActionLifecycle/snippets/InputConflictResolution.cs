// Extracted from Game.dll → Game.Input.InputConflictResolution
// Central authority for enabling/disabling actions based on conflict priority

namespace Game.Input;

public class InputConflictResolution
{
    private List<State> m_SystemActions = new();  // Camera, Tool, Editor — highest priority
    private List<State> m_UIActions = new();      // menu/nav — middle priority
    private List<State> m_ModActions = new();     // all mod actions — lowest priority

    // --- RefreshActions ---
    // Classifies every action into one of the three buckets
    private void RefreshActions()
    {
        // ...
        foreach (ProxyAction action in allActions)
        {
            if (!action.isBuiltIn)
                m_ModActions.Add(new State(action));
            else if (action.isSystemAction)
                m_SystemActions.Add(new State(action));
            else
                m_UIActions.Add(new State(action));
        }
    }

    // --- ResolveConflicts ---
    // Runs every frame. Priority: system > UI > mod.
    // If a mod action shares a key with an enabled system/UI action, mod loses.
    public void ResolveConflicts()
    {
        // Reset conflict flags
        foreach (State s in m_ModActions) s.m_HasConflict = false;
        foreach (State s in m_UIActions) s.m_HasConflict = false;

        // System vs UI
        foreach (State sys in m_SystemActions)
            foreach (State ui in m_UIActions)
                Resolve(sys, ui);

        // System vs Mod
        foreach (State sys in m_SystemActions)
            foreach (State mod in m_ModActions)
                Resolve(sys, mod);

        // UI vs Mod
        foreach (State ui in m_UIActions)
            foreach (State mod in m_ModActions)
                Resolve(ui, mod);

        // Apply results
        foreach (State mod in m_ModActions) mod.Apply();
        foreach (State ui in m_UIActions) ui.Apply();
    }

    static void Resolve(State primary, State secondary)
    {
        if (InputManager.HasConflicts(primary.m_Action, secondary.m_Action /*, ...*/))
            secondary.m_HasConflict = true;
    }

    // --- State ---
    private class State
    {
        public ProxyAction m_Action;
        public bool m_HasConflict;

        // enabled = pre-resolved enable AND no conflict
        public bool enabled => m_Action.preResolvedEnable && !m_HasConflict;

        public void Apply()
        {
            m_Action.ApplyState(enabled, /*preResolvedMask*/);
        }
    }
}
