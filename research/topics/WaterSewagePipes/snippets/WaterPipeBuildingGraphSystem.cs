using System.Linq;
using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaterPipeBuildingGraphSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateBuildingConnectionsJob : IJobChunk
	{
		private struct BuildingNodes
		{
			public BufferedEntity m_ProducerNode;

			public BufferedEntity m_ConsumerNode;
		}

		private struct MarkerNodeData
		{
			public Entity m_NetNode;

			public int m_FreshCapacity;

			public int m_SewageCapacity;
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> m_SubNetType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterPumpingStation> m_PumpingStationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.SewageOutlet> m_SewageOutletType;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> m_ConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public ComponentLookup<Node> m_NetNodes;

		[ReadOnly]
		public ComponentLookup<Orphan> m_NetOrphans;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedNetEdges;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleted;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<WaterPipeConnectionData> m_WaterPipeConnectionDatas;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> m_WaterPipeNodeConnections;

		[ReadOnly]
		public ComponentLookup<WaterPipeValveConnection> m_WaterPipeValveConnections;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<Entity>.ParallelWriter m_UpdatedRoadEdges;

		public EntityArchetype m_NodeArchetype;

		public EntityArchetype m_EdgeArchetype;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Game.Net.SubNet> bufferAccessor = chunk.GetBufferAccessor(ref m_SubNetType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			NativeArray<WaterPipeBuildingConnection> nativeArray2 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			bool flag = chunk.Has(ref m_PumpingStationType);
			bool flag2 = chunk.Has(ref m_SewageOutletType);
			bool flag3 = chunk.Has(ref m_ConsumerType);
			bool flag4 = chunk.Has(ref m_DestroyedType);
			NativeList<MarkerNodeData> result = new NativeList<MarkerNodeData>(Allocator.Temp);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				result.Clear();
				if (bufferAccessor.Length != 0)
				{
					FindMarkerNodes(bufferAccessor[i], result);
				}
				if (bufferAccessor2.Length != 0)
				{
					FindMarkerNodes(bufferAccessor2[i], result);
				}
				if (result.Length > 0)
				{
					BuildingNodes buildingNodes;
					if (!flag4 && (flag || flag2 || flag3))
					{
						WaterPipeBuildingConnection connection = ((nativeArray2.Length != 0) ? nativeArray2[i] : default(WaterPipeBuildingConnection));
						buildingNodes = CreateOrUpdateBuildingNodes(unfilteredChunkIndex, flag || flag2, flag3, ref connection);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, connection);
					}
					else
					{
						buildingNodes = default(BuildingNodes);
						if (nativeArray2.Length != 0)
						{
							DeleteBuildingNodes(unfilteredChunkIndex, entity, nativeArray2[i]);
						}
					}
					Entity roadEdge = nativeArray3[i].m_RoadEdge;
					if (roadEdge != Entity.Null && m_WaterPipeNodeConnections.TryGetComponent(roadEdge, out var _) && flag3)
					{
						m_UpdatedRoadEdges.Enqueue(roadEdge);
					}
					foreach (MarkerNodeData item in result)
					{
						CreateOrUpdateMarkerNode(unfilteredChunkIndex, item, buildingNodes);
					}
				}
				else if (nativeArray2.Length != 0)
				{
					DeleteBuildingNodes(unfilteredChunkIndex, entity, nativeArray2[i]);
				}
			}
		}

		private void FindMarkerNodes(DynamicBuffer<InstalledUpgrade> upgrades, NativeList<MarkerNodeData> result)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && m_SubNets.TryGetBuffer(installedUpgrade.m_Upgrade, out var bufferData))
				{
					FindMarkerNodes(bufferData, result);
				}
			}
		}

		private void FindMarkerNodes(DynamicBuffer<Game.Net.SubNet> subNets, NativeList<MarkerNodeData> result)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (m_NetNodes.HasComponent(subNet) && (m_WaterPipeValveConnections.HasComponent(subNet) || IsOrphan(subNet)) && !m_Deleted.HasComponent(subNet) && m_PrefabRefs.TryGetComponent(subNet, out var componentData) && m_WaterPipeConnectionDatas.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					MarkerNodeData value = new MarkerNodeData
					{
						m_NetNode = subNet,
						m_FreshCapacity = componentData2.m_FreshCapacity,
						m_SewageCapacity = componentData2.m_SewageCapacity
					};
					result.Add(in value);
				}
			}
		}

		private bool IsOrphan(Entity netNode)
		{
			if (m_NetOrphans.HasComponent(netNode))
			{
				return true;
			}
			if (m_ConnectedNetEdges.TryGetBuffer(netNode, out var bufferData))
			{
				foreach (ConnectedEdge item in bufferData)
				{
					if (m_Owners.HasComponent(item.m_Edge))
					{
						return false;
					}
				}
			}
			return true;
		}

		private BuildingNodes CreateOrUpdateBuildingNodes(int jobIndex, bool isProducer, bool isConsumer, ref WaterPipeBuildingConnection connection)
		{
			BuildingNodes result = default(BuildingNodes);
			if (isProducer)
			{
				if (connection.m_ProducerEdge == Entity.Null)
				{
					Entity entity = m_CommandBuffer.CreateEntity(jobIndex, m_NodeArchetype);
					connection.m_ProducerEdge = CreateFlowEdge(jobIndex, m_SourceNode, entity, 0, 0);
					result.m_ProducerNode = new BufferedEntity(entity, stored: false);
				}
				else
				{
					result.m_ProducerNode = new BufferedEntity(connection.GetProducerNode(ref m_FlowEdges), stored: true);
				}
			}
			else if (connection.m_ProducerEdge != Entity.Null)
			{
				connection.m_ProducerEdge = Entity.Null;
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, connection.GetProducerNode(ref m_FlowEdges));
			}
			if (isConsumer)
			{
				if (connection.m_ConsumerEdge == Entity.Null)
				{
					Entity entity2 = m_CommandBuffer.CreateEntity(jobIndex, m_NodeArchetype);
					connection.m_ConsumerEdge = CreateFlowEdge(jobIndex, entity2, m_SinkNode, 0, 0);
					result.m_ConsumerNode = new BufferedEntity(entity2, stored: false);
				}
				else
				{
					result.m_ConsumerNode = new BufferedEntity(connection.GetConsumerNode(ref m_FlowEdges), stored: true);
				}
			}
			else if (connection.m_ConsumerEdge != Entity.Null)
			{
				connection.m_ConsumerEdge = Entity.Null;
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, connection.GetConsumerNode(ref m_FlowEdges));
			}
			if (isProducer && isConsumer)
			{
				CreateOrUpdateFlowEdge(jobIndex, result.m_ProducerNode, result.m_ConsumerNode, 1073741823, 1073741823);
			}
			return result;
		}

		private void DeleteBuildingNodes(int jobIndex, Entity building, WaterPipeBuildingConnection connection)
		{
			WaterPipeGraphUtils.DeleteBuildingNodes(m_CommandBuffer, jobIndex, connection, ref m_FlowConnections, ref m_FlowEdges);
			m_CommandBuffer.RemoveComponent<WaterPipeBuildingConnection>(jobIndex, building);
		}

		private void CreateOrUpdateMarkerNode(int jobIndex, MarkerNodeData markerNodeData, BuildingNodes buildingNodes)
		{
			WaterPipeNodeConnection componentData;
			bool flag = m_WaterPipeNodeConnections.TryGetComponent(markerNodeData.m_NetNode, out componentData);
			if (!flag)
			{
				componentData = new WaterPipeNodeConnection
				{
					m_WaterPipeNode = m_CommandBuffer.CreateEntity(jobIndex, m_NodeArchetype)
				};
				m_CommandBuffer.AddComponent(jobIndex, markerNodeData.m_NetNode, componentData);
			}
			BufferedEntity bufferedEntity = new BufferedEntity(componentData.m_WaterPipeNode, flag);
			WaterPipeValveConnection componentData2;
			bool flag2 = m_WaterPipeValveConnections.TryGetComponent(markerNodeData.m_NetNode, out componentData2);
			if (!flag2)
			{
				componentData2 = new WaterPipeValveConnection
				{
					m_ValveNode = m_CommandBuffer.CreateEntity(jobIndex, m_NodeArchetype)
				};
				m_CommandBuffer.AddComponent(jobIndex, markerNodeData.m_NetNode, componentData2);
			}
			BufferedEntity bufferedEntity2 = new BufferedEntity(componentData2.m_ValveNode, flag2);
			CreateOrUpdateFlowEdge(jobIndex, bufferedEntity2, bufferedEntity, markerNodeData.m_FreshCapacity, markerNodeData.m_SewageCapacity);
			if (buildingNodes.m_ProducerNode.m_Value != Entity.Null)
			{
				CreateOrUpdateFlowEdge(jobIndex, buildingNodes.m_ProducerNode, bufferedEntity2, markerNodeData.m_FreshCapacity, markerNodeData.m_SewageCapacity);
			}
			if (buildingNodes.m_ConsumerNode.m_Value != Entity.Null)
			{
				CreateOrUpdateFlowEdge(jobIndex, bufferedEntity2, buildingNodes.m_ConsumerNode, markerNodeData.m_FreshCapacity, markerNodeData.m_SewageCapacity);
			}
			EnsureMarkerNodeEdgeConnections(jobIndex, markerNodeData, bufferedEntity);
		}

		private void EnsureMarkerNodeEdgeConnections(int jobIndex, MarkerNodeData markerNodeData, BufferedEntity markerNode)
		{
			foreach (ConnectedEdge item in m_ConnectedNetEdges[markerNodeData.m_NetNode])
			{
				if (m_WaterPipeNodeConnections.TryGetComponent(item.m_Edge, out var componentData))
				{
					Entity waterPipeNode = componentData.m_WaterPipeNode;
					if (!markerNode.m_Stored || !WaterPipeGraphUtils.HasAnyFlowEdge(markerNode.m_Value, waterPipeNode, ref m_FlowConnections, ref m_FlowEdges))
					{
						CreateFlowEdge(jobIndex, markerNode.m_Value, componentData.m_WaterPipeNode, markerNodeData.m_FreshCapacity, markerNodeData.m_SewageCapacity);
					}
				}
			}
		}

		private void CreateOrUpdateFlowEdge(int jobIndex, BufferedEntity startNode, BufferedEntity endNode, int freshCapacity, int sewageCapacity)
		{
			if (startNode.m_Stored && endNode.m_Stored && WaterPipeGraphUtils.TryGetFlowEdge(startNode.m_Value, endNode.m_Value, ref m_FlowConnections, ref m_FlowEdges, out var entity, out var edge))
			{
				if (edge.m_FreshCapacity != freshCapacity || edge.m_SewageCapacity != sewageCapacity)
				{
					edge.m_FreshCapacity = freshCapacity;
					edge.m_SewageCapacity = sewageCapacity;
					m_CommandBuffer.SetComponent(jobIndex, entity, edge);
				}
			}
			else
			{
				CreateFlowEdge(jobIndex, startNode.m_Value, endNode.m_Value, freshCapacity, sewageCapacity);
			}
		}

		private Entity CreateFlowEdge(int jobIndex, Entity startNode, Entity endNode, int freshCapacity, int sewageCapacity)
		{
			return WaterPipeGraphUtils.CreateFlowEdge(m_CommandBuffer, jobIndex, m_EdgeArchetype, startNode, endNode, freshCapacity, sewageCapacity);
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
		public BufferTypeHandle<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterPumpingStation> __Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.SewageOutlet> __Game_Buildings_SewageOutlet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeConnectionData> __Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeValveConnection> __Game_Simulation_WaterPipeValveConnection_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubNet>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeBuildingConnection>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterPumpingStation>(isReadOnly: true);
			__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.SewageOutlet>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterConsumer>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup = state.GetComponentLookup<WaterPipeConnectionData>(isReadOnly: true);
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeValveConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeValveConnection>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
		}
	}

	private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private ModificationBarrier4B m_ModificationBarrier;

	private EntityQuery m_UpdatedBuildingQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaterPipeRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_UpdatedBuildingQuery = GetEntityQuery(CreatedUpdatedBuildingDesc(new ComponentType[1] { ComponentType.ReadOnly<Game.Buildings.WaterPumpingStation>() }), CreatedUpdatedBuildingDesc(new ComponentType[1] { ComponentType.ReadOnly<Game.Buildings.SewageOutlet>() }), CreatedUpdatedBuildingDesc(new ComponentType[1] { ComponentType.ReadOnly<WaterPipeBuildingConnection>() }));
		RequireForUpdate(m_UpdatedBuildingQuery);
		static EntityQueryDesc CreatedUpdatedBuildingDesc(ComponentType[] all)
		{
			return new EntityQueryDesc
			{
				All = all.Concat(new ComponentType[1] { ComponentType.ReadOnly<Building>() }).ToArray(),
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<Created>(),
					ComponentType.ReadOnly<Updated>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
					ComponentType.ReadOnly<Temp>()
				}
			};
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		UpdateBuildingConnectionsJob jobData = new UpdateBuildingConnectionsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PumpingStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SewageOutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetOrphans = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedNetEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_Deleted = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPipeConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPipeNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPipeValveConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeValveConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_UpdatedRoadEdges = m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps).AsParallelWriter(),
			m_NodeArchetype = m_WaterPipeFlowSystem.nodeArchetype,
			m_EdgeArchetype = m_WaterPipeFlowSystem.edgeArchetype,
			m_SourceNode = m_WaterPipeFlowSystem.sourceNode,
			m_SinkNode = m_WaterPipeFlowSystem.sinkNode
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_UpdatedBuildingQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		m_WaterPipeRoadConnectionGraphSystem.AddQueueWriter(base.Dependency);
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
	public WaterPipeBuildingGraphSystem()
	{
	}
}
