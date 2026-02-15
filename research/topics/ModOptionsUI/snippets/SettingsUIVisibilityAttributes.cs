// Visibility attributes from Game.Settings
// HideByCondition, DisableByCondition, Button, Confirmation

using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public class SettingsUIHideByConditionAttribute : Attribute
{
	public readonly Type checkType;

	public readonly string checkMethod;

	public readonly bool invert;

	public SettingsUIHideByConditionAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}

	public SettingsUIHideByConditionAttribute(Type checkType, string checkMethod, bool invert)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
		this.invert = invert;
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public class SettingsUIDisableByConditionAttribute : Attribute
{
	public readonly Type checkType;

	public readonly string checkMethod;

	public readonly bool invert;

	public SettingsUIDisableByConditionAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}

	public SettingsUIDisableByConditionAttribute(Type checkType, string checkMethod, bool invert)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
		this.invert = invert;
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, Inherited = true)]
public class SettingsUIButtonAttribute : Attribute
{
}

public class SettingsUIConfirmationAttribute : Attribute
{
	public readonly string confirmMessageValue;

	public readonly string confirmMessageId;

	public SettingsUIConfirmationAttribute(string overrideConfirmMessageId = null, string overrideConfirmMessageValue = null)
	{
		confirmMessageValue = overrideConfirmMessageValue;
		confirmMessageId = overrideConfirmMessageId;
	}
}
