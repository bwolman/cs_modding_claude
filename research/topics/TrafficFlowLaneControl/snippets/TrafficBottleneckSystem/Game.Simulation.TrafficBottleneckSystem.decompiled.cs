using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TrafficBottleneckSystem : GameSystemBase
{
	private struct GroupData
	{
		public int m_Count;

		public int m_Merged;
	}

	private struct BottleneckData
	{
		public BottleneckState m_State;

		public int2 m_Range;
	}

	private enum BottleneckState
	{
		Remove,
		Keep,
		Add
	}

	[BurstCompile]
	private struct TrafficBottleneckJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Blocker> m_BlockerType;

		public ComponentTypeHandle<Bottleneck> m_BottleneckType;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_BlockerChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_BottleneckChunks;

		[ReadOnly]
		public TrafficConfigurationData m_TrafficConfigurationData;

		public IconCommandBuffer m_IconCommandBuffer;

		public EntityCommandBuffer m_EntityCommandBuffer;

		public NativeQueue<TriggerAction> m_TriggerActionQueue;

		public void Execute()
		{
			NativeParallelHashMap<Entity, int> groupMap = new NativeParallelHashMap<Entity, int>(1000, Allocator.Temp);
			NativeParallelHashMap<Entity, BottleneckData> bottleneckMap = new NativeParallelHashMap<Entity, BottleneckData>(10, Allocator.Temp);
			NativeList<GroupData> groups = new NativeList<GroupData>(1000, Allocator.Temp);
			for (int i = 0; i < m_BottleneckChunks.Length; i++)
			{
				FillBottlenecks(m_BottleneckChunks[i], bottleneckMap);
			}
			for (int j = 0; j < m_BlockerChunks.Length; j++)
			{
				FormGroups(m_BlockerChunks[j], groupMap, groups);
			}
			for (int k = 0; k < m_BlockerChunks.Length; k++)
			{
				AddBottlenecks(m_BlockerChunks[k], groupMap, groups, bottleneckMap);
			}
			int num = 0;
			for (int l = 0; l < m_BottleneckChunks.Length; l++)
			{
				num += CheckBottlenecks(m_BottleneckChunks[l], bottleneckMap);
			}
			m_TriggerActionQueue.Enqueue(new TriggerAction(TriggerType.TrafficBottleneck, Entity.Null, num));
			groupMap.Dispose();
			bottleneckMap.Dispose();
			groups.Dispose();
		}

		private void FillBottlenecks(ArchetypeChunk chunk, NativeParallelHashMap<Entity, BottleneckData> bottleneckMap)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity key = nativeArray[i];
				bottleneckMap.Add(key, new BottleneckData
				{
					m_State = BottleneckState.Remove
				});
			}
		}

		private void FormGroups(ArchetypeChunk chunk, NativeParallelHashMap<Entity, int> groupMap, NativeList<GroupData> groups)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Blocker> nativeArray2 = chunk.GetNativeArray(ref m_BlockerType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Blocker blocker = nativeArray2[i];
				if (blocker.m_Type != BlockerType.Continuing || !(blocker.m_Blocker != Entity.Null))
				{
					continue;
				}
				Entity entity2 = blocker.m_Blocker;
				if (m_ControllerData.TryGetComponent(entity2, out var componentData))
				{
					entity2 = componentData.m_Controller;
				}
				if (entity2 == entity)
				{
					UnityEngine.Debug.Log($"TrafficBottleneckSystem: Self blocking entity {entity.Index}");
					continue;
				}
				int item;
				bool num = groupMap.TryGetValue(entity, out item);
				int item2;
				bool flag = groupMap.TryGetValue(entity2, out item2);
				if (num)
				{
					GroupData value = groups[item];
					if (value.m_Merged != -1)
					{
						do
						{
							item = value.m_Merged;
							value = groups[item];
						}
						while (value.m_Merged != -1);
						groupMap[entity] = item;
					}
					if (flag)
					{
						GroupData value2 = groups[item2];
						while (value2.m_Merged != -1)
						{
							item2 = value2.m_Merged;
							value2 = groups[item2];
						}
						if (item != item2)
						{
							value.m_Count += value2.m_Count;
							value2.m_Count = 0;
							value2.m_Merged = item;
							groups[item] = value;
							groups[item2] = value2;
						}
						groupMap[entity2] = item;
					}
					else
					{
						value.m_Count++;
						groups[item] = value;
						groupMap.Add(entity2, item);
					}
				}
				else if (flag)
				{
					GroupData value3 = groups[item2];
					if (value3.m_Merged != -1)
					{
						do
						{
							item2 = value3.m_Merged;
							value3 = groups[item2];
						}
						while (value3.m_Merged != -1);
						groupMap[entity2] = item2;
					}
					value3.m_Count++;
					groups[item2] = value3;
					groupMap.Add(entity, item2);
				}
				else
				{
					groupMap.Add(entity, groups.Length);
					groupMap.Add(entity2, groups.Length);
					GroupData value4 = new GroupData
					{
						m_Count = 2,
						m_Merged = -1
					};
					groups.Add(in value4);
				}
			}
		}

		private void AddBottlenecks(ArchetypeChunk chunk, NativeParallelHashMap<Entity, int> groupMap, NativeList<GroupData> groups, NativeParallelHashMap<Entity, BottleneckData> bottleneckMap)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Blocker> nativeArray2 = chunk.GetNativeArray(ref m_BlockerType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Blocker blocker = nativeArray2[i];
				if (!groupMap.TryGetValue(entity, out var item))
				{
					continue;
				}
				GroupData groupData = groups[item];
				while (groupData.m_Merged != -1)
				{
					item = groupData.m_Merged;
					groupData = groups[item];
				}
				if (groupData.m_Count >= 10)
				{
					Entity lane = Entity.Null;
					float2 curvePosition = 0f;
					TrainCurrentLane componentData2;
					if (m_CarCurrentLaneData.TryGetComponent(entity, out var componentData))
					{
						lane = ((!(componentData.m_ChangeLane != Entity.Null)) ? componentData.m_Lane : componentData.m_ChangeLane);
						curvePosition = componentData.m_CurvePosition.xy;
					}
					else if (m_TrainCurrentLaneData.TryGetComponent(entity, out componentData2))
					{
						lane = componentData2.m_Front.m_Lane;
						curvePosition = componentData2.m_Front.m_CurvePosition.yz;
					}
					if ((blocker.m_Type == BlockerType.Continuing && blocker.m_Blocker != Entity.Null) || (long)groupData.m_Count < 50L)
					{
						KeepBottleneck(bottleneckMap, lane, curvePosition);
					}
					else
					{
						AddBottleneck(bottleneckMap, lane, curvePosition);
					}
				}
			}
		}

		private void KeepBottleneck(NativeParallelHashMap<Entity, BottleneckData> bottleneckMap, Entity lane, float2 curvePosition)
		{
			if (bottleneckMap.TryGetValue(lane, out var item) && item.m_State == BottleneckState.Remove)
			{
				bottleneckMap[lane] = new BottleneckData
				{
					m_State = BottleneckState.Keep
				};
			}
		}

		private void AddBottleneck(NativeParallelHashMap<Entity, BottleneckData> bottleneckMap, Entity lane, float2 curvePosition)
		{
			if (bottleneckMap.TryGetValue(lane, out var item))
			{
				curvePosition.y += curvePosition.y - curvePosition.x;
				int2 range = math.clamp(new int2(Mathf.RoundToInt(math.cmin(curvePosition) * 255f), Mathf.RoundToInt(math.cmax(curvePosition) * 255f)), 0, 255);
				if (item.m_State == BottleneckState.Add)
				{
					item.m_Range.x = math.min(item.m_Range.x, range.x);
					item.m_Range.y = math.max(item.m_Range.y, range.y);
					bottleneckMap[lane] = item;
				}
				else
				{
					bottleneckMap[lane] = new BottleneckData
					{
						m_State = BottleneckState.Add,
						m_Range = range
					};
				}
			}
			else if (m_CurveData.HasComponent(lane))
			{
				curvePosition.y += curvePosition.y - curvePosition.x;
				int2 range2 = math.clamp(new int2(Mathf.RoundToInt(math.cmin(curvePosition) * 255f), Mathf.RoundToInt(math.cmax(curvePosition) * 255f)), 0, 255);
				m_EntityCommandBuffer.AddComponent(lane, new Bottleneck((byte)range2.x, (byte)range2.y, 5));
				bottleneckMap.Add(lane, new BottleneckData
				{
					m_State = BottleneckState.Add,
					m_Range = range2
				});
			}
		}

		private int CheckBottlenecks(ArchetypeChunk chunk, NativeParallelHashMap<Entity, BottleneckData> bottleneckMap)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Bottleneck> nativeArray2 = chunk.GetNativeArray(ref m_BottleneckType);
			int num = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Bottleneck value = nativeArray2[i];
				if (value.m_Timer >= 20)
				{
					num++;
				}
				BottleneckData bottleneckData = bottleneckMap[entity];
				switch (bottleneckData.m_State)
				{
				case BottleneckState.Remove:
					if (value.m_Timer >= 23)
					{
						value.m_Timer -= 3;
					}
					else if (value.m_Timer >= 20)
					{
						value.m_Timer = 0;
						m_IconCommandBuffer.Remove(entity, m_TrafficConfigurationData.m_BottleneckNotification);
						m_EntityCommandBuffer.RemoveComponent<Bottleneck>(entity);
					}
					else if (value.m_Timer > 3)
					{
						value.m_Timer -= 3;
					}
					else
					{
						m_EntityCommandBuffer.RemoveComponent<Bottleneck>(entity);
					}
					break;
				case BottleneckState.Keep:
					if (value.m_Timer >= 21)
					{
						value.m_Timer--;
					}
					else if (value.m_Timer >= 20)
					{
						value.m_Timer = 0;
						m_IconCommandBuffer.Remove(entity, m_TrafficConfigurationData.m_BottleneckNotification);
						m_EntityCommandBuffer.RemoveComponent<Bottleneck>(entity);
					}
					else if (value.m_Timer > 1)
					{
						value.m_Timer--;
					}
					else
					{
						m_EntityCommandBuffer.RemoveComponent<Bottleneck>(entity);
					}
					break;
				case BottleneckState.Add:
				{
					int position = value.m_Position;
					value.m_MinPos = (byte)math.min(value.m_MinPos + 2, bottleneckData.m_Range.x);
					value.m_MaxPos = (byte)math.max(value.m_MaxPos - 2, bottleneckData.m_Range.y);
					if (value.m_Position < value.m_MinPos || value.m_Position > value.m_MaxPos)
					{
						value.m_Position = (byte)(value.m_MinPos + value.m_MaxPos + 1 >> 1);
					}
					if (value.m_Timer >= 20)
					{
						value.m_Timer = (byte)math.min(40, value.m_Timer + 5);
						if (position != value.m_Position)
						{
							float3 location = MathUtils.Position(m_CurveData[entity].m_Bezier, (float)(int)value.m_Position * 0.003921569f);
							m_IconCommandBuffer.Add(entity, m_TrafficConfigurationData.m_BottleneckNotification, location, IconPriority.Problem);
						}
					}
					else if (value.m_Timer >= 15)
					{
						value.m_Timer = 40;
						float3 location2 = MathUtils.Position(m_CurveData[entity].m_Bezier, (float)(int)value.m_Position * 0.003921569f);
						m_IconCommandBuffer.Add(entity, m_TrafficConfigurationData.m_BottleneckNotification, location2, IconPriority.Problem);
					}
					else
					{
						value.m_Timer += 5;
					}
					break;
				}
				}
				nativeArray2[i] = value;
			}
			return num;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Bottleneck> __Game_Net_Bottleneck_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_Blocker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>(isReadOnly: true);
			__Game_Net_Bottleneck_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Bottleneck>();
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
		}
	}

	private IconCommandSystem m_IconCommandSystem;

	private TriggerSystem m_TriggerSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_BlockerQuery;

	private EntityQuery m_BottleneckQuery;

	private EntityQuery m_ConfigurationQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_BlockerQuery = GetEntityQuery(ComponentType.ReadOnly<Blocker>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_BottleneckQuery = GetEntityQuery(ComponentType.ReadOnly<Bottleneck>(), ComponentType.Exclude<Deleted>());
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<TrafficConfigurationData>());
		RequireAnyForUpdate(m_BlockerQuery, m_BottleneckQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> blockerChunks = m_BlockerQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle outJobHandle2;
		NativeList<ArchetypeChunk> bottleneckChunks = m_BottleneckQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
		JobHandle jobHandle = IJobExtensions.Schedule(new TrafficBottleneckJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BottleneckType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Bottleneck_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BlockerChunks = blockerChunks,
			m_BottleneckChunks = bottleneckChunks,
			m_TrafficConfigurationData = m_ConfigurationQuery.GetSingleton<TrafficConfigurationData>(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
			m_TriggerActionQueue = m_TriggerSystem.CreateActionBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2));
		blockerChunks.Dispose(jobHandle);
		bottleneckChunks.Dispose(jobHandle);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public TrafficBottleneckSystem()
	{
	}
}
