using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DestroyAbandonedSystem : GameSystemBase
{
	[BurstCompile]
	private struct DestroyAbandonedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> m_AbandonedType;

		[ReadOnly]
		public EntityArchetype m_DamageEventArchetype;

		[ReadOnly]
		public EntityArchetype m_DestroyEventArchetype;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public uint m_SimulationFrame;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Abandoned> nativeArray2 = chunk.GetNativeArray(ref m_AbandonedType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (nativeArray2[i].m_AbandonmentTime + m_BuildingConfigurationData.m_AbandonedDestroyDelay <= m_SimulationFrame)
				{
					Entity entity = nativeArray[i];
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DamageEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Damage(entity, new float3(1f, 0f, 0f)));
					Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DestroyEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e2, new Destroy(entity, Entity.Null));
					m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
					m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
					m_IconCommandBuffer.Add(entity, m_BuildingConfigurationData.m_AbandonedCollapsedNotification, IconPriority.FatalProblem);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_AbandonedQuery;

	private EntityArchetype m_DamageEventArchetype;

	private EntityArchetype m_DestroyEventArchetype;

	private EntityQuery m_BuildingSettingsQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_AbandonedQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Abandoned>(), ComponentType.Exclude<Destroyed>());
		m_DamageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Damage>());
		m_DestroyEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Destroy>());
		m_BuildingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		RequireForUpdate(m_AbandonedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		DestroyAbandonedJob jobData = new DestroyAbandonedJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AbandonedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingConfigurationData = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
			m_DamageEventArchetype = m_DamageEventArchetype,
			m_DestroyEventArchetype = m_DestroyEventArchetype,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_AbandonedQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public DestroyAbandonedSystem()
	{
	}
}
