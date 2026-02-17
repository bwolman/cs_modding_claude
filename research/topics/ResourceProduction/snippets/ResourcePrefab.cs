using System.Collections.Generic;
using Game.Economy;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

public class ResourcePrefab : PrefabBase
{
	[SerializeField]
	public Color m_Color;

	public ResourceInEditor m_Resource;

	public bool m_IsProduceable;

	public bool m_IsTradable;

	[Tooltip("This means it's raw material which need to be extracted")]
	public bool m_IsMaterial;

	public bool m_IsLeisure;

	[Tooltip("The physical weight of the resource unit.")]
	public float m_Weight;

	[Tooltip("Initial price for a Resource. X = initial price for selling the resource. Y = initial amount of profit a Commercial company will make from selling this resource.")]
	public float2 m_InitialPrice;

	[Tooltip("How much the wealth of a household affects the probability of shopping for this Resource")]
	public float m_WealthModifier;

	[Tooltip("Base chance of shopping for this Resource. The higher the value the higher base probably a household has for shopping this Resource")]
	public float m_BaseConsumption;

	[Tooltip("Amount of Weight added to buying this Resource if a household has a vehicle, multiplied by the amount of vehicles in the household")]
	public int m_CarConsumption;

	[Tooltip("A relative importance of a Child wanting to acquire this Resource")]
	public int m_ChildWeight;

	[Tooltip("A relative importance of a Teen wanting to acquire this Resource")]
	public int m_TeenWeight;

	[Tooltip("A relative importance of a Adult wanting to acquire this Resource")]
	public int m_AdultWeight;

	[Tooltip("A relative importance of a Senior wanting to acquire this Resource")]
	public int m_ElderlyWeight;

	[Tooltip("Checkbox to determine does producing this Resource require a temperature equal or higher set in the next field")]
	public bool m_RequireTemperature;

	[Tooltip("Minimum required temperature for producing this Resource")]
	public float m_RequiredTemperature;

	[Tooltip("Determines whether or not producing this resource requires a set Natural Resource; Fertile Land, Forest, Ore, Oil")]
	public bool m_RequireNaturalResource;

	[Tooltip("How many work is needed to produce one unit, x - industrial like beverage processing work needed, y - commercial like beverage service work needed")]
	public int2 m_NeededWorkPerUnit = int2.op_Implicit(1);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ResourceData>());
	}
}
