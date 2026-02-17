using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new System.Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class OutsideConnection : ComponentBase
{
	public ResourceInEditor[] m_TradedResources;

	public bool m_Commuting;

	public OutsideConnectionTransferType m_TransferType;

	public float m_Remoteness;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		components.Add(ComponentType.ReadWrite<OutsideConnectionData>());
		components.Add(ComponentType.ReadWrite<StorageCompanyData>());
		components.Add(ComponentType.ReadWrite<TransportCompanyData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		components.Add(ComponentType.ReadWrite<Game.Objects.OutsideConnection>());
		components.Add(ComponentType.ReadWrite<Resources>());
		components.Add(ComponentType.ReadWrite<Game.Companies.StorageCompany>());
		components.Add(ComponentType.ReadWrite<TradeCost>());
		components.Add(ComponentType.ReadWrite<StorageTransferRequest>());
		components.Add(ComponentType.ReadWrite<TripNeeded>());
		components.Add(ComponentType.ReadWrite<ResourceSeller>());
		components.Add(ComponentType.ReadWrite<TransportCompany>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		components.Add(ComponentType.ReadWrite<GoodsDeliveryFacility>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		base.Initialize(entityManager, entity);
		((EntityManager)(ref entityManager)).SetComponentData<OutsideConnectionData>(entity, new OutsideConnectionData
		{
			m_Type = m_TransferType,
			m_Remoteness = m_Remoteness
		});
		StorageCompanyData storageCompanyData = new StorageCompanyData
		{
			m_StoredResources = Resource.NoResource
		};
		if (m_TradedResources != null && m_TradedResources.Length != 0)
		{
			for (int i = 0; i < m_TradedResources.Length; i++)
			{
				storageCompanyData.m_StoredResources |= EconomyUtils.GetResource(m_TradedResources[i]);
			}
		}
		((EntityManager)(ref entityManager)).SetComponentData<StorageCompanyData>(entity, storageCompanyData);
		((EntityManager)(ref entityManager)).SetComponentData<TransportCompanyData>(entity, new TransportCompanyData
		{
			m_MaxTransports = 2147483647
		});
	}
}
