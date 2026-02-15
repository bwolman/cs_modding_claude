// Key methods from Game.Input.InputManager
// (Extracted from full decompiled source — see README for full type documentation)

namespace Game.Input;

public class InputManager : IDisposable, IInputActionCollection
{
	// --- Singleton ---
	public static InputManager instance;

	// --- Map name constants ---
	public const string kShortcutsMap = "Shortcuts";
	public const string kToolMap = "Tool";
	public const string kCameraMap = "Camera";
	public const string kMenuMap = "Menu";
	public const string kNavigationMap = "Navigation";
	public const string kEditorMap = "Editor";
	public const string kDebugMap = "Debug";
	public const string kEngagementMap = "Engagement";
	public const string kPhotoModeMap = "Photo mode";

	// --- Modifier path constants ---
	public const string kShiftName = "<Keyboard>/shift";
	public const string kCtrlName = "<Keyboard>/ctrl";
	public const string kAltName = "<Keyboard>/alt";

	// --- Action lookup ---
	public ProxyAction FindAction(string mapName, string actionName)
	{
		return FindActionMap(mapName)?.FindAction(actionName);
	}

	public bool TryFindAction(string mapName, string actionName, out ProxyAction action)
	{
		action = FindAction(mapName, actionName);
		return action != null;
	}

	public ProxyActionMap FindActionMap(string name)
	{
		if (!m_Maps.TryGetValue(name, out var value))
			return null;
		return value;
	}

	// --- Action registration (called by ModSetting.RegisterKeyBindings) ---
	internal void AddActions(ProxyAction.Info[] actionsToAdd)
	{
		ProxyAction[] array = new ProxyAction[actionsToAdd.Length];
		using (DeferUpdating())
		{
			for (int i = 0; i < actionsToAdd.Length; i++)
			{
				ProxyActionMap orCreateMap = GetOrCreateMap(actionsToAdd[i].m_Map);
				array[i] = orCreateMap.AddAction(actionsToAdd[i], bulk: true);
			}
		}
		ProxyActionMap[] maps = array.Select(a => a.map).Distinct().ToArray();
		for (int i = 0; i < maps.Length; i++)
			maps[i].UpdateState();
	}

	// --- Device type enum ---
	[Flags]
	public enum DeviceType
	{
		None = 0,
		Keyboard = 1,
		Mouse = 2,
		Gamepad = 4,
		All = 7
	}
}
