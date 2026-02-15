using System;
using Game.Input;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SettingsUIKeyboardActionAttribute : SettingsUIInputActionAttribute
{
	public SettingsUIKeyboardActionAttribute(
		string name,
		ActionType type = ActionType.Button,
		RebindOptions rebindOptions = RebindOptions.All,
		ModifierOptions modifierOptions = ModifierOptions.Allow,
		bool canBeEmpty = true,
		bool developerOnly = false,
		Mode mode = Mode.DigitalNormalized,
		string[] usages = null,
		string[] interactions = null,
		string[] processors = null)
		: base(name, InputManager.DeviceType.Keyboard, type, rebindOptions,
			modifierOptions, canBeEmpty, developerOnly, mode, usages, interactions, processors)
	{
	}

	// Convenience overloads with custom usages
	public SettingsUIKeyboardActionAttribute(string name, ActionType type, Mode mode, params string[] customUsages)
		: base(name, InputManager.DeviceType.Keyboard, type, mode, customUsages) { }

	public SettingsUIKeyboardActionAttribute(string name, ActionType type, params string[] customUsages)
		: base(name, InputManager.DeviceType.Keyboard, type, Mode.DigitalNormalized, customUsages) { }

	public SettingsUIKeyboardActionAttribute(string name, Mode mode, params string[] customUsages)
		: base(name, InputManager.DeviceType.Keyboard, ActionType.Button, mode, customUsages) { }

	public SettingsUIKeyboardActionAttribute(string name, params string[] customUsages)
		: base(name, InputManager.DeviceType.Keyboard, ActionType.Button, Mode.DigitalNormalized, customUsages) { }
}
