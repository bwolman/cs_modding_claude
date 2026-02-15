using System;
using System.Collections.Generic;
using Game.Input;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property)]
public class SettingsUIKeyboardBindingAttribute : SettingsUIKeybindingAttribute
{
	public readonly BindingKeyboard defaultKey;

	public readonly bool alt;

	public readonly bool ctrl;

	public readonly bool shift;

	// Converts BindingKeyboard enum to Unity input path string
	// e.g. BindingKeyboard.F5 -> "<Keyboard>/f5"
	public override string control => defaultKey switch
	{
		BindingKeyboard.None => string.Empty,
		BindingKeyboard.Space => "<Keyboard>/space",
		BindingKeyboard.Enter => "<Keyboard>/enter",
		// ... (full switch for all 106 BindingKeyboard values)
		BindingKeyboard.F5 => "<Keyboard>/f5",
		// ...
		_ => string.Empty,
	};

	// Yields modifier control paths based on shift/ctrl/alt bools
	public override IEnumerable<string> modifierControls
	{
		get
		{
			if (shift) yield return "<Keyboard>/shift";
			if (ctrl) yield return "<Keyboard>/ctrl";
			if (alt) yield return "<Keyboard>/alt";
		}
	}

	// Button with default key + optional modifiers
	public SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, string actionName = null,
		bool alt = false, bool ctrl = false, bool shift = false)
		: this(actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}

	// Button with no default key
	public SettingsUIKeyboardBindingAttribute(string actionName = null)
		: base(actionName, InputManager.DeviceType.Keyboard, ActionType.Button, ActionComponent.Press)
	{
	}

	// Axis component
	public SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, AxisComponent component,
		string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(component, actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}
}
