using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Events;
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
public class FloodCheckSystem : GameSystemBase
{
	[BurstCompile]
	private struct FloodCheckJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<WaterLevelChange> m_WaterLevelChangeType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<InDanger> m_InDangerData;

		[ReadOnly]
		public ComponentLookup<WaterLevelChange> m_WaterLevelChangeData;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> m_PrefabWaterLevelChangeData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefaObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefaPlaceableObjectData;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_WaterLevelChangeChunks;

		[ReadOnly]
		public EntityArchetype m_SubmergeArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Transform transform = nativeArray2[i];
				if (IsFlooded(transform.m_Position, out var depth))
				{
					Entity entity = nativeArray[i];
					PrefabRef prefabRef = nativeArray3[i];
					if ((!m_PrefaObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || (componentData.m_Flags & GeometryFlags.CanSubmerge) == 0) && (!m_PrefaPlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) || (componentData2.m_Flags & (PlacementFlags.Floating | PlacementFlags.Swaying)) != (PlacementFlags.Floating | PlacementFlags.Swaying)))
					{
						Entity entity2 = FindFloodEvent(entity, transform.m_Position);
						Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_SubmergeArchetype);
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Submerge
						{
							m_Event = entity2,
							m_Target = entity,
							m_Depth = depth
						});
					}
				}
			}
		}

		private Entity FindFloodEvent(Entity entity, float3 position)
		{
			if (m_InDangerData.HasComponent(entity))
			{
				InDanger inDanger = m_InDangerData[entity];
				if (m_WaterLevelChangeData.HasComponent(inDanger.m_Event))
				{
					return inDanger.m_Event;
				}
			}
			Entity result = Entity.Null;
			float num = 0.001f;
			for (int i = 0; i < m_WaterLevelChangeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_WaterLevelChangeChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<WaterLevelChange> nativeArray2 = archetypeChunk.GetNativeArray(ref m_WaterLevelChangeType);
				NativeArray<Duration> nativeArray3 = archetypeChunk.GetNativeArray(ref m_DurationType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					WaterLevelChange waterLevelChange = nativeArray2[j];
					Duration duration = nativeArray3[j];
					PrefabRef prefabRef = nativeArray4[j];
					WaterLevelChangeData waterLevelChangeData = m_PrefabWaterLevelChangeData[prefabRef.m_Prefab];
					if (duration.m_StartFrame <= m_SimulationFrame && waterLevelChangeData.m_ChangeType == WaterLevelChangeType.Sine)
					{
						float num2 = (float)(m_SimulationFrame - duration.m_StartFrame) / 60f;
						float num3 = (float)(duration.m_EndFrame - WaterLevelChangeSystem.TsunamiEndDelay - duration.m_StartFrame) / 60f;
						float num4 = WaterSystem.WaveSpeed * 60f;
						float num5 = num2 * num4;
						float num6 = num5 - num3 * num4;
						float2 @float = WaterSystem.kMapSize / 2 * -waterLevelChange.m_Direction;
						float t;
						float num7 = MathUtils.Distance(new Line2(@float, @float + MathUtils.Right(waterLevelChange.m_Direction)), position.xz, out t);
						float num8 = math.lerp(num5, num6, 0.5f);
						float num9 = math.smoothstep((num5 - num6) * 0.75f, 0f, num7 - num8);
						if (num9 > num)
						{
							result = nativeArray[j];
							num = num9;
						}
					}
				}
			}
			return result;
		}

		private bool IsFlooded(float3 position, out float depth)
		{
			float num = WaterUtils.SampleDepth(ref m_WaterSurfaceData, position);
			if (num > 0.5f)
			{
				num += TerrainUtils.SampleHeight(ref m_TerrainHeightData, position) - position.y;
				if (num > 0.5f)
				{
					depth = num;
					return true;
				}
			}
			depth = 0f;
			return false;
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

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterLevelChange> __Game_Events_WaterLevelChange_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterLevelChange> __Game_Events_WaterLevelChange_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> __Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Events_WaterLevelChange_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterLevelChange>(isReadOnly: true);
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(isReadOnly: true);
			__Game_Events_WaterLevelChange_RO_ComponentLookup = state.GetComponentLookup<WaterLevelChange>(isReadOnly: true);
			__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup = state.GetComponentLookup<WaterLevelChangeData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 16u;

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_TargetQuery;

	private EntityQuery m_WaterLevelChangeQuery;

	private EntityArchetype m_SubmergeArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TargetQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Placeholder>(), ComponentType.Exclude<Flooded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_WaterLevelChangeQuery = GetEntityQuery(ComponentType.ReadOnly<WaterLevelChange>(), ComponentType.ReadOnly<Duration>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_SubmergeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Submerge>());
		RequireForUpdate(m_TargetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameIndex = (m_SimulationSystem.frameIndex >> 4) & 0xF;
		JobHandle deps;
		JobHandle outJobHandle;
		FloodCheckJob jobData = new FloodCheckJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterLevelChangeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WaterLevelChange_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InDangerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterLevelChangeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_WaterLevelChange_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWaterLevelChangeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefaObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefaPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = updateFrameIndex,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_WaterLevelChangeChunks = m_WaterLevelChangeQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_SubmergeArchetype = m_SubmergeArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TargetQuery, JobHandle.CombineDependencies(base.Dependency, deps, outJobHandle));
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		m_WaterSystem.AddSurfaceReader(base.Dependency);
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
	public FloodCheckSystem()
	{
	}
}
