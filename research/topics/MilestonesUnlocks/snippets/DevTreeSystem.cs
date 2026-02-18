using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.PSI;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.City;

[CompilerGenerated]
public class DevTreeSystem : GameSystemBase
{
	[BurstCompile]
	private struct AppendPointsJob : IJob
	{
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public NativeList<MilestoneReachedEvent> m_MilestoneReached;

		[ReadOnly]
		public ComponentLookup<MilestoneData> m_Milestones;

		public ComponentTypeHandle<DevTreePoints> m_PointsType;

		public void Execute()
		{
			NativeArray<DevTreePoints> nativeArray = m_Chunks[0].GetNativeArray(ref m_PointsType);
			int num = nativeArray[0].m_Points;
			for (int i = 0; i < m_MilestoneReached.Length; i++)
			{
				num += ((m_MilestoneReached[i].m_Milestone != Entity.Null) ? m_Milestones[m_MilestoneReached[i].m_Milestone].m_DevTreePoints : GetDefaultPoints(m_MilestoneReached[i].m_Index));
			}
			nativeArray[0] = new DevTreePoints
			{
				m_Points = num
			};
		}

		private int GetDefaultPoints(int level)
		{
			if (level <= 0)
			{
				return 0;
			}
			if (level >= 19)
			{
				return 10;
			}
			return (level + 1) / 2 + 1;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<MilestoneData> __Game_Prefabs_MilestoneData_RO_ComponentLookup;

		public ComponentTypeHandle<DevTreePoints> __Game_City_DevTreePoints_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<DevTreeNodeData> __Game_Prefabs_DevTreeNodeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_MilestoneData_RO_ComponentLookup = state.GetComponentLookup<MilestoneData>(isReadOnly: true);
			__Game_City_DevTreePoints_RW_ComponentTypeHandle = state.GetComponentTypeHandle<DevTreePoints>();
			__Game_Prefabs_DevTreeNodeData_RO_ComponentLookup = state.GetComponentLookup<DevTreeNodeData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_MilestoneReachedQuery;

	private EntityQuery m_DevTreePointsQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	public int points
	{
		get
		{
			if (!m_DevTreePointsQuery.IsEmptyIgnoreFilter)
			{
				return m_DevTreePointsQuery.GetSingleton<DevTreePoints>().m_Points;
			}
			return 0;
		}
		set
		{
			if (!m_DevTreePointsQuery.IsEmptyIgnoreFilter)
			{
				m_DevTreePointsQuery.SetSingleton(new DevTreePoints
				{
					m_Points = value
				});
			}
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_MilestoneReachedQuery = GetEntityQuery(ComponentType.ReadOnly<MilestoneReachedEvent>());
		m_DevTreePointsQuery = GetEntityQuery(ComponentType.ReadWrite<DevTreePoints>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Unlock>(), ComponentType.ReadWrite<Event>());
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		RequireForUpdate(m_DevTreePointsQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_MilestoneReachedQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			AppendPointsJob jobData = new AppendPointsJob
			{
				m_Chunks = m_DevTreePointsQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_MilestoneReached = m_MilestoneReachedQuery.ToComponentDataListAsync<MilestoneReachedEvent>(Allocator.TempJob, out outJobHandle2),
				m_Milestones = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MilestoneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PointsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_City_DevTreePoints_RW_ComponentTypeHandle, ref base.CheckedStateRef)
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2));
			jobData.m_Chunks.Dispose(base.Dependency);
			jobData.m_MilestoneReached.Dispose(base.Dependency);
		}
	}

	public void Purchase(DevTreeNodePrefab nodePrefab)
	{
		if (!m_DevTreePointsQuery.IsEmptyIgnoreFilter)
		{
			Entity entity = m_PrefabSystem.GetEntity(nodePrefab);
			Purchase(entity);
		}
	}

	public void Purchase(Entity node)
	{
		if (!m_DevTreePointsQuery.IsEmptyIgnoreFilter)
		{
			ComponentLookup<DevTreeNodeData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DevTreeNodeData_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<Locked> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef);
			DevTreeNodeData devTreeNodeData = componentLookup[node];
			int num = points;
			if (devTreeNodeData.m_Cost <= num && componentLookup2.HasEnabledComponent(node) && CheckService(devTreeNodeData.m_Service, componentLookup2) && (!base.EntityManager.HasComponent<DevTreeNodeRequirement>(node) || CheckRequirements(base.EntityManager.GetBuffer<DevTreeNodeRequirement>(node, isReadOnly: true), componentLookup2)))
			{
				num -= devTreeNodeData.m_Cost;
				points = num;
				EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
				Entity e = entityCommandBuffer.CreateEntity(m_UnlockEventArchetype);
				entityCommandBuffer.SetComponent(e, new Unlock(node));
				Telemetry.DevNodePurchased(m_PrefabSystem.GetPrefab<DevTreeNodePrefab>(node));
			}
		}
	}

	private static bool CheckRequirements(DynamicBuffer<DevTreeNodeRequirement> requirements, ComponentLookup<Locked> locked)
	{
		bool flag = false;
		for (int i = 0; i < requirements.Length; i++)
		{
			if (requirements[i].m_Node != Entity.Null)
			{
				flag = true;
				if (!locked.HasEnabledComponent(requirements[i].m_Node))
				{
					return true;
				}
			}
		}
		return !flag;
	}

	private static bool CheckService(Entity service, ComponentLookup<Locked> locked)
	{
		if (!(service == Entity.Null))
		{
			return !locked.HasEnabledComponent(service);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public DevTreeSystem()
	{
	}
}
