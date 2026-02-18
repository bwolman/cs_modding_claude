using System.Runtime.CompilerServices;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class MilestoneSystem : GameSystemBase, IMilestoneSystem
{
	private int m_LastRequired;

	private int m_NextRequired;

	private int m_Progress;

	private int m_NextMilestone;

	private CitySystem m_CitySystem;

	private ModificationEndBarrier m_ModificationEndBarrier;

	private EntityArchetype m_UnlockEventArchetype;

	private EntityArchetype m_MilestoneReachedEventArchetype;

	private EntityQuery m_MilestoneLevelGroup;

	private EntityQuery m_XPGroup;

	private EntityQuery m_MilestoneGroup;

	public int currentXP => m_Progress;

	public int requiredXP => nextRequiredXP - math.max(0, lastRequiredXP);

	public int lastRequiredXP => m_LastRequired;

	public int nextRequiredXP => m_NextRequired;

	public float progress => (float)m_Progress / (float)requiredXP;

	public int nextMilestone => m_NextMilestone;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Unlock>());
		m_MilestoneReachedEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<MilestoneReachedEvent>());
		m_MilestoneLevelGroup = GetEntityQuery(ComponentType.ReadWrite<MilestoneLevel>());
		m_XPGroup = GetEntityQuery(ComponentType.ReadOnly<XP>());
		m_MilestoneGroup = GetEntityQuery(ComponentType.ReadOnly<MilestoneData>());
		RequireForUpdate(m_MilestoneLevelGroup);
		RequireForUpdate(m_XPGroup);
		RequireForUpdate(m_MilestoneGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		MilestoneLevel singleton = m_MilestoneLevelGroup.GetSingleton<MilestoneLevel>();
		int achievedMilestone = singleton.m_AchievedMilestone;
		m_LastRequired = (TryGetMilestone(achievedMilestone, out var entity, out var milestone) ? milestone.m_XpRequried : 0);
		if (TryGetMilestone(achievedMilestone + 1, out entity, out var milestone2))
		{
			m_NextRequired = milestone2.m_XpRequried;
			if (m_CitySystem.XP >= m_NextRequired)
			{
				singleton.m_AchievedMilestone++;
				m_MilestoneLevelGroup.SetSingleton(singleton);
				NextMilestone(singleton.m_AchievedMilestone);
			}
		}
		m_Progress = m_CitySystem.XP - math.max(0, m_LastRequired);
		m_NextMilestone = singleton.m_AchievedMilestone + 1;
	}

	private void NextMilestone(int index)
	{
		EntityCommandBuffer entityCommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer();
		if (TryGetMilestone(index, out var entity, out var milestone))
		{
			Entity e = entityCommandBuffer.CreateEntity(m_MilestoneReachedEventArchetype);
			entityCommandBuffer.SetComponent(e, new MilestoneReachedEvent(entity, index));
			Entity e2 = entityCommandBuffer.CreateEntity(m_UnlockEventArchetype);
			entityCommandBuffer.SetComponent(e2, new Unlock(entity));
			PlayerMoney componentData = base.EntityManager.GetComponentData<PlayerMoney>(m_CitySystem.City);
			componentData.Add(milestone.m_Reward);
			base.EntityManager.SetComponentData(m_CitySystem.City, componentData);
			Creditworthiness componentData2 = base.EntityManager.GetComponentData<Creditworthiness>(m_CitySystem.City);
			componentData2.m_Amount += milestone.m_LoanLimit;
			base.EntityManager.SetComponentData(m_CitySystem.City, componentData2);
		}
		else
		{
			Entity e3 = entityCommandBuffer.CreateEntity(m_MilestoneReachedEventArchetype);
			entityCommandBuffer.SetComponent(e3, new MilestoneReachedEvent(Entity.Null, index));
			UnityEngine.Debug.LogWarning("Warning: did not find data for milestone " + index);
		}
	}

	private bool TryGetMilestone(int index, out Entity entity, out MilestoneData milestone)
	{
		NativeArray<Entity> nativeArray = m_MilestoneGroup.ToEntityArray(Allocator.TempJob);
		NativeArray<MilestoneData> nativeArray2 = m_MilestoneGroup.ToComponentDataArray<MilestoneData>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				if (nativeArray2[i].m_Index == index)
				{
					entity = nativeArray[i];
					milestone = nativeArray2[i];
					return true;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
		entity = default(Entity);
		milestone = default(MilestoneData);
		return false;
	}

	public void UnlockAllMilestones()
	{
		NativeArray<Entity> nativeArray = m_MilestoneGroup.ToEntityArray(Allocator.TempJob);
		NativeArray<MilestoneData> nativeArray2 = m_MilestoneGroup.ToComponentDataArray<MilestoneData>(Allocator.TempJob);
		MilestoneLevel singleton = m_MilestoneLevelGroup.GetSingleton<MilestoneLevel>();
		PlayerMoney componentData = base.EntityManager.GetComponentData<PlayerMoney>(m_CitySystem.City);
		Creditworthiness componentData2 = base.EntityManager.GetComponentData<Creditworthiness>(m_CitySystem.City);
		try
		{
			for (int i = singleton.m_AchievedMilestone; i < nativeArray2.Length; i++)
			{
				Entity entity = base.EntityManager.CreateEntity(m_UnlockEventArchetype);
				base.EntityManager.SetComponentData(entity, new Unlock(nativeArray[i]));
				singleton.m_AchievedMilestone = math.max(singleton.m_AchievedMilestone, nativeArray2[i].m_Index);
				componentData.Add(nativeArray2[i].m_Reward);
				componentData2.m_Amount += nativeArray2[i].m_LoanLimit;
			}
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
		m_MilestoneLevelGroup.SetSingleton(singleton);
		base.EntityManager.SetComponentData(m_CitySystem.City, componentData);
		base.EntityManager.SetComponentData(m_CitySystem.City, componentData2);
	}

	[Preserve]
	public MilestoneSystem()
	{
	}
}
