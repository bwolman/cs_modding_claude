using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public class SettingsUISectionAttribute : Attribute
{
	public const string kGeneral = "General";

	public readonly string tab;

	public readonly string simpleGroup;

	public readonly string advancedGroup;

	public SettingsUISectionAttribute(string tab, string simpleGroup, string advancedGroup)
	{
		this.tab = tab ?? "General";
		this.simpleGroup = simpleGroup ?? string.Empty;
		this.advancedGroup = advancedGroup ?? string.Empty;
	}

	public SettingsUISectionAttribute(string tab, string group)
		: this(tab, group, group)
	{
	}

	public SettingsUISectionAttribute(string group)
		: this(null, group, group)
	{
	}
}
