using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class LoadGameSystem : GameSystemBase
{
	public delegate void EventGameLoaded(Context serializationContext);

	public EventGameLoaded onOnSaveGameLoaded;

	private TaskCompletionSource<bool> m_TaskCompletionSource;

	private UpdateSystem m_UpdateSystem;

	private Context m_Context;

	public AsyncReadDescriptor dataDescriptor { get; set; }

	public Context context
	{
		get
		{
			return m_Context;
		}
		set
		{
			m_Context.Dispose();
			m_Context = value;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		base.Enabled = false;
	}

	public async Task RunOnce()
	{
		m_TaskCompletionSource = new TaskCompletionSource<bool>();
		base.Enabled = true;
		await m_TaskCompletionSource.Task;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_UpdateSystem.Update(SystemUpdatePhase.Deserialize);
		base.Enabled = false;
		onOnSaveGameLoaded?.Invoke(context);
		m_TaskCompletionSource?.SetResult(result: true);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Context.Dispose();
		onOnSaveGameLoaded = null;
		base.OnDestroy();
	}

	[Preserve]
	public LoadGameSystem()
	{
	}
}
