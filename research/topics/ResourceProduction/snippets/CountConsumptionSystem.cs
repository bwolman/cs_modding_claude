using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class CountConsumptionSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CopyConsumptionJob : IJob
	{
		public NativeArray<int> m_Accumulator;

		public NativeArray<int> m_Consumptions;

		public void Execute()
		{
			for (int i = 0; i < m_Accumulator.Length; i++)
			{
				m_Consumptions[i] = ((m_Consumptions[i] == 0) ? m_Accumulator[i] : Mathf.RoundToInt((float)kUpdatesPerDay * math.lerp((float)(m_Consumptions[i] / kUpdatesPerDay), (float)m_Accumulator[i], 0.3f)));
				m_Accumulator[i] = 0;
			}
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private NativeArray<int> m_Consumptions;

	private NativeArray<int> m_ConsumptionAccumulator;

	private JobHandle m_ReadDeps;

	private JobHandle m_WriteDeps;

	private JobHandle m_CopyDeps;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public NativeArray<int> GetConsumptions(out JobHandle deps)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		deps = m_CopyDeps;
		return m_Consumptions;
	}

	public NativeArray<int> GetConsumptionAccumulator(out JobHandle deps)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		deps = m_WriteDeps;
		return m_ConsumptionAccumulator;
	}

	public void AddConsumptionReader(JobHandle deps)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		m_ReadDeps = JobHandle.CombineDependencies(m_ReadDeps, deps);
	}

	public void AddConsumptionWriter(JobHandle deps)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		m_WriteDeps = JobHandle.CombineDependencies(m_WriteDeps, deps);
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_Consumptions = new NativeArray<int>(EconomyUtils.ResourceCount, (Allocator)4, (NativeArrayOptions)1);
		m_ConsumptionAccumulator = new NativeArray<int>(EconomyUtils.ResourceCount, (Allocator)4, (NativeArrayOptions)1);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ConsumptionAccumulator.Dispose();
		m_Consumptions.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		((COSystemBase)this).OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		CopyConsumptionJob copyConsumptionJob = new CopyConsumptionJob
		{
			m_Accumulator = m_ConsumptionAccumulator,
			m_Consumptions = m_Consumptions
		};
		((SystemBase)this).Dependency = IJobExtensions.Schedule<CopyConsumptionJob>(copyConsumptionJob, JobHandle.CombineDependencies(m_ReadDeps, m_WriteDeps));
		m_CopyDeps = ((SystemBase)this).Dependency;
		m_WriteDeps = ((SystemBase)this).Dependency;
	}

	public void SetDefaults(Context context)
	{
		for (int i = 0; i < m_ConsumptionAccumulator.Length; i++)
		{
			m_ConsumptionAccumulator[i] = 0;
			m_Consumptions[i] = 0;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<int> consumptionAccumulator = m_ConsumptionAccumulator;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(consumptionAccumulator);
		NativeArray<int> consumptions = m_Consumptions;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(consumptions);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Context context = ((IReader)reader).context;
		ContextFormat format = ((Context)(ref context)).format;
		if (((ContextFormat)(ref format)).Has<FormatTags>(FormatTags.FishResource))
		{
			NativeArray<int> consumptionAccumulator = m_ConsumptionAccumulator;
			((IReader)reader/*cast due to .constrained prefix*/).Read(consumptionAccumulator);
			NativeArray<int> consumptions = m_Consumptions;
			((IReader)reader/*cast due to .constrained prefix*/).Read(consumptions);
		}
		else
		{
			NativeArray<int> subArray = m_ConsumptionAccumulator.GetSubArray(0, 40);
			((IReader)reader/*cast due to .constrained prefix*/).Read(subArray);
			NativeArray<int> subArray2 = m_Consumptions.GetSubArray(0, 40);
			((IReader)reader/*cast due to .constrained prefix*/).Read(subArray2);
			m_ConsumptionAccumulator[40] = 0;
			m_Consumptions[40] = 0;
		}
	}

	[Preserve]
	public CountConsumptionSystem()
	{
	}
}
