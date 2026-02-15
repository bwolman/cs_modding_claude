// Decompiled from: Colossal.UI.Binding.dll -> Colossal.UI.Binding.GetterValueBinding<T>
// Poll-based binding: C# -> JS. Calls getter every frame via UISystemBase.OnUpdate().
// Only sends update events if the getter's return value changed since last poll.
//
// Usage:
//   var binding = new GetterValueBinding<int>("MyMod", "Count", () => _count);
//   AddUpdateBinding(binding);  // Note: AddUpdateBinding, not AddBinding

using System;
using System.Collections.Generic;

namespace Colossal.UI.Binding;

public class GetterValueBinding<T> : RawEventBindingBase, IUpdateBinding, IBinding
{
	private readonly Func<T> m_Getter;

	private readonly IWriter<T> m_Writer;

	private readonly EqualityComparer<T> m_Comparer;

	private bool m_ValueDirty = true;

	private T m_Value;

	private int m_ConsecutiveUpdates;

	private const int MAX_CONSECUTIVE_UPDATES = 100;

	public override DebugBindingType debugType => DebugBindingType.Value;

	public GetterValueBinding(string group, string name, Func<T> getter, IWriter<T> writer = null, EqualityComparer<T> comparer = null)
		: base(group, name)
	{
		m_Getter = getter ?? throw new ArgumentNullException("getter");
		m_Writer = writer ?? ValueWriters.Create<T>();
		m_Comparer = comparer ?? EqualityComparer<T>.Default;
	}

	protected override void OnSubscribe()
	{
		base.OnSubscribe();
		try
		{
			TriggerUpdate();
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in value binding '" + base.path + "'\n");
		}
	}

	/// <summary>
	/// Called every frame by UISystemBase.OnUpdate() via IUpdateBinding.
	/// Returns true if value was pushed (changed).
	/// </summary>
	public bool Update()
	{
		if (base.active)
		{
			T val = m_Getter();
			if (m_ValueDirty || !m_Comparer.Equals(m_Value, val))
			{
				m_Value = val;
				m_ValueDirty = false;
				TriggerUpdateImpl();
				return true;
			}
		}
		else
		{
			m_ValueDirty = true;  // Mark dirty when no subscribers
		}
		m_ConsecutiveUpdates = 0;
		return false;
	}

	public void TriggerUpdate()
	{
		if (base.active)
		{
			if (m_ValueDirty)
			{
				m_Value = m_Getter();
				m_ValueDirty = false;
			}
			TriggerUpdateImpl();
		}
	}

	private void TriggerUpdateImpl()
	{
		base.jsonWriter.BeginEvent(base.updateEventName, 1);
		m_Writer.Write(base.jsonWriter, m_Value);
		base.jsonWriter.EndEvent();
	}
}
