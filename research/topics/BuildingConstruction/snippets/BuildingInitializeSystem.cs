using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Mathematics;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class BuildingInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindConnectionRequirementsJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingDataType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgradeData> m_ServiceUpgradeDataType;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorFacilityData> m_ExtractorFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<ConsumptionData> m_ConsumptionDataType;

		[ReadOnly]
		public ComponentTypeHandle<WorkplaceData> m_WorkplaceDataType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStationData> m_WaterPumpingStationDataType;

		[ReadOnly]
		public ComponentTypeHandle<WaterTowerData> m_WaterTowerDataType;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutletData> m_SewageOutletDataType;

		[ReadOnly]
		public ComponentTypeHandle<WastewaterTreatmentPlantData> m_WastewaterTreatmentPlantDataType;

		[ReadOnly]
		public ComponentTypeHandle<TransformerData> m_TransformerDataType;

		[ReadOnly]
		public ComponentTypeHandle<ParkingFacilityData> m_ParkingFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<PublicTransportStationData> m_PublicTransportStationDataType;

		[ReadOnly]
		public ComponentTypeHandle<CargoTransportStationData> m_CargoTransportStationDataType;

		[ReadOnly]
		public ComponentTypeHandle<ParkData> m_ParkDataType;

		[ReadOnly]
		public ComponentTypeHandle<CoverageData> m_CoverageDataType;

		[ReadOnly]
		public BufferTypeHandle<SubNet> m_SubNetType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<SubMesh> m_SubMeshType;

		[ReadOnly]
		public BufferTypeHandle<ServiceUpgradeBuilding> m_ServiceUpgradeBuildingType;

		public ComponentTypeHandle<BuildingData> m_BuildingDataType;

		public ComponentTypeHandle<PlaceableObjectData> m_PlaceableObjectDataType;

		public BufferTypeHandle<Effect> m_EffectType;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<GateData> m_GateData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<EffectData> m_EffectData;

		[ReadOnly]
		public ComponentLookup<VFXData> m_VFXData;

		[ReadOnly]
		public BufferLookup<AudioSourceData> m_AudioSourceData;

		[ReadOnly]
		public ComponentLookup<AudioSpotData> m_AudioSpotData;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> m_AudioEffectData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public void Execute(int index)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0309: Unknown result type (might be due to invalid IL or missing references)
			//IL_030e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0318: Unknown result type (might be due to invalid IL or missing references)
			//IL_031d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_05f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0609: Unknown result type (might be due to invalid IL or missing references)
			//IL_060e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0614: Unknown result type (might be due to invalid IL or missing references)
			//IL_0619: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bea: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bf7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0235: Unknown result type (might be due to invalid IL or missing references)
			//IL_0644: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c14: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c19: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
			//IL_067e: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_06ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c3b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0263: Unknown result type (might be due to invalid IL or missing references)
			//IL_0268: Unknown result type (might be due to invalid IL or missing references)
			//IL_0701: Unknown result type (might be due to invalid IL or missing references)
			//IL_0706: Unknown result type (might be due to invalid IL or missing references)
			//IL_070b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0714: Unknown result type (might be due to invalid IL or missing references)
			//IL_0719: Unknown result type (might be due to invalid IL or missing references)
			//IL_071e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0722: Unknown result type (might be due to invalid IL or missing references)
			//IL_0729: Unknown result type (might be due to invalid IL or missing references)
			//IL_072e: Unknown result type (might be due to invalid IL or missing references)
			//IL_073a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0741: Unknown result type (might be due to invalid IL or missing references)
			//IL_0746: Unknown result type (might be due to invalid IL or missing references)
			//IL_074b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0750: Unknown result type (might be due to invalid IL or missing references)
			//IL_0752: Unknown result type (might be due to invalid IL or missing references)
			//IL_0469: Unknown result type (might be due to invalid IL or missing references)
			//IL_046e: Unknown result type (might be due to invalid IL or missing references)
			//IL_09a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_075f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0764: Unknown result type (might be due to invalid IL or missing references)
			//IL_0771: Unknown result type (might be due to invalid IL or missing references)
			//IL_0776: Unknown result type (might be due to invalid IL or missing references)
			//IL_077b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0780: Unknown result type (might be due to invalid IL or missing references)
			//IL_0782: Unknown result type (might be due to invalid IL or missing references)
			//IL_0789: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c71: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c95: Unknown result type (might be due to invalid IL or missing references)
			//IL_0282: Unknown result type (might be due to invalid IL or missing references)
			//IL_09ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_09b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_09b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_09be: Unknown result type (might be due to invalid IL or missing references)
			//IL_09c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_09c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_09cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_09cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_09d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_09d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_09d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_09dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_09e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_09e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_09f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_09fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a01: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a05: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a14: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a1b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a20: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a25: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a29: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a3b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a4d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a4f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a51: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a65: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a6a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a6c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a80: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a85: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a8a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a8f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a93: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a9a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ab6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0abd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0941: Unknown result type (might be due to invalid IL or missing references)
			//IL_0946: Unknown result type (might be due to invalid IL or missing references)
			//IL_094d: Unknown result type (might be due to invalid IL or missing references)
			//IL_094f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0958: Unknown result type (might be due to invalid IL or missing references)
			//IL_095d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0969: Unknown result type (might be due to invalid IL or missing references)
			//IL_096e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0797: Unknown result type (might be due to invalid IL or missing references)
			//IL_0799: Unknown result type (might be due to invalid IL or missing references)
			//IL_079b: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_07ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_07bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_07cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_07dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_07ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_07ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0807: Unknown result type (might be due to invalid IL or missing references)
			//IL_080c: Unknown result type (might be due to invalid IL or missing references)
			//IL_080e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0822: Unknown result type (might be due to invalid IL or missing references)
			//IL_0827: Unknown result type (might be due to invalid IL or missing references)
			//IL_082c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0831: Unknown result type (might be due to invalid IL or missing references)
			//IL_083c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0843: Unknown result type (might be due to invalid IL or missing references)
			//IL_0296: Unknown result type (might be due to invalid IL or missing references)
			//IL_0488: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bb3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0921: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ce3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0cec: Unknown result type (might be due to invalid IL or missing references)
			//IL_049c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d28: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d2c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d33: Unknown result type (might be due to invalid IL or missing references)
			//IL_0576: Unknown result type (might be due to invalid IL or missing references)
			//IL_057b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b9f: Unknown result type (might be due to invalid IL or missing references)
			//IL_090d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ae1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0aed: Unknown result type (might be due to invalid IL or missing references)
			//IL_0af7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0afc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b01: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b06: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b1a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b1f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b26: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b28: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b2a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b31: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b36: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b3b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b3d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b44: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b49: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b4e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b57: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b5c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b63: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b65: Unknown result type (might be due to invalid IL or missing references)
			//IL_0880: Unknown result type (might be due to invalid IL or missing references)
			//IL_0885: Unknown result type (might be due to invalid IL or missing references)
			//IL_088c: Unknown result type (might be due to invalid IL or missing references)
			//IL_088e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0890: Unknown result type (might be due to invalid IL or missing references)
			//IL_0897: Unknown result type (might be due to invalid IL or missing references)
			//IL_089c: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_08aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_08af: Unknown result type (might be due to invalid IL or missing references)
			//IL_08b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_08bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_08c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_08ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_08d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
			ArchetypeChunk val = m_Chunks[index];
			NativeArray<BuildingData> nativeArray = ((ArchetypeChunk)(ref val)).GetNativeArray<BuildingData>(ref m_BuildingDataType);
			BufferAccessor<SubMesh> bufferAccessor = ((ArchetypeChunk)(ref val)).GetBufferAccessor<SubMesh>(ref m_SubMeshType);
			BufferAccessor<SubObject> bufferAccessor2 = ((ArchetypeChunk)(ref val)).GetBufferAccessor<SubObject>(ref m_SubObjectType);
			BufferAccessor<Effect> bufferAccessor3 = ((ArchetypeChunk)(ref val)).GetBufferAccessor<Effect>(ref m_EffectType);
			if (((ArchetypeChunk)(ref val)).Has<SpawnableBuildingData>(ref m_SpawnableBuildingDataType))
			{
				DynamicBuffer<SubObject> subObjects = default(DynamicBuffer<SubObject>);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					BuildingData buildingData = nativeArray[i];
					buildingData.m_Flags |= BuildingFlags.RequireRoad | BuildingFlags.RestrictedPedestrian | BuildingFlags.RestrictedCar | BuildingFlags.RestrictedParking | BuildingFlags.RestrictedTrack;
					if (bufferAccessor[i].Length == 0)
					{
						buildingData.m_Flags |= BuildingFlags.ColorizeLot;
					}
					if (CollectionUtils.TryGet<SubObject>(bufferAccessor2, i, ref subObjects))
					{
						CheckPropFlags(ref buildingData.m_Flags, subObjects);
					}
					nativeArray[i] = buildingData;
				}
			}
			else if (((ArchetypeChunk)(ref val)).Has<ServiceUpgradeData>(ref m_ServiceUpgradeDataType) || ((ArchetypeChunk)(ref val)).Has<ExtractorFacilityData>(ref m_ExtractorFacilityDataType))
			{
				NativeArray<PlaceableObjectData> nativeArray2 = ((ArchetypeChunk)(ref val)).GetNativeArray<PlaceableObjectData>(ref m_PlaceableObjectDataType);
				BufferAccessor<ServiceUpgradeBuilding> bufferAccessor4 = ((ArchetypeChunk)(ref val)).GetBufferAccessor<ServiceUpgradeBuilding>(ref m_ServiceUpgradeBuildingType);
				BufferAccessor<SubNet> bufferAccessor5 = ((ArchetypeChunk)(ref val)).GetBufferAccessor<SubNet>(ref m_SubNetType);
				bool isParkingFacility = ((ArchetypeChunk)(ref val)).Has<ParkingFacilityData>(ref m_ParkingFacilityDataType);
				bool isPublicTransportStation = ((ArchetypeChunk)(ref val)).Has<PublicTransportStationData>(ref m_PublicTransportStationDataType);
				bool isCargoTransportStation = ((ArchetypeChunk)(ref val)).Has<CargoTransportStationData>(ref m_CargoTransportStationDataType);
				bool flag = ((ArchetypeChunk)(ref val)).Has<ExtractorFacilityData>(ref m_ExtractorFacilityDataType);
				bool flag2 = ((ArchetypeChunk)(ref val)).Has<CoverageData>(ref m_CoverageDataType);
				BuildingFlags restrictionFlags = GetRestrictionFlags(isParkingFacility, isPublicTransportStation, isCargoTransportStation);
				DynamicBuffer<ServiceUpgradeBuilding> serviceUpgradeBuildings = default(DynamicBuffer<ServiceUpgradeBuilding>);
				PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
				DynamicBuffer<SubObject> subObjects2 = default(DynamicBuffer<SubObject>);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					BuildingData buildingData2 = nativeArray[j];
					buildingData2.m_Flags |= BuildingFlags.NoRoadConnection | restrictionFlags;
					if (bufferAccessor[j].Length == 0)
					{
						buildingData2.m_Flags |= BuildingFlags.ColorizeLot;
					}
					bool flag3 = CollectionUtils.TryGet<ServiceUpgradeBuilding>(bufferAccessor4, j, ref serviceUpgradeBuildings) && IsGateUpgrade(serviceUpgradeBuildings);
					if (flag2 || flag3)
					{
						buildingData2.m_Flags &= ~BuildingFlags.NoRoadConnection;
						if ((buildingData2.m_Flags & (BuildingFlags.CanBeOnRoad | BuildingFlags.CanBeOnRoadArea)) == 0)
						{
							buildingData2.m_Flags |= BuildingFlags.RequireRoad;
						}
						if (CollectionUtils.TryGet<PlaceableObjectData>(nativeArray2, j, ref placeableObjectData))
						{
							placeableObjectData.m_Flags &= ~Game.Objects.PlacementFlags.OwnerSide;
							nativeArray2[j] = placeableObjectData;
						}
					}
					if (CollectionUtils.TryGet<SubObject>(bufferAccessor2, j, ref subObjects2))
					{
						CheckPropFlags(ref buildingData2.m_Flags, subObjects2);
					}
					if (flag && bufferAccessor5.Length != 0)
					{
						DynamicBuffer<SubNet> val2 = bufferAccessor5[j];
						for (int k = 0; k < val2.Length; k++)
						{
							SubNet subNet = val2[k];
							if (m_NetData.HasComponent(subNet.m_Prefab) && (m_NetData[subNet.m_Prefab].m_RequiredLayers & Layer.ResourceLine) != Layer.None)
							{
								buildingData2.m_Flags |= BuildingFlags.HasResourceNode;
							}
						}
					}
					nativeArray[j] = buildingData2;
				}
			}
			else
			{
				NativeArray<ConsumptionData> nativeArray3 = ((ArchetypeChunk)(ref val)).GetNativeArray<ConsumptionData>(ref m_ConsumptionDataType);
				NativeArray<WorkplaceData> nativeArray4 = ((ArchetypeChunk)(ref val)).GetNativeArray<WorkplaceData>(ref m_WorkplaceDataType);
				BufferAccessor<SubNet> bufferAccessor6 = ((ArchetypeChunk)(ref val)).GetBufferAccessor<SubNet>(ref m_SubNetType);
				bool flag4 = ((ArchetypeChunk)(ref val)).Has<WaterPumpingStationData>(ref m_WaterPumpingStationDataType);
				bool flag5 = ((ArchetypeChunk)(ref val)).Has<WaterTowerData>(ref m_WaterTowerDataType);
				bool flag6 = ((ArchetypeChunk)(ref val)).Has<SewageOutletData>(ref m_SewageOutletDataType);
				bool flag7 = ((ArchetypeChunk)(ref val)).Has<WastewaterTreatmentPlantData>(ref m_WastewaterTreatmentPlantDataType);
				bool flag8 = ((ArchetypeChunk)(ref val)).Has<TransformerData>(ref m_TransformerDataType);
				bool flag9 = ((ArchetypeChunk)(ref val)).Has<ParkingFacilityData>(ref m_ParkingFacilityDataType);
				bool flag10 = ((ArchetypeChunk)(ref val)).Has<PublicTransportStationData>(ref m_PublicTransportStationDataType);
				bool flag11 = ((ArchetypeChunk)(ref val)).Has<CargoTransportStationData>(ref m_CargoTransportStationDataType);
				bool flag12 = ((ArchetypeChunk)(ref val)).Has<ParkData>(ref m_ParkDataType);
				BuildingFlags restrictionFlags2 = GetRestrictionFlags(flag9, flag10, flag11);
				DynamicBuffer<SubObject> subObjects3 = default(DynamicBuffer<SubObject>);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Layer layer = Layer.None;
					Layer layer2 = Layer.None;
					Layer layer3 = Layer.None;
					if (nativeArray3.Length != 0)
					{
						ConsumptionData consumptionData = nativeArray3[l];
						if (consumptionData.m_ElectricityConsumption > 0f)
						{
							layer |= Layer.PowerlineLow;
						}
						if (consumptionData.m_GarbageAccumulation > 0f)
						{
							layer |= Layer.Road;
						}
						if (consumptionData.m_WaterConsumption > 0f)
						{
							layer |= Layer.WaterPipe | Layer.SewagePipe;
						}
					}
					if (nativeArray4.Length != 0 && nativeArray4[l].m_MaxWorkers > 0)
					{
						layer |= Layer.Road;
					}
					if (flag4 || flag5)
					{
						layer |= Layer.WaterPipe;
					}
					if (flag6 || flag7)
					{
						layer |= Layer.SewagePipe;
					}
					if (flag8)
					{
						layer |= Layer.PowerlineLow;
					}
					if (layer != Layer.None && bufferAccessor6.Length != 0)
					{
						DynamicBuffer<SubNet> val3 = bufferAccessor6[l];
						for (int m = 0; m < val3.Length; m++)
						{
							SubNet subNet2 = val3[m];
							if (m_NetData.HasComponent(subNet2.m_Prefab))
							{
								NetData netData = m_NetData[subNet2.m_Prefab];
								if ((netData.m_RequiredLayers & Layer.Road) == 0)
								{
									layer2 |= netData.m_RequiredLayers | netData.m_LocalConnectLayers;
									layer3 |= netData.m_RequiredLayers;
								}
							}
						}
					}
					BuildingData buildingData3 = nativeArray[l];
					buildingData3.m_Flags |= restrictionFlags2;
					if ((layer & ~layer2) != Layer.None)
					{
						buildingData3.m_Flags |= BuildingFlags.RequireRoad;
					}
					else if (flag9 || flag10 || flag11 || flag12)
					{
						buildingData3.m_Flags |= BuildingFlags.RequireAccess;
					}
					if ((layer3 & Layer.PowerlineLow) != Layer.None)
					{
						buildingData3.m_Flags |= BuildingFlags.HasLowVoltageNode;
					}
					if ((layer3 & Layer.WaterPipe) != Layer.None)
					{
						buildingData3.m_Flags |= BuildingFlags.HasWaterNode;
					}
					if ((layer3 & Layer.SewagePipe) != Layer.None)
					{
						buildingData3.m_Flags |= BuildingFlags.HasSewageNode;
					}
					if (bufferAccessor[l].Length == 0)
					{
						buildingData3.m_Flags |= BuildingFlags.ColorizeLot;
					}
					if (flag12 && (buildingData3.m_Flags & (BuildingFlags.LeftAccess | BuildingFlags.RightAccess | BuildingFlags.BackAccess)) != 0)
					{
						buildingData3.m_Flags &= ~BuildingFlags.RestrictedPedestrian;
					}
					if (CollectionUtils.TryGet<SubObject>(bufferAccessor2, l, ref subObjects3))
					{
						CheckPropFlags(ref buildingData3.m_Flags, subObjects3);
					}
					nativeArray[l] = buildingData3;
				}
			}
			Random random = m_RandomSeed.GetRandom(index);
			bool2 val6 = default(bool2);
			EffectData effectData = default(EffectData);
			MeshData meshData = default(MeshData);
			float2 val15 = default(float2);
			float3 val20 = default(float3);
			for (int n = 0; n < bufferAccessor3.Length; n++)
			{
				DynamicBuffer<Effect> val4 = bufferAccessor3[n];
				DynamicBuffer<SubMesh> val5 = bufferAccessor[n];
				((bool2)(ref val6))..ctor(false, nativeArray.Length == 0);
				int num = 0;
				while (true)
				{
					if (num < val4.Length)
					{
						if (m_EffectData.TryGetComponent(val4[num].m_Effect, ref effectData) && (effectData.m_Flags.m_RequiredFlags & EffectConditionFlags.Collapsing) != EffectConditionFlags.None)
						{
							val6.x |= m_VFXData.HasComponent(val4[num].m_Effect);
							val6.y |= m_AudioSourceData.HasBuffer(val4[num].m_Effect);
							if (math.all(val6))
							{
								break;
							}
						}
						num++;
						continue;
					}
					for (int num2 = 0; num2 < val5.Length; num2++)
					{
						SubMesh subMesh = val5[num2];
						if (!m_MeshData.TryGetComponent(subMesh.m_SubMesh, ref meshData))
						{
							continue;
						}
						float2 val7 = MathUtils.Center(((Bounds3)(ref meshData.m_Bounds)).xz);
						float2 val8 = MathUtils.Size(((Bounds3)(ref meshData.m_Bounds)).xz);
						float3 val9 = subMesh.m_Position + math.rotate(subMesh.m_Rotation, new float3(val7.x, 0f, val7.y));
						if (!val6.y)
						{
							int2 val10 = math.max(int2.op_Implicit(0), (int2)(val8 * m_BuildingConfigurationData.m_CollapseSFXDensity));
							if (val10.x * val10.y > 1)
							{
								float2 val11 = val8 / float2.op_Implicit(val10);
								float3 val12 = math.rotate(subMesh.m_Rotation, new float3(val11.x, 0f, 0f));
								float3 val13 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, val11.y));
								float3 val14 = val9 - (val12 * ((float)val10.x * 0.5f - 0.5f) + val13 * ((float)val10.y * 0.5f - 0.5f));
								val4.Capacity = val4.Length + val10.x * val10.y;
								for (int num3 = 0; num3 < val10.y; num3++)
								{
									for (int num4 = 0; num4 < val10.x; num4++)
									{
										((float2)(ref val15))..ctor((float)num4, (float)num3);
										val4.Add(new Effect
										{
											m_Effect = m_BuildingConfigurationData.m_CollapseSFX,
											m_Position = val14 + val12 * val15.x + val13 * val15.y,
											m_Rotation = subMesh.m_Rotation,
											m_Scale = float3.op_Implicit(0.5f),
											m_Intensity = 0.5f,
											m_ParentMesh = num2,
											m_AnimationIndex = -1,
											m_Procedural = true
										});
									}
								}
							}
							else
							{
								val4.Add(new Effect
								{
									m_Effect = m_BuildingConfigurationData.m_CollapseSFX,
									m_Position = val9,
									m_Rotation = subMesh.m_Rotation,
									m_Scale = float3.op_Implicit(1f),
									m_Intensity = 1f,
									m_ParentMesh = num2,
									m_AnimationIndex = -1,
									m_Procedural = true
								});
							}
						}
						if (val6.x)
						{
							continue;
						}
						int2 val16 = math.max(int2.op_Implicit(1), (int2)(math.sqrt(val8) * 0.5f));
						float2 val17 = val8 / float2.op_Implicit(val16);
						float3 val18 = math.rotate(subMesh.m_Rotation, new float3(val17.x, 0f, 0f));
						float3 val19 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, val17.y));
						((float3)(ref val20))..ctor(val17.x * 0.05f, 1f, val17.y * 0.05f);
						val9 -= val18 * ((float)val16.x * 0.5f - 0.5f) + val19 * ((float)val16.y * 0.5f - 0.5f);
						val20.y = (val20.x + val20.y) * 0.5f;
						val4.Capacity = val4.Length + val16.x * val16.y;
						for (int num5 = 0; num5 < val16.y; num5++)
						{
							for (int num6 = 0; num6 < val16.x; num6++)
							{
								float2 val21 = new float2((float)num6, (float)num5) + ((Random)(ref random)).NextFloat2(float2.op_Implicit(-0.25f), float2.op_Implicit(0.25f));
								val4.Add(new Effect
								{
									m_Effect = m_BuildingConfigurationData.m_CollapseVFX,
									m_Position = val9 + val18 * val21.x + val19 * val21.y,
									m_Rotation = subMesh.m_Rotation,
									m_Scale = val20,
									m_Intensity = 1f,
									m_ParentMesh = num2,
									m_AnimationIndex = -1,
									m_Procedural = true
								});
							}
						}
					}
					break;
				}
			}
			NativeList<Effect> val22 = default(NativeList<Effect>);
			val22..ctor(AllocatorHandle.op_Implicit((Allocator)2));
			NativeList<float3> val23 = default(NativeList<float3>);
			val23..ctor(AllocatorHandle.op_Implicit((Allocator)2));
			float num7 = 125f;
			bool2 hasFireSfxEffects = default(bool2);
			EffectData effectData2 = default(EffectData);
			for (int num8 = 0; num8 < bufferAccessor3.Length; num8++)
			{
				DynamicBuffer<Effect> effects = bufferAccessor3[num8];
				((bool2)(ref hasFireSfxEffects))..ctor(false, false);
				for (int num9 = 0; num9 < effects.Length; num9++)
				{
					if (m_EffectData.TryGetComponent(effects[num9].m_Effect, ref effectData2) && (effectData2.m_Flags.m_RequiredFlags & EffectConditionFlags.OnFire) != EffectConditionFlags.None)
					{
						hasFireSfxEffects.x |= m_AudioEffectData.HasComponent(effects[num9].m_Effect);
						hasFireSfxEffects.y |= m_AudioSpotData.HasComponent(effects[num9].m_Effect);
						Effect effect = effects[num9];
						val22.Add(ref effect);
					}
				}
				for (int num10 = 0; num10 < val22.Length; num10++)
				{
					Effect effect2 = val22[num10];
					bool flag13 = false;
					for (int num11 = 0; num11 < val23.Length; num11++)
					{
						if (math.distancesq(effect2.m_Position, val23[num11]) < num7 * num7)
						{
							flag13 = true;
							break;
						}
					}
					if (!flag13)
					{
						val23.Add(ref effect2.m_Position);
						AddFireSfxToBuilding(ref hasFireSfxEffects, effects, effect2.m_Position, effect2.m_Rotation, effect2.m_ParentMesh);
					}
				}
				val22.Clear();
				val23.Clear();
			}
		}

		private bool IsGateUpgrade(DynamicBuffer<ServiceUpgradeBuilding> serviceUpgradeBuildings)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < serviceUpgradeBuildings.Length; i++)
			{
				if (m_GateData.HasComponent(serviceUpgradeBuildings[i].m_Building))
				{
					return true;
				}
			}
			return false;
		}

		private BuildingFlags GetRestrictionFlags(bool isParkingFacility, bool isPublicTransportStation, bool isCargoTransportStation)
		{
			BuildingFlags buildingFlags = (BuildingFlags)0u;
			if (!isParkingFacility && !isPublicTransportStation)
			{
				buildingFlags |= BuildingFlags.RestrictedPedestrian;
			}
			if (!isParkingFacility && !isCargoTransportStation && !isPublicTransportStation)
			{
				buildingFlags |= BuildingFlags.RestrictedCar;
			}
			if (!isParkingFacility)
			{
				buildingFlags |= BuildingFlags.RestrictedParking;
			}
			if (!isPublicTransportStation && !isCargoTransportStation)
			{
				buildingFlags |= BuildingFlags.RestrictedTrack;
			}
			return buildingFlags;
		}

		private void AddFireSfxToBuilding(ref bool2 hasFireSfxEffects, DynamicBuffer<Effect> effects, float3 position, quaternion rotation, int parent)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			if (!hasFireSfxEffects.x)
			{
				effects.Add(new Effect
				{
					m_Effect = m_BuildingConfigurationData.m_FireLoopSFX,
					m_Position = position,
					m_Rotation = rotation,
					m_Scale = float3.op_Implicit(1f),
					m_Intensity = 1f,
					m_ParentMesh = parent,
					m_AnimationIndex = -1,
					m_Procedural = true
				});
			}
			if (!hasFireSfxEffects.y)
			{
				effects.Add(new Effect
				{
					m_Effect = m_BuildingConfigurationData.m_FireSpotSFX,
					m_Position = position,
					m_Rotation = rotation,
					m_Scale = float3.op_Implicit(1f),
					m_Intensity = 1f,
					m_ParentMesh = parent,
					m_AnimationIndex = -1,
					m_Procedural = true
				});
			}
		}

		private void CheckPropFlags(ref BuildingFlags flags, DynamicBuffer<SubObject> subObjects, int maxDepth = 10)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			if (--maxDepth < 0)
			{
				return;
			}
			SpawnLocationData spawnLocationData = default(SpawnLocationData);
			DynamicBuffer<SubObject> subObjects2 = default(DynamicBuffer<SubObject>);
			for (int i = 0; i < subObjects.Length; i++)
			{
				SubObject subObject = subObjects[i];
				if (m_SpawnLocationData.TryGetComponent(subObject.m_Prefab, ref spawnLocationData) && spawnLocationData.m_ActivityMask.m_Mask == 0 && (spawnLocationData.m_ConnectionType == RouteConnectionType.Pedestrian || (spawnLocationData.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData.m_RoadTypes == RoadTypes.Bicycle)))
				{
					flags |= BuildingFlags.HasInsideRoom;
				}
				if (m_SubObjects.TryGetBuffer(subObject.m_Prefab, ref subObjects2))
				{
					CheckPropFlags(ref flags, subObjects2, maxDepth);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPoweredData> __Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;

		public BufferTypeHandle<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;

		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneServiceConsumptionData> __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorFacilityData> __Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterTowerData> __Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WastewaterTreatmentPlantData> __Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransformerData> __Game_Prefabs_TransformerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PublicTransportStationData> __Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CargoTransportStationData> __Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkData> __Game_Prefabs_ParkData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubNet> __Game_Prefabs_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RO_BufferTypeHandle;

		public BufferTypeHandle<Effect> __Game_Prefabs_Effect_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GateData> __Game_Prefabs_GateData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VFXData> __Game_Prefabs_VFXData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AudioSourceData> __Game_Prefabs_AudioSourceData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<AudioSpotData> __Game_Prefabs_AudioSpotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
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
			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01db: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0202: Unknown result type (might be due to invalid IL or missing references)
			//IL_020a: Unknown result type (might be due to invalid IL or missing references)
			//IL_020f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0217: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0224: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Unknown result type (might be due to invalid IL or missing references)
			//IL_0231: Unknown result type (might be due to invalid IL or missing references)
			//IL_0236: Unknown result type (might be due to invalid IL or missing references)
			//IL_023e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_024b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0250: Unknown result type (might be due to invalid IL or missing references)
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Deleted>(true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabData>(true);
			__Game_Prefabs_BuildingData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<BuildingData>(false);
			__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<BuildingExtensionData>(false);
			__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<BuildingTerraformData>(false);
			__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ConsumptionData>(false);
			__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ObjectGeometryData>(false);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<SpawnableBuildingData>(true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<SignatureBuildingData>(true);
			__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PlaceableObjectData>(false);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ServiceUpgradeData>(true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<BuildingPropertyData>(true);
			__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WaterPoweredData>(true);
			__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<SewageOutletData>(true);
			__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<ServiceUpgradeBuilding>(true);
			__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<CollectedServiceBuildingBudgetData>(true);
			__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<ServiceUpkeepData>(false);
			__Game_Prefabs_ZoneData_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ZoneData>(false);
			__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ZoneServiceConsumptionData>(true);
			__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ExtractorFacilityData>(true);
			__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ConsumptionData>(true);
			__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WorkplaceData>(true);
			__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WaterPumpingStationData>(true);
			__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WaterTowerData>(true);
			__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WastewaterTreatmentPlantData>(true);
			__Game_Prefabs_TransformerData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<TransformerData>(true);
			__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ParkingFacilityData>(true);
			__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PublicTransportStationData>(true);
			__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<CargoTransportStationData>(true);
			__Game_Prefabs_ParkData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ParkData>(true);
			__Game_Prefabs_CoverageData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<CoverageData>(true);
			__Game_Prefabs_SubNet_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<SubNet>(true);
			__Game_Prefabs_SubObject_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<SubObject>(true);
			__Game_Prefabs_SubMesh_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<SubMesh>(true);
			__Game_Prefabs_Effect_RW_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<Effect>(false);
			__Game_Prefabs_NetData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<NetData>(true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SpawnLocationData>(true);
			__Game_Prefabs_GateData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<GateData>(true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<MeshData>(true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<EffectData>(true);
			__Game_Prefabs_VFXData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<VFXData>(true);
			__Game_Prefabs_AudioSourceData_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<AudioSourceData>(true);
			__Game_Prefabs_AudioSpotData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<AudioSpotData>(true);
			__Game_Prefabs_AudioEffectData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<AudioEffectData>(true);
			__Game_Prefabs_SubObject_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<SubObject>(true);
		}
	}

	private static ILog log;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_ConfigurationQuery;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_547773814_0;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		log = LogManager.GetLogger("Simulation");
		base.OnCreate();
		m_PrefabSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PrefabSystem>();
		EntityQueryDesc[] array = new EntityQueryDesc[2];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Created>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		val.Any = (ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadWrite<BuildingData>(),
			ComponentType.ReadWrite<BuildingExtensionData>(),
			ComponentType.ReadWrite<ServiceUpgradeData>(),
			ComponentType.ReadWrite<SpawnableBuildingData>()
		};
		array[0] = val;
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<PrefabData>(),
			ComponentType.ReadWrite<ServiceUpgradeData>()
		};
		array[1] = val;
		m_PrefabQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		m_ConfigurationQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<BuildingConfigurationData>() });
		((ComponentSystemBase)this).RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_1245: Unknown result type (might be due to invalid IL or missing references)
		//IL_124a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1262: Unknown result type (might be due to invalid IL or missing references)
		//IL_1267: Unknown result type (might be due to invalid IL or missing references)
		//IL_127f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1284: Unknown result type (might be due to invalid IL or missing references)
		//IL_129c: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_12b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_12be: Unknown result type (might be due to invalid IL or missing references)
		//IL_12d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_12db: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1310: Unknown result type (might be due to invalid IL or missing references)
		//IL_1315: Unknown result type (might be due to invalid IL or missing references)
		//IL_132d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1332: Unknown result type (might be due to invalid IL or missing references)
		//IL_134a: Unknown result type (might be due to invalid IL or missing references)
		//IL_134f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1367: Unknown result type (might be due to invalid IL or missing references)
		//IL_136c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1384: Unknown result type (might be due to invalid IL or missing references)
		//IL_1389: Unknown result type (might be due to invalid IL or missing references)
		//IL_13a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_13a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_13be: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_13db: Unknown result type (might be due to invalid IL or missing references)
		//IL_13e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_13f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_13fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1415: Unknown result type (might be due to invalid IL or missing references)
		//IL_141a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1432: Unknown result type (might be due to invalid IL or missing references)
		//IL_1437: Unknown result type (might be due to invalid IL or missing references)
		//IL_144f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1454: Unknown result type (might be due to invalid IL or missing references)
		//IL_146c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1471: Unknown result type (might be due to invalid IL or missing references)
		//IL_1489: Unknown result type (might be due to invalid IL or missing references)
		//IL_148e: Unknown result type (might be due to invalid IL or missing references)
		//IL_14a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_14ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_14c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_14c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_14e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_14e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_14fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1502: Unknown result type (might be due to invalid IL or missing references)
		//IL_151a: Unknown result type (might be due to invalid IL or missing references)
		//IL_151f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1537: Unknown result type (might be due to invalid IL or missing references)
		//IL_153c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1554: Unknown result type (might be due to invalid IL or missing references)
		//IL_1559: Unknown result type (might be due to invalid IL or missing references)
		//IL_1571: Unknown result type (might be due to invalid IL or missing references)
		//IL_1576: Unknown result type (might be due to invalid IL or missing references)
		//IL_158e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1593: Unknown result type (might be due to invalid IL or missing references)
		//IL_15ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_15b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_15c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_15cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_15e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_15e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1604: Unknown result type (might be due to invalid IL or missing references)
		//IL_160a: Unknown result type (might be due to invalid IL or missing references)
		//IL_160c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1611: Unknown result type (might be due to invalid IL or missing references)
		//IL_1624: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0544: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b23: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_058a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0600: Unknown result type (might be due to invalid IL or missing references)
		//IL_0605: Unknown result type (might be due to invalid IL or missing references)
		//IL_0613: Unknown result type (might be due to invalid IL or missing references)
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_062c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0631: Unknown result type (might be due to invalid IL or missing references)
		//IL_0636: Unknown result type (might be due to invalid IL or missing references)
		//IL_066a: Unknown result type (might be due to invalid IL or missing references)
		//IL_066f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b68: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_1135: Unknown result type (might be due to invalid IL or missing references)
		//IL_113a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1140: Unknown result type (might be due to invalid IL or missing references)
		//IL_1145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b87: Unknown result type (might be due to invalid IL or missing references)
		//IL_0773: Unknown result type (might be due to invalid IL or missing references)
		//IL_0778: Unknown result type (might be due to invalid IL or missing references)
		//IL_0781: Unknown result type (might be due to invalid IL or missing references)
		//IL_0786: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06db: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0708: Unknown result type (might be due to invalid IL or missing references)
		//IL_070f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0714: Unknown result type (might be due to invalid IL or missing references)
		//IL_0722: Unknown result type (might be due to invalid IL or missing references)
		//IL_0729: Unknown result type (might be due to invalid IL or missing references)
		//IL_073f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0746: Unknown result type (might be due to invalid IL or missing references)
		//IL_105d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b99: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09db: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0798: Unknown result type (might be due to invalid IL or missing references)
		//IL_079d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1158: Unknown result type (might be due to invalid IL or missing references)
		//IL_115d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1163: Unknown result type (might be due to invalid IL or missing references)
		//IL_1169: Unknown result type (might be due to invalid IL or missing references)
		//IL_116e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a24: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d48: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07de: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_098b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0acd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c02: Unknown result type (might be due to invalid IL or missing references)
		//IL_11b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_11b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0906: Unknown result type (might be due to invalid IL or missing references)
		//IL_090b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0912: Unknown result type (might be due to invalid IL or missing references)
		//IL_0917: Unknown result type (might be due to invalid IL or missing references)
		//IL_0923: Unknown result type (might be due to invalid IL or missing references)
		//IL_0928: Unknown result type (might be due to invalid IL or missing references)
		//IL_092d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0937: Unknown result type (might be due to invalid IL or missing references)
		//IL_0939: Unknown result type (might be due to invalid IL or missing references)
		//IL_0940: Unknown result type (might be due to invalid IL or missing references)
		//IL_0945: Unknown result type (might be due to invalid IL or missing references)
		//IL_094c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0951: Unknown result type (might be due to invalid IL or missing references)
		//IL_095d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0962: Unknown result type (might be due to invalid IL or missing references)
		//IL_0967: Unknown result type (might be due to invalid IL or missing references)
		//IL_096c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0892: Unknown result type (might be due to invalid IL or missing references)
		//IL_0899: Unknown result type (might be due to invalid IL or missing references)
		//IL_089e: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0821: Unknown result type (might be due to invalid IL or missing references)
		//IL_0828: Unknown result type (might be due to invalid IL or missing references)
		//IL_082e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0837: Unknown result type (might be due to invalid IL or missing references)
		//IL_083e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0843: Unknown result type (might be due to invalid IL or missing references)
		//IL_084a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0854: Unknown result type (might be due to invalid IL or missing references)
		//IL_0859: Unknown result type (might be due to invalid IL or missing references)
		//IL_085e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0863: Unknown result type (might be due to invalid IL or missing references)
		//IL_0865: Unknown result type (might be due to invalid IL or missing references)
		//IL_086c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0871: Unknown result type (might be due to invalid IL or missing references)
		//IL_0873: Unknown result type (might be due to invalid IL or missing references)
		//IL_0875: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0caa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cdd: Unknown result type (might be due to invalid IL or missing references)
		EntityCommandBuffer val = default(EntityCommandBuffer);
		((EntityCommandBuffer)(ref val))..ctor((Allocator)3, (PlaybackPolicy)0);
		NativeArray<ArchetypeChunk> chunks = ((EntityQuery)(ref m_PrefabQuery)).ToArchetypeChunkArray(AllocatorHandle.op_Implicit((Allocator)3));
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<Deleted> componentTypeHandle = InternalCompilerInterface.GetComponentTypeHandle<Deleted>(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<PrefabData> componentTypeHandle2 = InternalCompilerInterface.GetComponentTypeHandle<PrefabData>(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<BuildingData> componentTypeHandle3 = InternalCompilerInterface.GetComponentTypeHandle<BuildingData>(ref __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<BuildingExtensionData> componentTypeHandle4 = InternalCompilerInterface.GetComponentTypeHandle<BuildingExtensionData>(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<BuildingTerraformData> componentTypeHandle5 = InternalCompilerInterface.GetComponentTypeHandle<BuildingTerraformData>(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<ConsumptionData> componentTypeHandle6 = InternalCompilerInterface.GetComponentTypeHandle<ConsumptionData>(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<ObjectGeometryData> componentTypeHandle7 = InternalCompilerInterface.GetComponentTypeHandle<ObjectGeometryData>(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<SpawnableBuildingData> componentTypeHandle8 = InternalCompilerInterface.GetComponentTypeHandle<SpawnableBuildingData>(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<SignatureBuildingData> componentTypeHandle9 = InternalCompilerInterface.GetComponentTypeHandle<SignatureBuildingData>(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<PlaceableObjectData> componentTypeHandle10 = InternalCompilerInterface.GetComponentTypeHandle<PlaceableObjectData>(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<ServiceUpgradeData> componentTypeHandle11 = InternalCompilerInterface.GetComponentTypeHandle<ServiceUpgradeData>(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<BuildingPropertyData> componentTypeHandle12 = InternalCompilerInterface.GetComponentTypeHandle<BuildingPropertyData>(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<WaterPoweredData> componentTypeHandle13 = InternalCompilerInterface.GetComponentTypeHandle<WaterPoweredData>(ref __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<SewageOutletData> componentTypeHandle14 = InternalCompilerInterface.GetComponentTypeHandle<SewageOutletData>(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		BufferTypeHandle<ServiceUpgradeBuilding> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle<ServiceUpgradeBuilding>(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<CollectedServiceBuildingBudgetData> componentTypeHandle15 = InternalCompilerInterface.GetComponentTypeHandle<CollectedServiceBuildingBudgetData>(ref __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		BufferTypeHandle<ServiceUpkeepData> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle<ServiceUpkeepData>(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentLookup<ZoneData> componentLookup = InternalCompilerInterface.GetComponentLookup<ZoneData>(ref __TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef);
		ComponentLookup<ZoneServiceConsumptionData> componentLookup2 = InternalCompilerInterface.GetComponentLookup<ZoneServiceConsumptionData>(ref __TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef);
		((SystemBase)this).CompleteDependency();
		DynamicBuffer<BuildingUpgradeElement> val4 = default(DynamicBuffer<BuildingUpgradeElement>);
		PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
		Bounds2 xz2 = default(Bounds2);
		Bounds2 val7 = default(Bounds2);
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk val2 = chunks[i];
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref val2)).GetNativeArray(entityTypeHandle);
			BufferAccessor<ServiceUpgradeBuilding> bufferAccessor = ((ArchetypeChunk)(ref val2)).GetBufferAccessor<ServiceUpgradeBuilding>(ref bufferTypeHandle);
			if (((ArchetypeChunk)(ref val2)).Has<Deleted>(ref componentTypeHandle))
			{
				if (bufferAccessor.Length == 0)
				{
					continue;
				}
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					Entity upgrade = nativeArray[j];
					DynamicBuffer<ServiceUpgradeBuilding> val3 = bufferAccessor[j];
					for (int k = 0; k < val3.Length; k++)
					{
						ServiceUpgradeBuilding serviceUpgradeBuilding = val3[k];
						if (EntitiesExtensions.TryGetBuffer<BuildingUpgradeElement>(((ComponentSystemBase)this).EntityManager, serviceUpgradeBuilding.m_Building, false, ref val4))
						{
							CollectionUtils.RemoveValue<BuildingUpgradeElement>(val4, new BuildingUpgradeElement(upgrade));
						}
					}
				}
				continue;
			}
			NativeArray<PrefabData> nativeArray2 = ((ArchetypeChunk)(ref val2)).GetNativeArray<PrefabData>(ref componentTypeHandle2);
			NativeArray<ObjectGeometryData> nativeArray3 = ((ArchetypeChunk)(ref val2)).GetNativeArray<ObjectGeometryData>(ref componentTypeHandle7);
			NativeArray<BuildingData> nativeArray4 = ((ArchetypeChunk)(ref val2)).GetNativeArray<BuildingData>(ref componentTypeHandle3);
			NativeArray<BuildingExtensionData> nativeArray5 = ((ArchetypeChunk)(ref val2)).GetNativeArray<BuildingExtensionData>(ref componentTypeHandle4);
			NativeArray<ConsumptionData> nativeArray6 = ((ArchetypeChunk)(ref val2)).GetNativeArray<ConsumptionData>(ref componentTypeHandle6);
			NativeArray<SpawnableBuildingData> nativeArray7 = ((ArchetypeChunk)(ref val2)).GetNativeArray<SpawnableBuildingData>(ref componentTypeHandle8);
			NativeArray<PlaceableObjectData> nativeArray8 = ((ArchetypeChunk)(ref val2)).GetNativeArray<PlaceableObjectData>(ref componentTypeHandle10);
			NativeArray<ServiceUpgradeData> nativeArray9 = ((ArchetypeChunk)(ref val2)).GetNativeArray<ServiceUpgradeData>(ref componentTypeHandle11);
			NativeArray<BuildingPropertyData> nativeArray10 = ((ArchetypeChunk)(ref val2)).GetNativeArray<BuildingPropertyData>(ref componentTypeHandle12);
			BufferAccessor<ServiceUpkeepData> bufferAccessor2 = ((ArchetypeChunk)(ref val2)).GetBufferAccessor<ServiceUpkeepData>(ref bufferTypeHandle2);
			bool flag = ((ArchetypeChunk)(ref val2)).Has<CollectedServiceBuildingBudgetData>(ref componentTypeHandle15);
			bool flag2 = ((ArchetypeChunk)(ref val2)).Has<SignatureBuildingData>(ref componentTypeHandle9);
			bool flag3 = ((ArchetypeChunk)(ref val2)).Has<WaterPoweredData>(ref componentTypeHandle13);
			bool flag4 = ((ArchetypeChunk)(ref val2)).Has<SewageOutletData>(ref componentTypeHandle14);
			if (nativeArray4.Length != 0)
			{
				NativeArray<BuildingTerraformData> nativeArray11 = ((ArchetypeChunk)(ref val2)).GetNativeArray<BuildingTerraformData>(ref componentTypeHandle5);
				for (int l = 0; l < nativeArray4.Length; l++)
				{
					BuildingPrefab prefab = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray2[l]);
					BuildingTerraformOverride component = prefab.GetComponent<BuildingTerraformOverride>();
					ObjectGeometryData objectGeometryData = nativeArray3[l];
					BuildingTerraformData buildingTerraformData = nativeArray11[l];
					BuildingData buildingData = nativeArray4[l];
					InitializeLotSize(prefab, component, ref objectGeometryData, ref buildingTerraformData, ref buildingData);
					if (nativeArray7.Length != 0 && !flag2)
					{
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.DeleteOverridden;
					}
					else
					{
						objectGeometryData.m_Flags &= ~Game.Objects.GeometryFlags.Overridable;
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
					}
					if (flag3)
					{
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
					}
					else if (flag4 && prefab.GetComponent<SewageOutlet>().m_AllowSubmerged)
					{
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
					}
					objectGeometryData.m_Flags &= ~Game.Objects.GeometryFlags.Brushable;
					objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot;
					if (CollectionUtils.TryGet<PlaceableObjectData>(nativeArray8, l, ref placeableObjectData))
					{
						if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.OnGround | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Swaying)) == (Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Swaying))
						{
							objectGeometryData.m_Flags &= ~(Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.OccupyZone);
						}
						switch (prefab.m_AccessType)
						{
						case BuildingAccessType.OnRoad:
							placeableObjectData.m_Flags |= Game.Objects.PlacementFlags.NetObject | Game.Objects.PlacementFlags.RoadEdge;
							placeableObjectData.m_SubReplacementType = SubReplacementType.None;
							buildingData.m_Flags |= BuildingFlags.CanBeOnRoad;
							break;
						case BuildingAccessType.OnRoadArea:
							placeableObjectData.m_Flags |= Game.Objects.PlacementFlags.NetObject | Game.Objects.PlacementFlags.RoadEdge;
							placeableObjectData.m_SubReplacementType = SubReplacementType.None;
							buildingData.m_Flags |= BuildingFlags.CanBeOnRoadArea;
							break;
						}
						nativeArray8[l] = placeableObjectData;
					}
					nativeArray3[l] = objectGeometryData;
					nativeArray11[l] = buildingTerraformData;
					nativeArray4[l] = buildingData;
				}
			}
			if (nativeArray5.Length != 0)
			{
				NativeArray<BuildingTerraformData> nativeArray12 = ((ArchetypeChunk)(ref val2)).GetNativeArray<BuildingTerraformData>(ref componentTypeHandle5);
				for (int m = 0; m < nativeArray5.Length; m++)
				{
					BuildingExtensionPrefab prefab2 = m_PrefabSystem.GetPrefab<BuildingExtensionPrefab>(nativeArray2[m]);
					ObjectGeometryData objectGeometryData2 = nativeArray3[m];
					if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
					{
						float2 xz = ((float3)(ref objectGeometryData2.m_Pivot)).xz;
						float2 val5 = ((float3)(ref objectGeometryData2.m_LegSize)).xz * 0.5f + objectGeometryData2.m_LegOffset;
						((Bounds2)(ref xz2))..ctor(xz - val5, xz + val5);
					}
					else
					{
						xz2 = ((Bounds3)(ref objectGeometryData2.m_Bounds)).xz;
					}
					objectGeometryData2.m_Bounds.min = math.min(objectGeometryData2.m_Bounds.min, new float3(-0.5f, 0f, -0.5f));
					objectGeometryData2.m_Bounds.max = math.max(objectGeometryData2.m_Bounds.max, new float3(0.5f, 5f, 0.5f));
					objectGeometryData2.m_Flags &= ~(Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Brushable);
					objectGeometryData2.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot | Game.Objects.GeometryFlags.IgnoreElevatedGround;
					BuildingExtensionData buildingExtensionData = nativeArray5[m];
					buildingExtensionData.m_Position = prefab2.m_Position;
					buildingExtensionData.m_LotSize = prefab2.m_OverrideLotSize;
					buildingExtensionData.m_External = prefab2.m_ExternalLot;
					if (prefab2.m_OverrideHeight > 0f)
					{
						objectGeometryData2.m_Bounds.max.y = prefab2.m_OverrideHeight;
					}
					if (math.all(buildingExtensionData.m_LotSize > 0))
					{
						float2 val6 = float2.op_Implicit(buildingExtensionData.m_LotSize);
						val6 *= 8f;
						((Bounds2)(ref val7))..ctor(val6 * -0.5f, val6 * 0.5f);
						val6 -= 0.4f;
						((float3)(ref objectGeometryData2.m_Bounds.min)).xz = val6 * -0.5f;
						((float3)(ref objectGeometryData2.m_Bounds.max)).xz = val6 * 0.5f;
						if (bufferAccessor.Length != 0)
						{
							objectGeometryData2.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
						}
					}
					else
					{
						Bounds3 bounds = objectGeometryData2.m_Bounds;
						val7 = ((Bounds3)(ref objectGeometryData2.m_Bounds)).xz;
						if (bufferAccessor.Length != 0)
						{
							DynamicBuffer<ServiceUpgradeBuilding> val8 = bufferAccessor[m];
							for (int n = 0; n < val8.Length; n++)
							{
								ServiceUpgradeBuilding serviceUpgradeBuilding2 = val8[n];
								BuildingPrefab prefab3 = m_PrefabSystem.GetPrefab<BuildingPrefab>(serviceUpgradeBuilding2.m_Building);
								float2 val9 = float2.op_Implicit(new int2(prefab3.m_LotWidth, prefab3.m_LotDepth));
								val9 *= 8f;
								float2 val10 = val9;
								val9 -= 0.4f;
								if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Standing) == 0 && prefab3.TryGet<StandingObject>(out var component2))
								{
									val9 = ((float3)(ref component2.m_LegSize)).xz + math.select(default(float2), ((float3)(ref component2.m_LegSize)).xz + component2.m_LegGap, component2.m_LegGap != 0f);
									val9 -= 0.4f;
									val10 = val9;
									if (component2.m_CircularLeg)
									{
										objectGeometryData2.m_Flags |= Game.Objects.GeometryFlags.Circular;
									}
								}
								if (n == 0)
								{
									((Bounds3)(ref bounds)).xz = new Bounds2(val9 * -0.5f, val9 * 0.5f) - ((float3)(ref prefab2.m_Position)).xz;
									val7 = new Bounds2(val10 * -0.5f, val10 * 0.5f) - ((float3)(ref prefab2.m_Position)).xz;
								}
								else
								{
									((Bounds3)(ref bounds)).xz = ((Bounds3)(ref bounds)).xz & (new Bounds2(val9 * -0.5f, val9 * 0.5f) - ((float3)(ref prefab2.m_Position)).xz);
									val7 &= new Bounds2(val10 * -0.5f, val10 * 0.5f) - ((float3)(ref prefab2.m_Position)).xz;
								}
							}
							((Bounds3)(ref objectGeometryData2.m_Bounds)).xz = ((Bounds3)(ref bounds)).xz;
							objectGeometryData2.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
						}
						float2 val11 = math.min(-((float3)(ref bounds.min)).xz, ((float3)(ref bounds.max)).xz) * 0.25f - 0.01f;
						buildingExtensionData.m_LotSize.x = math.max(1, Mathf.CeilToInt(val11.x));
						buildingExtensionData.m_LotSize.y = math.max(1, Mathf.CeilToInt(val11.y));
					}
					if (buildingExtensionData.m_External)
					{
						float2 val12 = float2.op_Implicit(buildingExtensionData.m_LotSize);
						val12 *= 8f;
						objectGeometryData2.m_Layers |= MeshLayer.Default;
						objectGeometryData2.m_MinLod = math.min(objectGeometryData2.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(val12.x, 0f, val12.y))));
					}
					if (nativeArray12.Length != 0)
					{
						BuildingTerraformOverride component3 = prefab2.GetComponent<BuildingTerraformOverride>();
						BuildingTerraformData buildingTerraformData2 = nativeArray12[m];
						InitializeTerraformData(component3, ref buildingTerraformData2, val7, xz2);
						nativeArray12[m] = buildingTerraformData2;
					}
					objectGeometryData2.m_Size = math.max(ObjectUtils.GetSize(objectGeometryData2.m_Bounds), new float3(1f, 5f, 1f));
					nativeArray3[m] = objectGeometryData2;
					nativeArray5[m] = buildingExtensionData;
				}
			}
			if (nativeArray7.Length != 0)
			{
				for (int num = 0; num < nativeArray7.Length; num++)
				{
					Entity val13 = nativeArray[num];
					BuildingPrefab prefab4 = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray2[num]);
					BuildingPropertyData buildingPropertyData = ((nativeArray10.Length != 0) ? nativeArray10[num] : default(BuildingPropertyData));
					SpawnableBuildingData spawnableBuildingData = nativeArray7[num];
					if (!(spawnableBuildingData.m_ZonePrefab != Entity.Null))
					{
						continue;
					}
					Entity zonePrefab = spawnableBuildingData.m_ZonePrefab;
					ZoneData zoneData = componentLookup[zonePrefab];
					if (!flag2)
					{
						((EntityCommandBuffer)(ref val)).SetSharedComponent<BuildingSpawnGroupData>(val13, new BuildingSpawnGroupData(zoneData.m_ZoneType));
						ushort num2 = (ushort)math.clamp(Mathf.CeilToInt(nativeArray3[num].m_Size.y), 0, 65535);
						if (spawnableBuildingData.m_Level == 1)
						{
							if (prefab4.m_LotWidth == 1 && (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0)
							{
								zoneData.m_ZoneFlags |= ZoneFlags.SupportNarrow;
								componentLookup[zonePrefab] = zoneData;
							}
							if (prefab4.m_AccessType == BuildingAccessType.LeftCorner && (zoneData.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == 0)
							{
								zoneData.m_ZoneFlags |= ZoneFlags.SupportLeftCorner;
								componentLookup[zonePrefab] = zoneData;
							}
							if (prefab4.m_AccessType == BuildingAccessType.RightCorner && (zoneData.m_ZoneFlags & ZoneFlags.SupportRightCorner) == 0)
							{
								zoneData.m_ZoneFlags |= ZoneFlags.SupportRightCorner;
								componentLookup[zonePrefab] = zoneData;
							}
							if (prefab4.m_AccessType == BuildingAccessType.Front && prefab4.m_LotWidth <= 3 && prefab4.m_LotDepth <= 2)
							{
								if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 3) && num2 < zoneData.m_MinOddHeight)
								{
									zoneData.m_MinOddHeight = num2;
									componentLookup[zonePrefab] = zoneData;
								}
								if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 2) && num2 < zoneData.m_MinEvenHeight)
								{
									zoneData.m_MinEvenHeight = num2;
									componentLookup[zonePrefab] = zoneData;
								}
							}
						}
						if (num2 > zoneData.m_MaxHeight)
						{
							zoneData.m_MaxHeight = num2;
							componentLookup[zonePrefab] = zoneData;
						}
					}
					int level = spawnableBuildingData.m_Level;
					BuildingData buildingData2 = nativeArray4[num];
					int lotSize = buildingData2.m_LotSize.x * buildingData2.m_LotSize.y;
					if (nativeArray6.Length != 0 && !prefab4.Has<ServiceConsumption>() && componentLookup2.HasComponent(zonePrefab))
					{
						ZoneServiceConsumptionData zoneServiceConsumptionData = componentLookup2[zonePrefab];
						ref ConsumptionData reference = ref CollectionUtils.ElementAt<ConsumptionData>(nativeArray6, num);
						if (flag2)
						{
							level = 2;
						}
						bool isStorage = buildingPropertyData.m_AllowedStored != Resource.NoResource;
						EconomyParameterData economyParameterData = ((EntityQuery)(ref __query_547773814_0)).GetSingleton<EconomyParameterData>();
						reference.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, ref economyParameterData, isStorage);
					}
				}
			}
			if (nativeArray8.Length != 0)
			{
				if (nativeArray9.Length != 0)
				{
					for (int num3 = 0; num3 < nativeArray8.Length; num3++)
					{
						PlaceableObjectData placeableObjectData2 = nativeArray8[num3];
						ObjectGeometryData objectGeometryData3 = nativeArray3[num3];
						ServiceUpgradeData serviceUpgradeData = nativeArray9[num3];
						if (nativeArray4.Length != 0)
						{
							placeableObjectData2.m_Flags |= Game.Objects.PlacementFlags.OwnerSide;
							if (serviceUpgradeData.m_MaxPlacementDistance != 0f)
							{
								placeableObjectData2.m_Flags |= Game.Objects.PlacementFlags.RoadSide;
							}
						}
						if ((placeableObjectData2.m_Flags & Game.Objects.PlacementFlags.NetObject) != Game.Objects.PlacementFlags.None)
						{
							objectGeometryData3.m_Flags |= Game.Objects.GeometryFlags.IgnoreLegCollision;
							if (nativeArray4.Length != 0)
							{
								BuildingData buildingData3 = nativeArray4[num3];
								if ((buildingData3.m_Flags & (BuildingFlags.CanBeOnRoad | BuildingFlags.CanBeOnRoadArea)) == 0)
								{
									buildingData3.m_Flags |= BuildingFlags.CanBeOnRoad;
									nativeArray4[num3] = buildingData3;
								}
							}
							if ((placeableObjectData2.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None)
							{
								placeableObjectData2.m_Flags &= ~(Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.OwnerSide);
							}
						}
						if (nativeArray4.Length != 0 && (placeableObjectData2.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
						{
							BuildingData buildingData4 = nativeArray4[num3];
							buildingData4.m_Flags |= BuildingFlags.CanBeRoadSide;
							nativeArray4[num3] = buildingData4;
						}
						placeableObjectData2.m_ConstructionCost = serviceUpgradeData.m_UpgradeCost;
						nativeArray8[num3] = placeableObjectData2;
						nativeArray3[num3] = objectGeometryData3;
					}
				}
				else
				{
					for (int num4 = 0; num4 < nativeArray8.Length; num4++)
					{
						PlaceableObjectData placeableObjectData3 = nativeArray8[num4];
						ObjectGeometryData objectGeometryData4 = nativeArray3[num4];
						if (nativeArray4.Length != 0)
						{
							placeableObjectData3.m_Flags |= Game.Objects.PlacementFlags.RoadSide;
						}
						if ((placeableObjectData3.m_Flags & Game.Objects.PlacementFlags.NetObject) != Game.Objects.PlacementFlags.None)
						{
							objectGeometryData4.m_Flags |= Game.Objects.GeometryFlags.IgnoreLegCollision;
							if (nativeArray4.Length != 0)
							{
								BuildingData buildingData5 = nativeArray4[num4];
								if ((buildingData5.m_Flags & (BuildingFlags.CanBeOnRoad | BuildingFlags.CanBeOnRoadArea)) == 0)
								{
									buildingData5.m_Flags |= BuildingFlags.CanBeOnRoad;
									nativeArray4[num4] = buildingData5;
								}
							}
							if ((placeableObjectData3.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None)
							{
								placeableObjectData3.m_Flags &= ~Game.Objects.PlacementFlags.RoadSide;
							}
						}
						if (nativeArray4.Length != 0 && (placeableObjectData3.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
						{
							BuildingData buildingData6 = nativeArray4[num4];
							buildingData6.m_Flags |= BuildingFlags.CanBeRoadSide;
							nativeArray4[num4] = buildingData6;
						}
						nativeArray8[num4] = placeableObjectData3;
						nativeArray3[num4] = objectGeometryData4;
					}
				}
			}
			bool flag5 = false;
			if (flag)
			{
				for (int num5 = 0; num5 < nativeArray.Length; num5++)
				{
					if (nativeArray6.Length == 0 || nativeArray6[num5].m_Upkeep <= 0)
					{
						continue;
					}
					bool flag6 = false;
					DynamicBuffer<ServiceUpkeepData> val14 = bufferAccessor2[num5];
					for (int num6 = 0; num6 < val14.Length; num6++)
					{
						if (val14[num6].m_Upkeep.m_Resource == Resource.Money)
						{
							log.WarnFormat("Warning: {0} has monetary upkeep in both ConsumptionData and CityServiceUpkeep", (object)((Object)m_PrefabSystem.GetPrefab<PrefabBase>(nativeArray[num5])).name);
						}
					}
					if (!flag6)
					{
						val14.Add(new ServiceUpkeepData
						{
							m_ScaleWithUsage = false,
							m_Upkeep = new ResourceStack
							{
								m_Amount = nativeArray6[num5].m_Upkeep,
								m_Resource = Resource.Money
							}
						});
						flag5 = true;
					}
				}
			}
			if (bufferAccessor.Length == 0)
			{
				continue;
			}
			for (int num7 = 0; num7 < bufferAccessor.Length; num7++)
			{
				Entity upgrade2 = nativeArray[num7];
				DynamicBuffer<ServiceUpgradeBuilding> val15 = bufferAccessor[num7];
				for (int num8 = 0; num8 < val15.Length; num8++)
				{
					ServiceUpgradeBuilding serviceUpgradeBuilding3 = val15[num8];
					EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
					((EntityManager)(ref entityManager)).GetBuffer<BuildingUpgradeElement>(serviceUpgradeBuilding3.m_Building, false).Add(new BuildingUpgradeElement(upgrade2));
				}
				if (!flag5 && nativeArray6.Length != 0 && nativeArray6[num7].m_Upkeep > 0)
				{
					bufferAccessor2[num7].Add(new ServiceUpkeepData
					{
						m_ScaleWithUsage = false,
						m_Upkeep = new ResourceStack
						{
							m_Amount = nativeArray6[num7].m_Upkeep,
							m_Resource = Resource.Money
						}
					});
				}
			}
		}
		JobHandle val16 = IJobParallelForExtensions.Schedule<FindConnectionRequirementsJob>(new FindConnectionRequirementsJob
		{
			m_SpawnableBuildingDataType = InternalCompilerInterface.GetComponentTypeHandle<SpawnableBuildingData>(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ServiceUpgradeDataType = InternalCompilerInterface.GetComponentTypeHandle<ServiceUpgradeData>(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ExtractorFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle<ExtractorFacilityData>(ref __TypeHandle.__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ConsumptionDataType = InternalCompilerInterface.GetComponentTypeHandle<ConsumptionData>(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_WorkplaceDataType = InternalCompilerInterface.GetComponentTypeHandle<WorkplaceData>(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_WaterPumpingStationDataType = InternalCompilerInterface.GetComponentTypeHandle<WaterPumpingStationData>(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_WaterTowerDataType = InternalCompilerInterface.GetComponentTypeHandle<WaterTowerData>(ref __TypeHandle.__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_SewageOutletDataType = InternalCompilerInterface.GetComponentTypeHandle<SewageOutletData>(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_WastewaterTreatmentPlantDataType = InternalCompilerInterface.GetComponentTypeHandle<WastewaterTreatmentPlantData>(ref __TypeHandle.__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_TransformerDataType = InternalCompilerInterface.GetComponentTypeHandle<TransformerData>(ref __TypeHandle.__Game_Prefabs_TransformerData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ParkingFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle<ParkingFacilityData>(ref __TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PublicTransportStationDataType = InternalCompilerInterface.GetComponentTypeHandle<PublicTransportStationData>(ref __TypeHandle.__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_CargoTransportStationDataType = InternalCompilerInterface.GetComponentTypeHandle<CargoTransportStationData>(ref __TypeHandle.__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ParkDataType = InternalCompilerInterface.GetComponentTypeHandle<ParkData>(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_CoverageDataType = InternalCompilerInterface.GetComponentTypeHandle<CoverageData>(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle<SubNet>(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle<SubObject>(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_SubMeshType = InternalCompilerInterface.GetBufferTypeHandle<SubMesh>(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ServiceUpgradeBuildingType = InternalCompilerInterface.GetBufferTypeHandle<ServiceUpgradeBuilding>(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_BuildingDataType = InternalCompilerInterface.GetComponentTypeHandle<BuildingData>(ref __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PlaceableObjectDataType = InternalCompilerInterface.GetComponentTypeHandle<PlaceableObjectData>(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_EffectType = InternalCompilerInterface.GetBufferTypeHandle<Effect>(ref __TypeHandle.__Game_Prefabs_Effect_RW_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_NetData = InternalCompilerInterface.GetComponentLookup<NetData>(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup<SpawnLocationData>(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_GateData = InternalCompilerInterface.GetComponentLookup<GateData>(ref __TypeHandle.__Game_Prefabs_GateData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_MeshData = InternalCompilerInterface.GetComponentLookup<MeshData>(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_EffectData = InternalCompilerInterface.GetComponentLookup<EffectData>(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_VFXData = InternalCompilerInterface.GetComponentLookup<VFXData>(ref __TypeHandle.__Game_Prefabs_VFXData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_AudioSourceData = InternalCompilerInterface.GetBufferLookup<AudioSourceData>(ref __TypeHandle.__Game_Prefabs_AudioSourceData_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_AudioSpotData = InternalCompilerInterface.GetComponentLookup<AudioSpotData>(ref __TypeHandle.__Game_Prefabs_AudioSpotData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_AudioEffectData = InternalCompilerInterface.GetComponentLookup<AudioEffectData>(ref __TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup<SubObject>(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_Chunks = chunks,
			m_BuildingConfigurationData = ((EntityQuery)(ref m_ConfigurationQuery)).GetSingleton<BuildingConfigurationData>()
		}, chunks.Length, 1, default(JobHandle));
		((JobHandle)(ref val16)).Complete();
		chunks.Dispose();
		((EntityCommandBuffer)(ref val)).Playback(((ComponentSystemBase)this).EntityManager);
		((EntityCommandBuffer)(ref val)).Dispose();
	}

	private void InitializeLotSize(BuildingPrefab buildingPrefab, BuildingTerraformOverride terraformOverride, ref ObjectGeometryData objectGeometryData, ref BuildingTerraformData buildingTerraformData, ref BuildingData buildingData)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		buildingData.m_LotSize = new int2(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth);
		float2 val = default(float2);
		((float2)(ref val))..ctor((float)buildingPrefab.m_LotWidth, (float)buildingPrefab.m_LotDepth);
		val *= 8f;
		bool flag = false;
		Bounds2 xz2 = default(Bounds2);
		if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
		{
			int2 val2 = default(int2);
			val2.x = Mathf.RoundToInt((objectGeometryData.m_LegSize.x + objectGeometryData.m_LegOffset.x * 2f) / 8f);
			val2.y = Mathf.RoundToInt((objectGeometryData.m_LegSize.z + objectGeometryData.m_LegOffset.y * 2f) / 8f);
			flag = math.all(val2 == buildingData.m_LotSize);
			buildingData.m_LotSize = val2;
			float2 xz = ((float3)(ref objectGeometryData.m_Pivot)).xz;
			float2 val3 = ((float3)(ref objectGeometryData.m_LegSize)).xz * 0.5f + objectGeometryData.m_LegOffset;
			((Bounds2)(ref xz2))..ctor(xz - val3, xz + val3);
			((float3)(ref objectGeometryData.m_LegSize)).xz = float2.op_Implicit(val2) * 8f - objectGeometryData.m_LegOffset * 2f - 0.4f;
		}
		else
		{
			xz2 = ((Bounds3)(ref objectGeometryData.m_Bounds)).xz;
		}
		Bounds2 val4 = default(Bounds2);
		val4.max = float2.op_Implicit(buildingData.m_LotSize) * 4f;
		val4.min = -val4.max;
		InitializeTerraformData(terraformOverride, ref buildingTerraformData, val4, xz2);
		objectGeometryData.m_Layers |= MeshLayer.Default;
		objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(val.x, 0f, val.y))));
		switch (buildingPrefab.m_AccessType)
		{
		case BuildingAccessType.LeftCorner:
			buildingData.m_Flags |= BuildingFlags.LeftAccess;
			break;
		case BuildingAccessType.RightCorner:
			buildingData.m_Flags |= BuildingFlags.RightAccess;
			break;
		case BuildingAccessType.LeftAndRightCorner:
			buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.RightAccess;
			break;
		case BuildingAccessType.LeftAndBackCorner:
			buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.RightAndBackCorner:
			buildingData.m_Flags |= BuildingFlags.RightAccess | BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.FrontAndBack:
			buildingData.m_Flags |= BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.All:
			buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.RightAccess | BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.OnRoad:
			buildingData.m_Flags |= BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.OnRoadArea:
			buildingData.m_Flags |= BuildingFlags.BackAccess;
			break;
		}
		if (!flag)
		{
			if (math.any(((float3)(ref objectGeometryData.m_Size)).xz > val + 0.5f) && AssetDatabase.global.AreAssetsWarningsEnabled((AssetData)(object)buildingPrefab.asset))
			{
				log.WarnFormat("Building geometry doesn't fit inside the lot ({0}): {1}m x {2}m ({3}x{4})", (object)((Object)buildingPrefab).name, (object)objectGeometryData.m_Size.x, (object)objectGeometryData.m_Size.z, (object)buildingData.m_LotSize.x, (object)buildingData.m_LotSize.y);
			}
			val -= 0.4f;
			((float3)(ref objectGeometryData.m_Size)).xz = val;
			((float3)(ref objectGeometryData.m_Bounds.min)).xz = val * -0.5f;
			((float3)(ref objectGeometryData.m_Bounds.max)).xz = val * 0.5f;
		}
		objectGeometryData.m_Size.y = math.max(objectGeometryData.m_Size.y, 5f);
		objectGeometryData.m_Bounds.min.y = math.min(objectGeometryData.m_Bounds.min.y, 0f);
		objectGeometryData.m_Bounds.max.y = math.max(objectGeometryData.m_Bounds.max.y, 5f);
	}

	public static void InitializeTerraformData(BuildingTerraformOverride terraformOverride, ref BuildingTerraformData buildingTerraformData, Bounds2 lotBounds, Bounds2 flatBounds)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		float3 val = default(float3);
		((float3)(ref val))..ctor(1f, 0f, 1f);
		float3 val2 = default(float3);
		((float3)(ref val2))..ctor(1f, 0f, 1f);
		float3 val3 = default(float3);
		((float3)(ref val3))..ctor(1f, 0f, 1f);
		float3 val4 = default(float3);
		((float3)(ref val4))..ctor(1f, 0f, 1f);
		((float4)(ref buildingTerraformData.m_Smooth)).xy = lotBounds.min;
		((float4)(ref buildingTerraformData.m_Smooth)).zw = lotBounds.max;
		if ((Object)(object)terraformOverride != (Object)null)
		{
			ref float2 min = ref flatBounds.min;
			min += terraformOverride.m_LevelMinOffset;
			ref float2 max = ref flatBounds.max;
			max += terraformOverride.m_LevelMaxOffset;
			val.x = terraformOverride.m_LevelBackRight.x;
			val.z = terraformOverride.m_LevelFrontRight.x;
			val2.x = terraformOverride.m_LevelBackRight.y;
			val2.z = terraformOverride.m_LevelBackLeft.y;
			val3.x = terraformOverride.m_LevelBackLeft.x;
			val3.z = terraformOverride.m_LevelFrontLeft.x;
			val4.x = terraformOverride.m_LevelFrontRight.y;
			val4.z = terraformOverride.m_LevelFrontLeft.y;
			ref float4 smooth = ref buildingTerraformData.m_Smooth;
			((float4)(ref smooth)).xy = ((float4)(ref smooth)).xy + terraformOverride.m_SmoothMinOffset;
			ref float4 smooth2 = ref buildingTerraformData.m_Smooth;
			((float4)(ref smooth2)).zw = ((float4)(ref smooth2)).zw + terraformOverride.m_SmoothMaxOffset;
			buildingTerraformData.m_HeightOffset = terraformOverride.m_HeightOffset;
			buildingTerraformData.m_DontRaise = terraformOverride.m_DontRaise;
			buildingTerraformData.m_DontLower = terraformOverride.m_DontLower;
		}
		float3 val5 = flatBounds.min.x + val;
		float3 val6 = flatBounds.min.y + val2;
		float3 val7 = flatBounds.max.x - val3;
		float3 val8 = flatBounds.max.y - val4;
		float3 val9 = (val5 + val7) * 0.5f;
		float3 val10 = (val6 + val8) * 0.5f;
		buildingTerraformData.m_FlatX0 = math.min(val5, math.max(val9, val7));
		buildingTerraformData.m_FlatZ0 = math.min(val6, math.max(val10, val8));
		buildingTerraformData.m_FlatX1 = math.max(val7, math.min(val9, val5));
		buildingTerraformData.m_FlatZ1 = math.max(val8, math.min(val10, val6));
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		EntityQueryBuilder val2 = ((EntityQueryBuilder)(ref val)).WithAll<EconomyParameterData>();
		val2 = ((EntityQueryBuilder)(ref val2)).WithOptions((EntityQueryOptions)16);
		__query_547773814_0 = ((EntityQueryBuilder)(ref val2)).Build(ref state);
		((EntityQueryBuilder)(ref val)).Reset();
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public BuildingInitializeSystem()
	{
	}
}
