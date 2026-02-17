using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AttractionSystem : GameSystemBase
{
	public enum AttractivenessFactor
	{
		Efficiency,
		Maintenance,
		Forest,
		Beach,
		Height,
		Count
	}

	[BurstCompile]
	private struct AttractivenessJob : IJobChunk
	{
		public ComponentTypeHandle<AttractivenessProvider> m_AttractivenessType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<Signature> m_SignatureType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentLookup<AttractionData> m_AttractionDatas;

		[ReadOnly]
		public ComponentLookup<ParkData> m_ParkDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public CellMapData<TerrainAttractiveness> m_TerrainMap;

		[ReadOnly]
		public TerrainHeightData m_HeightData;

		public AttractivenessParameterData m_Parameters;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<AttractivenessProvider> nativeArray2 = chunk.GetNativeArray(ref m_AttractivenessType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			NativeArray<Game.Buildings.Park> nativeArray3 = chunk.GetNativeArray(ref m_ParkType);
			NativeArray<Game.Objects.Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			bool flag = chunk.Has(ref m_SignatureType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				AttractionData data = default(AttractionData);
				if (m_AttractionDatas.HasComponent(prefab))
				{
					data = m_AttractionDatas[prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_AttractionDatas);
				}
				float num = data.m_Attractiveness;
				if (!flag)
				{
					num *= BuildingUtils.GetEfficiency(bufferAccessor, i);
				}
				if (chunk.Has(ref m_ParkType) && m_ParkDatas.HasComponent(prefab))
				{
					Game.Buildings.Park park = nativeArray3[i];
					ParkData parkData = m_ParkDatas[prefab];
					float num2 = ((parkData.m_MaintenancePool > 0) ? ((float)park.m_Maintenance / (float)parkData.m_MaintenancePool) : 1f);
					num *= 0.8f + 0.2f * num2;
				}
				if (chunk.Has(ref m_TransformType))
				{
					float3 position = nativeArray4[i].m_Position;
					num *= 1f + 0.01f * TerrainAttractivenessSystem.EvaluateAttractiveness(position, m_TerrainMap, m_HeightData, m_Parameters, default(NativeArray<int>));
				}
				AttractivenessProvider value = new AttractivenessProvider
				{
					m_Attractiveness = Mathf.RoundToInt(num)
				};
				nativeArray2[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Signature> __Game_Buildings_Signature_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<AttractionData> __Game_Prefabs_AttractionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_Signature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Signature>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_AttractionData_RO_ComponentLookup = state.GetComponentLookup<AttractionData>(isReadOnly: true);
			__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_BuildingGroup;

	private EntityQuery m_SettingsQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public static void SetFactor(NativeArray<int> factors, AttractivenessFactor factor, float attractiveness)
	{
		if (factors.IsCreated && factors.Length == 5)
		{
			factors[(int)factor] = Mathf.RoundToInt(attractiveness);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
		m_BuildingGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadWrite<AttractivenessProvider>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		JobHandle dependencies;
		AttractivenessJob jobData = new AttractivenessJob
		{
			m_AttractivenessType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SignatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Signature_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_AttractionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AttractionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainMap = m_TerrainAttractivenessSystem.GetData(readOnly: true, out dependencies),
			m_HeightData = m_TerrainSystem.GetHeightData(),
			m_Parameters = m_SettingsQuery.GetSingleton<AttractivenessParameterData>(),
			m_UpdateFrameIndex = updateFrameWithInterval
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingGroup, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		m_TerrainAttractivenessSystem.AddReader(base.Dependency);
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
	public AttractionSystem()
	{
	}
}
