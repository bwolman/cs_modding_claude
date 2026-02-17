#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Prefabs;
using Unity.Assertions;
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
public class WaterTradeSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct SumJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> m_FlowConnectionType;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_FlowEdges;

		public NativePerThreadSumInt.Concurrent m_FreshExport;

		public NativePerThreadSumInt.Concurrent m_PollutedExport;

		public NativePerThreadSumInt.Concurrent m_FreshImport;

		public NativePerThreadSumInt.Concurrent m_SewageExport;

		public OutsideTradeParameterData m_OutsideTradeParameters;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ConnectedFlowEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_FlowConnectionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<ConnectedFlowEdge> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					WaterPipeEdge waterPipeEdge = m_FlowEdges[dynamicBuffer[j].m_Edge];
					if (waterPipeEdge.m_End == m_SinkNode)
					{
						Assert.IsTrue(waterPipeEdge.m_FreshFlow >= 0);
						Assert.AreEqual(0, waterPipeEdge.m_SewageFlow);
						m_FreshExport.Add(waterPipeEdge.m_FreshFlow);
						float num = waterPipeEdge.m_FreshPollution / m_OutsideTradeParameters.m_WaterExportPollutionTolerance;
						m_PollutedExport.Add(math.min((int)math.round(num * (float)waterPipeEdge.m_FreshFlow), waterPipeEdge.m_FreshFlow));
					}
					else if (waterPipeEdge.m_Start == m_SourceNode)
					{
						Assert.IsTrue(waterPipeEdge.m_FreshFlow >= 0);
						Assert.IsTrue(waterPipeEdge.m_SewageFlow >= 0);
						m_FreshImport.Add(waterPipeEdge.m_FreshFlow);
						m_SewageExport.Add(waterPipeEdge.m_SewageFlow);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct WaterTradeJob : IJob
	{
		public int m_AvailableWater;

		public NativePerThreadSumInt m_FreshExport;

		public NativePerThreadSumInt m_PollutedExport;

		public NativePerThreadSumInt m_FreshImport;

		public NativePerThreadSumInt m_SewageExport;

		public NativeQueue<ServiceFeeSystem.FeeEvent> m_FeeQueue;

		public OutsideTradeParameterData m_OutsideTradeParameters;

		public void Execute()
		{
			m_FreshExport.Count = math.max(math.min(m_AvailableWater, m_FreshExport.Count), 0);
			float num = (float)m_FreshExport.Count / 2048f;
			float num2 = (float)m_PollutedExport.Count / 2048f;
			float num3 = (float)m_FreshImport.Count / 2048f;
			float num4 = (float)m_SewageExport.Count / 2048f;
			float num5 = (num - num2) * m_OutsideTradeParameters.m_WaterExportPrice;
			float num6 = num3 * m_OutsideTradeParameters.m_WaterImportPrice;
			float num7 = num4 * m_OutsideTradeParameters.m_SewageExportPrice;
			if (num5 > 0f)
			{
				m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
				{
					m_Resource = PlayerResource.Water,
					m_Cost = num5,
					m_Amount = num,
					m_Outside = true
				});
			}
			if (num6 > 0f)
			{
				m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
				{
					m_Resource = PlayerResource.Water,
					m_Cost = num6,
					m_Amount = 0f - num3,
					m_Outside = true
				});
			}
			if (num7 > 0f)
			{
				m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
				{
					m_Resource = PlayerResource.Sewage,
					m_Cost = num7,
					m_Amount = 0f - num4,
					m_Outside = true
				});
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
		}
	}

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private WaterStatisticsSystem m_WaterStatisticsSystem;

	private EntityQuery m_TradeNodeGroup;

	private ServiceFeeSystem m_ServiceFeeSystem;

	private NativePerThreadSumInt m_FreshExport;

	private NativePerThreadSumInt m_PollutedExport;

	private NativePerThreadSumInt m_FreshImport;

	private NativePerThreadSumInt m_SewageExport;

	private int m_LastFreshExport;

	private int m_LastFreshImport;

	private int m_LastSewageExport;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1457460959_0;

	public int freshExport => m_LastFreshExport;

	public int freshImport => m_LastFreshImport;

	public int sewageExport => m_LastSewageExport;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 62;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_ServiceFeeSystem = base.World.GetOrCreateSystemManaged<ServiceFeeSystem>();
		m_WaterStatisticsSystem = base.World.GetOrCreateSystemManaged<WaterStatisticsSystem>();
		m_TradeNodeGroup = GetEntityQuery(ComponentType.ReadOnly<TradeNode>(), ComponentType.ReadOnly<WaterPipeNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>());
		RequireForUpdate<OutsideTradeParameterData>();
		m_FreshExport = new NativePerThreadSumInt(Allocator.Persistent);
		m_PollutedExport = new NativePerThreadSumInt(Allocator.Persistent);
		m_FreshImport = new NativePerThreadSumInt(Allocator.Persistent);
		m_SewageExport = new NativePerThreadSumInt(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_FreshExport.Dispose();
		m_PollutedExport.Dispose();
		m_FreshImport.Dispose();
		m_SewageExport.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_LastFreshExport;
		writer.Write(value);
		int value2 = m_LastFreshImport;
		writer.Write(value2);
		int value3 = m_LastSewageExport;
		writer.Write(value3);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int value = ref m_LastFreshExport;
		reader.Read(out value);
		ref int value2 = ref m_LastFreshImport;
		reader.Read(out value2);
		ref int value3 = ref m_LastSewageExport;
		reader.Read(out value3);
	}

	public void SetDefaults(Context context)
	{
		m_LastFreshExport = 0;
		m_LastFreshImport = 0;
		m_LastSewageExport = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_LastFreshExport = m_FreshExport.Count;
		m_LastFreshImport = m_FreshImport.Count;
		m_LastSewageExport = m_SewageExport.Count;
		int availableWater = m_WaterStatisticsSystem.freshCapacity - m_WaterStatisticsSystem.freshConsumption;
		m_FreshExport.Count = 0;
		m_PollutedExport.Count = 0;
		m_FreshImport.Count = 0;
		m_SewageExport.Count = 0;
		if (!m_TradeNodeGroup.IsEmptyIgnoreFilter)
		{
			OutsideTradeParameterData singleton = __query_1457460959_0.GetSingleton<OutsideTradeParameterData>();
			SumJob jobData = new SumJob
			{
				m_FlowConnectionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FreshExport = m_FreshExport.ToConcurrent(),
				m_PollutedExport = m_PollutedExport.ToConcurrent(),
				m_FreshImport = m_FreshImport.ToConcurrent(),
				m_SewageExport = m_SewageExport.ToConcurrent(),
				m_OutsideTradeParameters = singleton,
				m_SourceNode = m_WaterPipeFlowSystem.sourceNode,
				m_SinkNode = m_WaterPipeFlowSystem.sinkNode
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TradeNodeGroup, base.Dependency);
			JobHandle deps;
			WaterTradeJob jobData2 = new WaterTradeJob
			{
				m_AvailableWater = availableWater,
				m_FreshExport = m_FreshExport,
				m_PollutedExport = m_PollutedExport,
				m_FreshImport = m_FreshImport,
				m_SewageExport = m_SewageExport,
				m_FeeQueue = m_ServiceFeeSystem.GetFeeQueue(out deps),
				m_OutsideTradeParameters = singleton
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, deps));
			m_ServiceFeeSystem.AddQueueWriter(base.Dependency);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<OutsideTradeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1457460959_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public WaterTradeSystem()
	{
	}
}
