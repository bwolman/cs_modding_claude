using System;
using System.Collections.Generic;
using Game.Agents;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Simulation;
using Game.Vehicles;
using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Companies/", new Type[] { })]
public class CompanyPrefab : ArchetypePrefab
{
	public AreaType zone;

	public float profitability;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		if (zone == AreaType.Commercial)
		{
			components.Add(ComponentType.ReadWrite<CommercialCompanyData>());
		}
		else if (zone == AreaType.Industrial)
		{
			components.Add(ComponentType.ReadWrite<IndustrialCompanyData>());
		}
		components.Add(ComponentType.ReadWrite<CompanyBrandElement>());
		components.Add(ComponentType.ReadWrite<AffiliatedBrandElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<CompanyData>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
		components.Add(ComponentType.ReadWrite<Resources>());
		components.Add(ComponentType.ReadWrite<PropertySeeker>());
		components.Add(ComponentType.ReadWrite<TripNeeded>());
		components.Add(ComponentType.ReadWrite<CompanyNotifications>());
		components.Add(ComponentType.ReadWrite<GuestVehicle>());
		if (zone == AreaType.Commercial)
		{
			components.Add(ComponentType.ReadWrite<CommercialCompany>());
		}
		else if (zone == AreaType.Industrial)
		{
			components.Add(ComponentType.ReadWrite<IndustrialCompany>());
		}
	}
}
