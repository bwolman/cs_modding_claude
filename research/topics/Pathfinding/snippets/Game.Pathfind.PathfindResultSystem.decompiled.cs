using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Serialization;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Pathfind;

[CompilerGenerated]
public class PathfindResultSystem : GameSystemBase, IPreDeserialize
{
	public enum QueryType
	{
		Pathfind,
		Coverage,
		Availability
	}

	public struct ResultKey : IEquatable<ResultKey>
	{
		public object m_System;

		public QueryType m_QueryType;

		public SetupTargetType m_OriginType;

		public SetupTargetType m_DestinationType;

		public bool Equals(ResultKey other)
		{
			return (m_System == other.m_System) & (m_QueryType == other.m_QueryType) & (m_OriginType == other.m_OriginType) & (m_DestinationType == other.m_DestinationType);
		}

		public override int GetHashCode()
		{
			return ((m_System.GetHashCode() * 31 + m_QueryType.GetHashCode()) * 31 + m_OriginType.GetHashCode()) * 31 + m_DestinationType.GetHashCode();
		}
	}

	public struct ResultValue
	{
		public int m_QueryCount;

		public int m_SuccessCount;

		public float m_GraphTraversal;

		public float m_Efficiency;
	}

	private struct TypeHandle
	{
		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentLookup;

		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RW_ComponentLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public BufferLookup<PathInformations> __Game_Pathfind_PathInformations_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		public BufferLookup<CoverageElement> __Game_Pathfind_CoverageElement_RW_BufferLookup;

		public BufferLookup<AvailabilityElement> __Game_Pathfind_AvailabilityElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Pathfind_PathOwner_RW_ComponentLookup = state.GetComponentLookup<PathOwner>();
			__Game_Pathfind_PathInformation_RW_ComponentLookup = state.GetComponentLookup<PathInformation>();
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Pathfind_PathInformations_RW_BufferLookup = state.GetBufferLookup<PathInformations>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Pathfind_CoverageElement_RW_BufferLookup = state.GetBufferLookup<CoverageElement>();
			__Game_Pathfind_AvailabilityElement_RW_BufferLookup = state.GetBufferLookup<AvailabilityElement>();
		}
	}

	private PathfindQueueSystem m_PathfindQueueSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityCommandBuffer m_CommandBuffer;

	private EntityArchetype m_PathEventArchetype;

	private EntityArchetype m_CoverageEventArchetype;

	private uint m_PendingSimulationFrameIndex;

	private int m_PendingRequestCount;

	private Dictionary<Entity, int> m_ResultListIndex;

	private Dictionary<ResultKey, ResultValue> m_QueryStats;

	private NativeList<PathfindJobs.ResultItem> m_PathfindResultBuffer;

	private NativeList<CoverageJobs.ResultItem> m_CoverageResultBuffer;

	private NativeList<AvailabilityJobs.ResultItem> m_AvailabilityResultBuffer;

	private TypeHandle __TypeHandle;

	public uint pendingSimulationFrame => math.min(m_PendingSimulationFrameIndex, m_PathfindSetupSystem.pendingSimulationFrame);

	public int pendingRequestCount => m_PendingRequestCount + m_PathfindSetupSystem.pendingRequestCount;

	public Dictionary<ResultKey, ResultValue> queryStats => m_QueryStats;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<PathUpdated>());
		m_CoverageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<CoverageUpdated>());
		m_ResultListIndex = new Dictionary<Entity, int>(10);
		m_QueryStats = new Dictionary<ResultKey, ResultValue>(10);
		m_PathfindResultBuffer = new NativeList<PathfindJobs.ResultItem>(10, Allocator.Persistent);
		m_CoverageResultBuffer = new NativeList<CoverageJobs.ResultItem>(10, Allocator.Persistent);
		m_AvailabilityResultBuffer = new NativeList<AvailabilityJobs.ResultItem>(10, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_PathfindResultBuffer.Dispose();
		m_CoverageResultBuffer.Dispose();
		m_AvailabilityResultBuffer.Dispose();
		base.OnDestroy();
	}

	public void PreDeserialize(Context context)
	{
		m_PendingSimulationFrameIndex = uint.MaxValue;
		m_PendingRequestCount = 0;
		m_QueryStats.Clear();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outputDeps = base.Dependency;
		m_PendingSimulationFrameIndex = uint.MaxValue;
		m_PendingRequestCount = 0;
		m_CommandBuffer = default(EntityCommandBuffer);
		ProcessResults(m_PathfindQueueSystem.GetPathfindActions(), ref outputDeps, base.Dependency);
		ProcessResults(m_PathfindQueueSystem.GetCoverageActions(), ref outputDeps, base.Dependency);
		ProcessResults(m_PathfindQueueSystem.GetAvailabilityActions(), ref outputDeps, base.Dependency);
		ProcessResults(m_PathfindQueueSystem.GetCreateActions());
		ProcessResults(m_PathfindQueueSystem.GetUpdateActions());
		ProcessResults(m_PathfindQueueSystem.GetDeleteActions());
		ProcessResults(m_PathfindQueueSystem.GetDensityActions());
		ProcessResults(m_PathfindQueueSystem.GetTimeActions());
		ProcessResults(m_PathfindQueueSystem.GetFlowActions());
		base.Dependency = outputDeps;
	}

	private void AddQueryStats(object system, QueryType queryType, SetupTargetType originType, SetupTargetType destinationType, int resultLength, int graphTraversal)
	{
		ResultKey key = new ResultKey
		{
			m_System = system,
			m_QueryType = queryType,
			m_OriginType = originType,
			m_DestinationType = destinationType
		};
		if (m_QueryStats.TryGetValue(key, out var value))
		{
			value.m_QueryCount++;
			value.m_SuccessCount += math.min(1, resultLength);
			value.m_GraphTraversal += (float)graphTraversal / math.max(1f, m_PathfindQueueSystem.GetGraphSize());
			value.m_Efficiency += (float)resultLength / math.max(1f, graphTraversal);
			m_QueryStats[key] = value;
		}
		else
		{
			m_QueryStats.Add(key, new ResultValue
			{
				m_QueryCount = 1,
				m_SuccessCount = math.min(1, resultLength),
				m_GraphTraversal = (float)graphTraversal / math.max(1f, m_PathfindQueueSystem.GetGraphSize()),
				m_Efficiency = (float)resultLength / math.max(1f, graphTraversal)
			});
		}
	}

	private void ProcessResults(PathfindQueueSystem.ActionList<PathfindAction> list, ref JobHandle outputDeps, JobHandle inputDeps)
	{
		m_ResultListIndex.Clear();
		m_PathfindResultBuffer.Clear();
		int num = 0;
		PathfindJobs.ResultItem value2 = default(PathfindJobs.ResultItem);
		for (int i = 0; i < list.m_Items.Count; i++)
		{
			PathfindQueueSystem.ActionListItem<PathfindAction> value = list.m_Items[i];
			if ((value.m_Flags & PathFlags.Scheduled) != 0)
			{
				if (value.m_Action.data.m_State == PathfindActionState.Completed)
				{
					value.m_Flags &= ~PathFlags.Scheduled;
					ErrorCode errorCode = value.m_Action.data.m_Result[0].m_ErrorCode;
					int graphTraversal = value.m_Action.data.m_Result[value.m_Action.data.m_Result.Length - 1].m_GraphTraversal;
					int pathLength = value.m_Action.data.m_Result[value.m_Action.data.m_Result.Length - 1].m_PathLength;
					value2.m_Owner = value.m_Owner;
					value2.m_Result = value.m_Action.data.m_Result;
					value2.m_Path = value.m_Action.data.m_Path;
					if (m_ResultListIndex.TryGetValue(value.m_Owner, out var value3))
					{
						m_PathfindResultBuffer[value3] = value2;
					}
					else
					{
						m_ResultListIndex.Add(value.m_Owner, m_PathfindResultBuffer.Length);
						m_PathfindResultBuffer.Add(in value2);
					}
					if (errorCode != ErrorCode.None)
					{
						COSystemBase.baseLog.ErrorFormat("Pathfind error ({0}: {1} -> {2}): {3} (Request: {4})", value.m_System.GetType().Name, value.m_Action.data.m_OriginType, value.m_Action.data.m_DestinationType, errorCode, value.m_Owner);
					}
					AddQueryStats(value.m_System, QueryType.Pathfind, value.m_Action.data.m_OriginType, value.m_Action.data.m_DestinationType, pathLength, graphTraversal);
					if ((value.m_Flags & PathFlags.WantsEvent) != 0)
					{
						if (!m_CommandBuffer.IsCreated)
						{
							m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
						}
						Entity e = m_CommandBuffer.CreateEntity(m_PathEventArchetype);
						m_CommandBuffer.SetComponent(e, new PathUpdated(value.m_Owner, value.m_EventData));
					}
				}
				else
				{
					m_PendingSimulationFrameIndex = math.min(m_PendingSimulationFrameIndex, value.m_ResultFrame);
					m_PendingRequestCount++;
				}
			}
			else
			{
				if ((value.m_Flags & PathFlags.Pending) == 0)
				{
					value.Dispose();
					list.m_NextIndex--;
					continue;
				}
				m_PendingSimulationFrameIndex = math.min(m_PendingSimulationFrameIndex, value.m_ResultFrame);
				m_PendingRequestCount++;
			}
			list.m_Items[num++] = value;
		}
		if (num < list.m_Items.Count)
		{
			list.m_Items.RemoveRange(num, list.m_Items.Count - num);
		}
		if (m_PathfindResultBuffer.Length > 0)
		{
			JobHandle job = IJobParallelForExtensions.Schedule(new PathfindJobs.ProcessResultsJob
			{
				m_ResultItems = m_PathfindResultBuffer,
				m_PathOwner = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PathInformation = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
				m_PathInformations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathInformations_RW_BufferLookup, ref base.CheckedStateRef)
			}, m_PathfindResultBuffer.Length, 1, inputDeps);
			outputDeps = JobHandle.CombineDependencies(outputDeps, job);
		}
	}

	private void ProcessResults(PathfindQueueSystem.ActionList<CoverageAction> list, ref JobHandle outputDeps, JobHandle inputDeps)
	{
		m_ResultListIndex.Clear();
		m_CoverageResultBuffer.Clear();
		int num = 0;
		CoverageJobs.ResultItem value2 = default(CoverageJobs.ResultItem);
		for (int i = 0; i < list.m_Items.Count; i++)
		{
			PathfindQueueSystem.ActionListItem<CoverageAction> value = list.m_Items[i];
			if ((value.m_Flags & PathFlags.Scheduled) != 0)
			{
				if (value.m_Action.data.m_State == PathfindActionState.Completed)
				{
					value.m_Flags &= ~PathFlags.Scheduled;
					value2.m_Owner = value.m_Owner;
					value2.m_Results = value.m_Action.data.m_Results;
					if (m_ResultListIndex.TryGetValue(value.m_Owner, out var value3))
					{
						m_CoverageResultBuffer[value3] = value2;
					}
					else
					{
						m_ResultListIndex.Add(value.m_Owner, m_CoverageResultBuffer.Length);
						m_CoverageResultBuffer.Add(in value2);
					}
					AddQueryStats(value.m_System, QueryType.Coverage, SetupTargetType.None, SetupTargetType.None, value2.m_Results.Length, value2.m_Results.Length);
					if ((value.m_Flags & PathFlags.WantsEvent) != 0)
					{
						if (!m_CommandBuffer.IsCreated)
						{
							m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
						}
						Entity e = m_CommandBuffer.CreateEntity(m_CoverageEventArchetype);
						m_CommandBuffer.SetComponent(e, new CoverageUpdated(value.m_Owner, value.m_EventData));
					}
				}
				else
				{
					m_PendingSimulationFrameIndex = math.min(m_PendingSimulationFrameIndex, value.m_ResultFrame);
					m_PendingRequestCount++;
				}
			}
			else
			{
				if ((value.m_Flags & PathFlags.Pending) == 0)
				{
					value.Dispose();
					list.m_NextIndex--;
					continue;
				}
				m_PendingSimulationFrameIndex = math.min(m_PendingSimulationFrameIndex, value.m_ResultFrame);
				m_PendingRequestCount++;
			}
			list.m_Items[num++] = value;
		}
		if (num < list.m_Items.Count)
		{
			list.m_Items.RemoveRange(num, list.m_Items.Count - num);
		}
		if (m_CoverageResultBuffer.Length > 0)
		{
			JobHandle job = IJobParallelForExtensions.Schedule(new CoverageJobs.ProcessResultsJob
			{
				m_ResultItems = m_CoverageResultBuffer,
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CoverageElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_CoverageElement_RW_BufferLookup, ref base.CheckedStateRef)
			}, m_CoverageResultBuffer.Length, 1, inputDeps);
			outputDeps = JobHandle.CombineDependencies(outputDeps, job);
		}
	}

	private void ProcessResults(PathfindQueueSystem.ActionList<AvailabilityAction> list, ref JobHandle outputDeps, JobHandle inputDeps)
	{
		m_ResultListIndex.Clear();
		m_AvailabilityResultBuffer.Clear();
		int num = 0;
		AvailabilityJobs.ResultItem value2 = default(AvailabilityJobs.ResultItem);
		for (int i = 0; i < list.m_Items.Count; i++)
		{
			PathfindQueueSystem.ActionListItem<AvailabilityAction> value = list.m_Items[i];
			if ((value.m_Flags & PathFlags.Scheduled) != 0)
			{
				if (value.m_Action.data.m_State == PathfindActionState.Completed)
				{
					value.m_Flags &= ~PathFlags.Scheduled;
					value2.m_Owner = value.m_Owner;
					value2.m_Results = value.m_Action.data.m_Results;
					if (m_ResultListIndex.TryGetValue(value.m_Owner, out var value3))
					{
						m_AvailabilityResultBuffer[value3] = value2;
					}
					else
					{
						m_ResultListIndex.Add(value.m_Owner, m_AvailabilityResultBuffer.Length);
						m_AvailabilityResultBuffer.Add(in value2);
					}
					AddQueryStats(value.m_System, QueryType.Availability, SetupTargetType.None, SetupTargetType.None, value2.m_Results.Length, value2.m_Results.Length);
				}
				else
				{
					m_PendingSimulationFrameIndex = math.min(m_PendingSimulationFrameIndex, value.m_ResultFrame);
					m_PendingRequestCount++;
				}
			}
			else
			{
				if ((value.m_Flags & PathFlags.Pending) == 0)
				{
					value.Dispose();
					list.m_NextIndex--;
					continue;
				}
				m_PendingSimulationFrameIndex = math.min(m_PendingSimulationFrameIndex, value.m_ResultFrame);
				m_PendingRequestCount++;
			}
			list.m_Items[num++] = value;
		}
		if (num < list.m_Items.Count)
		{
			list.m_Items.RemoveRange(num, list.m_Items.Count - num);
		}
		if (m_AvailabilityResultBuffer.Length > 0)
		{
			JobHandle job = IJobParallelForExtensions.Schedule(new AvailabilityJobs.ProcessResultsJob
			{
				m_ResultItems = m_AvailabilityResultBuffer,
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AvailabilityElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_AvailabilityElement_RW_BufferLookup, ref base.CheckedStateRef)
			}, m_AvailabilityResultBuffer.Length, 1, inputDeps);
			outputDeps = JobHandle.CombineDependencies(outputDeps, job);
		}
	}

	private void ProcessResults<T>(PathfindQueueSystem.ActionList<T> list) where T : struct, IDisposable
	{
		int num = 0;
		for (int i = 0; i < list.m_Items.Count; i++)
		{
			PathfindQueueSystem.ActionListItem<T> value = list.m_Items[i];
			if ((value.m_Flags & PathFlags.Pending) == 0 && value.m_Dependencies.IsCompleted)
			{
				value.m_Dependencies.Complete();
				value.Dispose();
				list.m_NextIndex--;
			}
			else
			{
				list.m_Items[num++] = value;
			}
		}
		if (num < list.m_Items.Count)
		{
			list.m_Items.RemoveRange(num, list.m_Items.Count - num);
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
	public PathfindResultSystem()
	{
	}
}
