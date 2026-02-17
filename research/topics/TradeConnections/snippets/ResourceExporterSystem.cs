#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Assertions;
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
public class ResourceExporterSystem : GameSystemBase
{
	[BurstCompile]
	private struct ExportJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ResourceExporter> m_ResourceExporterType;

		public BufferTypeHandle<TripNeeded> m_TripType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformation;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		public NativeQueue<ExportEvent>.ParallelWriter m_ExportQueue;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<ResourceExporter> nativeArray = chunk.GetNativeArray(ref m_ResourceExporterType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray2[i];
				ResourceExporter resourceExporter = nativeArray[i];
				DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[i];
				bool flag = false;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (dynamicBuffer[j].m_Purpose == Purpose.Exporting)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					m_CommandBuffer.RemoveComponent<ResourceExporter>(unfilteredChunkIndex, entity);
					continue;
				}
				Entity entity2 = m_ResourcePrefabs[resourceExporter.m_Resource];
				if (m_ResourceDatas.HasComponent(entity2) && EconomyUtils.GetWeight(resourceExporter.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas) == 0f)
				{
					m_CommandBuffer.RemoveComponent<ResourceExporter>(unfilteredChunkIndex, entity);
					m_ExportQueue.Enqueue(new ExportEvent
					{
						m_Seller = entity,
						m_Buyer = Entity.Null,
						m_Distance = 0f,
						m_Amount = resourceExporter.m_Amount,
						m_Resource = resourceExporter.m_Resource
					});
				}
				else if (m_PathInformation.HasComponent(entity))
				{
					PathInformation pathInformation = m_PathInformation[entity];
					if ((pathInformation.m_State & PathFlags.Pending) != 0)
					{
						continue;
					}
					Entity destination = pathInformation.m_Destination;
					if (m_StorageCompanies.HasComponent(destination))
					{
						int num = resourceExporter.m_Amount;
						if (m_DeliveryTruckSelectData.TrySelectItem(ref random, resourceExporter.m_Resource, resourceExporter.m_Amount, out var item))
						{
							num = math.min(resourceExporter.m_Amount, item.m_Capacity);
						}
						m_ExportQueue.Enqueue(new ExportEvent
						{
							m_Seller = entity,
							m_Buyer = destination,
							m_Distance = pathInformation.m_Distance,
							m_Amount = num,
							m_Resource = resourceExporter.m_Resource
						});
						m_CommandBuffer.RemoveComponent<ResourceExporter>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity);
						m_CommandBuffer.AddBuffer<CurrentTrading>(unfilteredChunkIndex, entity).Add(new CurrentTrading
						{
							m_TradingResource = resourceExporter.m_Resource,
							m_TradingResourceAmount = -resourceExporter.m_Amount,
							m_OutsideConnectionType = (m_OutsideConnections.HasComponent(destination) ? BuildingUtils.GetOutsideConnectionType(destination, ref m_PrefabRefs, ref m_OutsideConnectionDatas) : OutsideConnectionTransferType.None),
							m_TradingStartFrameIndex = m_FrameIndex
						});
						dynamicBuffer.Add(new TripNeeded
						{
							m_TargetAgent = destination,
							m_Purpose = Purpose.Exporting,
							m_Resource = resourceExporter.m_Resource,
							m_Data = num
						});
					}
					else
					{
						m_CommandBuffer.RemoveComponent<ResourceExporter>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity);
					}
				}
				else
				{
					FindTarget(unfilteredChunkIndex, entity, resourceExporter.m_Resource, resourceExporter.m_Amount);
				}
			}
		}

		private void FindTarget(int chunkIndex, Entity exporter, Resource resource, int amount)
		{
			m_CommandBuffer.AddComponent(chunkIndex, exporter, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			m_CommandBuffer.AddBuffer<PathElement>(chunkIndex, exporter);
			float transportCost = EconomyUtils.GetTransportCost(1f, amount, m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(0.01f, 0.01f, transportCost, 0.01f),
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_IgnoredRules = (RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.ResourceExport,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car,
				m_Resource = resource,
				m_Value = amount
			};
			SetupQueueItem value = new SetupQueueItem(exporter, parameters, origin, destination);
			m_PathfindQueue.Enqueue(value);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct ExportEvent
	{
		public Resource m_Resource;

		public Entity m_Seller;

		public int m_Amount;

		public Entity m_Buyer;

		public float m_Distance;
	}

	[BurstCompile]
	private struct HandleExportsJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<Entity> m_OutsideConnectionEntities;

		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

		public BufferLookup<TradeCost> m_TradeCosts;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public NativeQueue<ExportEvent> m_ExportQueue;

		public Unity.Mathematics.Random m_Random;

		public void Execute()
		{
			ExportEvent item;
			while (m_ExportQueue.TryDequeue(out item))
			{
				int resources = EconomyUtils.GetResources(item.m_Resource, m_Resources[item.m_Seller]);
				if (item.m_Amount <= 0 || resources <= 0)
				{
					continue;
				}
				float industrialPrice = EconomyUtils.GetIndustrialPrice(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
				int y = (int)(2.1474836E+09f / industrialPrice) - 1000;
				item.m_Amount = math.min(math.min(item.m_Amount, resources), y);
				int num = Mathf.RoundToInt(industrialPrice * (float)item.m_Amount);
				float weight = EconomyUtils.GetWeight(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
				bool flag = weight == 0f;
				if (!flag && m_Storages.HasComponent(item.m_Buyer))
				{
					float num2 = (float)EconomyUtils.GetTransportCost(item.m_Distance, item.m_Resource, item.m_Amount, weight) / (float)item.m_Amount;
					if (m_TradeCosts.HasBuffer(item.m_Buyer) && m_TradeCosts.HasBuffer(item.m_Seller))
					{
						DynamicBuffer<TradeCost> costs = m_TradeCosts[item.m_Buyer];
						TradeCost tradeCost = EconomyUtils.GetTradeCost(item.m_Resource, costs);
						Assert.IsTrue(item.m_Amount != 0 && !float.IsNaN(tradeCost.m_BuyCost), $"NaN error of Entity:{item.m_Buyer.Index}");
						tradeCost.m_BuyCost = math.lerp(tradeCost.m_BuyCost, num2, 0.5f);
						Assert.IsTrue(!float.IsNaN(tradeCost.m_BuyCost), $"NaN error of Entity:{item.m_Buyer.Index}");
						EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, costs, keepLastTime: true);
						DynamicBuffer<TradeCost> costs2 = m_TradeCosts[item.m_Seller];
						TradeCost tradeCost2 = EconomyUtils.GetTradeCost(item.m_Resource, costs2);
						tradeCost2.m_SellCost = math.lerp(tradeCost2.m_SellCost, num2, 0.5f);
						EconomyUtils.SetTradeCost(item.m_Resource, tradeCost2, costs2, keepLastTime: true);
					}
					num -= Mathf.RoundToInt(num2);
				}
				else if (flag)
				{
					Entity entity = m_OutsideConnectionEntities[m_Random.NextInt(0, m_OutsideConnectionEntities.Length)];
					if (m_Storages.HasComponent(entity) && m_TradeCosts.HasBuffer(item.m_Seller))
					{
						DynamicBuffer<TradeCost> costs3 = m_TradeCosts[entity];
						TradeCost tradeCost3 = EconomyUtils.GetTradeCost(item.m_Resource, costs3);
						tradeCost3.m_BuyCost = math.lerp(tradeCost3.m_BuyCost, 0f, 0.75f);
						EconomyUtils.SetTradeCost(item.m_Resource, tradeCost3, costs3, keepLastTime: true);
						DynamicBuffer<TradeCost> costs4 = m_TradeCosts[item.m_Seller];
						tradeCost3.m_SellCost = math.lerp(tradeCost3.m_SellCost, 0f, 0.75f);
						EconomyUtils.SetTradeCost(item.m_Resource, tradeCost3, costs4, keepLastTime: true);
						num += (int)((float)item.m_Amount * tradeCost3.m_BuyCost);
						EconomyUtils.AddResources(Resource.Money, num, m_Resources[item.m_Seller]);
						EconomyUtils.AddResources(item.m_Resource, item.m_Amount, m_Resources[entity]);
					}
				}
				EconomyUtils.AddResources(item.m_Resource, -item.m_Amount, m_Resources[item.m_Seller]);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResourceExporter> __Game_Companies_ResourceExporter_RO_ComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Companies_ResourceExporter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceExporter>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Companies_TradeCost_RW_BufferLookup = state.GetBufferLookup<TradeCost>();
		}
	}

	private EntityQuery m_ExporterQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_EconomyParameterQuery;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private TaxSystem m_TaxSystem;

	private SimulationSystem m_SimulationSystem;

	private NativeQueue<ExportEvent> m_ExportQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ExporterQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[5]
			{
				ComponentType.ReadOnly<ResourceExporter>(),
				ComponentType.ReadOnly<TaxPayer>(),
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<Game.Economy.Resources>(),
				ComponentType.ReadWrite<TripNeeded>()
			},
			None = new ComponentType[3]
			{
				ComponentType.Exclude<ResourceBuyer>(),
				ComponentType.Exclude<Deleted>(),
				ComponentType.Exclude<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[5]
			{
				ComponentType.ReadOnly<ResourceExporter>(),
				ComponentType.ReadOnly<Game.Economy.Resources>(),
				ComponentType.ReadWrite<TripNeeded>(),
				ComponentType.ReadOnly<Game.Buildings.ResourceProducer>(),
				ComponentType.ReadOnly<CityServiceUpkeep>()
			},
			None = new ComponentType[2]
			{
				ComponentType.Exclude<Deleted>(),
				ComponentType.Exclude<Temp>()
			}
		});
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadWrite<TradeCost>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_ExportQueue = new NativeQueue<ExportEvent>(Allocator.Persistent);
		RequireForUpdate(m_ExporterQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ExportQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ExportJob jobData = new ExportJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ResourceExporterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ResourceExporter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathInformation = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExportQueue = m_ExportQueue.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_FrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ExporterQuery, base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
		NativeArray<Entity> outsideConnectionEntities = m_OutsideConnectionQuery.ToEntityArray(Allocator.TempJob);
		HandleExportsJob jobData2 = new HandleExportsJob
		{
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Storages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TradeCosts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup, ref base.CheckedStateRef),
			m_OutsideConnectionEntities = outsideConnectionEntities,
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ExportQueue = m_ExportQueue,
			m_Random = RandomSeed.Next().GetRandom(UnityEngine.Random.Range(0, int.MaxValue))
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		m_TaxSystem.AddReader(base.Dependency);
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
	public ResourceExporterSystem()
	{
	}
}
