using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Prefabs;
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
public class WaterPipePollutionSystem : GameSystemBase
{
	[BurstCompile]
	public struct NodePollutionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> m_FlowConnectionType;

		public ComponentTypeHandle<WaterPipeNode> m_NodeType;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		public float m_StaleWaterPipePurification;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<ConnectedFlowEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_FlowConnectionType);
			NativeArray<WaterPipeNode> nativeArray2 = chunk.GetNativeArray(ref m_NodeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				ref WaterPipeNode reference = ref nativeArray2.ElementAt(i);
				DynamicBuffer<ConnectedFlowEdge> dynamicBuffer = bufferAccessor[i];
				int num = 0;
				float num2 = 0f;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					WaterPipeEdge waterPipeEdge = m_FlowEdges[dynamicBuffer[j].m_Edge];
					int num3 = ((waterPipeEdge.m_Start == entity) ? (-waterPipeEdge.m_FreshFlow) : waterPipeEdge.m_FreshFlow);
					if (num3 > 0)
					{
						num += num3;
						num2 += waterPipeEdge.m_FreshPollution * (float)num3;
					}
				}
				if (num > 0)
				{
					reference.m_FreshPollution = num2 / (float)num;
				}
				else
				{
					reference.m_FreshPollution = math.max(0f, reference.m_FreshPollution - m_StaleWaterPipePurification);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	public struct EdgePollutionJob : IJobChunk
	{
		public ComponentTypeHandle<WaterPipeEdge> m_FlowEdgeType;

		[ReadOnly]
		public ComponentLookup<WaterPipeNode> m_FlowNodes;

		public Entity m_SourceNode;

		public bool m_Purify;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WaterPipeEdge> nativeArray = chunk.GetNativeArray(ref m_FlowEdgeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref WaterPipeEdge reference = ref nativeArray.ElementAt(i);
				WaterPipeNode waterPipeNode = m_FlowNodes[reference.m_Start];
				WaterPipeNode waterPipeNode2 = m_FlowNodes[reference.m_End];
				if (reference.m_Start != m_SourceNode)
				{
					float num = ((reference.m_FreshFlow > 0) ? waterPipeNode.m_FreshPollution : ((reference.m_FreshFlow >= 0) ? ((waterPipeNode.m_FreshPollution + waterPipeNode2.m_FreshPollution) / 2f) : waterPipeNode2.m_FreshPollution));
					if (!m_Purify || num == 0f)
					{
						reference.m_FreshPollution = num;
					}
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
		public BufferTypeHandle<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle;

		public ComponentTypeHandle<WaterPipeNode> __Game_Simulation_WaterPipeNode_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		public ComponentTypeHandle<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeNode> __Game_Simulation_WaterPipeNode_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeNode_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeNode>();
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeEdge>();
			__Game_Simulation_WaterPipeNode_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNode>(isReadOnly: true);
		}
	}

	private const int kUpdateInterval = 64;

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_NodeQuery;

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_ParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_NodeQuery = GetEntityQuery(ComponentType.ReadWrite<WaterPipeNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>(), ComponentType.Exclude<Deleted>());
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadWrite<WaterPipeEdge>(), ComponentType.Exclude<Deleted>());
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
		RequireForUpdate(m_NodeQuery);
		RequireForUpdate(m_EdgeQuery);
		RequireForUpdate(m_ParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		WaterPipeParameterData singleton = m_ParameterQuery.GetSingleton<WaterPipeParameterData>();
		bool purify = m_SimulationSystem.frameIndex / 64 % singleton.m_WaterPipePollutionSpreadInterval != 0;
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new NodePollutionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_FlowConnectionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeNode_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StaleWaterPipePurification = singleton.m_StaleWaterPipePurification
		}, m_NodeQuery, base.Dependency);
		EdgePollutionJob jobData = new EdgePollutionJob
		{
			m_FlowEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FlowNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNode_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SourceNode = m_WaterPipeFlowSystem.sourceNode,
			m_Purify = purify
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EdgeQuery, dependsOn);
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
	public WaterPipePollutionSystem()
	{
	}
}
