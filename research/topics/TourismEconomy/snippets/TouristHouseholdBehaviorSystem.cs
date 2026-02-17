#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TouristHouseholdBehaviorSystem : GameSystemBase
{
	[BurstCompile]
	private struct TouristHouseholdTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<TouristHousehold> m_TouristHouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<LodgingSeeker> m_LodgingSeekerType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<TouristHousehold> nativeArray2 = chunk.GetNativeArray(ref m_TouristHouseholdType);
			NativeArray<Target> nativeArray3 = chunk.GetNativeArray(ref m_TargetType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (nativeArray3.Length > 0)
				{
					if (!(nativeArray3[i].m_Target == Entity.Null) && m_Buildings.HasComponent(nativeArray3[i].m_Target))
					{
						continue;
					}
					m_CommandBuffer.RemoveComponent<Target>(unfilteredChunkIndex, nativeArray[i]);
				}
				Entity entity = nativeArray[i];
				if (chunk.Has(ref m_LodgingSeekerType))
				{
					continue;
				}
				TouristHousehold value = nativeArray2[i];
				Entity hotel = value.m_Hotel;
				if (hotel == Entity.Null || !m_RenterBufs.HasBuffer(hotel))
				{
					value.m_Hotel = Entity.Null;
					nativeArray2[i] = value;
				}
				else
				{
					UnityEngine.Debug.Assert(hotel != Entity.Null);
					DynamicBuffer<Renter> dynamicBuffer = m_RenterBufs[hotel];
					bool flag = false;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						if (dynamicBuffer[j].m_Renter.Equals(entity))
						{
							flag = true;
						}
					}
					if (!flag)
					{
						value.m_Hotel = Entity.Null;
						nativeArray2[i] = value;
					}
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(LodgingSeeker));
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

		public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RW_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LodgingSeeker> __Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingSeeker>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
		}
	}

	private EntityQuery m_TouristHouseholdGroup;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TouristHouseholdGroup = GetEntityQuery(ComponentType.ReadWrite<TouristHousehold>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_TouristHouseholdGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		TouristHouseholdTickJob jobData = new TouristHouseholdTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TouristHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_LodgingSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_UpdateFrameIndex = updateFrameWithInterval
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TouristHouseholdGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public TouristHouseholdBehaviorSystem()
	{
	}
}
