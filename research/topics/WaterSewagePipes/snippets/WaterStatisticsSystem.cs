using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaterStatisticsSystem : GameSystemBase, IWaterStatisticsSystem, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CountPumpCapacityJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStation> m_PumpType;

		public NativePerThreadSumInt.Concurrent m_Capacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WaterPumpingStation> nativeArray = chunk.GetNativeArray(ref m_PumpType);
			for (int i = 0; i < chunk.Count; i++)
			{
				m_Capacity.Add(nativeArray[i].m_Capacity);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CountOutletCapacityJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<SewageOutlet> m_OutletType;

		public NativePerThreadSumInt.Concurrent m_Capacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<SewageOutlet> nativeArray = chunk.GetNativeArray(ref m_OutletType);
			for (int i = 0; i < chunk.Count; i++)
			{
				m_Capacity.Add(nativeArray[i].m_Capacity);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CountWaterConsumptionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> m_ConsumerType;

		public NativePerThreadSumInt.Concurrent m_Consumption;

		public NativePerThreadSumInt.Concurrent m_FulfilledFreshConsumption;

		public NativePerThreadSumInt.Concurrent m_FulfilledSewageConsumption;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WaterConsumer> nativeArray = chunk.GetNativeArray(ref m_ConsumerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				WaterConsumer waterConsumer = nativeArray[i];
				m_Consumption.Add(waterConsumer.m_WantedConsumption);
				m_FulfilledFreshConsumption.Add(waterConsumer.m_FulfilledFresh);
				m_FulfilledSewageConsumption.Add(waterConsumer.m_FulfilledSewage);
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
		public ComponentTypeHandle<WaterPumpingStation> __Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutlet> __Game_Buildings_SewageOutlet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPumpingStation>(isReadOnly: true);
			__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SewageOutlet>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterConsumer>(isReadOnly: true);
		}
	}

	private EntityQuery m_PumpGroup;

	private EntityQuery m_OutletGroup;

	private EntityQuery m_ConsumerGroup;

	private NativePerThreadSumInt m_FreshCapacity;

	private NativePerThreadSumInt m_SewageCapacity;

	private NativePerThreadSumInt m_Consumption;

	private NativePerThreadSumInt m_FulfilledFreshConsumption;

	private NativePerThreadSumInt m_FulfilledSewageConsumption;

	private int m_LastFreshCapacity;

	private int m_LastFreshConsumption;

	private int m_LastFulfilledFreshConsumption;

	private int m_LastSewageCapacity;

	private int m_LastSewageConsumption;

	private int m_LastFulfilledSewageConsumption;

	private TypeHandle __TypeHandle;

	public int freshCapacity => m_LastFreshCapacity;

	public int freshConsumption => m_LastFreshConsumption;

	public int fulfilledFreshConsumption => m_LastFulfilledFreshConsumption;

	public int sewageCapacity => m_LastSewageCapacity;

	public int sewageConsumption => m_LastSewageConsumption;

	public int fulfilledSewageConsumption => m_LastFulfilledSewageConsumption;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 63;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PumpGroup = GetEntityQuery(ComponentType.ReadOnly<WaterPumpingStation>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_OutletGroup = GetEntityQuery(ComponentType.ReadOnly<SewageOutlet>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ConsumerGroup = GetEntityQuery(ComponentType.ReadOnly<WaterConsumer>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_FreshCapacity = new NativePerThreadSumInt(Allocator.Persistent);
		m_SewageCapacity = new NativePerThreadSumInt(Allocator.Persistent);
		m_Consumption = new NativePerThreadSumInt(Allocator.Persistent);
		m_FulfilledFreshConsumption = new NativePerThreadSumInt(Allocator.Persistent);
		m_FulfilledSewageConsumption = new NativePerThreadSumInt(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_FreshCapacity.Dispose();
		m_SewageCapacity.Dispose();
		m_Consumption.Dispose();
		m_FulfilledFreshConsumption.Dispose();
		m_FulfilledSewageConsumption.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_LastFreshCapacity;
		writer.Write(value);
		int value2 = m_LastFreshConsumption;
		writer.Write(value2);
		int value3 = m_LastFulfilledFreshConsumption;
		writer.Write(value3);
		int value4 = m_LastSewageCapacity;
		writer.Write(value4);
		int value5 = m_LastSewageConsumption;
		writer.Write(value5);
		int value6 = m_LastFulfilledSewageConsumption;
		writer.Write(value6);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int value = ref m_LastFreshCapacity;
		reader.Read(out value);
		ref int value2 = ref m_LastFreshConsumption;
		reader.Read(out value2);
		ref int value3 = ref m_LastFulfilledFreshConsumption;
		reader.Read(out value3);
		ref int value4 = ref m_LastSewageCapacity;
		reader.Read(out value4);
		ref int value5 = ref m_LastSewageConsumption;
		reader.Read(out value5);
		ref int value6 = ref m_LastFulfilledSewageConsumption;
		reader.Read(out value6);
	}

	public void SetDefaults(Context context)
	{
		m_LastFreshCapacity = 0;
		m_LastFreshConsumption = 0;
		m_LastFulfilledFreshConsumption = 0;
		m_LastSewageCapacity = 0;
		m_LastSewageConsumption = 0;
		m_LastFulfilledSewageConsumption = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_LastFreshCapacity = m_FreshCapacity.Count;
		m_LastFreshConsumption = m_Consumption.Count;
		m_LastFulfilledFreshConsumption = m_FulfilledFreshConsumption.Count;
		m_LastSewageCapacity = m_SewageCapacity.Count;
		m_LastSewageConsumption = m_Consumption.Count;
		m_LastFulfilledSewageConsumption = m_FulfilledSewageConsumption.Count;
		m_FreshCapacity.Count = 0;
		m_SewageCapacity.Count = 0;
		m_Consumption.Count = 0;
		m_FulfilledFreshConsumption.Count = 0;
		m_FulfilledSewageConsumption.Count = 0;
		JobHandle job = default(JobHandle);
		if (!m_PumpGroup.IsEmptyIgnoreFilter)
		{
			job = JobChunkExtensions.ScheduleParallel(new CountPumpCapacityJob
			{
				m_PumpType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPumpingStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Capacity = m_FreshCapacity.ToConcurrent()
			}, m_PumpGroup, base.Dependency);
		}
		JobHandle job2 = default(JobHandle);
		if (!m_PumpGroup.IsEmptyIgnoreFilter)
		{
			job2 = JobChunkExtensions.ScheduleParallel(new CountOutletCapacityJob
			{
				m_OutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_SewageOutlet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Capacity = m_SewageCapacity.ToConcurrent()
			}, m_OutletGroup, base.Dependency);
		}
		JobHandle job3 = default(JobHandle);
		if (!m_ConsumerGroup.IsEmptyIgnoreFilter)
		{
			job3 = JobChunkExtensions.ScheduleParallel(new CountWaterConsumptionJob
			{
				m_ConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Consumption = m_Consumption.ToConcurrent(),
				m_FulfilledFreshConsumption = m_FulfilledFreshConsumption.ToConcurrent(),
				m_FulfilledSewageConsumption = m_FulfilledSewageConsumption.ToConcurrent()
			}, m_ConsumerGroup, base.Dependency);
		}
		base.Dependency = JobHandle.CombineDependencies(job, job2, job3);
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
	public WaterStatisticsSystem()
	{
	}
}
