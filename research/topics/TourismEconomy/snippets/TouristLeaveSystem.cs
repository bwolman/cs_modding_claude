using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TouristLeaveSystem : GameSystemBase
{
	[BurstCompile]
	private struct TouristLeaveJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TouristHousehold> m_HouseholdType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourcesType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> m_LodgingProviders;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public uint m_UpdateFrameIndex;

		public float m_Time;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<TouristHousehold> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourcesType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				TouristHousehold touristHousehold = nativeArray2[i];
				bool num = m_LodgingProviders.HasComponent(touristHousehold.m_Hotel);
				int num2 = 0;
				int num3 = 0;
				if (num)
				{
					num2 = m_LodgingProviders[touristHousehold.m_Hotel].m_Price;
					num3 = EconomyUtils.GetResources(Resource.Money, bufferAccessor[i]);
				}
				MoveAwayReason moveAwayReason = ((!num && m_Time > 0.8f) ? MoveAwayReason.TouristNoHotel : ((num3 < num2 && m_Time > 0.7f) ? MoveAwayReason.TouristNoMoney : MoveAwayReason.None));
				if (moveAwayReason != MoveAwayReason.None)
				{
					CitizenUtils.HouseholdMoveAway(m_CommandBuffer, unfilteredChunkIndex, entity, moveAwayReason);
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.TouristLeftCity, Entity.Null, entity, Entity.Null));
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
		public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_TouristHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Companies_LodgingProvider_RO_ComponentLookup = state.GetComponentLookup<LodgingProvider>(isReadOnly: true);
		}
	}

	private EntityQuery m_TouristHouseholdGroup;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private TriggerSystem m_TriggerSystem;

	private TimeSystem m_TimeSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_TouristHouseholdGroup = GetEntityQuery(ComponentType.ReadWrite<TouristHousehold>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Created>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_TouristHouseholdGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		TouristLeaveJob jobData = new TouristLeaveJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LodgingProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_Time = m_TimeSystem.normalizedTime,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TouristHouseholdGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
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
	public TouristLeaveSystem()
	{
	}
}
