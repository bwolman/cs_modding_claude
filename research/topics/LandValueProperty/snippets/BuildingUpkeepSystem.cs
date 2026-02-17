using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BuildingUpkeepSystem : GameSystemBase
{
	private struct UpkeepPayment
	{
		public Entity m_RenterEntity;

		public int m_Price;
	}

	private struct LevelUpMaterial
	{
		public Resource m_Resource;

		public int m_Amount;
	}

	[BurstCompile]
	private struct BuildingUpkeepJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<BuildingCondition> m_ConditionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public BufferLookup<LevelUpResourceData> m_LevelUpResourceDataBufs;

		[ReadOnly]
		public BufferLookup<ZoneLevelUpResourceData> m_ZoneLevelUpResourceDataBufs;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifierBufs;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoned;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_Destroyed;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> m_SignatureDatas;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public DynamicBuffer<ZoneLevelUpResourceData> m_BuildingConfigLevelResourceBuf;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_Availabilities;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public EntityArchetype m_GoodsDeliveryRequestArchetype;

		public float m_TemperatureUpkeep;

		public bool m_DebugFastLeveling;

		public NativeQueue<UpkeepPayment>.ParallelWriter m_UpkeepExpenseQueue;

		public NativeQueue<Entity>.ParallelWriter m_LevelDownQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		private void RequestResourceDelivery(int jobIndex, Entity entity, DynamicBuffer<ResourceNeeding> resourceNeedings, Resource resource, int amount)
		{
			resourceNeedings.Add(new ResourceNeeding
			{
				m_Resource = resource,
				m_Amount = amount,
				m_Flags = ResourceNeedingFlags.Requested
			});
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_GoodsDeliveryRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new GoodsDeliveryRequest
			{
				m_ResourceNeeder = entity,
				m_Amount = amount,
				m_Resource = resource
			});
			m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
			m_IconCommandBuffer.Add(entity, m_BuildingConfigurationData.m_LevelingBuildingNotificationPrefab);
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<BuildingCondition> nativeArray2 = chunk.GetNativeArray(ref m_ConditionType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				BuildingCondition value = nativeArray2[i];
				DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
				Entity prefab = nativeArray3[i].m_Prefab;
				ConsumptionData consumptionData = m_ConsumptionDatas[prefab];
				DynamicBuffer<CityModifier> cityEffects = m_CityModifierBufs[m_City];
				SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingDatas[prefab];
				AreaType areaType = m_ZoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType;
				BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
				int levelingCost = BuildingUtils.GetLevelingCost(areaType, buildingPropertyData, spawnableBuildingData.m_Level, cityEffects);
				int abandonCost = BuildingUtils.GetAbandonCost(areaType, buildingPropertyData, spawnableBuildingData.m_Level, levelingCost, cityEffects);
				int num = consumptionData.m_Upkeep / kUpdatesPerDay;
				int num2 = num / kMaterialUpkeep;
				int num3 = num - num2;
				int num4 = 0;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (!m_Resources.TryGetBuffer(dynamicBuffer[j].m_Renter, out var bufferData))
					{
						continue;
					}
					if (m_Households.HasComponent(dynamicBuffer[j].m_Renter))
					{
						num4 += EconomyUtils.GetResources(Resource.Money, bufferData);
						continue;
					}
					Entity renter = dynamicBuffer[j].m_Renter;
					bool isIndustrial = !m_ServiceAvailables.HasComponent(renter);
					IndustrialProcessData componentData = default(IndustrialProcessData);
					if (m_PrefabRefs.TryGetComponent(renter, out var componentData2))
					{
						m_IndustrialProcessDatas.TryGetComponent(componentData2.m_Prefab, out componentData);
					}
					num4 = ((!m_OwnedVehicles.HasBuffer(dynamicBuffer[j].m_Renter)) ? (num4 + EconomyUtils.GetCompanyTotalWorth(isIndustrial, componentData, bufferData, m_ResourcePrefabs, ref m_ResourceDatas)) : (num4 + EconomyUtils.GetCompanyTotalWorth(isIndustrial, componentData, bufferData, m_OwnedVehicles[dynamicBuffer[j].m_Renter], ref m_LayoutElements, ref m_DeliveryTrucks, m_ResourcePrefabs, ref m_ResourceDatas)));
				}
				int num5 = 0;
				if (num3 > num4)
				{
					num5 = -m_BuildingConfigurationData.m_BuildingConditionDecrement * (int)math.pow(2f, (int)spawnableBuildingData.m_Level) * math.max(1, dynamicBuffer.Length);
				}
				else if (dynamicBuffer.Length > 0)
				{
					num5 = BuildingUtils.GetBuildingConditionChange(areaType, m_BuildingConfigurationData) * (int)math.pow(2f, (int)spawnableBuildingData.m_Level) * math.max(1, dynamicBuffer.Length);
					int num6 = num3 / dynamicBuffer.Length;
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						m_UpkeepExpenseQueue.Enqueue(new UpkeepPayment
						{
							m_RenterEntity = dynamicBuffer[k].m_Renter,
							m_Price = -num6
						});
					}
				}
				if (m_DebugFastLeveling)
				{
					value.m_Condition = levelingCost;
				}
				else
				{
					value.m_Condition += num5;
				}
				if (value.m_Condition >= levelingCost)
				{
					DynamicBuffer<ResourceNeeding> resourceNeedings = m_CommandBuffer.AddBuffer<ResourceNeeding>(unfilteredChunkIndex, entity);
					m_CommandBuffer.AddBuffer<GuestVehicle>(unfilteredChunkIndex, entity);
					DynamicBuffer<ZoneLevelUpResourceData> bufferData3;
					if (m_LevelUpResourceDataBufs.TryGetBuffer(prefab, out var bufferData2) && bufferData2.Length > 0)
					{
						for (int l = 0; l < bufferData2.Length; l++)
						{
							RequestResourceDelivery(unfilteredChunkIndex, entity, resourceNeedings, bufferData2[l].m_LevelUpResource.m_Resource, bufferData2[l].m_LevelUpResource.m_Amount);
						}
					}
					else if (m_ZoneLevelUpResourceDataBufs.TryGetBuffer(spawnableBuildingData.m_ZonePrefab, out bufferData3) && bufferData3.Length > 0)
					{
						for (int m = 0; m < bufferData3.Length; m++)
						{
							if (bufferData3[m].m_Level == spawnableBuildingData.m_Level)
							{
								RequestResourceDelivery(unfilteredChunkIndex, entity, resourceNeedings, bufferData3[m].m_LevelUpResource.m_Resource, bufferData3[m].m_LevelUpResource.m_Amount);
							}
						}
					}
					else
					{
						for (int n = 0; n < m_BuildingConfigLevelResourceBuf.Length; n++)
						{
							if (m_BuildingConfigLevelResourceBuf[n].m_Level == spawnableBuildingData.m_Level)
							{
								RequestResourceDelivery(unfilteredChunkIndex, entity, resourceNeedings, m_BuildingConfigLevelResourceBuf[n].m_LevelUpResource.m_Resource, m_BuildingConfigLevelResourceBuf[n].m_LevelUpResource.m_Amount);
							}
						}
					}
				}
				else if (!m_Abandoned.HasComponent(nativeArray[i]) && !m_Destroyed.HasComponent(nativeArray[i]) && nativeArray2[i].m_Condition <= -abandonCost && !m_SignatureDatas.HasComponent(prefab))
				{
					m_LevelDownQueue.Enqueue(nativeArray[i]);
					value.m_Condition += levelingCost;
				}
				nativeArray2[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ResourceNeedingUpkeepJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<BuildingCondition> m_ConditionType;

		public BufferTypeHandle<ResourceNeeding> m_ResourceNeedingType;

		[ReadOnly]
		public BufferLookup<GuestVehicle> m_GuestVehicleBufs;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public NativeQueue<Entity>.ParallelWriter m_LevelupQueue;

		public NativeQueue<LevelUpMaterial>.ParallelWriter m_LeveUpMaterialQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<ResourceNeeding> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceNeedingType);
			NativeArray<BuildingCondition> nativeArray2 = chunk.GetNativeArray(ref m_ConditionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				if (!m_GuestVehicleBufs.HasBuffer(entity))
				{
					continue;
				}
				DynamicBuffer<ResourceNeeding> dynamicBuffer = bufferAccessor[i];
				bool flag = true;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (dynamicBuffer[j].m_Flags != ResourceNeedingFlags.Delivered)
					{
						flag = false;
					}
				}
				if (flag)
				{
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						m_LeveUpMaterialQueue.Enqueue(new LevelUpMaterial
						{
							m_Resource = dynamicBuffer[k].m_Resource,
							m_Amount = dynamicBuffer[k].m_Amount
						});
					}
					m_CommandBuffer.RemoveComponent<ResourceNeeding>(unfilteredChunkIndex, entity);
					m_IconCommandBuffer.Remove(entity, m_BuildingConfigurationData.m_LevelingBuildingNotificationPrefab);
					m_LevelupQueue.Enqueue(entity);
					BuildingCondition value = nativeArray2[i];
					value.m_Condition = 0;
					nativeArray2[i] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpkeepPaymentJob : IJob
	{
		[ReadOnly]
		public uint m_FrameIndex;

		public BufferLookup<Resources> m_Resources;

		public ComponentLookup<Household> m_Households;

		public NativeQueue<UpkeepPayment> m_UpkeepExpenseQueue;

		public NativeQueue<LevelUpMaterial> m_LevelUpMaterialQueue;

		public NativeArray<int> m_UpkeepMaterialAccumulator;

		public void Execute()
		{
			UpkeepPayment item;
			while (m_UpkeepExpenseQueue.TryDequeue(out item))
			{
				if (m_Resources.HasBuffer(item.m_RenterEntity))
				{
					EconomyUtils.AddResources(Resource.Money, item.m_Price, m_Resources[item.m_RenterEntity]);
					if (m_Households.HasComponent(item.m_RenterEntity))
					{
						Household value = m_Households[item.m_RenterEntity];
						value.m_MoneySpendOnBuildingLevelingLastDay += item.m_Price;
						m_Households[item.m_RenterEntity] = value;
					}
				}
			}
			LevelUpMaterial item2;
			while (m_LevelUpMaterialQueue.TryDequeue(out item2))
			{
				m_UpkeepMaterialAccumulator[EconomyUtils.GetResourceIndex(item2.m_Resource)] += item2.m_Amount;
			}
		}
	}

	[BurstCompile]
	private struct LeveldownJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducers;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducers;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> m_OfficeBuilding;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		public ComponentLookup<CrimeProducer> m_CrimeProducers;

		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public NativeQueue<Entity> m_LeveldownQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<Entity> m_UpdatedElectricityRoadEdges;

		public NativeQueue<Entity> m_UpdatedWaterPipeRoadEdges;

		public IconCommandBuffer m_IconCommandBuffer;

		public uint m_SimulationFrame;

		public void Execute()
		{
			Entity item;
			while (m_LeveldownQueue.TryDequeue(out item))
			{
				if (!m_Prefabs.HasComponent(item))
				{
					continue;
				}
				Entity prefab = m_Prefabs[item].m_Prefab;
				if (!m_SpawnableBuildings.HasComponent(prefab))
				{
					continue;
				}
				_ = m_SpawnableBuildings[prefab];
				_ = m_BuildingDatas[prefab];
				BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
				m_CommandBuffer.AddComponent(item, new Abandoned
				{
					m_AbandonmentTime = m_SimulationFrame
				});
				m_CommandBuffer.AddComponent(item, default(Updated));
				if (m_ElectricityConsumers.HasComponent(item))
				{
					m_CommandBuffer.RemoveComponent<ElectricityConsumer>(item);
					Entity roadEdge = m_Buildings[item].m_RoadEdge;
					if (roadEdge != Entity.Null)
					{
						m_UpdatedElectricityRoadEdges.Enqueue(roadEdge);
					}
				}
				if (m_WaterConsumers.HasComponent(item))
				{
					m_CommandBuffer.RemoveComponent<WaterConsumer>(item);
					Entity roadEdge2 = m_Buildings[item].m_RoadEdge;
					if (roadEdge2 != Entity.Null)
					{
						m_UpdatedWaterPipeRoadEdges.Enqueue(roadEdge2);
					}
				}
				if (m_GarbageProducers.HasComponent(item))
				{
					m_CommandBuffer.RemoveComponent<GarbageProducer>(item);
				}
				if (m_MailProducers.HasComponent(item))
				{
					m_CommandBuffer.RemoveComponent<MailProducer>(item);
				}
				if (m_CrimeProducers.HasComponent(item))
				{
					CrimeProducer crimeProducer = m_CrimeProducers[item];
					m_CommandBuffer.SetComponent(item, new CrimeProducer
					{
						m_Crime = crimeProducer.m_Crime * 2f,
						m_PatrolRequest = crimeProducer.m_PatrolRequest
					});
				}
				if (m_Renters.HasBuffer(item))
				{
					DynamicBuffer<Renter> dynamicBuffer = m_Renters[item];
					for (int num = dynamicBuffer.Length - 1; num >= 0; num--)
					{
						m_CommandBuffer.RemoveComponent<PropertyRenter>(dynamicBuffer[num].m_Renter);
						dynamicBuffer.RemoveAt(num);
					}
				}
				if ((m_Buildings[item].m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
				{
					Building value = m_Buildings[item];
					m_IconCommandBuffer.Remove(item, m_BuildingConfigurationData.m_HighRentNotification);
					value.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
					m_Buildings[item] = value;
				}
				m_IconCommandBuffer.Remove(item, IconPriority.Problem);
				m_IconCommandBuffer.Remove(item, IconPriority.FatalProblem);
				m_IconCommandBuffer.Add(item, m_BuildingConfigurationData.m_AbandonedNotification, IconPriority.FatalProblem);
				if (buildingPropertyData.CountProperties(AreaType.Commercial) > 0)
				{
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownCommercialBuilding, Entity.Null, item, item));
				}
				if (buildingPropertyData.CountProperties(AreaType.Industrial) > 0)
				{
					if (m_OfficeBuilding.HasComponent(prefab))
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownOfficeBuilding, Entity.Null, item, item));
					}
					else
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownIndustrialBuilding, Entity.Null, item, item));
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct LevelupJob : IJob
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public int2 m_LotSize;

			public float2 m_StartPosition;

			public float2 m_Right;

			public float2 m_Forward;

			public int m_MaxHeight;

			public ComponentLookup<Block> m_BlockData;

			public ComponentLookup<ValidArea> m_ValidAreaData;

			public BufferLookup<Cell> m_Cells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds))
				{
					return;
				}
				ValidArea validArea = m_ValidAreaData[blockEntity];
				if (validArea.m_Area.y <= validArea.m_Area.x)
				{
					return;
				}
				Block block = m_BlockData[blockEntity];
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[blockEntity];
				float2 @float = m_StartPosition;
				int2 @int = default(int2);
				@int.y = 0;
				while (@int.y < m_LotSize.y)
				{
					float2 position = @float;
					@int.x = 0;
					while (@int.x < m_LotSize.x)
					{
						int2 cellIndex = ZoneUtils.GetCellIndex(block, position);
						if (math.all((cellIndex >= validArea.m_Area.xz) & (cellIndex < validArea.m_Area.yw)))
						{
							int index = cellIndex.y * block.m_Size.x + cellIndex.x;
							Cell cell = dynamicBuffer[index];
							if ((cell.m_State & CellFlags.Visible) != CellFlags.None)
							{
								m_MaxHeight = math.min(m_MaxHeight, cell.m_Height);
							}
						}
						position -= m_Right;
						@int.x++;
					}
					@float -= m_Forward;
					@int.y++;
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

		[ReadOnly]
		public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_Buildings;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> m_OfficeBuilding;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_SpawnableBuildingChunks;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public IconCommandBuffer m_IconCommandBuffer;

		public NativeQueue<Entity> m_LevelupQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		public NativeQueue<ZoneBuiltLevelUpdate> m_ZoneBuiltLevelQueue;

		public void Execute()
		{
			Random random = m_RandomSeed.GetRandom(0);
			Entity item;
			while (m_LevelupQueue.TryDequeue(out item))
			{
				Entity prefab = m_Prefabs[item].m_Prefab;
				if (!m_SpawnableBuildings.HasComponent(prefab))
				{
					continue;
				}
				SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildings[prefab];
				if (!m_PrefabDatas.IsComponentEnabled(spawnableBuildingData.m_ZonePrefab))
				{
					continue;
				}
				BuildingData prefabBuildingData = m_Buildings[prefab];
				BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
				ZoneData zoneData = m_ZoneData[spawnableBuildingData.m_ZonePrefab];
				float maxHeight = GetMaxHeight(item, prefabBuildingData);
				Entity entity = SelectSpawnableBuilding(zoneData.m_ZoneType, spawnableBuildingData.m_Level + 1, prefabBuildingData.m_LotSize, maxHeight, prefabBuildingData.m_Flags & (Game.Prefabs.BuildingFlags.LeftAccess | Game.Prefabs.BuildingFlags.RightAccess), buildingPropertyData, ref random);
				if (!(entity != Entity.Null))
				{
					continue;
				}
				m_CommandBuffer.AddComponent(item, new UnderConstruction
				{
					m_NewPrefab = entity,
					m_Progress = byte.MaxValue
				});
				if (buildingPropertyData.CountProperties(AreaType.Residential) > 0)
				{
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpResidentialBuilding, Entity.Null, item, item));
				}
				if (buildingPropertyData.CountProperties(AreaType.Commercial) > 0)
				{
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpCommercialBuilding, Entity.Null, item, item));
				}
				if (buildingPropertyData.CountProperties(AreaType.Industrial) > 0)
				{
					if (m_OfficeBuilding.HasComponent(prefab))
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpOfficeBuilding, Entity.Null, item, item));
					}
					else
					{
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpIndustrialBuilding, Entity.Null, item, item));
					}
				}
				m_ZoneBuiltLevelQueue.Enqueue(new ZoneBuiltLevelUpdate
				{
					m_Zone = spawnableBuildingData.m_ZonePrefab,
					m_FromLevel = spawnableBuildingData.m_Level,
					m_ToLevel = spawnableBuildingData.m_Level + 1,
					m_Squares = prefabBuildingData.m_LotSize.x * prefabBuildingData.m_LotSize.y
				});
				m_IconCommandBuffer.Add(item, m_BuildingConfigurationData.m_LevelUpNotification, IconPriority.Info, IconClusterLayer.Transaction);
			}
		}

		private Entity SelectSpawnableBuilding(ZoneType zoneType, int level, int2 lotSize, float maxHeight, Game.Prefabs.BuildingFlags accessFlags, BuildingPropertyData buildingPropertyData, ref Random random)
		{
			int num = 0;
			Entity result = Entity.Null;
			for (int i = 0; i < m_SpawnableBuildingChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_SpawnableBuildingChunks[i];
				if (!archetypeChunk.GetSharedComponent(m_BuildingSpawnGroupType).m_ZoneType.Equals(zoneType))
				{
					continue;
				}
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<SpawnableBuildingData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_SpawnableBuildingType);
				NativeArray<BuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_BuildingType);
				NativeArray<BuildingPropertyData> nativeArray4 = archetypeChunk.GetNativeArray(ref m_BuildingPropertyType);
				NativeArray<ObjectGeometryData> nativeArray5 = archetypeChunk.GetNativeArray(ref m_ObjectGeometryType);
				for (int j = 0; j < archetypeChunk.Count; j++)
				{
					SpawnableBuildingData spawnableBuildingData = nativeArray2[j];
					BuildingData buildingData = nativeArray3[j];
					BuildingPropertyData buildingPropertyData2 = nativeArray4[j];
					ObjectGeometryData objectGeometryData = nativeArray5[j];
					if (level == spawnableBuildingData.m_Level && lotSize.Equals(buildingData.m_LotSize) && objectGeometryData.m_Size.y <= maxHeight && (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.LeftAccess | Game.Prefabs.BuildingFlags.RightAccess)) == accessFlags && buildingPropertyData.m_ResidentialProperties <= buildingPropertyData2.m_ResidentialProperties && buildingPropertyData.m_AllowedManufactured == buildingPropertyData2.m_AllowedManufactured && buildingPropertyData.m_AllowedInput == buildingPropertyData2.m_AllowedInput && buildingPropertyData.m_AllowedSold == buildingPropertyData2.m_AllowedSold && buildingPropertyData.m_AllowedStored == buildingPropertyData2.m_AllowedStored)
					{
						int num2 = 100;
						num += num2;
						if (random.NextInt(num) < num2)
						{
							result = nativeArray[j];
						}
					}
				}
			}
			return result;
		}

		private float GetMaxHeight(Entity building, BuildingData prefabBuildingData)
		{
			Transform transform = m_TransformData[building];
			float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
			float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
			float2 @float = xz * ((float)prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
			float2 float2 = xz2 * ((float)prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
			float2 float3 = math.abs(float2) + math.abs(@float);
			Iterator iterator = new Iterator
			{
				m_Bounds = new Bounds2(transform.m_Position.xz - float3, transform.m_Position.xz + float3),
				m_LotSize = prefabBuildingData.m_LotSize,
				m_StartPosition = transform.m_Position.xz + float2 + @float,
				m_Right = xz,
				m_Forward = xz2,
				m_MaxHeight = int.MaxValue,
				m_BlockData = m_BlockData,
				m_ValidAreaData = m_ValidAreaData,
				m_Cells = m_Cells
			};
			m_ZoneSearchTree.Iterate(ref iterator);
			return (float)iterator.m_MaxHeight - transform.m_Position.y;
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LevelUpResourceData> __Game_Prefabs_LevelUpResourceData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ZoneLevelUpResourceData> __Game_Prefabs_ZoneLevelUpResourceData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		public BufferTypeHandle<ResourceNeeding> __Game_Buildings_ResourceNeeding_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;

		public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Prefabs_LevelUpResourceData_RO_BufferLookup = state.GetBufferLookup<LevelUpResourceData>(isReadOnly: true);
			__Game_Prefabs_ZoneLevelUpResourceData_RO_BufferLookup = state.GetBufferLookup<ZoneLevelUpResourceData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Buildings_ResourceNeeding_RW_BufferTypeHandle = state.GetBufferTypeHandle<ResourceNeeding>();
			__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<BuildingSpawnGroupData>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_OfficeBuilding_RO_ComponentLookup = state.GetComponentLookup<OfficeBuilding>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
			__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RW_ComponentLookup = state.GetComponentLookup<CrimeProducer>();
			__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
			__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	public static readonly int kMaterialUpkeep = 4;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private ClimateSystem m_ClimateSystem;

	private CitySystem m_CitySystem;

	private IconCommandSystem m_IconCommandSystem;

	private TriggerSystem m_TriggerSystem;

	private ZoneBuiltRequirementSystem m_ZoneBuiltRequirementSystemSystem;

	private Game.Zones.SearchSystem m_ZoneSearchSystem;

	private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;

	private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private NativeQueue<UpkeepPayment> m_UpkeepExpenseQueue;

	private NativeQueue<LevelUpMaterial> m_LevelUpMaterialQueue;

	private NativeQueue<Entity> m_LevelupQueue;

	private NativeQueue<Entity> m_LeveldownQueue;

	private EntityQuery m_BuildingPrefabGroup;

	private EntityQuery m_BuildingSettingsQuery;

	private EntityQuery m_BuildingGroup;

	private EntityQuery m_ResourceNeedingBuildingGroup;

	private EntityArchetype m_GoodsDeliveryRequestArchetype;

	public bool debugFastLeveling;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	public static float GetHeatingMultiplier(float temperature)
	{
		return math.max(0f, 15f - temperature);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ZoneBuiltRequirementSystemSystem = base.World.GetOrCreateSystemManaged<ZoneBuiltRequirementSystem>();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_ElectricityRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<ElectricityRoadConnectionGraphSystem>();
		m_WaterPipeRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_UpkeepExpenseQueue = new NativeQueue<UpkeepPayment>(Allocator.Persistent);
		m_LevelUpMaterialQueue = new NativeQueue<LevelUpMaterial>(Allocator.Persistent);
		m_BuildingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>(), ComponentType.ReadOnly<ZoneLevelUpResourceData>());
		m_LevelupQueue = new NativeQueue<Entity>(Allocator.Persistent);
		m_LeveldownQueue = new NativeQueue<Entity>(Allocator.Persistent);
		m_BuildingGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<BuildingCondition>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadWrite<ResourceNeeding>()
			}
		});
		m_ResourceNeedingBuildingGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadWrite<ResourceNeeding>(),
				ComponentType.ReadOnly<BuildingCondition>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_BuildingPrefabGroup = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<BuildingSpawnGroupData>(), ComponentType.ReadOnly<PrefabData>());
		m_GoodsDeliveryRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<GoodsDeliveryRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_BuildingGroup);
		RequireForUpdate(m_BuildingSettingsQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_UpkeepExpenseQueue.Dispose();
		m_LevelUpMaterialQueue.Dispose();
		m_LevelupQueue.Dispose();
		m_LeveldownQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		m_BuildingGroup.SetSharedComponentFilter(new UpdateFrame(updateFrame));
		BuildingConfigurationData singleton = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>();
		BuildingUpkeepJob jobData = new BuildingUpkeepJob
		{
			m_ConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Availabilities = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
			m_LevelUpResourceDataBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LevelUpResourceData_RO_BufferLookup, ref base.CheckedStateRef),
			m_ZoneLevelUpResourceDataBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ZoneLevelUpResourceData_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifierBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_SignatureDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Abandoned = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Destroyed = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingConfigurationData = singleton,
			m_BuildingConfigLevelResourceBuf = m_BuildingSettingsQuery.GetSingletonBuffer<ZoneLevelUpResourceData>(isReadOnly: true),
			m_TemperatureUpkeep = GetHeatingMultiplier(m_ClimateSystem.temperature),
			m_DebugFastLeveling = debugFastLeveling,
			m_GoodsDeliveryRequestArchetype = m_GoodsDeliveryRequestArchetype,
			m_UpkeepExpenseQueue = m_UpkeepExpenseQueue.AsParallelWriter(),
			m_LevelDownQueue = m_LeveldownQueue.AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		ResourceNeedingUpkeepJob jobData2 = new ResourceNeedingUpkeepJob
		{
			m_ConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ResourceNeedingType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_ResourceNeeding_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_GuestVehicleBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingConfigurationData = singleton,
			m_LeveUpMaterialQueue = m_LevelUpMaterialQueue.AsParallelWriter(),
			m_LevelupQueue = m_LevelupQueue.AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_ResourceNeedingBuildingGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		JobHandle outJobHandle;
		JobHandle dependencies;
		JobHandle deps;
		LevelupJob jobData3 = new LevelupJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingSpawnGroupType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ValidAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OfficeBuilding = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingConfigurationData = singleton,
			m_SpawnableBuildingChunks = m_BuildingPrefabGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_ZoneSearchTree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_RandomSeed = RandomSeed.Next(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_LevelupQueue = m_LevelupQueue,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer(),
			m_ZoneBuiltLevelQueue = m_ZoneBuiltRequirementSystemSystem.GetZoneBuiltLevelQueue(out deps)
		};
		base.Dependency = IJobExtensions.Schedule(jobData3, JobUtils.CombineDependencies(base.Dependency, outJobHandle, dependencies, deps));
		m_ZoneSearchSystem.AddSearchTreeReader(base.Dependency);
		m_ZoneBuiltRequirementSystemSystem.AddWriter(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		JobHandle deps2;
		JobHandle deps3;
		LeveldownJob jobData4 = new LeveldownJob
		{
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OfficeBuilding = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer(),
			m_CrimeProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref base.CheckedStateRef),
			m_BuildingConfigurationData = singleton,
			m_LeveldownQueue = m_LeveldownQueue,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
			m_UpdatedElectricityRoadEdges = m_ElectricityRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps2),
			m_UpdatedWaterPipeRoadEdges = m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps3),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_SimulationFrame = m_SimulationSystem.frameIndex
		};
		base.Dependency = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(base.Dependency, deps2, deps3));
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_ElectricityRoadConnectionGraphSystem.AddQueueWriter(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		JobHandle deps4;
		UpkeepPaymentJob jobData5 = new UpkeepPaymentJob
		{
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpkeepExpenseQueue = m_UpkeepExpenseQueue,
			m_LevelUpMaterialQueue = m_LevelUpMaterialQueue,
			m_UpkeepMaterialAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.LevelUp, out deps4)
		};
		base.Dependency = IJobExtensions.Schedule(jobData5, JobHandle.CombineDependencies(base.Dependency, deps4));
	}

	public void DebugLevelUp(Entity building, ComponentLookup<BuildingCondition> conditions, ComponentLookup<SpawnableBuildingData> spawnables, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<ZoneData> zoneDatas, ComponentLookup<BuildingPropertyData> propertyDatas)
	{
		if (conditions.HasComponent(building) && prefabRefs.HasComponent(building))
		{
			Entity prefab = prefabRefs[building].m_Prefab;
			if (spawnables.HasComponent(prefab) && propertyDatas.HasComponent(prefab) && zoneDatas.HasComponent(spawnables[prefab].m_ZonePrefab))
			{
				m_LevelupQueue.Enqueue(building);
			}
		}
	}

	public void DebugLevelDown(Entity building, ComponentLookup<BuildingCondition> conditions, ComponentLookup<SpawnableBuildingData> spawnables, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<ZoneData> zoneDatas, ComponentLookup<BuildingPropertyData> propertyDatas)
	{
		if (!conditions.HasComponent(building) || !prefabRefs.HasComponent(building))
		{
			return;
		}
		BuildingCondition value = conditions[building];
		Entity prefab = prefabRefs[building].m_Prefab;
		if (spawnables.HasComponent(prefab) && propertyDatas.HasComponent(prefab))
		{
			SpawnableBuildingData spawnableBuildingData = spawnables[prefab];
			if (zoneDatas.HasComponent(spawnableBuildingData.m_ZonePrefab))
			{
				AreaType areaType = zoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType;
				int num = BuildingUtils.GetAbandonCost(levelingCost: BuildingUtils.GetLevelingCost(areaType, propertyDatas[prefab], spawnableBuildingData.m_Level, base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true)), areaType: areaType, buildingPropertyData: propertyDatas[prefab], currentLevel: spawnableBuildingData.m_Level, cityEffects: base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true));
				value.m_Condition = -3 * num / 2;
				conditions[building] = value;
				m_LeveldownQueue.Enqueue(building);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public BuildingUpkeepSystem()
	{
	}
}
