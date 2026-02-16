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
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class SewageOutletAISystem : GameSystemBase
{
	[BurstCompile]
	public struct OutletTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<Game.Buildings.SewageOutlet> m_SewageOutletType;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> m_OutletDatas;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> m_SewageOutletDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<WaterSourceData> m_WaterSources;

		public IconCommandBuffer m_IconCommandBuffer;

		public WaterPipeParameterData m_Parameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<WaterPipeBuildingConnection> nativeArray3 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Game.Objects.SubObject> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<IconElement> bufferAccessor4 = chunk.GetBufferAccessor(ref m_IconElementType);
			NativeArray<Game.Buildings.SewageOutlet> nativeArray4 = chunk.GetNativeArray(ref m_SewageOutletType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				WaterPipeBuildingConnection waterPipeBuildingConnection = nativeArray3[i];
				DynamicBuffer<IconElement> iconElements = ((bufferAccessor4.Length != 0) ? bufferAccessor4[i] : default(DynamicBuffer<IconElement>));
				ref Game.Buildings.SewageOutlet reference = ref nativeArray4.ElementAt(i);
				if (waterPipeBuildingConnection.m_ProducerEdge == Entity.Null)
				{
					UnityEngine.Debug.LogError("SewageOutlet is missing producer edge!");
					continue;
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				SewageOutletData data = m_OutletDatas[prefab];
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_Prefabs, ref m_SewageOutletDatas);
				}
				int num = math.max(0, reference.m_LastProcessed - reference.m_LastPurified);
				int num2 = reference.m_LastPurified - reference.m_UsedPurified;
				int num3 = num + num2;
				if (bufferAccessor3.Length != 0)
				{
					DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = bufferAccessor3[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subObject = dynamicBuffer[j].m_SubObject;
						if (m_WaterSources.TryGetComponent(subObject, out var componentData))
						{
							componentData.m_Height = math.min(2.5f, m_Parameters.m_SurfaceWaterUsageMultiplier * (float)num3);
							if ((float)num3 == 0f)
							{
								componentData.m_Polluted = 0f;
								componentData.m_Modifier = 0f;
							}
							else
							{
								componentData.m_Polluted = (float)num / (float)num3;
								componentData.m_Modifier = 1f;
							}
							m_WaterSources[subObject] = componentData;
						}
					}
				}
				reference.m_Capacity = Mathf.RoundToInt(efficiency * (float)data.m_Capacity);
				WaterPipeEdge value = m_FlowEdges[waterPipeBuildingConnection.m_ProducerEdge];
				reference.m_LastProcessed = value.m_SewageFlow;
				reference.m_LastPurified = Mathf.RoundToInt(data.m_Purification * (float)reference.m_LastProcessed);
				reference.m_UsedPurified = 0;
				value.m_SewageCapacity = reference.m_Capacity;
				m_FlowEdges[waterPipeBuildingConnection.m_ProducerEdge] = value;
				bool flag = (value.m_Flags & WaterPipeEdgeFlags.SewageBackup) != 0;
				UpdateNotification(entity, m_Parameters.m_NotEnoughSewageCapacityNotification, reference.m_Capacity > 0 && flag, iconElements);
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
		public ComponentTypeHandle<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.SewageOutlet> __Game_Buildings_SewageOutlet_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RW_ComponentLookup;

		public ComponentLookup<WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeBuildingConnection>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
			__Game_Buildings_SewageOutlet_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.SewageOutlet>();
			__Game_Prefabs_SewageOutletData_RO_ComponentLookup = state.GetComponentLookup<SewageOutletData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RW_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>();
			__Game_Simulation_WaterSourceData_RW_ComponentLookup = state.GetComponentLookup<WaterSourceData>();
		}
	}

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_OutletQuery;

	private EntityQuery m_ParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 64;
	}

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_OutletQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Buildings.SewageOutlet>(), ComponentType.ReadOnly<WaterPipeBuildingConnection>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
		RequireForUpdate(m_OutletQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		OutletTickJob jobData = new OutletTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SewageOutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_SewageOutlet_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OutletDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SewageOutletDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WaterSources = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_Parameters = m_ParameterQuery.GetSingleton<WaterPipeParameterData>()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_OutletQuery, base.Dependency);
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
	public SewageOutletAISystem()
	{
	}
}
