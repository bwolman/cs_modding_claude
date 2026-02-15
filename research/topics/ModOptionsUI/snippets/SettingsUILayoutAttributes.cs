// Layout attributes from Game.Settings
// TabOrder, GroupOrder, ShowGroupName, ButtonGroup

using System;
using System.Collections.ObjectModel;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SettingsUITabOrderAttribute : Attribute
{
	public readonly ReadOnlyCollection<string> tabs;

	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUITabOrderAttribute(params string[] tabs)
	{
		this.tabs = new ReadOnlyCollection<string>(tabs);
	}

	public SettingsUITabOrderAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SettingsUIGroupOrderAttribute : Attribute
{
	public readonly ReadOnlyCollection<string> groups;

	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUIGroupOrderAttribute(params string[] groups)
	{
		this.groups = new ReadOnlyCollection<string>(groups);
	}

	public SettingsUIGroupOrderAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public class SettingsUIShowGroupNameAttribute : Attribute
{
	public readonly bool showAll;

	public readonly ReadOnlyCollection<string> groups;

	public SettingsUIShowGroupNameAttribute()
	{
		showAll = true;
	}

	public SettingsUIShowGroupNameAttribute(params string[] groups)
	{
		this.groups = new ReadOnlyCollection<string>(groups);
	}
}
