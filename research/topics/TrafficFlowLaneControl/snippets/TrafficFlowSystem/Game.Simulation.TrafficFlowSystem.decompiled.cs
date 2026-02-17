using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Pathfind;
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
public class TrafficFlowSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateLaneFlowJob : IJobChunk
	{
		[ReadOnly]
		public float4 m_TimeFactors;

		public ComponentTypeHandle<LaneFlow> m_LaneFlowType;

		public ComponentTypeHandle<SecondaryFlow> m_SecondaryFlowType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<LaneFlow> nativeArray = chunk.GetNativeArray(ref m_LaneFlowType);
			NativeArray<SecondaryFlow> nativeArray2 = chunk.GetNativeArray(ref m_SecondaryFlowType);
			float4 t = m_TimeFactors * 0.125f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				LaneFlow value = nativeArray[i];
				value.m_Duration = math.lerp(value.m_Duration, value.m_Next.x, t);
				value.m_Distance = math.lerp(value.m_Distance, value.m_Next.y, t);
				value.m_Next = default(float2);
				nativeArray[i] = value;
			}
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				SecondaryFlow value2 = nativeArray2[j];
				value2.m_Duration = math.lerp(value2.m_Duration, value2.m_Next.x, t);
				value2.m_Distance = math.lerp(value2.m_Distance, value2.m_Next.y, t);
				value2.m_Next = default(float2);
				nativeArray2[j] = value2;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateRoadFlowJob : IJobChunk
	{
		[ReadOnly]
		public float4 m_TimeFactors;

		[ReadOnly]
		public BufferTypeHandle<SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentLookup<LaneFlow> m_LaneFlowData;

		[ReadOnly]
		public ComponentLookup<SecondaryFlow> m_SecondaryFlowData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		public ComponentTypeHandle<Road> m_RoadType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CarLane> m_CarLaneData;

		public NativeQueue<FlowActionData>.ParallelWriter m_FlowActions;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Road> nativeArray = chunk.GetNativeArray(ref m_RoadType);
			BufferAccessor<SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Road value = nativeArray[i];
				DynamicBuffer<SubLane> dynamicBuffer = bufferAccessor[i];
				value.m_TrafficFlowDuration0 = default(float4);
				value.m_TrafficFlowDuration1 = default(float4);
				value.m_TrafficFlowDistance0 = default(float4);
				value.m_TrafficFlowDistance1 = default(float4);
				int num = -1;
				MasterLane masterLane = default(MasterLane);
				float4 duration = default(float4);
				float4 distance = default(float4);
				bool isRoundabout;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity subLane = dynamicBuffer[j].m_SubLane;
					if (num != -1 && j > masterLane.m_MaxIndex)
					{
						Entity subLane2 = dynamicBuffer[num].m_SubLane;
						float4 trafficFlowSpeed = NetUtils.GetTrafficFlowSpeed(duration, distance);
						UpdateLaneFlow(subLane2, trafficFlowSpeed, out isRoundabout);
						num = -1;
					}
					if (m_LaneFlowData.TryGetComponent(subLane, out var componentData) | m_SecondaryFlowData.TryGetComponent(subLane, out var componentData2))
					{
						float4 @float = componentData.m_Duration + componentData2.m_Duration;
						float4 float2 = componentData.m_Distance + componentData2.m_Distance;
						float4 trafficFlowSpeed2 = NetUtils.GetTrafficFlowSpeed(@float, float2);
						UpdateLaneFlow(subLane, trafficFlowSpeed2, out var isRoundabout2);
						EdgeLane componentData3;
						float2 float3 = ((!m_EdgeLaneData.TryGetComponent(subLane, out componentData3)) ? ((float2)math.select(1f, 1f / 3f, isRoundabout2)) : math.select(0f, 1f, new bool2(math.any(componentData3.m_EdgeDelta == 0f), math.any(componentData3.m_EdgeDelta == 1f))));
						value.m_TrafficFlowDuration0 += componentData.m_Duration * float3.x;
						value.m_TrafficFlowDuration1 += componentData.m_Duration * float3.y;
						value.m_TrafficFlowDistance0 += componentData.m_Distance * float3.x;
						value.m_TrafficFlowDistance1 += componentData.m_Distance * float3.y;
						duration += @float;
						distance += float2;
					}
					else if (m_MasterLaneData.HasComponent(subLane))
					{
						num = j;
						masterLane = m_MasterLaneData[subLane];
						duration = default(float4);
						distance = default(float4);
					}
				}
				if (num != -1)
				{
					Entity subLane3 = dynamicBuffer[num].m_SubLane;
					float4 trafficFlowSpeed3 = NetUtils.GetTrafficFlowSpeed(duration, distance);
					UpdateLaneFlow(subLane3, trafficFlowSpeed3, out isRoundabout);
				}
				nativeArray[i] = value;
			}
		}

		private void UpdateLaneFlow(Entity lane, float4 flowSpeed, out bool isRoundabout)
		{
			isRoundabout = false;
			if (m_CarLaneData.TryGetComponent(lane, out var componentData))
			{
				isRoundabout = (componentData.m_Flags & (CarLaneFlags.Approach | CarLaneFlags.Roundabout)) == CarLaneFlags.Roundabout;
				byte b = (byte)math.clamp(256 - Mathf.RoundToInt(math.dot(flowSpeed, m_TimeFactors) * 256f), 0, 255);
				if (b != componentData.m_FlowOffset)
				{
					componentData.m_FlowOffset = b;
					m_CarLaneData[lane] = componentData;
					m_FlowActions.Enqueue(new FlowActionData
					{
						m_Owner = lane,
						m_FlowOffset = b
					});
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
		public ComponentTypeHandle<LaneFlow> __Game_Net_LaneFlow_RW_ComponentTypeHandle;

		public ComponentTypeHandle<SecondaryFlow> __Game_Net_SecondaryFlow_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<LaneFlow> __Game_Net_LaneFlow_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SecondaryFlow> __Game_Net_SecondaryFlow_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		public ComponentTypeHandle<Road> __Game_Net_Road_RW_ComponentTypeHandle;

		public ComponentLookup<CarLane> __Game_Net_CarLane_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_LaneFlow_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LaneFlow>();
			__Game_Net_SecondaryFlow_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SecondaryFlow>();
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
			__Game_Net_LaneFlow_RO_ComponentLookup = state.GetComponentLookup<LaneFlow>(isReadOnly: true);
			__Game_Net_SecondaryFlow_RO_ComponentLookup = state.GetComponentLookup<SecondaryFlow>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_Road_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Road>();
			__Game_Net_CarLane_RW_ComponentLookup = state.GetComponentLookup<CarLane>();
		}
	}

	public const int UPDATES_PER_DAY = 32;

	private SimulationSystem m_SimulationSystem;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private TimeSystem m_TimeSystem;

	private EntityQuery m_LaneQuery;

	private EntityQuery m_RoadQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_LaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<UpdateFrame>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadWrite<LaneFlow>(),
				ComponentType.ReadWrite<SecondaryFlow>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_RoadQuery = GetEntityQuery(ComponentType.ReadWrite<Road>(), ComponentType.ReadOnly<SubLane>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireAnyForUpdate(m_LaneQuery, m_RoadQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float num = m_TimeSystem.normalizedTime * 4f;
		float4 x = new float4(math.max(num - 3f, 1f - num), 1f - math.abs(num - new float3(1f, 2f, 3f)));
		x = math.saturate(x);
		FlowAction action = new FlowAction(Allocator.Persistent);
		m_LaneQuery.ResetFilter();
		m_LaneQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 32, 16)));
		m_RoadQuery.ResetFilter();
		m_RoadQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 32, 16)));
		UpdateLaneFlowJob jobData = new UpdateLaneFlowJob
		{
			m_TimeFactors = x,
			m_LaneFlowType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneFlow_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SecondaryFlowType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SecondaryFlow_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateRoadFlowJob
		{
			m_TimeFactors = x,
			m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LaneFlowData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneFlow_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryFlowData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryFlow_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_FlowActions = action.m_FlowData.AsParallelWriter()
		}, dependsOn: JobChunkExtensions.ScheduleParallel(jobData, m_LaneQuery, base.Dependency), query: m_RoadQuery);
		m_PathfindQueueSystem.Enqueue(action, jobHandle);
		base.Dependency = jobHandle;
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
	public TrafficFlowSystem()
	{
	}
}
