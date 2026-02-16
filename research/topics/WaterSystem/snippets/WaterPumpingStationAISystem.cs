using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
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
public class WaterPumpingStationAISystem : GameSystemBase
{
	[BurstCompile]
	public struct PumpTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

		public ComponentTypeHandle<Game.Buildings.WaterPumpingStation> m_WaterPumpingStationType;

		public ComponentTypeHandle<Game.Buildings.SewageOutlet> m_SewageOutletType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> m_PumpDatas;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		public ComponentLookup<WaterSourceData> m_WaterSources;

		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public NativeArray<GroundWater> m_GroundWaterMap;

		public IconCommandBuffer m_IconCommandBuffer;

		public WaterPipeParameterData m_Parameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<Game.Objects.SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			NativeArray<WaterPipeBuildingConnection> nativeArray4 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			BufferAccessor<IconElement> bufferAccessor3 = chunk.GetBufferAccessor(ref m_IconElementType);
			NativeArray<Game.Buildings.WaterPumpingStation> nativeArray5 = chunk.GetNativeArray(ref m_WaterPumpingStationType);
			NativeArray<Game.Buildings.SewageOutlet> nativeArray6 = chunk.GetNativeArray(ref m_SewageOutletType);
			BufferAccessor<Efficiency> bufferAccessor4 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			Span<float> factors = stackalloc float[32];
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				WaterPipeBuildingConnection waterPipeBuildingConnection = nativeArray4[i];
				DynamicBuffer<IconElement> iconElements = ((bufferAccessor3.Length != 0) ? bufferAccessor3[i] : default(DynamicBuffer<IconElement>));
				ref Game.Buildings.WaterPumpingStation reference = ref nativeArray5.ElementAt(i);
				WaterPumpingStationData data = m_PumpDatas[prefab];
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_Prefabs, ref m_PumpDatas);
				}
				if (waterPipeBuildingConnection.m_ProducerEdge == Entity.Null)
				{
					UnityEngine.Debug.LogError("WaterPumpingStation is missing producer edge!");
					continue;
				}
				if (bufferAccessor4.Length != 0)
				{
					BuildingUtils.GetEfficiencyFactors(bufferAccessor4[i], factors);
					factors[19] = 1f;
				}
				else
				{
					factors.Fill(1f);
				}
				float efficiency = BuildingUtils.GetEfficiency(factors);
				WaterPipeEdge value = m_FlowEdges[waterPipeBuildingConnection.m_ProducerEdge];
				reference.m_LastProduction = value.m_FreshFlow;
				float num = reference.m_LastProduction;
				reference.m_Pollution = 0f;
				reference.m_Capacity = 0;
				int num2 = 0;
				if (nativeArray6.Length != 0)
				{
					ref Game.Buildings.SewageOutlet reference2 = ref nativeArray6.ElementAt(i);
					num2 = reference2.m_LastPurified;
					reference2.m_UsedPurified = math.min(reference.m_LastProduction, reference2.m_LastPurified);
					num -= (float)reference2.m_UsedPurified;
				}
				float num3 = 0f;
				float num4 = 0f;
				bool flag = false;
				bool flag2 = false;
				if (data.m_Types != AllowedWaterTypes.None)
				{
					if ((data.m_Types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None)
					{
						GroundWater groundWater = GroundWaterSystem.GetGroundWater(nativeArray3[i].m_Position, m_GroundWaterMap);
						float num5 = (float)groundWater.m_Polluted / math.max(1f, groundWater.m_Amount);
						float num6 = (float)groundWater.m_Amount / m_Parameters.m_GroundwaterPumpEffectiveAmount;
						float num7 = math.clamp(num6 * (float)data.m_Capacity, 0f, (float)data.m_Capacity - num3);
						num3 += num7;
						num4 += num5 * num7;
						flag = num6 < 0.75f && (float)groundWater.m_Amount < 0.75f * (float)groundWater.m_Max;
						int num8 = (int)math.ceil(num * m_Parameters.m_GroundwaterUsageMultiplier);
						int num9 = math.min(num8, groundWater.m_Amount);
						GroundWaterSystem.ConsumeGroundWater(nativeArray3[i].m_Position, m_GroundWaterMap, num9);
						num = Mathf.FloorToInt((float)(num8 - num9) / m_Parameters.m_GroundwaterUsageMultiplier);
					}
					if ((data.m_Types & AllowedWaterTypes.SurfaceWater) != AllowedWaterTypes.None && bufferAccessor.Length != 0)
					{
						DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = bufferAccessor[i];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							Entity subObject = dynamicBuffer[j].m_SubObject;
							if (m_WaterSources.TryGetComponent(subObject, out var componentData) && m_Transforms.TryGetComponent(subObject, out var componentData2))
							{
								float surfaceWaterAvailability = GetSurfaceWaterAvailability(componentData2.m_Position, data.m_Types, m_WaterSurfaceData, m_Parameters.m_SurfaceWaterPumpEffectiveDepth);
								float num10 = WaterUtils.SamplePolluted(ref m_WaterSurfaceData, componentData2.m_Position);
								float num11 = math.clamp(surfaceWaterAvailability * (float)data.m_Capacity, 0f, (float)data.m_Capacity - num3);
								num3 += num11;
								num4 += num11 * num10;
								flag2 = surfaceWaterAvailability < 0.75f;
								componentData.m_Polluted = 0f;
								componentData.m_Height = -0.0001f * num3 * efficiency;
								m_WaterSources[subObject] = componentData;
								num = 0f;
							}
						}
					}
				}
				else
				{
					num3 = data.m_Capacity;
					num4 = 0f;
					num = 0f;
				}
				reference.m_Capacity = (int)math.round(efficiency * num3 + (float)num2);
				reference.m_Pollution = ((reference.m_Capacity > 0) ? ((1f - data.m_Purification) * num4 / (float)reference.m_Capacity) : 0f);
				value.m_FreshCapacity = reference.m_Capacity;
				value.m_FreshPollution = ((reference.m_Capacity > 0) ? reference.m_Pollution : 0f);
				m_FlowEdges[waterPipeBuildingConnection.m_ProducerEdge] = value;
				if (bufferAccessor4.Length != 0)
				{
					if (data.m_Capacity > 0)
					{
						float num12 = (num3 + (float)num2) / (float)(data.m_Capacity + num2);
						factors[19] = num12;
					}
					BuildingUtils.SetEfficiencyFactors(bufferAccessor4[i], factors);
				}
				bool flag3 = num3 < 0.1f * (float)data.m_Capacity;
				UpdateNotification(entity, m_Parameters.m_NotEnoughGroundwaterNotification, flag && flag3, iconElements);
				UpdateNotification(entity, m_Parameters.m_NotEnoughSurfaceWaterNotification, flag2 && flag3, iconElements);
				UpdateNotification(entity, m_Parameters.m_DirtyWaterPumpNotification, reference.m_Pollution > m_Parameters.m_MaxToleratedPollution, iconElements);
				bool flag4 = (value.m_Flags & WaterPipeEdgeFlags.WaterShortage) != 0;
				UpdateNotification(entity, m_Parameters.m_NotEnoughWaterCapacityNotification, reference.m_Capacity > 0 && flag4, iconElements);
			}
		}

		private void UpdateNotification(Entity entity, Entity notificationPrefab, bool enabled, DynamicBuffer<IconElement> iconElements)
		{
			bool flag = HasNotification(iconElements, notificationPrefab);
			if (enabled != flag)
			{
				if (enabled)
				{
					m_IconCommandBuffer.Add(entity, notificationPrefab);
				}
				else
				{
					m_IconCommandBuffer.Remove(entity, notificationPrefab);
				}
			}
		}

		private bool HasNotification(DynamicBuffer<IconElement> iconElements, Entity notificationPrefab)
		{
			if (iconElements.IsCreated)
			{
				foreach (IconElement item in iconElements)
				{
					if (m_Prefabs[item.m_Icon].m_Prefab == notificationPrefab)
					{
						return true;
					}
				}
			}
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

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.WaterPumpingStation> __Game_Buildings_WaterPumpingStation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Buildings.SewageOutlet> __Game_Buildings_SewageOutlet_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		public ComponentLookup<WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentLookup;

		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeBuildingConnection>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
			__Game_Buildings_WaterPumpingStation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterPumpingStation>();
			__Game_Buildings_SewageOutlet_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.SewageOutlet>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup = state.GetComponentLookup<WaterPumpingStationData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Simulation_WaterSourceData_RW_ComponentLookup = state.GetComponentLookup<WaterSourceData>();
			__Game_Simulation_WaterPipeEdge_RW_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>();
		}
	}

	private GroundWaterSystem m_GroundWaterSystem;

	private WaterSystem m_WaterSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_PumpQuery;

	private EntityQuery m_ParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_PumpQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Buildings.WaterPumpingStation>(), ComponentType.ReadOnly<WaterPipeBuildingConnection>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
		RequireForUpdate(m_PumpQuery);
		RequireForUpdate(m_ParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle dependencies;
		PumpTickJob jobData = new PumpTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_WaterPumpingStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPumpingStation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SewageOutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_SewageOutlet_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PumpDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterSources = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_GroundWaterMap = m_GroundWaterSystem.GetMap(readOnly: false, out dependencies),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_Parameters = m_ParameterQuery.GetSingleton<WaterPipeParameterData>()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_PumpQuery, JobHandle.CombineDependencies(base.Dependency, deps, dependencies));
		m_GroundWaterSystem.AddWriter(base.Dependency);
		m_WaterSystem.AddSurfaceReader(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
	}

	public static float GetSurfaceWaterAvailability(float3 position, AllowedWaterTypes allowedTypes, WaterSurfaceData<SurfaceWater> waterSurfaceData, float effectiveDepth)
	{
		return math.clamp(WaterUtils.SampleDepth(ref waterSurfaceData, position) / effectiveDepth, 0f, 1f);
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
	public WaterPumpingStationAISystem()
	{
	}
}
