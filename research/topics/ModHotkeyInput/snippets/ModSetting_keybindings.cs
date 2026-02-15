// Keybinding-related methods from Game.Modding.ModSetting
// (Extracted from full decompiled source — see README for full lifecycle documentation)

using Game.Input;
using Game.Settings;

namespace Game.Modding;

public class ModSetting : Setting
{
	// ID format: {AssemblyName}.{Namespace}.{ModType}
	// This becomes the action map name for all keybindings
	public string id { get; }

	public bool keyBindingRegistered { get; private set; }

	// All ProxyBinding properties found via reflection
	private PropertyInfo[] keyBindingProperties => (from p in GetType().GetProperties()
		where p.CanRead && p.CanWrite && p.PropertyType == typeof(ProxyBinding)
		select p).ToArray();

	public ModSetting(IMod mod)
	{
		Type type = mod.GetType();
		id = type.Assembly.GetName().Name + "." + type.Namespace + "." + type.Name;
		InitializeKeyBindings(); // Sets default values from attributes
	}

	// Reads attributes from ProxyBinding properties, generates default bindings
	private void InitializeKeyBindings()
	{
		foreach (PropertyInfo prop in keyBindingProperties)
		{
			ProxyBinding binding = GenerateBinding(prop);
			prop.SetValue(this, binding);
		}
	}

	// Main registration method — call AFTER LoadSettings
	public void RegisterKeyBindings()
	{
		if (keyBindingRegistered) return;

		// 1. Read [SettingsUIKeyboardAction] attrs from class
		// 2. Group ProxyBinding properties by actionName
		// 3. Build ProxyAction.Info[] with composites
		// 4. Call InputManager.instance.AddActions(actionsToAdd)
		// 5. Create ProxyBinding.Watcher for each property (auto-sync on rebind)

		keyBindingRegistered = true;
	}

	public void RegisterInOptionsUI()
	{
		RegisterInOptionsUI(id, addPrefix: true);
	}
}
