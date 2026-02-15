using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUISliderAttribute : Attribute
{
	public float min;

	public float max = 100f;

	public float step = 1f;

	public string unit = "integer";

	public float scalarMultiplier = 1f;

	public bool scaleDragVolume;

	public bool updateOnDragEnd;
}
