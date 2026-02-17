using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Tools;
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
public class WaterPipeGraphDeleteSystem : GameSystemBase
{
	[BurstCompile]
	private struct DeleteConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WaterPipeNodeConnection> m_NodeConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeValveConnection> m_ValveConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			foreach (WaterPipeNodeConnection item in chunk.GetNativeArray(ref m_NodeConnectionType))
			{
				if (item.m_WaterPipeNode != Entity.Null)
				{
					WaterPipeGraphUtils.DeleteFlowNode(m_CommandBuffer, unfilteredChunkIndex, item.m_WaterPipeNode, ref m_FlowConnections);
				}
				else
				{
					UnityEngine.Debug.LogWarning("Found null WaterPipeNode");
				}
			}
			foreach (WaterPipeValveConnection item2 in chunk.GetNativeArray(ref m_ValveConnectionType))
			{
				WaterPipeGraphUtils.DeleteFlowNode(m_CommandBuffer, unfilteredChunkIndex, item2.m_ValveNode, ref m_FlowConnections);
			}
			foreach (WaterPipeBuildingConnection item3 in chunk.GetNativeArray(ref m_BuildingConnectionType))
			{
				WaterPipeGraphUtils.DeleteBuildingNodes(m_CommandBuffer, unfilteredChunkIndex, item3, ref m_FlowConnections, ref m_FlowEdges);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DeleteValveNodesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeValveConnection> m_ValveConnectionType;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WaterPipeValveConnection> nativeArray2 = chunk.GetNativeArray(ref m_ValveConnectionType);
			m_CommandBuffer.RemoveComponent<WaterPipeValveConnection>(unfilteredChunkIndex, nativeArray);
			foreach (WaterPipeValveConnection item in nativeArray2)
			{
				WaterPipeGraphUtils.DeleteFlowNode(m_CommandBuffer, unfilteredChunkIndex, item.m_ValveNode, ref m_FlowConnections);
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
		public ComponentTypeHandle<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeValveConnection> __Game_Simulation_WaterPipeValveConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeNodeConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeValveConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeValveConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeBuildingConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DeletedConnectionQuery;

	private EntityQuery m_DeletedValveNodeQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DeletedConnectionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<WaterPipeNodeConnection>(),
				ComponentType.ReadOnly<WaterPipeValveConnection>(),
				ComponentType.ReadOnly<WaterPipeBuildingConnection>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_DeletedValveNodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<WaterPipeValveConnection>(),
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		RequireAnyForUpdate(m_DeletedConnectionQuery, m_DeletedValveNodeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = default(JobHandle);
		if (!m_DeletedConnectionQuery.IsEmptyIgnoreFilter)
		{
			jobHandle = JobChunkExtensions.ScheduleParallel(new DeleteConnectionsJob
			{
				m_NodeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ValveConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeValveConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_DeletedConnectionQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		}
		JobHandle jobHandle2 = default(JobHandle);
		if (!m_DeletedValveNodeQuery.IsEmptyIgnoreFilter)
		{
			jobHandle2 = JobChunkExtensions.ScheduleParallel(new DeleteValveNodesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ValveConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeValveConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_DeletedValveNodeQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		}
		base.Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2);
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
	public WaterPipeGraphDeleteSystem()
	{
	}
}
