using Game.Agents;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Effects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class ClearSystem : GameSystemBase
{
	private EntityQuery m_ClearQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClearQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[19]
			{
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<LoadedIndex>(),
				ComponentType.ReadOnly<ElectricityFlowNode>(),
				ComponentType.ReadOnly<ElectricityFlowEdge>(),
				ComponentType.ReadOnly<WaterPipeNode>(),
				ComponentType.ReadOnly<WaterPipeEdge>(),
				ComponentType.ReadOnly<ServiceRequest>(),
				ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(),
				ComponentType.ReadOnly<Game.City.City>(),
				ComponentType.ReadOnly<SchoolSeeker>(),
				ComponentType.ReadOnly<JobSeeker>(),
				ComponentType.ReadOnly<CityStatistic>(),
				ComponentType.ReadOnly<ServiceBudgetData>(),
				ComponentType.ReadOnly<FloodCounterData>(),
				ComponentType.ReadOnly<CoordinatedMeeting>(),
				ComponentType.ReadOnly<LookingForPartner>(),
				ComponentType.ReadOnly<EffectInstance>(),
				ComponentType.ReadOnly<CreationDefinition>(),
				ComponentType.ReadOnly<TimeData>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<NetCompositionData>(),
				ComponentType.ReadOnly<PrefabData>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.EntityManager.DestroyEntity(m_ClearQuery);
	}

	[Preserve]
	public ClearSystem()
	{
	}
}
