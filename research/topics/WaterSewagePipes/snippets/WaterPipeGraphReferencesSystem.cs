using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaterPipeGraphReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateGraphReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeEdge> m_WaterPipeEdgeType;

		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WaterPipeEdge> nativeArray2 = chunk.GetNativeArray(ref m_WaterPipeEdgeType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity edge = nativeArray[i];
				WaterPipeEdge waterPipeEdge = nativeArray2[i];
				DynamicBuffer<ConnectedFlowEdge> buffer = m_ConnectedFlowEdges[waterPipeEdge.m_Start];
				DynamicBuffer<ConnectedFlowEdge> buffer2 = m_ConnectedFlowEdges[waterPipeEdge.m_End];
				CollectionUtils.RemoveValue(buffer, new ConnectedFlowEdge(edge));
				CollectionUtils.RemoveValue(buffer2, new ConnectedFlowEdge(edge));
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
		public ComponentTypeHandle<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentTypeHandle;

		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_WaterPipeEdge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeEdge>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>();
		}
	}

	private EntityQuery m_EdgeGroup;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EdgeGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<WaterPipeEdge>() },
			Any = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		RequireForUpdate(m_EdgeGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateGraphReferencesJob jobData = new UpdateGraphReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_WaterPipeEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_EdgeGroup, base.Dependency);
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
	public WaterPipeGraphReferencesSystem()
	{
	}
}
