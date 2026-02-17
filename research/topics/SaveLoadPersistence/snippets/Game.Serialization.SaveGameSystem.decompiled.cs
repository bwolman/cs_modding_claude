using System.IO;
using System.Threading.Tasks;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class SaveGameSystem : GameSystemBase
{
	private TaskCompletionSource<bool> m_TaskCompletionSource;

	private UpdateSystem m_UpdateSystem;

	private WriteSystem m_WriteSystem;

	private bool m_Writing;

	private Context m_Context;

	public Stream stream { get; set; }

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

	public NativeArray<Entity> referencedContent { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_WriteSystem = base.World.GetOrCreateSystemManaged<WriteSystem>();
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Context.Dispose();
		if (referencedContent.IsCreated)
		{
			referencedContent.Dispose();
		}
		base.OnDestroy();
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
		if (m_Writing)
		{
			if (m_WriteSystem.writeDependency.IsCompleted)
			{
				m_WriteSystem.writeDependency.Complete();
				m_Writing = false;
				base.Enabled = false;
				m_TaskCompletionSource?.SetResult(result: true);
			}
		}
		else
		{
			m_Writing = true;
			m_UpdateSystem.Update(SystemUpdatePhase.Serialize);
		}
	}

	[Preserve]
	public SaveGameSystem()
	{
	}
}
