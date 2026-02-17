using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
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
public class OutsideConnectionDelaySystem : GameSystemBase
{
	private struct AccumulationData
	{
		public Entity m_Lane;

		public float2 m_Delay;
	}

	[BurstCompile]
	private struct OutsideConnectionDelayJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> m_PrefabPathfindConnectionData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Net.OutsideConnection> m_OutsideConnectionData;

		public NativeQueue<TimeActionData>.ParallelWriter m_TimeActions;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
			BufferAccessor<ConnectedEdge> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
			NativeParallelHashMap<PathNode, AccumulationData> nativeParallelHashMap = default(NativeParallelHashMap<PathNode, AccumulationData>);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = bufferAccessor[i];
				bool flag = false;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Game.Net.SubLane subLane = dynamicBuffer[j];
					if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) == 0 || !m_ConnectionLaneData.HasComponent(subLane.m_SubLane))
					{
						continue;
					}
					Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[subLane.m_SubLane];
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Start) != 0 && (connectionLane.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
					{
						Lane lane = m_LaneData[subLane.m_SubLane];
						if (!nativeParallelHashMap.IsCreated)
						{
							nativeParallelHashMap = new NativeParallelHashMap<PathNode, AccumulationData>(10, Allocator.Temp);
						}
						nativeParallelHashMap.TryAdd(lane.m_StartNode, new AccumulationData
						{
							m_Lane = subLane.m_SubLane
						});
						flag = true;
					}
				}
				if (!flag)
				{
					continue;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer2 = bufferAccessor2[i];
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					ConnectedEdge connectedEdge = dynamicBuffer2[k];
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer3 = m_SubLanes[connectedEdge.m_Edge];
					for (int l = 0; l < dynamicBuffer3.Length; l++)
					{
						Game.Net.SubLane subLane2 = dynamicBuffer3[l];
						if ((subLane2.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) != 0)
						{
							Lane lane2;
							if (m_SlaveLaneData.TryGetComponent(subLane2.m_SubLane, out var componentData))
							{
								Game.Net.SubLane subLane3 = dynamicBuffer3[componentData.m_MasterIndex];
								lane2 = m_LaneData[subLane3.m_SubLane];
							}
							else
							{
								lane2 = m_LaneData[subLane2.m_SubLane];
							}
							if (nativeParallelHashMap.TryGetValue(lane2.m_StartNode, out var item))
							{
								item.m_Delay.x += CalculateDelay(subLane2.m_SubLane);
								item.m_Delay.y += 1f;
								nativeParallelHashMap[lane2.m_StartNode] = item;
							}
						}
					}
				}
				NativeParallelHashMap<PathNode, AccumulationData>.Enumerator enumerator = nativeParallelHashMap.GetEnumerator();
				while (enumerator.MoveNext())
				{
					AccumulationData value = enumerator.Current.Value;
					if (m_OutsideConnectionData.TryGetComponent(value.m_Lane, out var componentData2))
					{
						componentData2.m_Delay = math.select(value.m_Delay.x / value.m_Delay.y, 0f, value.m_Delay.y == 0f);
						m_OutsideConnectionData[value.m_Lane] = componentData2;
						Lane lane3 = m_LaneData[value.m_Lane];
						PrefabRef prefabRef = m_PrefabRefData[value.m_Lane];
						NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
						PathfindConnectionData pathfindConnectionData = m_PrefabPathfindConnectionData[netLaneData.m_PathfindPrefab];
						TimeActionData value2 = new TimeActionData
						{
							m_Owner = value.m_Lane,
							m_StartNode = lane3.m_StartNode,
							m_EndNode = lane3.m_EndNode,
							m_SecondaryStartNode = lane3.m_StartNode,
							m_SecondaryEndNode = lane3.m_EndNode,
							m_Flags = (TimeActionFlags.SetPrimary | TimeActionFlags.SetSecondary | TimeActionFlags.EnableForward | TimeActionFlags.EnableBackward),
							m_Time = pathfindConnectionData.m_BorderCost.m_Value.x + componentData2.m_Delay
						};
						m_TimeActions.Enqueue(value2);
					}
				}
				enumerator.Dispose();
				nativeParallelHashMap.Clear();
			}
			if (nativeParallelHashMap.IsCreated)
			{
				nativeParallelHashMap.Dispose();
			}
		}

		private float CalculateDelay(Entity lane)
		{
			float num = 0f;
			if (m_LaneObjects.TryGetBuffer(lane, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					LaneObject laneObject = bufferData[i];
					if (m_CarCurrentLaneData.TryGetComponent(laneObject.m_LaneObject, out var componentData) && (componentData.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
					{
						num += componentData.m_Duration;
					}
				}
			}
			return num;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> __Game_Prefabs_PathfindConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		public ComponentLookup<Game.Net.OutsideConnection> __Game_Net_OutsideConnection_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup = state.GetComponentLookup<PathfindConnectionData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_OutsideConnection_RW_ComponentLookup = state.GetComponentLookup<Game.Net.OutsideConnection>();
		}
	}

	public const int UPDATES_PER_DAY = 64;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private EntityQuery m_NodeQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_NodeQuery = GetEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Game.Net.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_NodeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TimeAction action = new TimeAction(Allocator.Persistent);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new OutsideConnectionDelayJob
		{
			m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPathfindConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TimeActions = action.m_TimeData.AsParallelWriter()
		}, m_NodeQuery, base.Dependency);
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
	public OutsideConnectionDelaySystem()
	{
	}
}
