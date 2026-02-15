// Decompiled from: Colossal.UI.Binding.dll -> Colossal.UI.Binding.ValueBinding<T>
// Push-based binding: C# -> JS. Call .Update(newValue) to push changes.
// Only sends update events if the value actually changed (EqualityComparer).
//
// Inheritance: ValueBinding<T> -> RawEventBindingBase -> EventBindingBase -> BindingBase
//
// Usage:
//   var binding = new ValueBinding<bool>("MyMod", "IsActive", false);
//   AddBinding(binding);
//   binding.Update(true);  // Pushes to JS

using System;
using System.Collections.Generic;

namespace Colossal.UI.Binding;

public class ValueBinding<T> : RawEventBindingBase
{
	private readonly IWriter<T> m_Writer;

	private readonly EqualityComparer<T> m_Comparer;

	public override DebugBindingType debugType => DebugBindingType.Value;

	public T value { get; private set; }

	/// <summary>
	/// Creates a push-based value binding.
	/// </summary>
	/// <param name="group">Binding group (use mod ID)</param>
	/// <param name="name">Binding name (unique within group)</param>
	/// <param name="initialValue">Initial value pushed on first subscribe</param>
	/// <param name="writer">Custom serializer (auto-resolved if null)</param>
	/// <param name="comparer">Custom equality check (default EqualityComparer if null)</param>
	public ValueBinding(string group, string name, T initialValue, IWriter<T> writer = null, EqualityComparer<T> comparer = null)
		: base(group, name)
	{
		m_Writer = writer ?? ValueWriters.Create<T>();
		m_Comparer = comparer ?? EqualityComparer<T>.Default;
		value = initialValue;
	}

	protected override void OnSubscribe()
	{
		base.OnSubscribe();
		try
		{
			TriggerUpdate();  // Push current value to new subscriber
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in value binding '" + base.path + "'\n");
		}
	}

	/// <summary>
	/// Update the binding value. Only sends to JS if value changed.
	/// </summary>
	public void Update(T newValue)
	{
		if (!m_Comparer.Equals(value, newValue))
		{
			value = newValue;
			TriggerUpdate();
		}
	}

	/// <summary>
	/// Force-push current value to JS regardless of change detection.
	/// </summary>
	public void TriggerUpdate()
	{
		if (base.active)  // Only send if JS has active subscribers
		{
			base.jsonWriter.BeginEvent(base.updateEventName, 1);
			m_Writer.Write(base.jsonWriter, value);
			base.jsonWriter.EndEvent();
		}
	}
}
