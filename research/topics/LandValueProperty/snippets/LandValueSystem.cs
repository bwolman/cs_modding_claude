#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class LandValueSystem : CellMapSystem<LandValueCell>, IJobSerializable
{
	private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public int m_TotalCount;

		public float m_TotalLandValueBonus;

		public Bounds3 m_Bounds;

		public ComponentLookup<LandValue> m_LandValueData;

		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_LandValueData.HasComponent(entity) && m_EdgeGeometryData.HasComponent(entity))
			{
				LandValue landValue = m_LandValueData[entity];
				if (landValue.m_LandValue > 0f)
				{
					m_TotalLandValueBonus += landValue.m_LandValue;
					m_TotalCount++;
				}
			}
		}
	}

	[BurstCompile]
	private struct LandValueMapUpdateJob : IJobParallelFor
	{
		public NativeArray<LandValueCell> m_LandValueMap;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeArray<TerrainAttractiveness> m_AttractiveMap;

		[ReadOnly]
		public NativeArray<GroundPollution> m_GroundPollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public NativeArray<AvailabilityInfoCell> m_AvailabilityInfoMap;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverageMap;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValueData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public AttractivenessParameterData m_AttractivenessParameterData;

		[ReadOnly]
		public LandValueParameterData m_LandValueParameterData;

		public float m_CellSize;

		public void Execute(int index)
		{
			float3 cellCenter = CellMapSystem<LandValueCell>.GetCellCenter(index, kTextureSize);
			if (WaterUtils.SampleDepth(ref m_WaterSurfaceData, cellCenter) > 1f)
			{
				m_LandValueMap[index] = new LandValueCell
				{
					m_LandValue = m_LandValueParameterData.m_LandValueBaseline
				};
				return;
			}
			NetIterator iterator = new NetIterator
			{
				m_TotalCount = 0,
				m_TotalLandValueBonus = 0f,
				m_Bounds = new Bounds3(cellCenter - new float3(1.5f * m_CellSize, 10000f, 1.5f * m_CellSize), cellCenter + new float3(1.5f * m_CellSize, 10000f, 1.5f * m_CellSize)),
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_LandValueData = m_LandValueData
			};
			m_NetSearchTree.Iterate(ref iterator);
			float num = GroundPollutionSystem.GetPollution(cellCenter, m_GroundPollutionMap).m_Pollution;
			float num2 = AirPollutionSystem.GetPollution(cellCenter, m_AirPollutionMap).m_Pollution;
			float num3 = NoisePollutionSystem.GetPollution(cellCenter, m_NoisePollutionMap).m_Pollution;
			float x = AvailabilityInfoToGridSystem.GetAvailabilityInfo(cellCenter, m_AvailabilityInfoMap).m_AvailabilityInfo.x;
			float num4 = TelecomCoverage.SampleNetworkQuality(m_TelecomCoverageMap, cellCenter);
			LandValueCell value = m_LandValueMap[index];
			float num5 = (((float)iterator.m_TotalCount > 0f) ? (iterator.m_TotalLandValueBonus / (float)iterator.m_TotalCount) : 0f);
			float num6 = math.min((x - 5f) * m_LandValueParameterData.m_AttractivenessBonusMultiplier, m_LandValueParameterData.m_CommonFactorMaxBonus);
			float num7 = math.min(num4 * m_LandValueParameterData.m_TelecomCoverageBonusMultiplier, m_LandValueParameterData.m_CommonFactorMaxBonus);
			num5 += num6 + num7;
			float num8 = WaterUtils.SamplePolluted(ref m_WaterSurfaceData, cellCenter);
			float num9 = 0f;
			if (num8 <= 0f && num <= 0f)
			{
				num9 = TerrainAttractivenessSystem.EvaluateAttractiveness(TerrainUtils.SampleHeight(ref m_TerrainHeightData, cellCenter), m_AttractiveMap[index], m_AttractivenessParameterData);
				num5 += math.min(math.max(num9 - 5f, 0f) * m_LandValueParameterData.m_AttractivenessBonusMultiplier, m_LandValueParameterData.m_CommonFactorMaxBonus);
			}
			float num10 = num * m_LandValueParameterData.m_GroundPollutionPenaltyMultiplier + num2 * m_LandValueParameterData.m_AirPollutionPenaltyMultiplier + num3 * m_LandValueParameterData.m_NoisePollutionPenaltyMultiplier;
			float num11 = math.max(m_LandValueParameterData.m_LandValueBaseline, m_LandValueParameterData.m_LandValueBaseline + num5 - num10);
			if (math.abs(value.m_LandValue - num11) >= 0.1f)
			{
				value.m_LandValue = math.lerp(value.m_LandValue, num11, 0.4f);
			}
			m_LandValueMap[index] = value;
		}
	}

	[BurstCompile]
	private struct EdgeUpdateJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.ServiceCoverage> m_ServiceCoverageType;

		[ReadOnly]
		public BufferTypeHandle<ResourceAvailability> m_AvailabilityType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<LandValue> m_LandValues;

		[ReadOnly]
		public LandValueParameterData m_LandValueParameterData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Edge> nativeArray2 = chunk.GetNativeArray(ref m_EdgeType);
			BufferAccessor<Game.Net.ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceCoverageType);
			BufferAccessor<ResourceAvailability> bufferAccessor2 = chunk.GetBufferAccessor(ref m_AvailabilityType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				if (bufferAccessor.Length > 0)
				{
					DynamicBuffer<Game.Net.ServiceCoverage> dynamicBuffer = bufferAccessor[i];
					Game.Net.ServiceCoverage serviceCoverage = dynamicBuffer[0];
					num = math.lerp(serviceCoverage.m_Coverage.x, serviceCoverage.m_Coverage.y, 0.5f) * m_LandValueParameterData.m_HealthCoverageBonusMultiplier;
					Game.Net.ServiceCoverage serviceCoverage2 = dynamicBuffer[5];
					num2 = math.lerp(serviceCoverage2.m_Coverage.x, serviceCoverage2.m_Coverage.y, 0.5f) * m_LandValueParameterData.m_EducationCoverageBonusMultiplier;
					Game.Net.ServiceCoverage serviceCoverage3 = dynamicBuffer[2];
					num3 = math.lerp(serviceCoverage3.m_Coverage.x, serviceCoverage3.m_Coverage.y, 0.5f) * m_LandValueParameterData.m_PoliceCoverageBonusMultiplier;
				}
				float num4 = 0f;
				float num5 = 0f;
				float num6 = 0f;
				if (bufferAccessor2.Length > 0)
				{
					DynamicBuffer<ResourceAvailability> dynamicBuffer2 = bufferAccessor2[i];
					ResourceAvailability resourceAvailability = dynamicBuffer2[1];
					num4 = math.lerp(resourceAvailability.m_Availability.x, resourceAvailability.m_Availability.y, 0.5f) * m_LandValueParameterData.m_CommercialServiceBonusMultiplier;
					ResourceAvailability resourceAvailability2 = dynamicBuffer2[31];
					num5 = math.lerp(resourceAvailability2.m_Availability.x, resourceAvailability2.m_Availability.y, 0.5f) * m_LandValueParameterData.m_BusBonusMultiplier;
					ResourceAvailability resourceAvailability3 = dynamicBuffer2[32];
					num6 = math.lerp(resourceAvailability3.m_Availability.x, resourceAvailability3.m_Availability.y, 0.5f) * m_LandValueParameterData.m_TramSubwayBonusMultiplier;
				}
				LandValue value = m_LandValues[entity];
				float num7 = math.max(num + num2 + num3 + num4 + num5 + num6, 0f);
				if (math.abs(value.m_LandValue - num7) >= 0.1f)
				{
					float x = math.lerp(value.m_LandValue, num7, 0.6f);
					value.m_LandValue = math.max(x, 0f);
					m_LandValues[entity] = value;
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
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferTypeHandle;

		public ComponentLookup<LandValue> __Game_Net_LandValue_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>(isReadOnly: true);
			__Game_Net_LandValue_RW_ComponentLookup = state.GetComponentLookup<LandValue>();
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
		}
	}

	public static readonly int kTextureSize = 128;

	public static readonly int kUpdatesPerDay = 32;

	private EntityQuery m_EdgeGroup;

	private EntityQuery m_NodeGroup;

	private EntityQuery m_AttractivenessParameterQuery;

	private EntityQuery m_LandValueParameterQuery;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private AvailabilityInfoToGridSystem m_AvailabilityInfoToGridSystem;

	private SearchSystem m_NetSearchSystem;

	private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private TypeHandle __TypeHandle;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<LandValueCell>.GetCellCenter(index, kTextureSize);
	}

	public static int GetCellIndex(float3 pos)
	{
		int num = CellMapSystem<LandValueCell>.kMapSize / kTextureSize;
		return Mathf.FloorToInt(((float)(CellMapSystem<LandValueCell>.kMapSize / 2) + pos.x) / (float)num) + Mathf.FloorToInt(((float)(CellMapSystem<LandValueCell>.kMapSize / 2) + pos.z) / (float)num) * kTextureSize;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		Assert.IsTrue(kTextureSize == TerrainAttractivenessSystem.kTextureSize);
		CreateTextures(kTextureSize);
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
		m_AvailabilityInfoToGridSystem = base.World.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_AttractivenessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
		m_LandValueParameterQuery = GetEntityQuery(ComponentType.ReadOnly<LandValueParameterData>());
		m_EdgeGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadWrite<LandValue>(),
				ComponentType.ReadOnly<Curve>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireAnyForUpdate(m_EdgeGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_EdgeGroup.IsEmptyIgnoreFilter)
		{
			EdgeUpdateJob jobData = new EdgeUpdateJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceCoverageType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_AvailabilityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_LandValues = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RW_ComponentLookup, ref base.CheckedStateRef),
				m_LandValueParameterData = m_LandValueParameterQuery.GetSingleton<LandValueParameterData>()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EdgeGroup, base.Dependency);
		}
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		JobHandle dependencies5;
		JobHandle dependencies6;
		JobHandle dependencies7;
		JobHandle deps;
		LandValueMapUpdateJob jobData2 = new LandValueMapUpdateJob
		{
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_AttractiveMap = m_TerrainAttractivenessSystem.GetMap(readOnly: true, out dependencies2),
			m_GroundPollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies4),
			m_NoisePollutionMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies5),
			m_AvailabilityInfoMap = m_AvailabilityInfoToGridSystem.GetMap(readOnly: true, out dependencies6),
			m_TelecomCoverageMap = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies7),
			m_LandValueMap = m_Map,
			m_LandValueData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttractivenessParameterData = m_AttractivenessParameterQuery.GetSingleton<AttractivenessParameterData>(),
			m_LandValueParameterData = m_LandValueParameterQuery.GetSingleton<LandValueParameterData>(),
			m_CellSize = (float)CellMapSystem<LandValueCell>.kMapSize / (float)kTextureSize
		};
		base.Dependency = IJobParallelForExtensions.Schedule(jobData2, kTextureSize * kTextureSize, kTextureSize, JobHandle.CombineDependencies(dependencies, dependencies2, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, JobHandle.CombineDependencies(base.Dependency, deps, JobHandle.CombineDependencies(dependencies3, dependencies5, JobHandle.CombineDependencies(dependencies6, dependencies4, dependencies7))))));
		AddWriter(base.Dependency);
		m_NetSearchSystem.AddNetSearchTreeReader(base.Dependency);
		m_WaterSystem.AddSurfaceReader(base.Dependency);
		m_TerrainAttractivenessSystem.AddReader(base.Dependency);
		m_GroundPollutionSystem.AddReader(base.Dependency);
		m_AirPollutionSystem.AddReader(base.Dependency);
		m_NoisePollutionSystem.AddReader(base.Dependency);
		m_AvailabilityInfoToGridSystem.AddReader(base.Dependency);
		m_TelecomCoverageSystem.AddReader(base.Dependency);
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
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
	public LandValueSystem()
	{
	}
}
