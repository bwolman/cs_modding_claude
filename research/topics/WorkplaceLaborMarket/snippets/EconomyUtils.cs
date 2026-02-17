using System;
using Colossal.Annotations;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Companies;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Economy;

public class EconomyUtils
{
	public static readonly int kCompanyUpdatesPerDay = 256;

	public static readonly int ResourceCount = GetResourceIndex(Resource.Last);

	public static int GetResourceIndex(Resource r)
	{
		switch (r)
		{
		case Resource.NoResource:
		case Resource.Money:
		case Resource.Grain:
		case (Resource)3uL:
		case Resource.ConvenienceFood:
		case (Resource)5uL:
		case (Resource)6uL:
		case (Resource)7uL:
		case Resource.Food:
		{
			Resource num = r - 1;
			if (num <= (Resource)3uL)
			{
				switch (num)
				{
				case Resource.NoResource:
					return 0;
				case Resource.Money:
					return 1;
				case (Resource)3uL:
					return 2;
				case Resource.Grain:
					goto end_IL_0007;
				}
			}
			if (r != Resource.Food)
			{
				break;
			}
			return 3;
		}
		case Resource.Vegetables:
			return 4;
		case Resource.Meals:
			return 5;
		case Resource.Wood:
			return 6;
		case Resource.Timber:
			return 7;
		case Resource.Paper:
			return 8;
		case Resource.Furniture:
			return 9;
		case Resource.Vehicles:
			return 10;
		case Resource.Lodging:
			return 11;
		case Resource.UnsortedMail:
			return 12;
		case Resource.LocalMail:
			return 13;
		case Resource.OutgoingMail:
			return 14;
		case Resource.Oil:
			return 15;
		case Resource.Petrochemicals:
			return 16;
		case Resource.Ore:
			return 17;
		case Resource.Plastics:
			return 18;
		case Resource.Metals:
			return 19;
		case Resource.Electronics:
			return 20;
		case Resource.Software:
			return 21;
		case Resource.Coal:
			return 22;
		case Resource.Stone:
			return 23;
		case Resource.Livestock:
			return 24;
		case Resource.Cotton:
			return 25;
		case Resource.Steel:
			return 26;
		case Resource.Minerals:
			return 27;
		case Resource.Concrete:
			return 28;
		case Resource.Machinery:
			return 29;
		case Resource.Chemicals:
			return 30;
		case Resource.Pharmaceuticals:
			return 31;
		case Resource.Beverages:
			return 32;
		case Resource.Textiles:
			return 33;
		case Resource.Telecom:
			return 34;
		case Resource.Financial:
			return 35;
		case Resource.Media:
			return 36;
		case Resource.Entertainment:
			return 37;
		case Resource.Recreation:
			return 38;
		case Resource.Garbage:
			return 39;
		case Resource.Fish:
			return 40;
		case Resource.Last:
			{
				return 41;
			}
			end_IL_0007:
			break;
		}
		return -1;
	}

	public static float3 BuildPseudoTradeCost(float distance, IndustrialProcessData process, ref ComponentLookup<ResourceData> resourceDatas, ResourcePrefabs resourcePrefabs)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		float3 val = default(float3);
		if (process.m_Input1.m_Resource != Resource.NoResource)
		{
			val.y = GetTransportCost(distance, process.m_Input1.m_Resource, 20000, resourceDatas[resourcePrefabs[process.m_Input1.m_Resource]].m_Weight);
		}
		if (process.m_Input2.m_Resource != Resource.NoResource)
		{
			val.z = GetTransportCost(distance, process.m_Input2.m_Resource, 20000, resourceDatas[resourcePrefabs[process.m_Input2.m_Resource]].m_Weight);
		}
		val.x = (float)GetTransportCost(distance, process.m_Output.m_Resource, 20000, resourceDatas[resourcePrefabs[process.m_Output.m_Resource]].m_Weight) - (val.y * (float)process.m_Input1.m_Amount - val.z * (float)process.m_Input2.m_Amount) / (float)process.m_Output.m_Amount;
		return val / 20000f;
	}

	public static Resource GetAllResources()
	{
		return (Resource)2199023255551uL;
	}

	public static bool IsProducedFrom(Resource product, Resource material)
	{
		return material switch
		{
			Resource.Grain => (product & (Resource)4295032844uL) != 0, 
			Resource.Wood => (product & Resource.Timber) != 0, 
			Resource.Oil => (product & Resource.Petrochemicals) != 0, 
			Resource.Ore => (product & Resource.Metals) != 0, 
			Resource.Petrochemicals => (product & (Resource)1074003968uL) != 0, 
			Resource.Plastics => (product & (Resource)1049600uL) != 0, 
			Resource.Electronics => (product & (Resource)17181966336uL) != 0, 
			Resource.Metals => (product & (Resource)536871936uL) != 0, 
			Resource.Coal => (product & Resource.Steel) != 0, 
			Resource.Stone => (product & (Resource)402653184uL) != 0, 
			Resource.Cotton => (product & Resource.Textiles) != 0, 
			Resource.Livestock => (product & (Resource)8589934604uL) != 0, 
			Resource.Vegetables => (product & (Resource)4294967304uL) != 0, 
			Resource.Steel => (product & Resource.Machinery) != 0, 
			Resource.Minerals => (product & (Resource)1074790400uL) != 0, 
			Resource.Chemicals => (product & (Resource)2147483904uL) != 0, 
			Resource.Timber => (product & Resource.Furniture) != 0, 
			Resource.Software => (product & (Resource)120259084288uL) != 0, 
			Resource.Food => (product & (Resource)2080uL) != 0, 
			Resource.Beverages => (product & (Resource)137438955552uL) != 0, 
			Resource.Fish => (product & (Resource)12uL) != 0, 
			_ => false, 
		};
	}

	public static Resource GetResources([CanBeNull] ResourceInEditor[] resources, Resource defaultResources = Resource.NoResource)
	{
		Resource resource = defaultResources;
		if (resources != null && resources.Length != 0)
		{
			resource = Resource.NoResource;
			foreach (ResourceInEditor resource2 in resources)
			{
				resource |= GetResource(resource2);
			}
		}
		return resource;
	}

	public static Resource GetResource(ResourceInEditor resource)
	{
		return GetResource((int)(resource - 1));
	}

	public static Resource GetResource(int index)
	{
		if (index < 0)
		{
			return Resource.NoResource;
		}
		return (Resource)(1L << index);
	}

	public static int CountResources(Resource resource)
	{
		int num = 0;
		ResourceIterator iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			if ((resource & iterator.resource) != Resource.NoResource)
			{
				num++;
			}
		}
		return num;
	}

	public static TradeCost GetTradeCost(Resource resource, DynamicBuffer<TradeCost> costs)
	{
		for (int i = 0; i < costs.Length; i++)
		{
			TradeCost result = costs[i];
			if (result.m_Resource == resource)
			{
				return result;
			}
		}
		return new TradeCost
		{
			m_Resource = resource
		};
	}

	public static long GetLastTradeRequestTime(DynamicBuffer<TradeCost> costs)
	{
		long num = 0L;
		for (int i = 0; i < costs.Length; i++)
		{
			TradeCost tradeCost = costs[i];
			if (tradeCost.m_LastTransferRequestTime > num)
			{
				num = tradeCost.m_LastTransferRequestTime;
			}
		}
		return num;
	}

	public static void SetTradeCost(Resource resource, TradeCost newcost, DynamicBuffer<TradeCost> costs, bool keepLastTime, float buyLerp = 1f, float sellLerp = 1f)
	{
		Assert.IsTrue(!float.IsNaN(newcost.m_SellCost));
		Assert.IsTrue(!float.IsNaN(newcost.m_BuyCost));
		for (int i = 0; i < costs.Length; i++)
		{
			TradeCost tradeCost = costs[i];
			if (tradeCost.m_Resource == resource)
			{
				if (keepLastTime)
				{
					newcost.m_LastTransferRequestTime = tradeCost.m_LastTransferRequestTime;
				}
				newcost.m_BuyCost = ((tradeCost.m_BuyCost == 0f) ? newcost.m_BuyCost : math.lerp(tradeCost.m_BuyCost, newcost.m_BuyCost, buyLerp));
				newcost.m_SellCost = ((tradeCost.m_SellCost == 0f) ? newcost.m_SellCost : math.lerp(tradeCost.m_SellCost, newcost.m_SellCost, sellLerp));
				costs[i] = newcost;
				return;
			}
		}
		costs.Add(newcost);
	}

	public static void SetResources(Resource resource, DynamicBuffer<Resources> resources, int amount)
	{
		for (int i = 0; i < resources.Length; i++)
		{
			Resources resources2 = resources[i];
			if (resources2.m_Resource == resource)
			{
				resources2.m_Amount = amount;
				resources[i] = resources2;
				return;
			}
		}
		resources.Add(new Resources
		{
			m_Resource = resource,
			m_Amount = amount
		});
	}

	public static int GetResources(Resource resource, DynamicBuffer<Resources> resources)
	{
		for (int i = 0; i < resources.Length; i++)
		{
			Resources resources2 = resources[i];
			if (resources2.m_Resource == resource)
			{
				return resources2.m_Amount;
			}
		}
		return 0;
	}

	public static int AddResources(Resource resource, int amount, DynamicBuffer<Resources> resources)
	{
		for (int i = 0; i < resources.Length; i++)
		{
			Resources resources2 = resources[i];
			if (resources2.m_Resource == resource)
			{
				resources2.m_Amount = (int)math.clamp((long)resources2.m_Amount + (long)amount, -2147483648L, 2147483647L);
				resources[i] = resources2;
				return resources2.m_Amount;
			}
		}
		resources.Add(new Resources
		{
			m_Resource = resource,
			m_Amount = amount
		});
		return amount;
	}

	public static Resource GetResource(AvailableResource available)
	{
		return available switch
		{
			AvailableResource.GrainSupply => Resource.Grain, 
			AvailableResource.TextilesSupply => Resource.Textiles, 
			AvailableResource.VegetableSupply => Resource.Vegetables, 
			AvailableResource.ConvenienceFoodSupply => Resource.ConvenienceFood, 
			AvailableResource.PaperSupply => Resource.Paper, 
			AvailableResource.VehiclesSupply => Resource.Vehicles, 
			AvailableResource.WoodSupply => Resource.Wood, 
			AvailableResource.MetalsSupply => Resource.Metals, 
			AvailableResource.OilSupply => Resource.Oil, 
			AvailableResource.OreSupply => Resource.Ore, 
			AvailableResource.PetrochemicalsSupply => Resource.Petrochemicals, 
			AvailableResource.ElectronicsSupply => Resource.Electronics, 
			AvailableResource.PlasticsSupply => Resource.Plastics, 
			AvailableResource.CoalSupply => Resource.Coal, 
			AvailableResource.StoneSupply => Resource.Stone, 
			AvailableResource.LivestockSupply => Resource.Livestock, 
			AvailableResource.CottonSupply => Resource.Cotton, 
			AvailableResource.SteelSupply => Resource.Steel, 
			AvailableResource.MineralSupply => Resource.Minerals, 
			AvailableResource.ChemicalSupply => Resource.Chemicals, 
			AvailableResource.BeveragesSupply => Resource.Beverages, 
			AvailableResource.TimberSupply => Resource.Timber, 
			AvailableResource.MachinerySupply => Resource.Machinery, 
			AvailableResource.FishSupply => Resource.Fish, 
			_ => Resource.NoResource, 
		};
	}

	public static AvailableResource GetAvailableResourceSupply(Resource resource)
	{
		return resource switch
		{
			Resource.Grain => AvailableResource.GrainSupply, 
			Resource.Wood => AvailableResource.WoodSupply, 
			Resource.ConvenienceFood => AvailableResource.ConvenienceFoodSupply, 
			Resource.Paper => AvailableResource.PaperSupply, 
			Resource.Vehicles => AvailableResource.VehiclesSupply, 
			Resource.Oil => AvailableResource.OilSupply, 
			Resource.Petrochemicals => AvailableResource.PetrochemicalsSupply, 
			Resource.Ore => AvailableResource.OreSupply, 
			Resource.Metals => AvailableResource.MetalsSupply, 
			Resource.Electronics => AvailableResource.ElectronicsSupply, 
			Resource.Plastics => AvailableResource.PlasticsSupply, 
			Resource.Coal => AvailableResource.CoalSupply, 
			Resource.Stone => AvailableResource.StoneSupply, 
			Resource.Cotton => AvailableResource.CottonSupply, 
			Resource.Livestock => AvailableResource.LivestockSupply, 
			Resource.Vegetables => AvailableResource.VegetableSupply, 
			Resource.Steel => AvailableResource.SteelSupply, 
			Resource.Minerals => AvailableResource.MineralSupply, 
			Resource.Chemicals => AvailableResource.ChemicalSupply, 
			Resource.Timber => AvailableResource.TimberSupply, 
			Resource.Machinery => AvailableResource.MachinerySupply, 
			Resource.Beverages => AvailableResource.BeveragesSupply, 
			Resource.Textiles => AvailableResource.TextilesSupply, 
			Resource.Fish => AvailableResource.FishSupply, 
			_ => AvailableResource.Count, 
		};
	}

	public static int GetHouseholdTotalWealth(Household householdData, DynamicBuffer<Resources> resources)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		int resources2 = GetResources(Resource.Money, resources);
		return (int)math.min(2147483647L, (long)householdData.m_Resources + (long)resources2);
	}

	public static int GetHouseholdSpendableMoney(Household householdData, DynamicBuffer<Resources> resources, ref BufferLookup<Renter> m_RenterBufs, ref ComponentLookup<ConsumptionData> consumptionDatas, ref ComponentLookup<PrefabRef> prefabRefs, PropertyRenter propertyRenter)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		int num = GetResources(Resource.Money, resources);
		if (propertyRenter.m_Property != Entity.Null && m_RenterBufs.HasBuffer(propertyRenter.m_Property))
		{
			int length = m_RenterBufs[propertyRenter.m_Property].Length;
			num -= propertyRenter.m_Rent;
			Entity prefab = prefabRefs[propertyRenter.m_Property].m_Prefab;
			if (length == 0)
			{
				Debug.LogWarning((object)$"Property:{propertyRenter.m_Property.Index} has 0 renter");
			}
			int num2 = consumptionDatas[prefab].m_Upkeep / (length + 1);
			num -= num2;
		}
		return num;
	}

	public static int GetHouseholdIncome(DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Worker> workers, ref ComponentLookup<Citizen> citizenDatas, ref ComponentLookup<HealthProblem> healthProblems, ref EconomyParameterData economyParameters, NativeArray<int> taxRates)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		for (int i = 0; i < citizens.Length; i++)
		{
			Entity citizen = citizens[i].m_Citizen;
			if (CitizenUtils.IsDead(citizen, ref healthProblems))
			{
				continue;
			}
			CitizenAge age = citizenDatas[citizen].GetAge();
			if (workers.HasComponent(citizen))
			{
				int level = workers[citizen].m_Level;
				int wage = economyParameters.GetWage(level);
				num += wage;
				int num2 = wage - economyParameters.m_ResidentialMinimumEarnings;
				if (num2 > 0)
				{
					num -= Mathf.RoundToInt((float)num2 * ((float)TaxSystem.GetResidentialTaxRate(level, taxRates) / 100f));
				}
				continue;
			}
			switch (age)
			{
			case CitizenAge.Child:
			case CitizenAge.Teen:
				num += economyParameters.m_FamilyAllowance;
				continue;
			case CitizenAge.Elderly:
				num += economyParameters.m_Pension;
				continue;
			}
			if ((float)citizenDatas[citizen].m_UnemploymentCounter < economyParameters.m_UnemploymentAllowanceMaxDays * (float)PayWageSystem.kUpdatesPerDay)
			{
				num += economyParameters.m_UnemploymentBenefit;
			}
		}
		return Mathf.RoundToInt((float)num);
	}

	public static int GetCompanyTotalWorth(bool isIndustrial, IndustrialProcessData industrialProcessData, DynamicBuffer<Resources> resources, DynamicBuffer<OwnedVehicle> vehicles, ref BufferLookup<LayoutElement> layouts, ref ComponentLookup<Game.Vehicles.DeliveryTruck> deliveryTrucks, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		long num = GetCompanyTotalWorth(isIndustrial, industrialProcessData, resources, resourcePrefabs, ref resourceDatas);
		if (!vehicles.IsCreated)
		{
			return (int)math.clamp(num, -2147483648L, 2147483647L);
		}
		for (int i = 0; i < vehicles.Length; i++)
		{
			Entity vehicle = vehicles[i].m_Vehicle;
			if (!deliveryTrucks.HasComponent(vehicle))
			{
				continue;
			}
			DynamicBuffer<LayoutElement> val = default(DynamicBuffer<LayoutElement>);
			if (layouts.HasBuffer(vehicle))
			{
				val = layouts[vehicle];
			}
			if (val.IsCreated && val.Length != 0)
			{
				for (int j = 0; j < val.Length; j++)
				{
					Entity vehicle2 = val[j].m_Vehicle;
					if (deliveryTrucks.HasComponent(vehicle2) && (deliveryTrucks[vehicle2].m_State & DeliveryTruckFlags.Loaded) != 0)
					{
						Game.Vehicles.DeliveryTruck deliveryTruck = deliveryTrucks[vehicle2];
						int num2 = Mathf.RoundToInt((float)deliveryTruck.m_Amount * GetIndustrialPrice(deliveryTruck.m_Resource, resourcePrefabs, ref resourceDatas));
						num += num2;
					}
				}
			}
			else if ((deliveryTrucks[vehicle].m_State & DeliveryTruckFlags.Loaded) != 0)
			{
				Game.Vehicles.DeliveryTruck deliveryTruck2 = deliveryTrucks[vehicle];
				int num3 = Mathf.RoundToInt((float)deliveryTruck2.m_Amount * GetIndustrialPrice(deliveryTruck2.m_Resource, resourcePrefabs, ref resourceDatas));
				num += num3;
			}
		}
		return (int)math.clamp(num, -2147483648L, 2147483647L);
	}

	public static int GetCompanyTotalWorth(bool isIndustrial, IndustrialProcessData industrialProcessData, DynamicBuffer<Resources> resources, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas)
	{
		long num = 0L;
		Resource resource = Resource.NoResource;
		if (!isIndustrial)
		{
			resource = industrialProcessData.m_Output.m_Resource;
		}
		for (int i = 0; i < resources.Length; i++)
		{
			num = ((isIndustrial || resources[i].m_Resource != resource) ? (num + (long)((float)resources[i].m_Amount * GetIndustrialPrice(resources[i].m_Resource, resourcePrefabs, ref resourceDatas))) : (num + (long)((float)resources[i].m_Amount * GetMarketPrice(resources[i].m_Resource, resourcePrefabs, ref resourceDatas))));
		}
		return (int)math.clamp(num, -2147483648L, 2147483647L);
	}

	public static void AddResources(DynamicBuffer<Resources> from, DynamicBuffer<Resources> to)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < from.Length; i++)
		{
			AddResources(from[i].m_Resource, from[i].m_Amount, to);
		}
	}

	public static int GetTotalStorageUsed(DynamicBuffer<Resources> resources)
	{
		int num = 0;
		for (int i = 0; i < resources.Length; i++)
		{
			Resources resources2 = resources[i];
			if (resources2.m_Resource != Resource.Money)
			{
				num += resources2.m_Amount;
			}
		}
		return num;
	}

	public static float GetServicePriceMultiplier(float serviceAvailable, int maxServiceAvailable)
	{
		return math.lerp(0.7f, 1.3f, math.saturate(1f - serviceAvailable / (float)maxServiceAvailable));
	}

	public static int GetTransportCost(float distance, Resource resource, int amount, float weight)
	{
		if (resource != Resource.NoResource)
		{
			return Mathf.RoundToInt(distance * 0.03f * weight * (float)(1 + Mathf.FloorToInt((float)(amount / 1000))));
		}
		return 0;
	}

	public static float GetTransportCost(float distance, int amount, float weight, StorageTransferFlags flags)
	{
		if ((flags & StorageTransferFlags.Car) != 0)
		{
			return distance * 0.02f * (10f + weight * (float)amount / 1000f);
		}
		if ((flags & StorageTransferFlags.Transport) != 0)
		{
			return distance * 0.002f * weight * (float)(10 + amount / 10000);
		}
		return distance * 0.002f * (10f + weight * (float)amount / 1000f);
	}

	public static string GetNames(Resource r)
	{
		string text = "";
		for (int i = 0; i < ResourceCount; i++)
		{
			if ((r & GetResource(i)) != Resource.NoResource)
			{
				text = text + GetName(GetResource(i)) + "|";
			}
		}
		return text;
	}

	public static string GetName(Resource r)
	{
		switch (r)
		{
		case Resource.NoResource:
		case Resource.Money:
		case Resource.Grain:
		case (Resource)3uL:
		case Resource.ConvenienceFood:
		case (Resource)5uL:
		case (Resource)6uL:
		case (Resource)7uL:
		case Resource.Food:
		case (Resource)9uL:
		case (Resource)10uL:
		case (Resource)11uL:
		case (Resource)12uL:
		case (Resource)13uL:
		case (Resource)14uL:
		case (Resource)15uL:
		case Resource.Vegetables:
			if (r <= Resource.Food)
			{
				switch (r)
				{
				case Resource.NoResource:
					return "none";
				case Resource.Grain:
					return "grain";
				case Resource.ConvenienceFood:
					return "conv.food";
				case Resource.Food:
					return "food";
				case Resource.Money:
					return "money";
				case (Resource)3uL:
				case (Resource)5uL:
				case (Resource)6uL:
				case (Resource)7uL:
					goto end_IL_0007;
				}
			}
			if (r != Resource.Vegetables)
			{
				break;
			}
			return "vegetables";
		case Resource.Furniture:
			return "furniture";
		case Resource.Meals:
			return "meals";
		case Resource.Paper:
			return "paper";
		case Resource.Timber:
			return "timber";
		case Resource.Vehicles:
			return "vehicles";
		case Resource.Wood:
			return "wood";
		case Resource.Lodging:
			return "lodging";
		case Resource.UnsortedMail:
			return "unsorted mail";
		case Resource.LocalMail:
			return "local mail";
		case Resource.OutgoingMail:
			return "outgoing mail";
		case Resource.Oil:
			return "oil";
		case Resource.Petrochemicals:
			return "petrochemicals";
		case Resource.Ore:
			return "ore";
		case Resource.Plastics:
			return "plastics";
		case Resource.Metals:
			return "metals";
		case Resource.Electronics:
			return "electronics";
		case Resource.Software:
			return "software";
		case Resource.Coal:
			return "coal";
		case Resource.Stone:
			return "stone";
		case Resource.Livestock:
			return "livestock";
		case Resource.Cotton:
			return "cotton";
		case Resource.Steel:
			return "steel";
		case Resource.Minerals:
			return "minerals";
		case Resource.Concrete:
			return "concrete";
		case Resource.Machinery:
			return "machinery";
		case Resource.Chemicals:
			return "chemicals";
		case Resource.Pharmaceuticals:
			return "pharmaceuticals";
		case Resource.Beverages:
			return "beverages";
		case Resource.Textiles:
			return "textiles";
		case Resource.Telecom:
			return "telecom";
		case Resource.Financial:
			return "financial";
		case Resource.Media:
			return "media";
		case Resource.Entertainment:
			return "entertainment";
		case Resource.Recreation:
			return "recreation";
		case Resource.Garbage:
			return "garbage";
		case Resource.Fish:
			{
				return "fish";
			}
			end_IL_0007:
			break;
		}
		return "none";
	}

	public static FixedString32Bytes GetNameFixed(Resource r)
	{
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_043e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		switch (r)
		{
		case Resource.NoResource:
		case Resource.Money:
		case Resource.Grain:
		case (Resource)3uL:
		case Resource.ConvenienceFood:
		case (Resource)5uL:
		case (Resource)6uL:
		case (Resource)7uL:
		case Resource.Food:
		case (Resource)9uL:
		case (Resource)10uL:
		case (Resource)11uL:
		case (Resource)12uL:
		case (Resource)13uL:
		case (Resource)14uL:
		case (Resource)15uL:
		case Resource.Vegetables:
			if (r <= Resource.Food)
			{
				switch (r)
				{
				case Resource.NoResource:
					return FixedString32Bytes.op_Implicit("none");
				case Resource.Grain:
					return FixedString32Bytes.op_Implicit("grain");
				case Resource.ConvenienceFood:
					return FixedString32Bytes.op_Implicit("conv.food");
				case Resource.Food:
					return FixedString32Bytes.op_Implicit("food");
				case Resource.Money:
					return FixedString32Bytes.op_Implicit("money");
				case (Resource)3uL:
				case (Resource)5uL:
				case (Resource)6uL:
				case (Resource)7uL:
					goto end_IL_0007;
				}
			}
			if (r != Resource.Vegetables)
			{
				break;
			}
			return FixedString32Bytes.op_Implicit("vegetables");
		case Resource.Furniture:
			return FixedString32Bytes.op_Implicit("furniture");
		case Resource.Meals:
			return FixedString32Bytes.op_Implicit("meals");
		case Resource.Paper:
			return FixedString32Bytes.op_Implicit("paper");
		case Resource.Timber:
			return FixedString32Bytes.op_Implicit("timber");
		case Resource.Vehicles:
			return FixedString32Bytes.op_Implicit("vehicles");
		case Resource.Wood:
			return FixedString32Bytes.op_Implicit("wood");
		case Resource.Lodging:
			return FixedString32Bytes.op_Implicit("lodging");
		case Resource.UnsortedMail:
			return FixedString32Bytes.op_Implicit("unsorted mail");
		case Resource.LocalMail:
			return FixedString32Bytes.op_Implicit("local mail");
		case Resource.OutgoingMail:
			return FixedString32Bytes.op_Implicit("outgoing mail");
		case Resource.Oil:
			return FixedString32Bytes.op_Implicit("oil");
		case Resource.Petrochemicals:
			return FixedString32Bytes.op_Implicit("petrochemicals");
		case Resource.Ore:
			return FixedString32Bytes.op_Implicit("ore");
		case Resource.Plastics:
			return FixedString32Bytes.op_Implicit("plastics");
		case Resource.Metals:
			return FixedString32Bytes.op_Implicit("metals");
		case Resource.Electronics:
			return FixedString32Bytes.op_Implicit("electronics");
		case Resource.Software:
			return FixedString32Bytes.op_Implicit("software");
		case Resource.Coal:
			return FixedString32Bytes.op_Implicit("coal");
		case Resource.Stone:
			return FixedString32Bytes.op_Implicit("stone");
		case Resource.Livestock:
			return FixedString32Bytes.op_Implicit("livestock");
		case Resource.Cotton:
			return FixedString32Bytes.op_Implicit("cotton");
		case Resource.Steel:
			return FixedString32Bytes.op_Implicit("steel");
		case Resource.Minerals:
			return FixedString32Bytes.op_Implicit("minerals");
		case Resource.Concrete:
			return FixedString32Bytes.op_Implicit("concrete");
		case Resource.Machinery:
			return FixedString32Bytes.op_Implicit("machinery");
		case Resource.Chemicals:
			return FixedString32Bytes.op_Implicit("chemicals");
		case Resource.Pharmaceuticals:
			return FixedString32Bytes.op_Implicit("pharmaceuticals");
		case Resource.Beverages:
			return FixedString32Bytes.op_Implicit("beverages");
		case Resource.Textiles:
			return FixedString32Bytes.op_Implicit("textiles");
		case Resource.Telecom:
			return FixedString32Bytes.op_Implicit("telecom");
		case Resource.Financial:
			return FixedString32Bytes.op_Implicit("financial");
		case Resource.Media:
			return FixedString32Bytes.op_Implicit("media");
		case Resource.Entertainment:
			return FixedString32Bytes.op_Implicit("entertainment");
		case Resource.Recreation:
			return FixedString32Bytes.op_Implicit("recreation");
		case Resource.Garbage:
			return FixedString32Bytes.op_Implicit("garbage");
		case Resource.Fish:
			{
				return FixedString32Bytes.op_Implicit("fish");
			}
			end_IL_0007:
			break;
		}
		return FixedString32Bytes.op_Implicit("none");
	}

	public static Color GetResourceColor(Resource r)
	{
		//IL_0b88: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a28: Unknown result type (might be due to invalid IL or missing references)
		//IL_0944: Unknown result type (might be due to invalid IL or missing references)
		//IL_097a: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0906: Unknown result type (might be due to invalid IL or missing references)
		//IL_085c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0892: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_081e: Unknown result type (might be due to invalid IL or missing references)
		//IL_073a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0770: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_064e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05da: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b84: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a62: Unknown result type (might be due to invalid IL or missing references)
		//IL_097e: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_090a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0940: Unknown result type (might be due to invalid IL or missing references)
		//IL_0896: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0822: Unknown result type (might be due to invalid IL or missing references)
		//IL_0858: Unknown result type (might be due to invalid IL or missing references)
		//IL_0774: Unknown result type (might be due to invalid IL or missing references)
		//IL_07aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0700: Unknown result type (might be due to invalid IL or missing references)
		//IL_0736: Unknown result type (might be due to invalid IL or missing references)
		//IL_0652: Unknown result type (might be due to invalid IL or missing references)
		//IL_0688: Unknown result type (might be due to invalid IL or missing references)
		//IL_05de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0614: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_052c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ada: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b10: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_068c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_056a: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
		switch (r)
		{
		case Resource.NoResource:
		case Resource.Money:
		case Resource.Grain:
		case (Resource)3uL:
		case Resource.ConvenienceFood:
		case (Resource)5uL:
		case (Resource)6uL:
		case (Resource)7uL:
		case Resource.Food:
		case (Resource)9uL:
		case (Resource)10uL:
		case (Resource)11uL:
		case (Resource)12uL:
		case (Resource)13uL:
		case (Resource)14uL:
		case (Resource)15uL:
		case Resource.Vegetables:
			if (r <= Resource.Food)
			{
				switch (r)
				{
				case Resource.NoResource:
					return Color.white;
				case Resource.Grain:
					return new Color
					{
						r = 0.59607846f,
						g = 48f / 85f,
						b = 0.427451f,
						a = 1f
					};
				case Resource.ConvenienceFood:
					return new Color
					{
						r = 0.7607843f,
						g = 0.5480962f,
						b = 0.35294122f,
						a = 1f
					};
				case Resource.Food:
					return new Color
					{
						r = 0.59607846f,
						g = 0.1607843f,
						b = 0.26029193f,
						a = 1f
					};
				case Resource.Money:
					return new Color
					{
						r = 26f / 85f,
						g = 0.46274513f,
						b = 0.3372549f,
						a = 1f
					};
				case (Resource)3uL:
				case (Resource)5uL:
				case (Resource)6uL:
				case (Resource)7uL:
					goto end_IL_0007;
				}
			}
			if (r != Resource.Vegetables)
			{
				break;
			}
			return new Color
			{
				r = 0.55274945f,
				g = 87f / 106f,
				b = 0.37682444f,
				a = 1f
			};
		case Resource.Furniture:
			return new Color
			{
				r = 0.45098042f,
				g = 0.35686275f,
				b = 0.20000002f,
				a = 1f
			};
		case Resource.Meals:
			return new Color
			{
				r = 16f / 51f,
				g = 0.654902f,
				b = 29f / 51f,
				a = 1f
			};
		case Resource.Paper:
			return new Color
			{
				r = 0.8000001f,
				g = 0.7960785f,
				b = 0.6313726f,
				a = 1f
			};
		case Resource.Timber:
			return new Color
			{
				r = 0.4039216f,
				g = 0.28627452f,
				b = 13f / 85f,
				a = 1f
			};
		case Resource.Vehicles:
			return new Color
			{
				r = 16f / 51f,
				g = 0.38823533f,
				b = 0.47058827f,
				a = 1f
			};
		case Resource.Wood:
			return new Color
			{
				r = 0.4039216f,
				g = 0.3529412f,
				b = 0.29803923f,
				a = 1f
			};
		case Resource.Lodging:
			return new Color
			{
				r = 0.32156864f,
				g = 0.3803922f,
				b = 0.5921569f,
				a = 1f
			};
		case Resource.UnsortedMail:
			return new Color
			{
				r = 1f,
				g = 0.9921569f,
				b = 43f / 85f,
				a = 1f
			};
		case Resource.LocalMail:
			return new Color
			{
				r = 37f / 51f,
				g = 61f / 85f,
				b = 0.36078432f,
				a = 1f
			};
		case Resource.OutgoingMail:
			return new Color
			{
				r = 0.5372549f,
				g = 8f / 15f,
				b = 24f / 85f,
				a = 1f
			};
		case Resource.Oil:
			return new Color
			{
				r = 0.16862746f,
				g = 2f / 15f,
				b = 4f / 51f,
				a = 1f
			};
		case Resource.Petrochemicals:
			return new Color
			{
				r = 0.19032574f,
				g = 0.21465941f,
				b = 0.4433962f,
				a = 1f
			};
		case Resource.Ore:
			return new Color
			{
				r = 0.26143643f,
				g = 0.43639776f,
				b = 0.4433962f,
				a = 1f
			};
		case Resource.Plastics:
			return new Color
			{
				r = 0.29803923f,
				g = 0.4901961f,
				b = 0.86666673f,
				a = 1f
			};
		case Resource.Metals:
			return new Color
			{
				r = 0.48627454f,
				g = 0.48627454f,
				b = 0.48627454f,
				a = 1f
			};
		case Resource.Electronics:
			return new Color
			{
				r = 63f / 85f,
				g = 1f,
				b = 0.47450984f,
				a = 1f
			};
		case Resource.Software:
			return new Color
			{
				r = 0.7843138f,
				g = 0.6745098f,
				b = 0.7725491f,
				a = 1f
			};
		case Resource.Coal:
			return new Color
			{
				r = 0.1137255f,
				g = 0.1137255f,
				b = 0.14901961f,
				a = 1f
			};
		case Resource.Stone:
			return new Color
			{
				r = 0.227451f,
				g = 0.2509804f,
				b = 0.3019608f,
				a = 1f
			};
		case Resource.Livestock:
			return new Color
			{
				r = 0.7725491f,
				g = 0.7725491f,
				b = 0.7725491f,
				a = 1f
			};
		case Resource.Cotton:
			return new Color
			{
				r = 0.9568628f,
				g = 0.9568628f,
				b = 0.9568628f,
				a = 1f
			};
		case Resource.Steel:
			return new Color
			{
				r = 28f / 51f,
				g = 0.5921569f,
				b = 0.6039216f,
				a = 1f
			};
		case Resource.Minerals:
			return new Color
			{
				r = 46f / 85f,
				g = 0.4431373f,
				b = 0.6313726f,
				a = 1f
			};
		case Resource.Concrete:
			return new Color
			{
				r = 0.4039216f,
				g = 0.42352945f,
				b = 44f / 85f,
				a = 1f
			};
		case Resource.Machinery:
			return new Color
			{
				r = 27f / 85f,
				g = 0.36078432f,
				b = 0.9490197f,
				a = 1f
			};
		case Resource.Chemicals:
			return new Color
			{
				r = 0.39373434f,
				g = 0.7924528f,
				b = 0.49253064f,
				a = 1f
			};
		case Resource.Pharmaceuticals:
			return new Color
			{
				r = 0.78823537f,
				g = 47f / 85f,
				b = 0.6039216f,
				a = 1f
			};
		case Resource.Beverages:
			return new Color
			{
				r = 0.7843138f,
				g = 0.8000001f,
				b = 28f / 51f,
				a = 1f
			};
		case Resource.Textiles:
			return new Color
			{
				r = 0.9803922f,
				g = 28f / 51f,
				b = 0.9490197f,
				a = 1f
			};
		case Resource.Telecom:
			return new Color
			{
				r = 0.49803925f,
				g = 0.7960785f,
				b = 0.7058824f,
				a = 1f
			};
		case Resource.Financial:
			return new Color
			{
				r = 0.21264978f,
				g = 0.5283019f,
				b = 0.21951182f,
				a = 1f
			};
		case Resource.Media:
			return new Color
			{
				r = 0.2509804f,
				g = 0.80392164f,
				b = 0.83921576f,
				a = 1f
			};
		case Resource.Entertainment:
			return new Color
			{
				r = 0.91372555f,
				g = 0.3254902f,
				b = 0.3254902f,
				a = 1f
			};
		case Resource.Recreation:
			return new Color
			{
				r = 0.83921576f,
				g = 0.1764706f,
				b = 0.44705886f,
				a = 1f
			};
		case Resource.Garbage:
			return new Color
			{
				r = 0.3019608f,
				g = 18f / 85f,
				b = 18f / 85f,
				a = 1f
			};
		case Resource.Fish:
			{
				return new Color
				{
					r = 0.9725491f,
					g = 0.7725491f,
					b = 0.7725491f,
					a = 1f
				};
			}
			end_IL_0007:
			break;
		}
		return Color.black;
	}

	public static float GetIndustrialPrice(Resource r, ResourcePrefabs prefabs, ref ComponentLookup<ResourceData> resourceDatas)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Entity val = prefabs[r];
		if (resourceDatas.HasComponent(val))
		{
			return resourceDatas[val].m_Price.x;
		}
		return 0f;
	}

	public static float GetServicePrice(Resource r, ResourcePrefabs prefabs, ref ComponentLookup<ResourceData> resourceDatas)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Entity val = prefabs[r];
		if (resourceDatas.HasComponent(val))
		{
			return resourceDatas[val].m_Price.y;
		}
		return 0f;
	}

	public static float GetMarketPrice(Resource r, ResourcePrefabs prefabs, ref ComponentLookup<ResourceData> resourceDatas)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Entity val = prefabs[r];
		if (resourceDatas.HasComponent(val))
		{
			return resourceDatas[val].m_Price.x + resourceDatas[val].m_Price.y;
		}
		return 0f;
	}

	public static float GetMarketPrice(Resource r, ResourcePrefabs prefabs, EntityManager entityManager)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		Entity val = prefabs[r];
		ResourceData resourceData = default(ResourceData);
		if (EntitiesExtensions.TryGetComponent<ResourceData>(entityManager, val, ref resourceData))
		{
			return resourceData.m_Price.x + resourceData.m_Price.y;
		}
		return 0f;
	}

	public static float GetMarketPrice(ResourceData data)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return data.m_Price.x + data.m_Price.y;
	}

	public static float GetWeight(EntityManager entityManager, Resource r, ResourcePrefabs prefabs)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (r == Resource.NoResource || r == Resource.All || r == Resource.Last)
		{
			return 0f;
		}
		ResourceData resourceData = default(ResourceData);
		if (!EntitiesExtensions.TryGetComponent<ResourceData>(entityManager, prefabs[r], ref resourceData))
		{
			return 1f;
		}
		return resourceData.m_Weight;
	}

	public static float GetWeight(Resource r, ResourcePrefabs prefabs, ref ComponentLookup<ResourceData> datas)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (r == Resource.NoResource || r == Resource.All || r == Resource.Last)
		{
			return 0f;
		}
		Entity val = prefabs[r];
		ResourceData resourceData = default(ResourceData);
		if (!datas.TryGetComponent(val, ref resourceData))
		{
			return 1f;
		}
		return resourceData.m_Weight;
	}

	public static bool IsResourceHasWeight(Resource r, ResourcePrefabs prefabs, ref ComponentLookup<ResourceData> datas)
	{
		return GetWeight(r, prefabs, ref datas) > 0f;
	}

	public static bool IsOfficeResource(Resource resource)
	{
		return (resource & (Resource)120261181440uL) != 0;
	}

	public static bool IsOfficeResource(ResourceInEditor resourceData)
	{
		if (resourceData != ResourceInEditor.Software && resourceData != ResourceInEditor.Telecom && resourceData != ResourceInEditor.Financial)
		{
			return resourceData == ResourceInEditor.Media;
		}
		return true;
	}

	public static bool IsCommercialResource(Resource resource)
	{
		return (resource & (Resource)428424367916uL) != 0;
	}

	public static bool IsExtractorResource(Resource resource)
	{
		return (resource & (Resource)1099574706258uL) != 0;
	}

	public static bool IsIndustrialResource(ResourceData resourceData, bool includeMaterial, bool includeOffice)
	{
		if (resourceData.m_IsProduceable && includeMaterial == resourceData.m_IsMaterial)
		{
			return includeOffice == (resourceData.m_Weight == 0f);
		}
		return false;
	}

	public static bool GetProcessComplexity(NativeList<ArchetypeChunk> m_IndustrialProcessDataChunks, ref ComponentLookup<WorkplaceData> workplaceDatas, Resource r, EntityTypeHandle entityType, ComponentTypeHandle<IndustrialProcessData> processType, out WorkplaceComplexity complexity)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < m_IndustrialProcessDataChunks.Length; i++)
		{
			ArchetypeChunk val = m_IndustrialProcessDataChunks[i];
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref val)).GetNativeArray(entityType);
			NativeArray<IndustrialProcessData> nativeArray2 = ((ArchetypeChunk)(ref val)).GetNativeArray<IndustrialProcessData>(ref processType);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				if (nativeArray2[j].m_Output.m_Resource == r)
				{
					Entity val2 = nativeArray[j];
					if (workplaceDatas.HasComponent(val2))
					{
						complexity = workplaceDatas[val2].m_Complexity;
						return true;
					}
				}
			}
		}
		complexity = WorkplaceComplexity.Simple;
		return false;
	}

	public static int CalculateTotalWage(DynamicBuffer<Employee> employees, ref EconomyParameterData econParams)
	{
		int num = 0;
		for (int i = 0; i < employees.Length; i++)
		{
			num += econParams.GetWage(employees[i].m_Level);
		}
		return num;
	}

	public static int CalculateTotalWage(int totalWorkers, WorkplaceComplexity complexity, int buildingLevel, EconomyParameterData econParams)
	{
		int num = 0;
		Workplaces workplaces = CalculateNumberOfWorkplaces(totalWorkers, complexity, buildingLevel);
		for (int i = 0; i < 5; i++)
		{
			num += workplaces[i] * econParams.GetWage(i);
		}
		return num;
	}

	public static Workplaces CalculateNumberOfWorkplaces(int totalWorkers, WorkplaceComplexity complexity, int buildingLevel)
	{
		Workplaces result = default(Workplaces);
		int num = 4 * (int)complexity + buildingLevel - 1;
		int num2 = totalWorkers;
		int num3 = 0;
		for (int i = 0; i < 5; i++)
		{
			int num4 = math.max(0, 8 - math.abs(num - 4 * i));
			if (i == 0)
			{
				num4 += math.max(0, 8 - math.abs(num + 4));
			}
			if (i == 4)
			{
				num4 += math.max(0, 8 - math.abs(num - 20));
			}
			int num5 = totalWorkers * num4 / 16;
			int num6 = totalWorkers * num4 % 16;
			if (num2 > num5 && num6 + num3 > 0)
			{
				num5++;
				num3 -= 16;
			}
			num3 += num6;
			num5 = math.min(num5, num2);
			num2 -= num5;
			result[i] = num5;
		}
		return result;
	}

	public static float GetWorkforce(DynamicBuffer<Employee> employees, ref ComponentLookup<Citizen> citizens)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		for (int i = 0; i < employees.Length; i++)
		{
			if (citizens.HasComponent(employees[i].m_Worker))
			{
				Employee employee = employees[i];
				num += GetWorkerWorkforce(citizens[employee.m_Worker].Happiness, employee.m_Level);
			}
		}
		return num;
	}

	public static float GetAverageWorkforce(int maxWorkers, WorkplaceComplexity complexity, int buildingLevel)
	{
		Workplaces workplaces = CalculateNumberOfWorkplaces(maxWorkers, complexity, buildingLevel);
		float num = 0f;
		for (int i = 0; i < 5; i++)
		{
			num += (float)workplaces[i] * GetWorkerWorkforce(50, i);
		}
		return num;
	}

	public static float GetAverageWorkforce(Workplaces workplaces)
	{
		float num = 0f;
		for (int i = 0; i < 5; i++)
		{
			num += (float)workplaces[i] * GetWorkerWorkforce(50, i);
		}
		return num;
	}

	public static float GetAverageWorkforce(DynamicBuffer<Employee> employees)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		Enumerator<Employee> enumerator = employees.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				num += GetWorkerWorkforce(50, enumerator.Current.m_Level);
			}
			return num;
		}
		finally
		{
			((System.IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
		}
	}

	public static float GetWorkerWorkforce(int happiness, int level)
	{
		return (((level == 0) ? 2f : 1f) + 2.5f * (float)level) * (0.75f + (float)happiness / 200f);
	}

	public static int GetCompanyMaxProfitPerDay(WorkProvider workProvider, bool isIndustrial, int level, IndustrialProcessData processData, ResourcePrefabs resourcePrefabs, WorkplaceData workplaceData, ref ComponentLookup<ResourceData> resourceDatas, ref EconomyParameterData economyParameters)
	{
		int num = CalculateTotalWage(workProvider.m_MaxWorkers, workplaceData.m_Complexity, level, economyParameters);
		int companyProductionPerDay = GetCompanyProductionPerDay(1f, workProvider.m_MaxWorkers, level, isIndustrial, workplaceData, processData, resourcePrefabs, ref resourceDatas, ref economyParameters);
		return Mathf.RoundToInt(GetCompanyProfitPerUnit(isIndustrial, processData, resourcePrefabs, ref resourceDatas) * (float)companyProductionPerDay - (float)num);
	}

	public static float GetCompanyProfitPerUnit(bool isIndustrial, IndustrialProcessData processData, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas)
	{
		return ((isIndustrial ? GetIndustrialPrice(processData.m_Output.m_Resource, resourcePrefabs, ref resourceDatas) : GetMarketPrice(processData.m_Output.m_Resource, resourcePrefabs, ref resourceDatas)) * (float)processData.m_Output.m_Amount - (float)processData.m_Input1.m_Amount * GetIndustrialPrice(processData.m_Input1.m_Resource, resourcePrefabs, ref resourceDatas) - (float)processData.m_Input2.m_Amount * GetIndustrialPrice(processData.m_Input2.m_Resource, resourcePrefabs, ref resourceDatas)) / (float)processData.m_Output.m_Amount;
	}

	public static int GetCompanyProfitPerDay(float buildingEfficiency, bool isIndustrial, DynamicBuffer<Employee> employees, IndustrialProcessData processData, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, ref ComponentLookup<Citizen> citizens, ref EconomyParameterData economyParameters, ServiceAvailable serviceAvailable = default(ServiceAvailable), ServiceCompanyData serviceCompanyData = default(ServiceCompanyData))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		int num = CalculateTotalWage(employees, ref economyParameters);
		int companyProductionPerDay = GetCompanyProductionPerDay(buildingEfficiency, isIndustrial, employees, processData, resourcePrefabs, ref resourceDatas, ref citizens, ref economyParameters, serviceAvailable, serviceCompanyData);
		return Mathf.RoundToInt(GetCompanyProfitPerUnit(isIndustrial, processData, resourcePrefabs, ref resourceDatas) * (float)companyProductionPerDay - (float)num);
	}

	public static int GetCompanyProductionPerDay(float buildingEfficiency, bool isIndustrial, DynamicBuffer<Employee> employees, IndustrialProcessData processData, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, ref ComponentLookup<Citizen> citizens, ref EconomyParameterData economyParameters, ServiceAvailable serviceAvailable = default(ServiceAvailable), ServiceCompanyData serviceCompanyData = default(ServiceCompanyData))
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		ResourceData resourceData = resourceDatas[resourcePrefabs[processData.m_Output.m_Resource]];
		return GetCompanyProductionPerDay(buildingEfficiency, isIndustrial, employees, processData, resourceData, ref citizens, ref economyParameters, serviceAvailable, serviceCompanyData);
	}

	public static int GetCompanyProductionPerDay(float buildingEfficiency, bool isIndustrial, DynamicBuffer<Employee> employees, IndustrialProcessData processData, ResourceData resourceData, ref ComponentLookup<Citizen> citizens, ref EconomyParameterData economyParameters, ServiceAvailable serviceAvailable, ServiceCompanyData serviceCompanyData)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		float num = (IsExtractorResource(processData.m_Output.m_Resource) ? economyParameters.m_ExtractorProductionEfficiency : (isIndustrial ? economyParameters.m_IndustrialEfficiency : economyParameters.m_CommercialEfficiency));
		float num2 = buildingEfficiency * num * GetWorkforce(employees, ref citizens) * (float)kCompanyUpdatesPerDay;
		float num3 = (isIndustrial ? resourceData.m_NeededWorkPerUnit.x : resourceData.m_NeededWorkPerUnit.y);
		int num4 = (int)math.ceil((float)processData.m_Output.m_Amount * (num2 / num3));
		if (!isIndustrial)
		{
			float num5 = (float)serviceAvailable.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService;
			if (num5 >= 0.8f)
			{
				float num6 = math.saturate((num5 - 0.8f) / 0.2f);
				num4 = (int)math.ceil(math.lerp((float)num4, 0f, num6));
			}
		}
		return num4;
	}

	public static int GetCompanyProductionPerDay(float buildingEfficiency, int workerAmount, int level, bool isIndustrial, WorkplaceData workplaceData, IndustrialProcessData processData, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, ref EconomyParameterData economyParameters)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		ResourceData resourceData = resourceDatas[resourcePrefabs[processData.m_Output.m_Resource]];
		return GetCompanyProductionPerDay(buildingEfficiency, workerAmount, level, isIndustrial, workplaceData, processData, resourceData, ref economyParameters);
	}

	public static int GetCompanyProductionPerDay(float buildingEfficiency, int workerAmount, int level, bool isIndustrial, WorkplaceData workplaceData, IndustrialProcessData processData, ResourceData resourceData, ref EconomyParameterData economyParameters)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		float num = (IsExtractorResource(processData.m_Output.m_Resource) ? economyParameters.m_ExtractorProductionEfficiency : (isIndustrial ? economyParameters.m_IndustrialEfficiency : economyParameters.m_CommercialEfficiency));
		float num2 = buildingEfficiency * num * GetAverageWorkforce(workerAmount, workplaceData.m_Complexity, level) * (float)kCompanyUpdatesPerDay;
		float num3 = (isIndustrial ? resourceData.m_NeededWorkPerUnit.x : resourceData.m_NeededWorkPerUnit.y);
		return (int)math.ceil((float)processData.m_Output.m_Amount * (num2 / num3));
	}

	public static IncomeSource GetIncomeSource(PlayerResource resource)
	{
		switch (resource)
		{
		case PlayerResource.Electricity:
			return IncomeSource.FeeElectricity;
		case PlayerResource.Healthcare:
			return IncomeSource.FeeHealthcare;
		case PlayerResource.BasicEducation:
		case PlayerResource.SecondaryEducation:
		case PlayerResource.HigherEducation:
			return IncomeSource.FeeEducation;
		case PlayerResource.Water:
		case PlayerResource.Sewage:
			return IncomeSource.FeeWater;
		case PlayerResource.PublicTransport:
			return IncomeSource.FeePublicTransport;
		case PlayerResource.Parking:
			return IncomeSource.FeeParking;
		case PlayerResource.Garbage:
			return IncomeSource.FeeGarbage;
		default:
			return IncomeSource.Count;
		}
	}
}
