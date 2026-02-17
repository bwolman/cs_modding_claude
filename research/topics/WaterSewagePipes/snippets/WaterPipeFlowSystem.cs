#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Serialization;
using Game.Simulation.Flow;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaterPipeFlowSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
	private enum Phase
	{
		Prepare,
		Flow,
		Apply
	}

	[BurstCompile]
	private struct PrepareNetworkJob : IJob
	{
		public NativeList<Game.Simulation.Flow.Node> m_FreshNodes;

		public NativeList<Game.Simulation.Flow.Node> m_SewageNodes;

		public NativeList<Game.Simulation.Flow.Edge> m_FreshEdges;

		public NativeList<Game.Simulation.Flow.Edge> m_SewageEdges;

		public NativeList<Connection> m_Connections;

		public NativeList<int> m_TradeNodes;

		public int m_NodeCount;

		public int m_EdgeCount;

		public void Execute()
		{
			m_FreshNodes.ResizeUninitialized(m_NodeCount + 1);
			m_SewageNodes.ResizeUninitialized(m_NodeCount + 1);
			ref NativeList<Game.Simulation.Flow.Node> reference = ref m_FreshNodes;
			Game.Simulation.Flow.Node value = (m_SewageNodes[0] = default(Game.Simulation.Flow.Node));
			reference[0] = value;
			m_FreshEdges.ResizeUninitialized(m_EdgeCount + 1);
			m_SewageEdges.ResizeUninitialized(m_EdgeCount + 1);
			ref NativeList<Game.Simulation.Flow.Edge> reference2 = ref m_FreshEdges;
			Game.Simulation.Flow.Edge value2 = (m_SewageEdges[0] = default(Game.Simulation.Flow.Edge));
			reference2[0] = value2;
			m_Connections.Clear();
			m_Connections.Capacity = 2 * m_EdgeCount + 1;
			m_Connections.Add(default(Connection));
			m_TradeNodes.Clear();
		}
	}

	[BurstCompile]
	private struct PrepareNodesJob : IJobChunk
	{
		public ComponentTypeHandle<WaterPipeNode> m_FlowNodeType;

		public int m_MaxChunkCapacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = unfilteredChunkIndex * m_MaxChunkCapacity;
			NativeArray<WaterPipeNode> nativeArray = chunk.GetNativeArray(ref m_FlowNodeType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				nativeArray.ElementAt(i).m_Index = num + i + 1;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PrepareEdgesJob : IJobChunk
	{
		public ComponentTypeHandle<WaterPipeEdge> m_FlowEdgeType;

		[NativeDisableParallelForRestriction]
		public NativeArray<Game.Simulation.Flow.Edge> m_FreshEdges;

		[NativeDisableParallelForRestriction]
		public NativeArray<Game.Simulation.Flow.Edge> m_SewageEdges;

		public int m_MaxChunkCapacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = unfilteredChunkIndex * m_MaxChunkCapacity;
			NativeArray<WaterPipeEdge> nativeArray = chunk.GetNativeArray(ref m_FlowEdgeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref WaterPipeEdge reference = ref nativeArray.ElementAt(i);
				int index = num + i + 1;
				m_FreshEdges[index] = new Game.Simulation.Flow.Edge
				{
					m_Capacity = reference.m_FreshCapacity,
					m_Direction = FlowDirection.Both
				};
				m_SewageEdges[index] = new Game.Simulation.Flow.Edge
				{
					m_Capacity = reference.m_SewageCapacity,
					m_Direction = FlowDirection.Both
				};
				reference.m_Index = index;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PrepareConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> m_ConnectedFlowEdgeType;

		[ReadOnly]
		public ComponentTypeHandle<TradeNode> m_TradeNodeType;

		[ReadOnly]
		public ComponentLookup<WaterPipeNode> m_FlowNodes;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		public NativeArray<Game.Simulation.Flow.Node> m_FreshNodes;

		public NativeArray<Game.Simulation.Flow.Node> m_SewageNodes;

		public NativeList<Connection> m_Connections;

		public NativeList<int> m_TradeNodes;

		public int m_MaxChunkCapacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = unfilteredChunkIndex * m_MaxChunkCapacity;
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<ConnectedFlowEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedFlowEdgeType);
			bool flag = chunk.Has(ref m_TradeNodeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<ConnectedFlowEdge> dynamicBuffer = bufferAccessor[i];
				int value = num + i + 1;
				ref Game.Simulation.Flow.Node reference = ref m_FreshNodes.ElementAt(value);
				ref Game.Simulation.Flow.Node reference2 = ref m_SewageNodes.ElementAt(value);
				reference.m_FirstConnection = (reference2.m_FirstConnection = m_Connections.Length);
				reference.m_LastConnection = (reference2.m_LastConnection = m_Connections.Length + dynamicBuffer.Length);
				foreach (ConnectedFlowEdge item in dynamicBuffer)
				{
					WaterPipeEdge waterPipeEdge = m_FlowEdges[item.m_Edge];
					bool flag2 = waterPipeEdge.m_End == entity;
					WaterPipeNode waterPipeNode = m_FlowNodes[flag2 ? waterPipeEdge.m_Start : waterPipeEdge.m_End];
					m_Connections.Add(new Connection
					{
						m_StartNode = value,
						m_EndNode = waterPipeNode.m_Index,
						m_Edge = waterPipeEdge.m_Index,
						m_Backwards = flag2
					});
				}
				if (flag)
				{
					m_TradeNodes.Add(in value);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PopulateNodeIndicesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<WaterPipeNode> m_FlowNodes;

		public NativeReference<NodeIndices> m_NodeIndices;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public void Execute()
		{
			m_NodeIndices.Value = new NodeIndices
			{
				m_SourceNode = m_FlowNodes[m_SourceNode].m_Index,
				m_SinkNode = m_FlowNodes[m_SinkNode].m_Index
			};
		}
	}

	[BurstCompile]
	private struct ApplyEdgesJob : IJobChunk
	{
		public ComponentTypeHandle<WaterPipeEdge> m_FlowEdgeType;

		[ReadOnly]
		public NativeArray<Game.Simulation.Flow.Edge> m_FreshEdges;

		[ReadOnly]
		public NativeArray<Game.Simulation.Flow.Edge> m_SewageEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WaterPipeEdge> nativeArray = chunk.GetNativeArray(ref m_FlowEdgeType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ref WaterPipeEdge reference = ref nativeArray.ElementAt(i);
				Game.Simulation.Flow.Edge edge = m_FreshEdges[reference.m_Index];
				Game.Simulation.Flow.Edge edge2 = m_SewageEdges[reference.m_Index];
				reference.m_FreshFlow = edge.m_FinalFlow;
				reference.m_SewageFlow = edge2.m_FinalFlow;
				reference.m_Flags = WaterPipeEdgeFlags.None;
				if (edge.m_CutElementId.m_Version == -1)
				{
					reference.m_Flags |= WaterPipeEdgeFlags.WaterShortage;
				}
				else if (edge.m_CutElementId.m_Version != -2)
				{
					reference.m_Flags |= WaterPipeEdgeFlags.WaterDisconnected;
				}
				if (edge2.m_CutElementId.m_Version == -1)
				{
					reference.m_Flags |= WaterPipeEdgeFlags.SewageBackup;
				}
				else if (edge2.m_CutElementId.m_Version != -2)
				{
					reference.m_Flags |= WaterPipeEdgeFlags.SewageDisconnected;
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
		public ComponentTypeHandle<WaterPipeNode> __Game_Simulation_WaterPipeNode_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TradeNode> __Game_Simulation_TradeNode_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeNode> __Game_Simulation_WaterPipeNode_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_WaterPipeNode_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeNode>();
			__Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeEdge>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_TradeNode_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TradeNode>(isReadOnly: true);
			__Game_Simulation_WaterPipeNode_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNode>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
		}
	}

	public const int kUpdateInterval = 128;

	public const int kUpdateOffset = 64;

	public const int kUpdatesPerDay = 2048;

	public const int kStartFrames = 2;

	public const int kAdjustFrame = 64;

	public const int kPrepareFrame = 65;

	public const int kFlowFrames = 124;

	public const int kFlowCompletionFrame = 61;

	public const int kEndFrames = 2;

	public const int kApplyFrame = 62;

	public const int kStatusFrame = 63;

	public const int kMaxEdgeCapacity = 1073741823;

	private const int kLayerHeight = 20;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_NodeGroup;

	private EntityQuery m_EdgeGroup;

	private EntityArchetype m_NodeArchetype;

	private EntityArchetype m_EdgeArchetype;

	private WaterPipeFlowJob.Data m_FreshData;

	private WaterPipeFlowJob.Data m_SewageData;

	private NativeList<Connection> m_Connections;

	private NativeReference<NodeIndices> m_NodeIndices;

	private NativeList<int> m_TradeNodes;

	private Entity m_SourceNode;

	private Entity m_SinkNode;

	private Phase m_NextPhase;

	private JobHandle m_DataDependency;

	private TypeHandle __TypeHandle;

	public bool ready { get; private set; }

	public EntityArchetype nodeArchetype => m_NodeArchetype;

	public EntityArchetype edgeArchetype => m_EdgeArchetype;

	public Entity sourceNode => m_SourceNode;

	public Entity sinkNode => m_SinkNode;

	public bool fluidFlowEnabled { get; set; } = true;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_NodeGroup = GetEntityQuery(ComponentType.ReadWrite<WaterPipeNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>(), ComponentType.Exclude<Deleted>());
		m_EdgeGroup = GetEntityQuery(ComponentType.ReadWrite<WaterPipeEdge>(), ComponentType.Exclude<Deleted>());
		m_NodeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<WaterPipeNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>());
		m_EdgeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<WaterPipeEdge>());
		m_FreshData = new WaterPipeFlowJob.Data(200000, Allocator.Persistent);
		m_SewageData = new WaterPipeFlowJob.Data(200000, Allocator.Persistent);
		m_Connections = new NativeList<Connection>(Allocator.Persistent);
		m_NodeIndices = new NativeReference<NodeIndices>(Allocator.Persistent);
		m_TradeNodes = new NativeList<int>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DataDependency.Complete();
		m_FreshData.Dispose();
		m_SewageData.Dispose();
		m_Connections.Dispose();
		m_NodeIndices.Dispose();
		m_TradeNodes.Dispose();
		base.OnDestroy();
	}

	public void Reset()
	{
		m_DataDependency.Complete();
		m_NextPhase = Phase.Prepare;
		m_FreshData.m_State.Value = new WaterPipeFlowJob.State(200000);
		m_SewageData.m_State.Value = new WaterPipeFlowJob.State(200000);
		ready = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_NextPhase == Phase.Prepare)
		{
			PreparePhase();
		}
		else if (m_NextPhase == Phase.Flow)
		{
			FlowPhase();
		}
		else if (m_NextPhase == Phase.Apply)
		{
			ApplyPhase();
		}
	}

	private void PreparePhase()
	{
		if (m_SimulationSystem.frameIndex % 128 == 65)
		{
			int chunkCapacity = m_NodeArchetype.ChunkCapacity;
			int chunkCapacity2 = m_EdgeArchetype.ChunkCapacity;
			int nodeCount = chunkCapacity * m_NodeGroup.CalculateChunkCountWithoutFiltering();
			int edgeCount = chunkCapacity2 * m_EdgeGroup.CalculateChunkCountWithoutFiltering();
			JobHandle dependsOn = IJobExtensions.Schedule(new PrepareNetworkJob
			{
				m_FreshNodes = m_FreshData.m_Nodes,
				m_SewageNodes = m_SewageData.m_Nodes,
				m_FreshEdges = m_FreshData.m_Edges,
				m_SewageEdges = m_SewageData.m_Edges,
				m_Connections = m_Connections,
				m_TradeNodes = m_TradeNodes,
				m_NodeCount = nodeCount,
				m_EdgeCount = edgeCount
			}, JobHandle.CombineDependencies(base.Dependency, m_DataDependency));
			JobHandle job = JobChunkExtensions.ScheduleParallel(new PrepareNodesJob
			{
				m_FlowNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeNode_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MaxChunkCapacity = chunkCapacity
			}, m_NodeGroup, base.Dependency);
			JobHandle job2 = JobChunkExtensions.ScheduleParallel(new PrepareEdgesJob
			{
				m_FlowEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FreshEdges = m_FreshData.m_Edges.AsDeferredJobArray(),
				m_SewageEdges = m_SewageData.m_Edges.AsDeferredJobArray(),
				m_MaxChunkCapacity = chunkCapacity2
			}, m_EdgeGroup, dependsOn);
			JobHandle job3 = JobChunkExtensions.Schedule(new PrepareConnectionsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ConnectedFlowEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TradeNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_TradeNode_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FlowNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNode_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FreshNodes = m_FreshData.m_Nodes.AsDeferredJobArray(),
				m_SewageNodes = m_SewageData.m_Nodes.AsDeferredJobArray(),
				m_Connections = m_Connections,
				m_TradeNodes = m_TradeNodes,
				m_MaxChunkCapacity = chunkCapacity
			}, m_NodeGroup, JobHandle.CombineDependencies(job, job2));
			JobHandle job4 = IJobExtensions.Schedule(new PopulateNodeIndicesJob
			{
				m_FlowNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNode_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeIndices = m_NodeIndices,
				m_SourceNode = m_SourceNode,
				m_SinkNode = m_SinkNode
			}, JobHandle.CombineDependencies(job, m_DataDependency));
			base.Dependency = (m_DataDependency = JobHandle.CombineDependencies(job3, job4));
			m_NextPhase = Phase.Flow;
		}
	}

	private void FlowPhase()
	{
		uint num = m_SimulationSystem.frameIndex % 128;
		Assert.IsTrue(num != 64 && num != 65 && num != 62 && num != 63);
		bool flag = num == 61;
		JobHandle job = ScheduleFlowJob(m_FreshData, 1073741823, 1073741823, flag);
		JobHandle job2 = ScheduleFlowJob(m_SewageData, 1073741823, 0, flag);
		m_DataDependency = JobHandle.CombineDependencies(job, job2);
		if (flag)
		{
			m_NextPhase = Phase.Apply;
		}
	}

	private JobHandle ScheduleFlowJob(WaterPipeFlowJob.Data jobData, int importCapacity, int exportCapacity, bool finalFrame)
	{
		return IJobExtensions.Schedule(new WaterPipeFlowJob
		{
			m_State = jobData.m_State,
			m_Nodes = jobData.m_Nodes.AsDeferredJobArray(),
			m_Edges = jobData.m_Edges.AsDeferredJobArray(),
			m_Connections = m_Connections.AsDeferredJobArray(),
			m_NodeIndices = m_NodeIndices,
			m_TradeNodes = m_TradeNodes.AsDeferredJobArray(),
			m_MaxFlowState = jobData.m_MaxFlowState,
			m_LayerStates = jobData.m_LayerStates,
			m_LayerElements = jobData.m_LayerElements,
			m_LayerElementRefs = jobData.m_LayerElementRefs,
			m_FluidFlowState = jobData.m_FluidFlowState,
			m_ImportCapacity = importCapacity,
			m_ExportCapacity = exportCapacity,
			m_FluidFlowEnabled = fluidFlowEnabled,
			m_LayerHeight = 20,
			m_FrameCount = 1,
			m_FinalFrame = finalFrame
		}, m_DataDependency);
	}

	private void ApplyPhase()
	{
		if (m_SimulationSystem.frameIndex % 128 == 62)
		{
			JobHandle dataDependency = JobChunkExtensions.ScheduleParallel(new ApplyEdgesJob
			{
				m_FlowEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FreshEdges = m_FreshData.m_Edges.AsDeferredJobArray(),
				m_SewageEdges = m_SewageData.m_Edges.AsDeferredJobArray()
			}, m_EdgeGroup, JobHandle.CombineDependencies(base.Dependency, m_DataDependency));
			base.Dependency = (m_DataDependency = dataDependency);
			m_NextPhase = Phase.Prepare;
			ready = true;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		m_DataDependency.Complete();
		Entity value = m_SourceNode;
		writer.Write(value);
		Entity value2 = m_SinkNode;
		writer.Write(value2);
		int lastTotalSteps = m_FreshData.m_State.Value.m_LastTotalSteps;
		writer.Write(lastTotalSteps);
		int lastTotalSteps2 = m_SewageData.m_State.Value.m_LastTotalSteps;
		writer.Write(lastTotalSteps2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_DataDependency.Complete();
		if (reader.context.version >= Version.waterPipeFlowSim)
		{
			ref Entity value = ref m_SourceNode;
			reader.Read(out value);
			ref Entity value2 = ref m_SinkNode;
			reader.Read(out value2);
		}
		else
		{
			if (reader.context.version >= Version.waterTrade)
			{
				reader.Read(out Entity _);
			}
			m_SourceNode = Entity.Null;
			m_SinkNode = Entity.Null;
		}
		if (reader.context.version > Version.flowJobImprovements)
		{
			ref int lastTotalSteps = ref m_FreshData.m_State.ValueAsRef().m_LastTotalSteps;
			reader.Read(out lastTotalSteps);
			ref int lastTotalSteps2 = ref m_SewageData.m_State.ValueAsRef().m_LastTotalSteps;
			reader.Read(out lastTotalSteps2);
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_SourceNode = Entity.Null;
		m_SinkNode = Entity.Null;
	}

	public void PostDeserialize(Context context)
	{
		if (m_SourceNode == Entity.Null && m_SinkNode == Entity.Null)
		{
			m_SourceNode = base.EntityManager.CreateEntity(m_NodeArchetype);
			m_SinkNode = base.EntityManager.CreateEntity(m_NodeArchetype);
		}
		Reset();
		DispatchWaterSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<DispatchWaterSystem>();
		if (context.version < Version.waterPipeFlowSim && context.purpose == Purpose.LoadGame)
		{
			UnityEngine.Debug.LogWarning("Detected legacy water pipes, disabling water & sewage notifications!");
			orCreateSystemManaged.freshConsumptionDisabled = true;
			orCreateSystemManaged.sewageConsumptionDisabled = true;
		}
		else
		{
			orCreateSystemManaged.freshConsumptionDisabled = false;
			orCreateSystemManaged.sewageConsumptionDisabled = false;
		}
		if (!(context.version < Version.waterPipePollution))
		{
			return;
		}
		EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WaterPipeNodeConnection>());
		try
		{
			NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.Temp);
			NativeArray<WaterPipeNodeConnection> nativeArray2 = entityQuery.ToComponentDataArray<WaterPipeNodeConnection>(Allocator.Temp);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				if (nativeArray2[i].m_WaterPipeNode == Entity.Null)
				{
					COSystemBase.baseLog.WarnFormat("{0} has null WaterPipeNode! Removing...", nativeArray[i]);
					base.EntityManager.RemoveComponent<WaterPipeNodeConnection>(nativeArray[i]);
				}
			}
		}
		finally
		{
			entityQuery.Dispose();
		}
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
	public WaterPipeFlowSystem()
	{
	}
}
