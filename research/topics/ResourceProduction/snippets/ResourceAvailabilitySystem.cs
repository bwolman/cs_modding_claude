using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResourceAvailabilitySystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct FindWorkplaceLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<WorkProvider>(ref m_WorkProviderType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 4 * nativeArray2[i].m_MaxWorkers, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindAttractionLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<AttractivenessProvider> m_AttractivenessProviderType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<AttractivenessProvider> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<AttractivenessProvider>(ref m_AttractivenessProviderType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], nativeArray2[i].m_Attractiveness, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindServiceLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<ServiceAvailable> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<ServiceAvailable>(ref m_ServiceAvailableType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 5f + (float)nativeArray2[i].m_ServiceAvailable / 100f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindConsumerLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public bool m_Educated;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			BufferAccessor<Renter> bufferAccessor = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<Renter>(ref m_RenterType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity provider = nativeArray[i];
				DynamicBuffer<Renter> val = bufferAccessor[i];
				int num = 0;
				for (int j = 0; j < val.Length; j++)
				{
					if (!m_HouseholdCitizens.HasBuffer(val[j].m_Renter))
					{
						continue;
					}
					DynamicBuffer<HouseholdCitizen> val2 = m_HouseholdCitizens[val[j].m_Renter];
					for (int k = 0; k < val2.Length; k++)
					{
						Entity citizen = val2[k].m_Citizen;
						if (!m_Citizens.HasComponent(citizen))
						{
							continue;
						}
						Citizen citizen2 = m_Citizens[citizen];
						if (m_Citizens[citizen].GetAge() == CitizenAge.Adult)
						{
							int educationLevel = citizen2.GetEducationLevel();
							if ((educationLevel > 1 && m_Educated) || (educationLevel <= 1 && !m_Educated))
							{
								num++;
							}
						}
					}
				}
				if (num != 0)
				{
					AddProvider(provider, 2f * (float)num, m_Providers, ref m_TargetSeeker);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindConvenienceFoodStoreLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity provider = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (m_IndustrialProcessDatas.HasComponent(prefab) && (m_IndustrialProcessDatas[prefab].m_Output.m_Resource & Resource.ConvenienceFood) != Resource.NoResource)
				{
					AddProvider(provider, 10f, m_Providers, ref m_TargetSeeker);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindOutsideConnectionLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 10f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindSellerLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessData;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageDatas;

		[ReadOnly]
		public BufferLookup<TradeCost> m_TradeCosts;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public Resource m_Resource;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity val = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!m_ProcessData.HasComponent(prefab))
				{
					continue;
				}
				if (m_StorageCompanies.HasComponent(val))
				{
					if ((m_Resource & m_StorageDatas[prefab].m_StoredResources) != Resource.NoResource)
					{
						DynamicBuffer<TradeCost> costs = m_TradeCosts[val];
						TradeCost tradeCost = EconomyUtils.GetTradeCost(m_Resource, costs);
						AddProvider(val, 100f, m_Providers, ref m_TargetSeeker, 100f * tradeCost.m_BuyCost);
					}
				}
				else if ((m_Resource & m_ProcessData[prefab].m_Output.m_Resource) != Resource.NoResource)
				{
					AddProvider(val, 100f, m_Providers, ref m_TargetSeeker);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindTaxiLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0201: Unknown result type (might be due to invalid IL or missing references)
			//IL_0208: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Game.Buildings.TransportDepot>(ref m_TransportDepotType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray3 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Game.Buildings.TransportDepot transportDepot = nativeArray2[i];
					if ((transportDepot.m_Flags & (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter)) == (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter))
					{
						PrefabRef prefabRef = nativeArray3[i];
						if (m_PrefabTransportDepotData[prefabRef.m_Prefab].m_TransportType == TransportType.Taxi)
						{
							AddProvider(nativeArray[i], (int)transportDepot.m_AvailableVehicles, m_Providers, ref m_TargetSeeker);
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.Taxi> nativeArray4 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Game.Vehicles.Taxi>(ref m_TaxiType);
			if (nativeArray4.Length == 0)
			{
				return;
			}
			NativeArray<Owner> nativeArray5 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Owner>(ref m_OwnerType);
			NativeArray<PathOwner> nativeArray6 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PathOwner>(ref m_PathOwnerType);
			BufferAccessor<PathElement> bufferAccessor = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<PathElement>(ref m_PathElementType);
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				Owner owner = nativeArray5[j];
				if (!m_TransportDepotData.HasComponent(owner.m_Owner) || (m_TransportDepotData[owner.m_Owner].m_Flags & TransportDepotFlags.HasDispatchCenter) == 0)
				{
					continue;
				}
				Game.Vehicles.Taxi taxi = nativeArray4[j];
				DynamicBuffer<PathElement> val = bufferAccessor[j];
				Entity val2 = nativeArray[j];
				PathOwner pathOwner = nativeArray6[j];
				if ((taxi.m_State & TaxiFlags.Dispatched) != 0)
				{
					AddProvider(val2, 0.1f, m_Providers, ref m_TargetSeeker);
					continue;
				}
				int num = val.Length - taxi.m_ExtraPathElementCount;
				if (num <= 0 || num > val.Length)
				{
					AddProvider(val2, 1f, m_Providers, ref m_TargetSeeker);
					continue;
				}
				float cost = math.max(0f, (float)(num - pathOwner.m_ElementIndex) * taxi.m_PathElementTime);
				PathElement pathElement = val[num - 1];
				m_TargetSeeker.m_Buffer.Enqueue(new PathTarget(val2, pathElement.m_Target, pathElement.m_TargetDelta.y, 0f));
				m_Providers.Enqueue(new AvailabilityProvider(val2, 1f, cost));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindBusStopLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 10f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindTramSubwayLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<SubwayStop> m_SubWayStopData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public ParallelWriter<AvailabilityProvider> m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			Owner owner = default(Owner);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity val = nativeArray[i];
				if (m_SubWayStopData.HasComponent(val) && m_OwnerData.TryGetComponent(val, ref owner))
				{
					val = owner.m_Owner;
				}
				AddProvider(val, 10f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ClearAvailabilityJob : IJobChunk
	{
		[ReadOnly]
		public AvailableResource m_ResourceType;

		public BufferTypeHandle<ResourceAvailability> m_ResourceAvailabilityType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			BufferAccessor<ResourceAvailability> bufferAccessor = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<ResourceAvailability>(ref m_ResourceAvailabilityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ResourceAvailability> val = bufferAccessor[i];
				val[(int)m_ResourceType] = default(ResourceAvailability);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ApplyAvailabilityJob : IJobParallelFor
	{
		[ReadOnly]
		public AvailableResource m_ResourceType;

		[ReadOnly]
		public NativeArray<AvailabilityElement> m_AvailabilityElements;

		[NativeDisableParallelForRestriction]
		public BufferLookup<ResourceAvailability> m_ResourceAvailability;

		public void Execute(int index)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			AvailabilityElement availabilityElement = m_AvailabilityElements[index];
			if (m_ResourceAvailability.HasBuffer(availabilityElement.m_Edge))
			{
				DynamicBuffer<ResourceAvailability> val = m_ResourceAvailability[availabilityElement.m_Edge];
				val[(int)m_ResourceType] = new ResourceAvailability
				{
					m_Availability = availabilityElement.m_Availability
				};
			}
		}
	}

	[BurstCompile]
	private struct FindTaxiDistrictsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public ParallelWriter<Entity> m_Districts;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_018f: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Game.Buildings.TransportDepot>(ref m_TransportDepotType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray3 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
				DynamicBuffer<ServiceDistrict> val2 = default(DynamicBuffer<ServiceDistrict>);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					if ((nativeArray2[i].m_Flags & (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter)) != (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter))
					{
						continue;
					}
					PrefabRef prefabRef = nativeArray3[i];
					if (m_PrefabTransportDepotData[prefabRef.m_Prefab].m_TransportType != TransportType.Taxi)
					{
						continue;
					}
					Entity val = nativeArray[i];
					if (m_ServiceDistricts.TryGetBuffer(val, ref val2) && val2.Length != 0)
					{
						for (int j = 0; j < val2.Length; j++)
						{
							m_Districts.Add(val2[j].m_District);
						}
					}
					else
					{
						m_Districts.Add(Entity.Null);
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.Taxi> nativeArray4 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Game.Vehicles.Taxi>(ref m_TaxiType);
			if (nativeArray4.Length == 0)
			{
				return;
			}
			NativeArray<Owner> nativeArray5 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Owner>(ref m_OwnerType);
			DynamicBuffer<ServiceDistrict> val3 = default(DynamicBuffer<ServiceDistrict>);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Owner owner = nativeArray5[k];
				if (!m_TransportDepotData.HasComponent(owner.m_Owner) || (m_TransportDepotData[owner.m_Owner].m_Flags & TransportDepotFlags.HasDispatchCenter) == 0)
				{
					continue;
				}
				if (m_ServiceDistricts.TryGetBuffer(owner.m_Owner, ref val3) && val3.Length != 0)
				{
					for (int l = 0; l < val3.Length; l++)
					{
						m_Districts.Add(val3[l].m_District);
					}
				}
				else
				{
					m_Districts.Add(Entity.Null);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ApplyTaxiAvailabilityJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<AvailabilityElement> m_AvailabilityElements;

		[ReadOnly]
		public NativeParallelHashSet<Entity> m_Districts;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<ResourceAvailability> m_ResourceAvailability;

		public void Execute(int index)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_0220: Unknown result type (might be due to invalid IL or missing references)
			AvailabilityElement availabilityElement = m_AvailabilityElements[index];
			if (!m_Districts.Contains(Entity.Null))
			{
				BorderDistrict borderDistrict = default(BorderDistrict);
				Owner owner = default(Owner);
				if (m_BorderDistrictData.TryGetComponent(availabilityElement.m_Edge, ref borderDistrict))
				{
					if (!m_Districts.Contains(borderDistrict.m_Left) && !m_Districts.Contains(borderDistrict.m_Right))
					{
						availabilityElement.m_Availability = float2.op_Implicit(0f);
					}
				}
				else if (m_OwnerData.TryGetComponent(availabilityElement.m_Edge, ref owner))
				{
					CurrentDistrict currentDistrict = default(CurrentDistrict);
					while (true)
					{
						if (m_CurrentDistrictData.TryGetComponent(owner.m_Owner, ref currentDistrict))
						{
							if (!m_Districts.Contains(currentDistrict.m_District))
							{
								availabilityElement.m_Availability = float2.op_Implicit(0f);
							}
							break;
						}
						if (m_OwnerData.HasComponent(owner.m_Owner))
						{
							owner = m_OwnerData[owner.m_Owner];
							continue;
						}
						availabilityElement.m_Availability = float2.op_Implicit(0f);
						break;
					}
				}
			}
			if (m_ResourceAvailability.HasBuffer(availabilityElement.m_Edge))
			{
				DynamicBuffer<ResourceAvailability> val = m_ResourceAvailability[availabilityElement.m_Edge];
				val[30] = new ResourceAvailability
				{
					m_Availability = availabilityElement.m_Availability
				};
			}
			if (!m_SubLanes.HasBuffer(availabilityElement.m_Edge))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> val2 = m_SubLanes[availabilityElement.m_Edge];
			int num = Mathf.RoundToInt(math.min(65535f, math.csum(availabilityElement.m_Availability) * 32767.5f));
			for (int i = 0; i < val2.Length; i++)
			{
				Entity subLane = val2[i].m_SubLane;
				if (m_ParkingLaneData.HasComponent(subLane))
				{
					Game.Net.ParkingLane parkingLane = m_ParkingLaneData[subLane];
					int num2 = math.select(parkingLane.m_TaxiAvailability * 3 + num + 3 >> 2, 0, num == 0);
					if (num2 != parkingLane.m_TaxiAvailability)
					{
						parkingLane.m_TaxiAvailability = (ushort)num2;
						parkingLane.m_Flags |= ParkingLaneFlags.TaxiAvailabilityChanged;
					}
					parkingLane.m_Flags |= ParkingLaneFlags.TaxiAvailabilityUpdated;
					m_ParkingLaneData[subLane] = parkingLane;
				}
			}
		}
	}

	[BurstCompile]
	private struct RefreshTaxiAvailabilityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> m_PathfindTransportData;

		public ComponentTypeHandle<Game.Net.ParkingLane> m_ParkingLaneType;

		public ParallelWriter<TimeActionData> m_TimeActions;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<Lane> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Lane>(ref m_LaneType);
			NativeArray<Game.Net.ParkingLane> nativeArray3 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Game.Net.ParkingLane>(ref m_ParkingLaneType);
			NativeArray<PrefabRef> nativeArray4 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Game.Net.ParkingLane parkingLane = nativeArray3[i];
				if ((parkingLane.m_Flags & ParkingLaneFlags.TaxiAvailabilityUpdated) == 0 && parkingLane.m_TaxiAvailability != 0)
				{
					parkingLane.m_TaxiAvailability = 0;
					parkingLane.m_Flags |= ParkingLaneFlags.TaxiAvailabilityChanged;
				}
				if ((parkingLane.m_Flags & ParkingLaneFlags.TaxiAvailabilityChanged) != 0)
				{
					Lane lane = nativeArray2[i];
					TimeActionData timeActionData = new TimeActionData
					{
						m_Owner = nativeArray[i],
						m_StartNode = lane.m_StartNode,
						m_EndNode = lane.m_EndNode,
						m_Flags = (TimeActionFlags.SetSecondary | TimeActionFlags.EnableForward)
					};
					if ((parkingLane.m_Flags & ParkingLaneFlags.AdditionalStart) != 0)
					{
						timeActionData.m_SecondaryStartNode = parkingLane.m_AdditionalStartNode;
						timeActionData.m_SecondaryEndNode = lane.m_EndNode;
					}
					else
					{
						timeActionData.m_SecondaryStartNode = lane.m_StartNode;
						timeActionData.m_SecondaryEndNode = lane.m_EndNode;
					}
					if (parkingLane.m_TaxiAvailability != 0)
					{
						PrefabRef prefabRef = nativeArray4[i];
						NetLaneData netLaneData = m_NetLaneData[prefabRef.m_Prefab];
						PathfindTransportData pathfindTransportData = m_PathfindTransportData[netLaneData.m_PathfindPrefab];
						timeActionData.m_Flags |= TimeActionFlags.EnableBackward;
						timeActionData.m_Time = pathfindTransportData.m_OrderingCost.m_Value.x + pathfindTransportData.m_StartingCost.m_Value.x + PathUtils.GetTaxiAvailabilityDelay(parkingLane);
					}
					m_TimeActions.Enqueue(timeActionData);
				}
				parkingLane.m_Flags &= ~(ParkingLaneFlags.TaxiAvailabilityUpdated | ParkingLaneFlags.TaxiAvailabilityChanged);
				nativeArray3[i] = parkingLane;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> __Game_Areas_ServiceDistrict_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RW_ComponentLookup;

		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> __Game_Prefabs_PathfindTransportData_RO_ComponentLookup;

		public ComponentTypeHandle<Game.Net.ParkingLane> __Game_Net_ParkingLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;

		public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<SubwayStop> __Game_Routes_SubwayStop_RO_ComponentLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0180: Unknown result type (might be due to invalid IL or missing references)
			//IL_0188: Unknown result type (might be due to invalid IL or missing references)
			//IL_018d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			__Game_Net_ResourceAvailability_RW_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<ResourceAvailability>(false);
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Buildings_TransportDepot_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Game.Buildings.TransportDepot>(true);
			__Game_Vehicles_Taxi_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Game.Vehicles.Taxi>(true);
			__Game_Common_Owner_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Owner>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabRef>(true);
			__Game_Buildings_TransportDepot_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Game.Buildings.TransportDepot>(true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<TransportDepotData>(true);
			__Game_Areas_ServiceDistrict_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ServiceDistrict>(true);
			__Game_Common_Owner_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Owner>(true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<CurrentDistrict>(true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<BorderDistrict>(true);
			__Game_Net_SubLane_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Net.SubLane>(true);
			__Game_Net_ParkingLane_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Game.Net.ParkingLane>(false);
			__Game_Net_ResourceAvailability_RW_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ResourceAvailability>(false);
			__Game_Net_Lane_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Lane>(true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<NetLaneData>(true);
			__Game_Prefabs_PathfindTransportData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PathfindTransportData>(true);
			__Game_Net_ParkingLane_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Game.Net.ParkingLane>(false);
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WorkProvider>(true);
			__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ServiceAvailable>(true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<Renter>(true);
			__Game_Citizens_Citizen_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Citizen>(true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<HouseholdCitizen>(true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<IndustrialProcessData>(true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Game.Objects.OutsideConnection>(true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Game.Companies.StorageCompany>(true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<StorageCompanyData>(true);
			__Game_Companies_TradeCost_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<TradeCost>(true);
			__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<AttractivenessProvider>(false);
			__Game_Pathfind_PathOwner_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PathOwner>(true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<PathElement>(true);
			__Game_Routes_SubwayStop_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SubwayStop>(true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private EntityQuery m_EdgeGroup;

	private EntityQuery m_WorkplaceGroup;

	private EntityQuery m_ServiceGroup;

	private EntityQuery m_RenterGroup;

	private EntityQuery m_ConvenienceFoodStoreGroup;

	private EntityQuery m_OutsideConnectionGroup;

	private EntityQuery m_AttractionGroup;

	private EntityQuery m_ResourceSellerGroup;

	private EntityQuery m_TaxiQuery;

	private EntityQuery m_BusStopQuery;

	private EntityQuery m_TramSubwayQuery;

	private EntityQuery m_ParkingLaneQuery;

	private SimulationSystem m_SimulationSystem;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private AirwaySystem m_AirwaySystem;

	private ResourceSystem m_ResourceSystem;

	private PathfindTargetSeekerData m_TargetSeekerData;

	private Entity m_AvailabilityContainer;

	private AvailableResource m_LastQueriedResource;

	private AvailableResource m_LastWrittenResource;

	private TypeHandle __TypeHandle;

	[field: CompilerGenerated]
	public AvailableResource appliedResource
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		private set;
	}

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Expected O, but got Unknown
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Expected O, but got Unknown
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Expected O, but got Unknown
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindQueueSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_AirwaySystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<AirwaySystem>();
		m_ResourceSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ResourceSystem>();
		m_EdgeGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<Game.Net.Edge>(),
			ComponentType.ReadWrite<ResourceAvailability>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		m_WorkplaceGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<WorkProvider>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>(),
			ComponentType.Exclude<Game.Objects.OutsideConnection>()
		});
		m_ServiceGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<ServiceAvailable>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		m_RenterGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<ResidentialProperty>(),
			ComponentType.ReadOnly<Renter>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		m_ConvenienceFoodStoreGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<ServiceAvailable>(),
			ComponentType.ReadOnly<ResourceSeller>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		m_OutsideConnectionGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[5]
		{
			ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
			ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(),
			ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		m_AttractionGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<AttractivenessProvider>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		EntityQueryDesc[] array = new EntityQueryDesc[1];
		EntityQueryDesc val = new EntityQueryDesc();
		val.Any = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<ResourceSeller>(),
			ComponentType.ReadOnly<Game.Companies.StorageCompany>()
		};
		val.None = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<ServiceAvailable>(),
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Temp>()
		};
		array[0] = val;
		m_ResourceSellerGroup = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		EntityQueryDesc[] array2 = new EntityQueryDesc[1];
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<ServiceDispatch>(),
			ComponentType.ReadOnly<PrefabRef>()
		};
		val.Any = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),
			ComponentType.ReadOnly<Game.Vehicles.Taxi>()
		};
		val.None = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Destroyed>(),
			ComponentType.ReadOnly<Temp>()
		};
		array2[0] = val;
		m_TaxiQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array2);
		m_BusStopQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<BusStop>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		EntityQueryDesc[] array3 = new EntityQueryDesc[1];
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Game.Routes.TransportStop>(),
			ComponentType.ReadOnly<PrefabRef>()
		};
		val.Any = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TramStop>(),
			ComponentType.ReadOnly<SubwayStop>()
		};
		val.None = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Destroyed>(),
			ComponentType.ReadOnly<Temp>()
		};
		array3[0] = val;
		m_TramSubwayQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array3);
		m_ParkingLaneQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<Game.Net.ParkingLane>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Destroyed>(),
			ComponentType.Exclude<Temp>()
		});
		m_TargetSeekerData = new PathfindTargetSeekerData((SystemBase)(object)this);
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		m_AvailabilityContainer = ((EntityManager)(ref entityManager)).CreateEntity((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadWrite<AvailabilityElement>() });
	}

	private AvailabilityParameters GetAvailabilityParameters(AvailableResource resource, ResourcePrefabs prefabs, ComponentLookup<ResourceData> datas)
	{
		switch (resource)
		{
		case AvailableResource.GrainSupply:
		case AvailableResource.VegetableSupply:
		case AvailableResource.WoodSupply:
		case AvailableResource.TextilesSupply:
		case AvailableResource.ConvenienceFoodSupply:
		case AvailableResource.PaperSupply:
		case AvailableResource.VehiclesSupply:
		case AvailableResource.OilSupply:
		case AvailableResource.PetrochemicalsSupply:
		case AvailableResource.OreSupply:
		case AvailableResource.MetalsSupply:
		case AvailableResource.ElectronicsSupply:
		case AvailableResource.PlasticsSupply:
		case AvailableResource.CoalSupply:
		case AvailableResource.StoneSupply:
		case AvailableResource.LivestockSupply:
		case AvailableResource.CottonSupply:
		case AvailableResource.SteelSupply:
		case AvailableResource.MineralSupply:
		case AvailableResource.ChemicalSupply:
		case AvailableResource.MachinerySupply:
		case AvailableResource.BeveragesSupply:
		case AvailableResource.TimberSupply:
		{
			Resource resource2 = EconomyUtils.GetResource(resource);
			float costFactor = 0.1f;
			if (resource2 != Resource.NoResource)
			{
				costFactor = 0.1f * EconomyUtils.GetTransportCost(1f, 0, EconomyUtils.GetWeight(resource2, prefabs, ref datas), StorageTransferFlags.Car);
			}
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = costFactor,
				m_ResultFactor = 0.01f
			};
		}
		case AvailableResource.Workplaces:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 0.08f
			};
		case AvailableResource.UneducatedCitizens:
		case AvailableResource.EducatedCitizens:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.5f,
				m_ResultFactor = 1f
			};
		case AvailableResource.Services:
		case AvailableResource.ConvenienceFoodStore:
		case AvailableResource.Attractiveness:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 1f
			};
		case AvailableResource.OutsideConnection:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 3f
			};
		case AvailableResource.Taxi:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.1f,
				m_CostFactor = 0.05f,
				m_ResultFactor = 1f
			};
		case AvailableResource.Bus:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 1f
			};
		case AvailableResource.TramSubway:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 1f
			};
		default:
			return new AvailabilityParameters
			{
				m_DensityWeight = 1f,
				m_CostFactor = 1f,
				m_ResultFactor = 1f
			};
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		((IWriter)writer/*cast due to .constrained prefix*/).Write((int)m_LastWrittenResource);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		int num = default(int);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref num);
		m_LastQueriedResource = AvailableResource.Count;
		m_LastWrittenResource = (AvailableResource)math.clamp(num, 0, 33);
		appliedResource = AvailableResource.Count;
	}

	public void SetDefaults(Context context)
	{
		m_LastQueriedResource = AvailableResource.Count;
		m_LastWrittenResource = AvailableResource.FishSupply;
		appliedResource = AvailableResource.Count;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		if (m_LastQueriedResource != AvailableResource.Count)
		{
			m_LastWrittenResource = m_LastQueriedResource;
			appliedResource = m_LastWrittenResource;
		}
		else
		{
			m_LastQueriedResource = m_LastWrittenResource;
			m_LastWrittenResource = AvailableResource.Count;
		}
		if (++m_LastQueriedResource == AvailableResource.Count)
		{
			m_LastQueriedResource = AvailableResource.Workplaces;
		}
		AvailabilityAction action = new AvailabilityAction((Allocator)4, GetAvailabilityParameters(m_LastQueriedResource, m_ResourceSystem.GetPrefabs(), ((SystemBase)this).GetComponentLookup<ResourceData>(true)));
		JobHandle val = FindLocations(m_LastQueriedResource, action.data.m_Sources, action.data.m_Providers, ((SystemBase)this).Dependency);
		if (m_LastWrittenResource != AvailableResource.Count)
		{
			JobHandle val2 = ApplyAvailability(m_LastWrittenResource, ((SystemBase)this).Dependency, val);
			((SystemBase)this).Dependency = JobHandle.CombineDependencies(val2, val);
		}
		else
		{
			((SystemBase)this).Dependency = val;
		}
		m_PathfindQueueSystem.Enqueue(action, m_AvailabilityContainer, val, m_SimulationSystem.frameIndex + 64, this);
	}

	private JobHandle ApplyAvailability(AvailableResource resource, JobHandle inputDeps, JobHandle pathDeps)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		NativeArray<AvailabilityElement> availabilityElements = ((EntityManager)(ref entityManager)).GetBuffer<AvailabilityElement>(m_AvailabilityContainer, true).AsNativeArray();
		JobHandle val = JobChunkExtensions.ScheduleParallel<ClearAvailabilityJob>(new ClearAvailabilityJob
		{
			m_ResourceType = resource,
			m_ResourceAvailabilityType = InternalCompilerInterface.GetBufferTypeHandle<ResourceAvailability>(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef)
		}, m_EdgeGroup, inputDeps);
		if (resource == AvailableResource.Taxi)
		{
			TimeAction action = new TimeAction((Allocator)4);
			NativeParallelHashSet<Entity> districts = default(NativeParallelHashSet<Entity>);
			districts..ctor(((EntityQuery)(ref m_TaxiQuery)).CalculateEntityCount(), AllocatorHandle.op_Implicit((Allocator)3));
			FindTaxiDistrictsJob findTaxiDistrictsJob = new FindTaxiDistrictsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle<Game.Buildings.TransportDepot>(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TaxiType = InternalCompilerInterface.GetComponentTypeHandle<Game.Vehicles.Taxi>(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TransportDepotData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.TransportDepot>(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup<TransportDepotData>(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_ServiceDistricts = InternalCompilerInterface.GetBufferLookup<ServiceDistrict>(ref __TypeHandle.__Game_Areas_ServiceDistrict_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
				m_Districts = districts.AsParallelWriter()
			};
			ApplyTaxiAvailabilityJob applyTaxiAvailabilityJob = new ApplyTaxiAvailabilityJob
			{
				m_AvailabilityElements = availabilityElements,
				m_Districts = districts,
				m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup<CurrentDistrict>(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup<BorderDistrict>(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup<Game.Net.SubLane>(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup<Game.Net.ParkingLane>(ref __TypeHandle.__Game_Net_ParkingLane_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_ResourceAvailability = InternalCompilerInterface.GetBufferLookup<ResourceAvailability>(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferLookup, ref ((SystemBase)this).CheckedStateRef)
			};
			RefreshTaxiAvailabilityJob obj = new RefreshTaxiAvailabilityJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle<Lane>(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_NetLaneData = InternalCompilerInterface.GetComponentLookup<NetLaneData>(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_PathfindTransportData = InternalCompilerInterface.GetComponentLookup<PathfindTransportData>(ref __TypeHandle.__Game_Prefabs_PathfindTransportData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle<Game.Net.ParkingLane>(ref __TypeHandle.__Game_Net_ParkingLane_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TimeActions = action.m_TimeData.AsParallelWriter()
			};
			JobHandle val2 = JobChunkExtensions.ScheduleParallel<FindTaxiDistrictsJob>(findTaxiDistrictsJob, m_TaxiQuery, inputDeps);
			JobHandle val3 = IJobParallelForExtensions.Schedule<ApplyTaxiAvailabilityJob>(applyTaxiAvailabilityJob, availabilityElements.Length, 4, JobHandle.CombineDependencies(val, val2, pathDeps));
			JobHandle val4 = JobChunkExtensions.ScheduleParallel<RefreshTaxiAvailabilityJob>(obj, m_ParkingLaneQuery, val3);
			districts.Dispose(val3);
			m_PathfindQueueSystem.Enqueue(action, val4);
			return val4;
		}
		return IJobParallelForExtensions.Schedule<ApplyAvailabilityJob>(new ApplyAvailabilityJob
		{
			m_ResourceType = resource,
			m_AvailabilityElements = availabilityElements,
			m_ResourceAvailability = InternalCompilerInterface.GetBufferLookup<ResourceAvailability>(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferLookup, ref ((SystemBase)this).CheckedStateRef)
		}, availabilityElements.Length, 16, val);
	}

	private JobHandle FindLocations(AvailableResource resource, UnsafeQueue<PathTarget> pathTargets, UnsafeQueue<AvailabilityProvider> providers, JobHandle inputDeps)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0796: Unknown result type (might be due to invalid IL or missing references)
		//IL_079b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_081c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0821: Unknown result type (might be due to invalid IL or missing references)
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_083e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0856: Unknown result type (might be due to invalid IL or missing references)
		//IL_085b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0873: Unknown result type (might be due to invalid IL or missing references)
		//IL_0878: Unknown result type (might be due to invalid IL or missing references)
		//IL_0890: Unknown result type (might be due to invalid IL or missing references)
		//IL_0895: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0904: Unknown result type (might be due to invalid IL or missing references)
		//IL_0909: Unknown result type (might be due to invalid IL or missing references)
		//IL_091a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0938: Unknown result type (might be due to invalid IL or missing references)
		//IL_093d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0945: Unknown result type (might be due to invalid IL or missing references)
		//IL_094a: Unknown result type (might be due to invalid IL or missing references)
		//IL_094c: Unknown result type (might be due to invalid IL or missing references)
		//IL_096d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0972: Unknown result type (might be due to invalid IL or missing references)
		//IL_0983: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09db: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a10: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a15: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a51: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a56: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a58: Unknown result type (might be due to invalid IL or missing references)
		//IL_0675: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0693: Unknown result type (might be due to invalid IL or missing references)
		//IL_0698: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_070a: Unknown result type (might be due to invalid IL or missing references)
		//IL_070f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0727: Unknown result type (might be due to invalid IL or missing references)
		//IL_072c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0744: Unknown result type (might be due to invalid IL or missing references)
		//IL_0749: Unknown result type (might be due to invalid IL or missing references)
		//IL_0761: Unknown result type (might be due to invalid IL or missing references)
		//IL_0766: Unknown result type (might be due to invalid IL or missing references)
		//IL_076e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0773: Unknown result type (might be due to invalid IL or missing references)
		//IL_0775: Unknown result type (might be due to invalid IL or missing references)
		m_TargetSeekerData.Update((SystemBase)(object)this, m_AirwaySystem.GetAirwayData());
		PathfindParameters pathfindParameters = new PathfindParameters
		{
			m_MaxSpeed = float2.op_Implicit(111.111115f),
			m_WalkSpeed = float2.op_Implicit(5.555556f),
			m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
			m_Methods = PathMethod.Road,
			m_PathfindFlags = (PathfindFlags.Stable | PathfindFlags.IgnoreFlow | PathfindFlags.Simplified),
			m_IgnoredRules = (RuleFlags.HasBlockage | RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
		};
		SetupQueueTarget setupQueueTarget = new SetupQueueTarget
		{
			m_Methods = PathMethod.Road,
			m_RoadTypes = RoadTypes.Car
		};
		switch (resource)
		{
		case AvailableResource.Workplaces:
			return JobChunkExtensions.ScheduleParallel<FindWorkplaceLocationsJob>(new FindWorkplaceLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle<WorkProvider>(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_WorkplaceGroup, inputDeps);
		case AvailableResource.Services:
			return JobChunkExtensions.ScheduleParallel<FindServiceLocationsJob>(new FindServiceLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle<ServiceAvailable>(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_ServiceGroup, inputDeps);
		case AvailableResource.UneducatedCitizens:
			return JobChunkExtensions.ScheduleParallel<FindConsumerLocationsJob>(new FindConsumerLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetBufferTypeHandle<Renter>(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup<Citizen>(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter(),
				m_Educated = false
			}, m_RenterGroup, inputDeps);
		case AvailableResource.EducatedCitizens:
			return JobChunkExtensions.ScheduleParallel<FindConsumerLocationsJob>(new FindConsumerLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetBufferTypeHandle<Renter>(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup<Citizen>(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter(),
				m_Educated = true
			}, m_RenterGroup, inputDeps);
		case AvailableResource.ConvenienceFoodStore:
			return JobChunkExtensions.ScheduleParallel<FindConvenienceFoodStoreLocationsJob>(new FindConvenienceFoodStoreLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup<IndustrialProcessData>(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_ConvenienceFoodStoreGroup, inputDeps);
		case AvailableResource.OutsideConnection:
			return JobChunkExtensions.ScheduleParallel<FindOutsideConnectionLocationsJob>(new FindOutsideConnectionLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle<Game.Objects.OutsideConnection>(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_OutsideConnectionGroup, inputDeps);
		case AvailableResource.GrainSupply:
		case AvailableResource.VegetableSupply:
		case AvailableResource.WoodSupply:
		case AvailableResource.TextilesSupply:
		case AvailableResource.ConvenienceFoodSupply:
		case AvailableResource.PaperSupply:
		case AvailableResource.VehiclesSupply:
		case AvailableResource.OilSupply:
		case AvailableResource.PetrochemicalsSupply:
		case AvailableResource.OreSupply:
		case AvailableResource.MetalsSupply:
		case AvailableResource.ElectronicsSupply:
		case AvailableResource.PlasticsSupply:
		case AvailableResource.CoalSupply:
		case AvailableResource.StoneSupply:
		case AvailableResource.LivestockSupply:
		case AvailableResource.CottonSupply:
		case AvailableResource.SteelSupply:
		case AvailableResource.MineralSupply:
		case AvailableResource.ChemicalSupply:
		case AvailableResource.MachinerySupply:
		case AvailableResource.BeveragesSupply:
		case AvailableResource.TimberSupply:
		case AvailableResource.FishSupply:
		{
			Resource resource2;
			switch (resource)
			{
			case AvailableResource.GrainSupply:
				resource2 = Resource.Grain;
				break;
			case AvailableResource.TextilesSupply:
				resource2 = Resource.Textiles;
				break;
			case AvailableResource.VegetableSupply:
				resource2 = Resource.Vegetables;
				break;
			case AvailableResource.WoodSupply:
				resource2 = Resource.Wood;
				break;
			case AvailableResource.ConvenienceFoodSupply:
				resource2 = Resource.ConvenienceFood;
				break;
			case AvailableResource.PaperSupply:
				resource2 = Resource.Paper;
				break;
			case AvailableResource.VehiclesSupply:
				resource2 = Resource.Vehicles;
				break;
			case AvailableResource.MetalsSupply:
				resource2 = Resource.Metals;
				break;
			case AvailableResource.OilSupply:
				resource2 = Resource.Oil;
				break;
			case AvailableResource.OreSupply:
				resource2 = Resource.Ore;
				break;
			case AvailableResource.PetrochemicalsSupply:
				resource2 = Resource.Petrochemicals;
				break;
			case AvailableResource.ElectronicsSupply:
				resource2 = Resource.Electronics;
				break;
			case AvailableResource.PlasticsSupply:
				resource2 = Resource.Plastics;
				break;
			case AvailableResource.CoalSupply:
				resource2 = Resource.Coal;
				break;
			case AvailableResource.StoneSupply:
				resource2 = Resource.Stone;
				break;
			case AvailableResource.LivestockSupply:
				resource2 = Resource.Livestock;
				break;
			case AvailableResource.CottonSupply:
				resource2 = Resource.Cotton;
				break;
			case AvailableResource.SteelSupply:
				resource2 = Resource.Steel;
				break;
			case AvailableResource.MineralSupply:
				resource2 = Resource.Minerals;
				break;
			case AvailableResource.ChemicalSupply:
				resource2 = Resource.Chemicals;
				break;
			case AvailableResource.TimberSupply:
				resource2 = Resource.Timber;
				break;
			case AvailableResource.MachinerySupply:
				resource2 = Resource.Machinery;
				break;
			case AvailableResource.BeveragesSupply:
				resource2 = Resource.Beverages;
				break;
			case AvailableResource.FishSupply:
				resource2 = Resource.Fish;
				break;
			default:
				return inputDeps;
			}
			return JobChunkExtensions.ScheduleParallel<FindSellerLocationsJob>(new FindSellerLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter(),
				m_Resource = resource2,
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_ProcessData = InternalCompilerInterface.GetComponentLookup<IndustrialProcessData>(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_StorageCompanies = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_StorageDatas = InternalCompilerInterface.GetComponentLookup<StorageCompanyData>(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_TradeCosts = InternalCompilerInterface.GetBufferLookup<TradeCost>(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef)
			}, m_ResourceSellerGroup, inputDeps);
		}
		case AvailableResource.Attractiveness:
			return JobChunkExtensions.ScheduleParallel<FindAttractionLocationsJob>(new FindAttractionLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_AttractivenessProviderType = InternalCompilerInterface.GetComponentTypeHandle<AttractivenessProvider>(ref __TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_AttractionGroup, inputDeps);
		case AvailableResource.Taxi:
			return JobChunkExtensions.ScheduleParallel<FindTaxiLocationsJob>(new FindTaxiLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle<Game.Buildings.TransportDepot>(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TaxiType = InternalCompilerInterface.GetComponentTypeHandle<Game.Vehicles.Taxi>(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle<PathOwner>(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle<PathElement>(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TransportDepotData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.TransportDepot>(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup<TransportDepotData>(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_TaxiQuery, inputDeps);
		case AvailableResource.Bus:
			return JobChunkExtensions.ScheduleParallel<FindBusStopLocationsJob>(new FindBusStopLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_BusStopQuery, inputDeps);
		case AvailableResource.TramSubway:
			return JobChunkExtensions.ScheduleParallel<FindTramSubwayLocationsJob>(new FindTramSubwayLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
				m_SubWayStopData = InternalCompilerInterface.GetComponentLookup<SubwayStop>(ref __TypeHandle.__Game_Routes_SubwayStop_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_TramSubwayQuery, inputDeps);
		default:
			return inputDeps;
		}
	}

	private static void AddProvider(Entity provider, float capacity, ParallelWriter<AvailabilityProvider> providers, ref PathfindTargetSeeker<PathfindTargetBuffer> targetSeeker, float cost)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (targetSeeker.FindTargets(provider, cost) != 0)
		{
			providers.Enqueue(new AvailabilityProvider(provider, capacity, cost));
		}
	}

	private static void AddProvider(Entity provider, float capacity, ParallelWriter<AvailabilityProvider> providers, ref PathfindTargetSeeker<PathfindTargetBuffer> targetSeeker)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (targetSeeker.FindTargets(provider, 0f) != 0)
		{
			providers.Enqueue(new AvailabilityProvider(provider, capacity, 0f));
		}
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public ResourceAvailabilitySystem()
	{
	}
}
