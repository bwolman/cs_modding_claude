using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUIDropdownAttribute : Attribute
{
	public readonly Type itemsGetterType;

	public readonly string itemsGetterMethod;

	public SettingsUIDropdownAttribute(Type itemsGetterType, string itemsGetterMethod)
	{
		this.itemsGetterType = itemsGetterType;
		this.itemsGetterMethod = itemsGetterMethod;
	}
}
