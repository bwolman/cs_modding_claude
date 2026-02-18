using System.Runtime.CompilerServices;
using Game.City;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class XPSystem : GameSystemBase, IXPSystem
{
	private struct XPQueueProcessJob : IJob
	{
		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public uint m_FrameIndex;

		public ComponentLookup<XP> m_CityXPs;

		public NativeQueue<XPGain> m_XPQueue;

		public NativeQueue<XPMessage> m_XPMessages;

		public void Execute()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			XP xP = m_CityXPs[m_City];
			XPGain xPGain = default(XPGain);
			while (m_XPQueue.TryDequeue(ref xPGain))
			{
				if (xPGain.amount != 0)
				{
					xP.m_XP += xPGain.amount;
					m_XPMessages.Enqueue(new XPMessage(m_FrameIndex, xPGain.amount, xPGain.reason));
				}
			}
			m_CityXPs[m_City] = xP;
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<XP> __Game_City_XP_RW_ComponentLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			__Game_City_XP_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<XP>(false);
		}
	}

	private NativeQueue<XPMessage> m_XPMessages;

	private NativeQueue<XPGain> m_XPQueue;

	private JobHandle m_QueueWriters;

	private CitySystem m_CitySystem;

	private SimulationSystem m_SimulationSystem;

	private TypeHandle __TypeHandle;

	public void TransferMessages(IXPMessageHandler handler)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		JobHandle dependency = ((SystemBase)this).Dependency;
		((JobHandle)(ref dependency)).Complete();
		while (m_XPMessages.Count > 0)
		{
			XPMessage message = m_XPMessages.Dequeue();
			handler.AddMessage(message);
		}
	}

	public NativeQueue<XPGain> GetQueue(out JobHandle deps)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		deps = m_QueueWriters;
		return m_XPQueue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		m_QueueWriters = JobHandle.CombineDependencies(m_QueueWriters, handle);
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_CitySystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CitySystem>();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		m_XPMessages = new NativeQueue<XPMessage>(AllocatorHandle.op_Implicit((Allocator)4));
		m_XPQueue = new NativeQueue<XPGain>(AllocatorHandle.op_Implicit((Allocator)4));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_XPQueue.Dispose();
		m_XPMessages.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!(m_CitySystem.City == Entity.Null))
		{
			XPQueueProcessJob xPQueueProcessJob = new XPQueueProcessJob
			{
				m_City = m_CitySystem.City,
				m_FrameIndex = m_SimulationSystem.frameIndex,
				m_XPMessages = m_XPMessages,
				m_XPQueue = m_XPQueue,
				m_CityXPs = InternalCompilerInterface.GetComponentLookup<XP>(ref __TypeHandle.__Game_City_XP_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef)
			};
			((SystemBase)this).Dependency = IJobExtensions.Schedule<XPQueueProcessJob>(xPQueueProcessJob, JobHandle.CombineDependencies(m_QueueWriters, ((SystemBase)this).Dependency));
			m_QueueWriters = ((SystemBase)this).Dependency;
		}
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public XPSystem()
	{
	}
}
