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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityTradeSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct SumJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> m_FlowConnectionType;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public NativePerThreadSumInt.Concurrent m_Export;

		public NativePerThreadSumInt.Concurrent m_Import;

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
					ElectricityFlowEdge electricityFlowEdge = m_FlowEdges[dynamicBuffer[j].m_Edge];
					if (electricityFlowEdge.m_End == m_SinkNode)
					{
						Assert.IsTrue(electricityFlowEdge.m_Flow >= 0);
						m_Export.Add(electricityFlowEdge.m_Flow);
					}
					else if (electricityFlowEdge.m_Start == m_SourceNode)
					{
						Assert.IsTrue(electricityFlowEdge.m_Flow >= 0);
						m_Import.Add(electricityFlowEdge.m_Flow);
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
	private struct ElectricityTradeJob : IJob
	{
		public NativePerThreadSumInt m_Export;

		public NativePerThreadSumInt m_Import;

		public NativeQueue<ServiceFeeSystem.FeeEvent> m_FeeQueue;

		public OutsideTradeParameterData m_OutsideTradeParameters;

		public void Execute()
		{
			float num = (float)m_Export.Count / 2048f;
			float num2 = (float)m_Import.Count / 2048f;
			float num3 = num * m_OutsideTradeParameters.m_ElectricityExportPrice;
			float num4 = num2 * m_OutsideTradeParameters.m_ElectricityImportPrice;
			if (num3 > 0f)
			{
				m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
				{
					m_Resource = PlayerResource.Electricity,
					m_Cost = num3,
					m_Amount = num,
					m_Outside = true
				});
			}
			if (num4 > 0f)
			{
				m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
				{
					m_Resource = PlayerResource.Electricity,
					m_Cost = num4,
					m_Amount = 0f - num2,
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
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
		}
	}

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private EntityQuery m_TradeNodeGroup;

	private ServiceFeeSystem m_ServiceFeeSystem;

	private NativePerThreadSumInt m_Export;

	private NativePerThreadSumInt m_Import;

	private int m_LastExport;

	private int m_LastImport;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1233563293_0;

	public int export => m_LastExport;

	public int import => m_LastImport;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 126;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_ServiceFeeSystem = base.World.GetOrCreateSystemManaged<ServiceFeeSystem>();
		m_TradeNodeGroup = GetEntityQuery(ComponentType.ReadOnly<TradeNode>(), ComponentType.ReadOnly<ElectricityFlowNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>());
		RequireForUpdate<OutsideTradeParameterData>();
		m_Export = new NativePerThreadSumInt(Allocator.Persistent);
		m_Import = new NativePerThreadSumInt(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Export.Dispose();
		m_Import.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_LastExport;
		writer.Write(value);
		int value2 = m_LastImport;
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int value = ref m_LastExport;
		reader.Read(out value);
		ref int value2 = ref m_LastImport;
		reader.Read(out value2);
	}

	public void SetDefaults(Context context)
	{
		m_LastExport = 0;
		m_LastImport = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_LastExport = m_Export.Count;
		m_LastImport = m_Import.Count;
		m_Export.Count = 0;
		m_Import.Count = 0;
		if (!m_TradeNodeGroup.IsEmptyIgnoreFilter)
		{
			SumJob jobData = new SumJob
			{
				m_FlowConnectionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Export = m_Export.ToConcurrent(),
				m_Import = m_Import.ToConcurrent(),
				m_SourceNode = m_ElectricityFlowSystem.sourceNode,
				m_SinkNode = m_ElectricityFlowSystem.sinkNode
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TradeNodeGroup, base.Dependency);
			JobHandle deps;
			ElectricityTradeJob jobData2 = new ElectricityTradeJob
			{
				m_Export = m_Export,
				m_Import = m_Import,
				m_FeeQueue = m_ServiceFeeSystem.GetFeeQueue(out deps),
				m_OutsideTradeParameters = __query_1233563293_0.GetSingleton<OutsideTradeParameterData>()
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
		__query_1233563293_0 = entityQueryBuilder2.Build(ref state);
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
	public ElectricityTradeSystem()
	{
	}
}
