using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaterPipeOutsideConnectionGraphSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateOutsideConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> m_WaterPipeNodeConnections;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public EntityArchetype m_EdgeArchetype;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (m_WaterPipeNodeConnections.TryGetComponent(nativeArray[i].m_Owner, out var componentData))
				{
					m_CommandBuffer.AddComponent<TradeNode>(unfilteredChunkIndex, componentData.m_WaterPipeNode);
					CreateOutsideFlowEdge(unfilteredChunkIndex, m_SourceNode, componentData.m_WaterPipeNode);
					CreateOutsideFlowEdge(unfilteredChunkIndex, componentData.m_WaterPipeNode, m_SinkNode);
				}
			}
		}

		private void CreateOutsideFlowEdge(int jobIndex, Entity startNode, Entity endNode)
		{
			WaterPipeGraphUtils.CreateFlowEdge(m_CommandBuffer, jobIndex, m_EdgeArchetype, startNode, endNode, 0, 0);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
		}
	}

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private ModificationBarrier3 m_ModificationBarrier;

	private EntityQuery m_CreatedConnectionQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier3>();
		m_CreatedConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeOutsideConnection>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedConnectionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CreateOutsideConnectionsJob jobData = new CreateOutsideConnectionsJob
		{
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPipeNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EdgeArchetype = m_WaterPipeFlowSystem.edgeArchetype,
			m_SourceNode = m_WaterPipeFlowSystem.sourceNode,
			m_SinkNode = m_WaterPipeFlowSystem.sinkNode
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedConnectionQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public WaterPipeOutsideConnectionGraphSystem()
	{
	}
}
