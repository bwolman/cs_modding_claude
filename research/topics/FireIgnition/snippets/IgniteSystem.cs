using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class IgniteSystem : GameSystemBase
{
	[BurstCompile]
	private struct IgniteFireJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Ignite> m_IgniteType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		public ComponentLookup<OnFire> m_OnFireData;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityArchetype m_JournalDataArchetype;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, OnFire> nativeParallelHashMap = new NativeParallelHashMap<Entity, OnFire>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<Ignite> nativeArray = m_Chunks[j].GetNativeArray(ref m_IgniteType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Ignite ignite = nativeArray[k];
					if (!m_PrefabRefData.HasComponent(ignite.m_Target))
					{
						continue;
					}
					OnFire onFire = new OnFire(ignite.m_Event, ignite.m_Intensity, ignite.m_RequestFrame);
					if (nativeParallelHashMap.TryGetValue(ignite.m_Target, out var item))
					{
						if (onFire.m_Intensity > item.m_Intensity)
						{
							nativeParallelHashMap[ignite.m_Target] = onFire;
						}
					}
					else if (m_OnFireData.HasComponent(ignite.m_Target))
					{
						item = m_OnFireData[ignite.m_Target];
						if (onFire.m_Intensity > item.m_Intensity)
						{
							nativeParallelHashMap.TryAdd(ignite.m_Target, onFire);
						}
					}
					else
					{
						nativeParallelHashMap.TryAdd(ignite.m_Target, onFire);
					}
				}
			}
			if (nativeParallelHashMap.Count() == 0)
			{
				return;
			}
			NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
			for (int l = 0; l < keyArray.Length; l++)
			{
				Entity entity = keyArray[l];
				OnFire onFire2 = nativeParallelHashMap[entity];
				if (m_OnFireData.HasComponent(entity))
				{
					OnFire onFire3 = m_OnFireData[entity];
					if (onFire3.m_Event != onFire2.m_Event)
					{
						if (m_TargetElements.HasBuffer(onFire2.m_Event))
						{
							CollectionUtils.TryAddUniqueValue(m_TargetElements[onFire2.m_Event], new TargetElement(entity));
						}
						AddJournalData(entity, onFire2);
					}
					if (onFire3.m_RequestFrame < onFire2.m_RequestFrame)
					{
						onFire2.m_RequestFrame = onFire3.m_RequestFrame;
					}
					onFire2.m_RescueRequest = onFire3.m_RescueRequest;
					m_OnFireData[entity] = onFire2;
					continue;
				}
				if (m_TargetElements.HasBuffer(onFire2.m_Event))
				{
					CollectionUtils.TryAddUniqueValue(m_TargetElements[onFire2.m_Event], new TargetElement(entity));
				}
				m_CommandBuffer.AddComponent(entity, onFire2);
				m_CommandBuffer.AddComponent(entity, default(BatchesUpdated));
				AddJournalData(entity, onFire2);
				if (!m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData))
				{
					continue;
				}
				for (int m = 0; m < bufferData.Length; m++)
				{
					Entity upgrade = bufferData[m].m_Upgrade;
					if (!m_BuildingData.HasComponent(upgrade))
					{
						m_CommandBuffer.AddComponent<BatchesUpdated>(upgrade);
					}
				}
			}
		}

		private void AddJournalData(Entity target, OnFire onFire)
		{
			if (m_BuildingData.HasComponent(target))
			{
				Entity e = m_CommandBuffer.CreateEntity(m_JournalDataArchetype);
				m_CommandBuffer.SetComponent(e, new AddEventJournalData(onFire.m_Event, EventDataTrackingType.Damages));
			}
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_IgniteQuery;

	private EntityArchetype m_JournalDataArchetype;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_IgniteQuery = GetEntityQuery(ComponentType.ReadOnly<Ignite>(), ComponentType.ReadOnly<Game.Common.Event>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_IgniteQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		// Collects all Ignite events into a hash map (deduplicating by target, keeping highest intensity).
		// For each unique target:
		//   - If already OnFire: updates intensity (if higher), preserves existing RescueRequest and earliest RequestFrame
		//   - If not OnFire: adds OnFire component + BatchesUpdated, adds to event's TargetElement buffer
		//   - For buildings: creates journal data for damage tracking
		//   - For buildings with upgrades: marks non-building upgrades as BatchesUpdated
		// Uses ModificationBarrier4 for deferred structural changes.
	}
}
