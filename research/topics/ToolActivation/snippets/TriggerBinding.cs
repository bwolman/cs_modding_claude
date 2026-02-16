using System;
using Colossal.Annotations;
using cohtml.Net;

namespace Colossal.UI.Binding;

public class TriggerBinding : BindingBase
{
	private readonly Action m_Callback;

	private BoundEventHandle m_Handle;

	public override DebugBindingType debugType => DebugBindingType.Trigger;

	public TriggerBinding(string group, string name, [NotNull] Action callback)
		: base(group, name)
	{
		m_Callback = callback ?? throw new ArgumentNullException("callback");
	}

	private void Callback()
	{
		try
		{
			m_Callback();
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in trigger binding callback '" + base.path + "' '" + base.group + "' '" + base.name + "'");
		}
	}

	public override void Attach(View view)
	{
		base.Attach(view);
		m_Handle = view.RegisterForEvent(base.path, new Action(Callback));
	}

	public override void Detach()
	{
		base.view?.UnregisterFromEvent(m_Handle);
		base.Detach();
	}
}
public class TriggerBinding<T> : RawTriggerBindingBase
{
	private readonly Action<T> m_Callback;

	private readonly IReader<T> m_Reader;

	public TriggerBinding(string group, string name, [NotNull] Action<T> callback, IReader<T> reader = null)
		: base(group, name)
	{
		m_Callback = callback ?? throw new ArgumentNullException("callback");
		m_Reader = reader ?? ValueReaders.Create<T>();
	}

	protected override void Callback()
	{
		try
		{
			m_Reader.Read(base.jsonReader, out var value);
			m_Callback(value);
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in trigger binding callback '" + base.path + "' '" + base.group + "' '" + base.name + "'");
		}
	}
}
public class TriggerBinding<T1, T2> : RawTriggerBindingBase
{
	private readonly Action<T1, T2> m_Callback;

	private readonly IReader<T1> m_Reader1;

	private readonly IReader<T2> m_Reader2;

	public TriggerBinding(string group, string name, [NotNull] Action<T1, T2> callback, IReader<T1> reader1 = null, IReader<T2> reader2 = null)
		: base(group, name)
	{
		m_Callback = callback ?? throw new ArgumentNullException("callback");
		m_Reader1 = reader1 ?? ValueReaders.Create<T1>();
		m_Reader2 = reader2 ?? ValueReaders.Create<T2>();
	}

	protected override void Callback()
	{
		try
		{
			m_Reader1.Read(base.jsonReader, out var value);
			m_Reader2.Read(base.jsonReader, out var value2);
			m_Callback(value, value2);
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in trigger binding callback '" + base.path + "' '" + base.group + "' '" + base.name + "'");
		}
	}
}
public class TriggerBinding<T1, T2, T3> : RawTriggerBindingBase
{
	private readonly Action<T1, T2, T3> m_Callback;

	private readonly IReader<T1> m_Reader1;

	private readonly IReader<T2> m_Reader2;

	private readonly IReader<T3> m_Reader3;

	public TriggerBinding(string group, string name, [NotNull] Action<T1, T2, T3> callback, IReader<T1> reader1 = null, IReader<T2> reader2 = null, IReader<T3> reader3 = null)
		: base(group, name)
	{
		m_Callback = callback ?? throw new ArgumentNullException("callback");
		m_Reader1 = reader1 ?? ValueReaders.Create<T1>();
		m_Reader2 = reader2 ?? ValueReaders.Create<T2>();
		m_Reader3 = reader3 ?? ValueReaders.Create<T3>();
	}

	protected override void Callback()
	{
		try
		{
			m_Reader1.Read(base.jsonReader, out var value);
			m_Reader2.Read(base.jsonReader, out var value2);
			m_Reader3.Read(base.jsonReader, out var value3);
			m_Callback(value, value2, value3);
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in trigger binding callback '" + base.path + "' '" + base.group + "' '" + base.name + "'");
		}
	}
}
public class TriggerBinding<T1, T2, T3, T4> : RawTriggerBindingBase
{
	private readonly Action<T1, T2, T3, T4> m_Callback;

	private readonly IReader<T1> m_Reader1;

	private readonly IReader<T2> m_Reader2;

	private readonly IReader<T3> m_Reader3;

	private readonly IReader<T4> m_Reader4;

	public TriggerBinding(string group, string name, [NotNull] Action<T1, T2, T3, T4> callback, IReader<T1> reader1 = null, IReader<T2> reader2 = null, IReader<T3> reader3 = null, IReader<T4> reader4 = null)
		: base(group, name)
	{
		m_Callback = callback ?? throw new ArgumentNullException("callback");
		m_Reader1 = reader1 ?? ValueReaders.Create<T1>();
		m_Reader2 = reader2 ?? ValueReaders.Create<T2>();
		m_Reader3 = reader3 ?? ValueReaders.Create<T3>();
		m_Reader4 = reader4 ?? ValueReaders.Create<T4>();
	}

	protected override void Callback()
	{
		try
		{
			m_Reader1.Read(base.jsonReader, out var value);
			m_Reader2.Read(base.jsonReader, out var value2);
			m_Reader3.Read(base.jsonReader, out var value3);
			m_Reader4.Read(base.jsonReader, out var value4);
			m_Callback(value, value2, value3, value4);
		}
		catch (Exception exception)
		{
			BindingBase.log.Error(exception, "Error in trigger binding callback '" + base.path + "' '" + base.group + "' '" + base.name + "'");
		}
	}
}
