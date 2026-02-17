using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DispatchWaterSystem : GameSystemBase
{
	[BurstCompile]
	private struct DispatchWaterJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> m_BuildingConnectionType;

		public ComponentTypeHandle<WaterConsumer> m_ConsumerType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> m_NodeConnections;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		public IconCommandBuffer m_IconCommandBuffer;

		public WaterPipeParameterData m_Parameters;

		public BuildingEfficiencyParameterData m_EfficiencyParameters;

		public Entity m_SinkNode;

		public RandomSeed m_RandomSeed;

		public bool m_FreshConsumptionDisabled;

		public bool m_SewageConsumptionDisabled;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Building> nativeArray2 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<WaterPipeBuildingConnection> nativeArray3 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			NativeArray<WaterConsumer> nativeArray4 = chunk.GetNativeArray(ref m_ConsumerType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref WaterConsumer reference = ref nativeArray4.ElementAt(i);
				int num = 0;
				int fulfilledSewage = 0;
				float num2 = 0f;
				WaterPipeEdgeFlags waterPipeEdgeFlags = WaterPipeEdgeFlags.None;
				if (nativeArray3.Length != 0)
				{
					if (nativeArray3[i].m_ConsumerEdge != Entity.Null)
					{
						WaterPipeEdge waterPipeEdge = m_FlowEdges[nativeArray3[i].m_ConsumerEdge];
						num = waterPipeEdge.m_FreshFlow;
						fulfilledSewage = waterPipeEdge.m_SewageFlow;
						num2 = waterPipeEdge.m_FreshPollution;
						waterPipeEdgeFlags = waterPipeEdge.m_Flags;
					}
					else
					{
						UnityEngine.Debug.LogError("WaterBuildingConnection is missing consumer edge!");
					}
				}
				else
				{
					Entity roadEdge = nativeArray2[i].m_RoadEdge;
					if (roadEdge != Entity.Null && m_NodeConnections.TryGetComponent(roadEdge, out var componentData) && WaterPipeGraphUtils.TryGetFlowEdge(componentData.m_WaterPipeNode, m_SinkNode, ref m_FlowConnections, ref m_FlowEdges, out WaterPipeEdge edge))
					{
						if (edge.m_FreshCapacity == edge.m_FreshFlow)
						{
							num = reference.m_WantedConsumption;
						}
						else if (edge.m_FreshCapacity > 0)
						{
							float num3 = (float)edge.m_FreshFlow / (float)edge.m_FreshCapacity;
							num = (int)math.floor((float)reference.m_WantedConsumption * num3);
						}
						if (edge.m_SewageCapacity == edge.m_SewageFlow)
						{
							fulfilledSewage = reference.m_WantedConsumption;
						}
						else if (edge.m_SewageCapacity > 0)
						{
							float num4 = (float)edge.m_SewageFlow / (float)edge.m_SewageCapacity;
							fulfilledSewage = (int)math.floor((float)reference.m_WantedConsumption * num4);
						}
						num2 = edge.m_FreshPollution;
						waterPipeEdgeFlags = edge.m_Flags;
					}
				}
				if (m_FreshConsumptionDisabled)
				{
					num = reference.m_WantedConsumption;
					waterPipeEdgeFlags &= ~(WaterPipeEdgeFlags.WaterShortage | WaterPipeEdgeFlags.WaterDisconnected);
				}
				if (m_SewageConsumptionDisabled)
				{
					fulfilledSewage = reference.m_WantedConsumption;
					waterPipeEdgeFlags &= ~(WaterPipeEdgeFlags.SewageBackup | WaterPipeEdgeFlags.SewageDisconnected);
				}
				reference.m_FulfilledFresh = num;
				reference.m_FulfilledSewage = fulfilledSewage;
				bool flag = reference.m_FulfilledFresh < reference.m_WantedConsumption;
				bool flag2 = reference.m_FulfilledSewage < reference.m_WantedConsumption;
				HandleCooldown(nativeArray[i], m_Parameters.m_WaterNotification, flag, ref reference.m_FreshCooldownCounter, ref random);
				HandleCooldown(nativeArray[i], m_Parameters.m_SewageNotification, flag2, ref reference.m_SewageCooldownCounter, ref random);
				bool flag3 = reference.m_Pollution > m_Parameters.m_MaxToleratedPollution;
				reference.m_Pollution = ((num > 0) ? num2 : 0f);
				if (reference.m_WantedConsumption == 0)
				{
					flag = (waterPipeEdgeFlags & (WaterPipeEdgeFlags.WaterShortage | WaterPipeEdgeFlags.WaterDisconnected)) != 0;
					flag2 = (waterPipeEdgeFlags & (WaterPipeEdgeFlags.SewageBackup | WaterPipeEdgeFlags.SewageDisconnected)) != 0;
				}
				reference.m_Flags = WaterConsumerFlags.None;
				if (!flag)
				{
					reference.m_Flags |= WaterConsumerFlags.WaterConnected;
				}
				if (!flag2)
				{
					reference.m_Flags |= WaterConsumerFlags.SewageConnected;
				}
				if (reference.m_Pollution > m_Parameters.m_MaxToleratedPollution)
				{
					if (!flag3)
					{
						m_IconCommandBuffer.Add(nativeArray[i], m_Parameters.m_DirtyWaterNotification, IconPriority.Problem);
					}
				}
				else if (flag3)
				{
					m_IconCommandBuffer.Remove(nativeArray[i], m_Parameters.m_DirtyWaterNotification);
				}
				if (bufferAccessor.Length != 0)
				{
					float efficiency = 1f - m_EfficiencyParameters.m_WaterPenalty * math.saturate((float)(int)reference.m_FreshCooldownCounter / m_EfficiencyParameters.m_WaterPenaltyDelay);
					BuildingUtils.SetEfficiencyFactor(bufferAccessor[i], EfficiencyFactor.WaterSupply, efficiency);
					float efficiency2 = 1f - m_EfficiencyParameters.m_WaterPollutionPenalty * math.round(reference.m_Pollution * 100f) / 100f;
					BuildingUtils.SetEfficiencyFactor(bufferAccessor[i], EfficiencyFactor.DirtyWater, efficiency2);
					float efficiency3 = 1f - m_EfficiencyParameters.m_SewagePenalty * math.saturate((float)(int)reference.m_SewageCooldownCounter / m_EfficiencyParameters.m_SewagePenaltyDelay);
					BuildingUtils.SetEfficiencyFactor(bufferAccessor[i], EfficiencyFactor.SewageHandling, efficiency3);
				}
			}
		}

		private void HandleCooldown(Entity building, Entity notificationPrefab, bool enabled, ref byte cooldown, ref Unity.Mathematics.Random random)
		{
			bool flag = cooldown >= kAlertCooldown;
			if (enabled)
			{
				if (cooldown < byte.MaxValue)
				{
					cooldown++;
				}
				if (!flag && cooldown >= kAlertCooldown)
				{
					m_IconCommandBuffer.Add(building, notificationPrefab, IconPriority.Problem);
				}
			}
			else
			{
				cooldown = 0;
				if (flag)
				{
					m_IconCommandBuffer.Remove(building, notificationPrefab);
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
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle;

		public ComponentTypeHandle<WaterConsumer> __Game_Buildings_WaterConsumer_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeBuildingConnection>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterConsumer>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
		}
	}

	public static readonly short kAlertCooldown = 2;

	public static readonly short kHealthPenaltyCooldown = 10;

	private const float kNotificationMaxDelay = 2f;

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_ConsumerQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1010455350_0;

	private EntityQuery __query_1010455350_1;

	public bool freshConsumptionDisabled { get; set; }

	public bool sewageConsumptionDisabled { get; set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 62;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_ConsumerQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadWrite<WaterConsumer>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_ConsumerQuery);
		RequireForUpdate<WaterPipeParameterData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_WaterPipeFlowSystem.ready)
		{
			DispatchWaterJob jobData = new DispatchWaterJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterConsumer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_NodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
				m_Parameters = __query_1010455350_0.GetSingleton<WaterPipeParameterData>(),
				m_EfficiencyParameters = __query_1010455350_1.GetSingleton<BuildingEfficiencyParameterData>(),
				m_SinkNode = m_WaterPipeFlowSystem.sinkNode,
				m_RandomSeed = RandomSeed.Next(),
				m_FreshConsumptionDisabled = freshConsumptionDisabled,
				m_SewageConsumptionDisabled = sewageConsumptionDisabled
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ConsumerQuery, base.Dependency);
			m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<WaterPipeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1010455350_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1010455350_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public DispatchWaterSystem()
	{
	}
}
