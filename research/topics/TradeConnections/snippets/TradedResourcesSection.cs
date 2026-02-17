using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TradedResourcesSection : InfoSectionBase
{
	private ResourcePrefabs m_ResourcePrefabs;

	protected override string group => "TradedResourcesSection";

	protected override bool displayForUpgrades => true;

	private NativeList<UIResource> rawMaterials { get; set; }

	private NativeList<UIResource> processedGoods { get; set; }

	private NativeList<UIResource> mail { get; set; }

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.CargoTransportStation>(selectedEntity) || base.EntityManager.HasComponent<Game.Companies.StorageCompany>(selectedEntity))
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var component) || !m_PrefabSystem.TryGetPrefab<PrefabBase>(component, out var prefab) || !prefab.TryGet<Game.Prefabs.CargoTransportStation>(out var component2))
		{
			return false;
		}
		if (component2.m_TradedResources.Length == 0)
		{
			return false;
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void Reset()
	{
		rawMaterials.Clear();
		processedGoods.Clear();
		mail.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		rawMaterials = new NativeList<UIResource>(Allocator.Persistent);
		processedGoods = new NativeList<UIResource>(Allocator.Persistent);
		mail = new NativeList<UIResource>(Allocator.Persistent);
		m_ResourcePrefabs = base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var component) && m_PrefabSystem.TryGetPrefab<PrefabBase>(component, out var prefab) && prefab.TryGet<Game.Prefabs.CargoTransportStation>(out var component2))
		{
			ResourceInEditor[] tradedResources = component2.m_TradedResources;
			for (int i = 0; i < tradedResources.Length; i++)
			{
				UIResource.CategorizeResources(EconomyUtils.GetResource(tradedResources[i]), 0, rawMaterials, processedGoods, mail, base.EntityManager, m_ResourcePrefabs);
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		rawMaterials.Sort();
		writer.PropertyName("rawMaterials");
		writer.ArrayBegin(rawMaterials.Length);
		for (int i = 0; i < rawMaterials.Length; i++)
		{
			writer.Write(rawMaterials[i]);
		}
		writer.ArrayEnd();
		processedGoods.Sort();
		writer.PropertyName("processedGoods");
		writer.ArrayBegin(processedGoods.Length);
		for (int j = 0; j < processedGoods.Length; j++)
		{
			writer.Write(processedGoods[j]);
		}
		writer.ArrayEnd();
		mail.Sort();
		writer.PropertyName("mail");
		writer.ArrayBegin(mail.Length);
		for (int k = 0; k < mail.Length; k++)
		{
			writer.Write(mail[k]);
		}
		writer.ArrayEnd();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		rawMaterials.Dispose();
		processedGoods.Dispose();
		mail.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	public TradedResourcesSection()
	{
	}
}
