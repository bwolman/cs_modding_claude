using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class FireHazardSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct FireHazardJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_FirePrefabChunks;

		[ReadOnly]
		public EventHelpers.FireHazardData m_FireHazardData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public bool m_NaturalDisasters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			// Only processes 1 in 64 chunks per update (random.NextInt(64) != 0 -> skip)
			// This combined with the 4096-frame update interval means very low frequency

			// BUILDING PATH:
			// - Skips owned buildings (extensions) unless floating (docks, etc.)
			// - Skips buildings under construction (m_Progress < 255)
			// - Calls FireHazardData.GetFireHazard(building overload)
			// - If hazard > 0: calls TryStartFire with EventTargetType.Building

			// TREE PATH:
			// - Only processes trees without Owner (wild trees, not landscaped)
			// - Requires m_NaturalDisasters == true (city setting)
			// - Calls FireHazardData.GetFireHazard(tree overload)
			// - If hazard > 0: calls TryStartFire with EventTargetType.WildTree
		}

		private void TryStartFire(int jobIndex, ref Random random, Entity entity, float fireHazard, EventTargetType targetType)
		{
			// Iterates all fire event prefabs looking for one matching targetType
			// Final probability: fireHazard * fireData.m_StartProbability
			// Roll: random.NextFloat(10000) < probability
			// If successful: creates fire event entity with prefab archetype + TargetElement buffer
		}

		private void CreateFireEvent(int jobIndex, Entity targetEntity, Entity eventPrefab, EventData eventData, FireData fireData)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(eventPrefab));
			m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, e).Add(new TargetElement(targetEntity));
		}
	}

	private const int UPDATES_PER_DAY = 64;

	private EventHelpers.FireHazardData m_FireHazardData;

	public float noRainDays { get; private set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096; // Very infrequent -- about once per in-game day
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Query: (Building OR Tree), excluding FireStation, Placeholder, OnFire, Deleted, Overridden, Temp
		// Fire prefab query: EventData + FireData, excluding Locked
		// Creates FireHazardData helper
	}

	[Preserve]
	protected override void OnUpdate()
	{
		// Tracks noRainDays: increments by 1/64 each update when not raining, resets to 0 when raining
		// Updates FireHazardData with current temperature, noRainDays, local effects
		// Schedules FireHazardJob
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(noRainDays);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out float value);
		noRainDays = value;
	}

	public void SetDefaults(Context context)
	{
		noRainDays = 0f;
	}
}
