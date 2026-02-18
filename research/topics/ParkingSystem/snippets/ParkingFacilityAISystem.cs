using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ParkingFacilityAISystem : GameSystemBase
{
	[BurstCompile]
	private struct ParkingFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		public ComponentTypeHandle<Game.Buildings.ParkingFacility> m_ParkingFacilityType;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ParkingFacilityData> m_PrefabParkingFacilityData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.ParkingFacility> nativeArray3 = chunk.GetNativeArray(ref m_ParkingFacilityType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Game.Buildings.ParkingFacility parkingFacility = nativeArray3[i];
				m_PrefabParkingFacilityData.TryGetComponent(prefabRef.m_Prefab, out var componentData);
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabParkingFacilityData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				Tick(unfilteredChunkIndex, entity, ref parkingFacility, componentData, efficiency);
				nativeArray3[i] = parkingFacility;
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Game.Buildings.ParkingFacility parkingFacility, ParkingFacilityData prefabParkingFacilityData, float efficiency)
		{
			float num = math.saturate(prefabParkingFacilityData.m_ComfortFactor * efficiency);
			bool flag = efficiency > 0f;
			bool flag2 = (parkingFacility.m_Flags & ParkingFacilityFlags.ParkingSpacesActive) != 0;
			if (num != parkingFacility.m_ComfortFactor || flag != flag2)
			{
				parkingFacility.m_ComfortFactor = num;
				if (flag)
				{
					parkingFacility.m_Flags |= ParkingFacilityFlags.ParkingSpacesActive;
				}
				else
				{
					parkingFacility.m_Flags &= ~ParkingFacilityFlags.ParkingSpacesActive;
				}
				if (m_SubLanes.TryGetBuffer(entity, out var bufferData))
				{
					UpdateParkingLanes(jobIndex, bufferData);
				}
				if (m_SubNets.TryGetBuffer(entity, out var bufferData2))
				{
					UpdateParkingLanes(jobIndex, bufferData2);
				}
				if (m_SubObjects.TryGetBuffer(entity, out var bufferData3))
				{
					UpdateParkingLanes(jobIndex, bufferData3);
				}
			}
		}

		private void UpdateParkingLanes(int jobIndex, DynamicBuffer<Game.Objects.SubObject> subObjects)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_SubLanes.TryGetBuffer(subObject, out var bufferData))
				{
					UpdateParkingLanes(jobIndex, bufferData);
				}
				if (m_SubObjects.TryGetBuffer(subObject, out var bufferData2))
				{
					UpdateParkingLanes(jobIndex, bufferData2);
				}
			}
		}

		private void UpdateParkingLanes(int jobIndex, DynamicBuffer<Game.Net.SubNet> subNets)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (m_SubLanes.TryGetBuffer(subNet, out var bufferData))
				{
					UpdateParkingLanes(jobIndex, bufferData);
				}
			}
		}

		private void UpdateParkingLanes(int jobIndex, DynamicBuffer<Game.Net.SubLane> subLanes)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				if (m_ParkingLaneData.TryGetComponent(subLane, out var componentData))
				{
					if ((componentData.m_Flags & ParkingLaneFlags.VirtualLane) == 0)
					{
						m_CommandBuffer.AddComponent(jobIndex, subLane, default(PathfindUpdated));
					}
				}
				else if (m_GarageLaneData.HasComponent(subLane))
				{
					m_CommandBuffer.AddComponent(jobIndex, subLane, default(PathfindUpdated));
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.ParkingFacility> __Game_Buildings_ParkingFacility_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_ParkingFacility_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ParkingFacility>();
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ParkingFacilityData_RO_ComponentLookup = state.GetComponentLookup<ParkingFacilityData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 192;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.ParkingFacility>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new ParkingFacilityTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ParkingFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ParkingFacility_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_BuildingQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public ParkingFacilityAISystem()
	{
	}
}
