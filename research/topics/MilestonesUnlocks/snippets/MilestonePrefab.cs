using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public class MilestonePrefab : PrefabBase
{
	public int m_Index;

	public int m_Reward;

	public int m_DevTreePoints;

	public int m_MapTiles;

	public int m_LoanLimit;

	public int m_XpRequried;

	public bool m_Major;

	public bool m_IsVictory;

	public string m_Image;

	public Color m_BackgroundColor = new Color(0f, 0f, 0f, 0f);

	public Color m_AccentColor = new Color(0.18f, 0.235f, 0.337f, 1f);

	public Color m_TextColor = Color.white;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<MilestoneData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new MilestoneData
		{
			m_Index = m_Index,
			m_Reward = m_Reward,
			m_DevTreePoints = m_DevTreePoints,
			m_MapTiles = m_MapTiles,
			m_LoanLimit = m_LoanLimit,
			m_XpRequried = m_XpRequried,
			m_Major = m_Major,
			m_IsVictory = m_IsVictory
		});
	}
}
