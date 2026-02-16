using System;
using cohtml.Net;

namespace Colossal.UI.Binding;

public abstract class RawTriggerBindingBase : BindingBase
{
	private BoundEventHandle m_Handle;

	public bool active { get; set; } = true;

	protected JsonReader jsonReader { get; }

	public override DebugBindingType debugType => DebugBindingType.Unknown;

	protected RawTriggerBindingBase(string group, string name)
		: base(group, name)
	{
		jsonReader = new JsonReader(base.path);
	}

	private void BaseCallback()
	{
		if (active)
		{
			Callback();
			return;
		}
		while (jsonReader.PeekValueType() != cohtml.Net.ValueType.Null)
		{
			jsonReader.SkipValue();
		}
	}

	protected abstract void Callback();

	public override void Attach(View attachView)
	{
		base.Attach(attachView);
		jsonReader.binder = attachView.GetBinder();
		m_Handle = attachView.RegisterForEvent(base.path, new Action(BaseCallback));
	}

	public override void Detach()
	{
		base.view?.UnregisterFromEvent(m_Handle);
		jsonReader.binder = IntPtr.Zero;
		base.Detach();
	}
}
