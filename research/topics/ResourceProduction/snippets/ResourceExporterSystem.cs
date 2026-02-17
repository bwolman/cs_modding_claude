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

		public ParallelWriter<ExportEvent> m_ExportQueue;

		public ParallelWriter<SetupQueueItem> m_PathfindQueue;

		public ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_032c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0165: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_017e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0183: Unknown result type (might be due to invalid IL or missing references)
			//IL_018b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0305: Unknown result type (might be due to invalid IL or missing references)
			//IL_0313: Unknown result type (might be due to invalid IL or missing references)
			//IL_0321: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Unknown result type (might be due to invalid IL or missing references)
			//IL_0235: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_0251: Unknown result type (might be due to invalid IL or missing references)
			//IL_0253: Unknown result type (might be due to invalid IL or missing references)
			//IL_0258: Unknown result type (might be due to invalid IL or missing references)
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_0295: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<ResourceExporter> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray<ResourceExporter>(ref m_ResourceExporterType);
			NativeArray<Entity> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			BufferAccessor<TripNeeded> bufferAccessor = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<TripNeeded>(ref m_TripType);
			for (int i = 0; i < ((ArchetypeChunk)(ref chunk)).Count; i++)
			{
				Entity val = nativeArray2[i];
				ResourceExporter resourceExporter = nativeArray[i];
				DynamicBuffer<TripNeeded> val2 = bufferAccessor[i];
				bool flag = false;
				for (int j = 0; j < val2.Length; j++)
				{
					if (val2[j].m_Purpose == Purpose.Exporting)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<ResourceExporter>(unfilteredChunkIndex, val);
					continue;
				}
				Entity val3 = m_ResourcePrefabs[resourceExporter.m_Resource];
				if (m_ResourceDatas.HasComponent(val3) && EconomyUtils.GetWeight(resourceExporter.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas) == 0f)
				{
					((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<ResourceExporter>(unfilteredChunkIndex, val);
					m_ExportQueue.Enqueue(new ExportEvent
					{
						m_Seller = val,
						m_Buyer = Entity.Null,
						m_Distance = 0f,
						m_Amount = resourceExporter.m_Amount,
						m_Resource = resourceExporter.m_Resource
					});
				}
				else if (m_PathInformation.HasComponent(val))
				{
					PathInformation pathInformation = m_PathInformation[val];
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
							m_Seller = val,
							m_Buyer = destination,
							m_Distance = pathInformation.m_Distance,
							m_Amount = num,
							m_Resource = resourceExporter.m_Resource
						});
						((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<ResourceExporter>(unfilteredChunkIndex, val);
						((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<PathInformation>(unfilteredChunkIndex, val);
						((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<PathElement>(unfilteredChunkIndex, val);
						((ParallelWriter)(ref m_CommandBuffer)).AddBuffer<CurrentTrading>(unfilteredChunkIndex, val).Add(new CurrentTrading
						{
							m_TradingResource = resourceExporter.m_Resource,
							m_TradingResourceAmount = -resourceExporter.m_Amount,
							m_OutsideConnectionType = (m_OutsideConnections.HasComponent(destination) ? BuildingUtils.GetOutsideConnectionType(destination, ref m_PrefabRefs, ref m_OutsideConnectionDatas) : OutsideConnectionTransferType.None),
							m_TradingStartFrameIndex = m_FrameIndex
						});
						val2.Add(new TripNeeded
						{
							m_TargetAgent = destination,
							m_Purpose = Purpose.Exporting,
							m_Resource = resourceExporter.m_Resource,
							m_Data = num
						});
					}
					else
					{
						((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<ResourceExporter>(unfilteredChunkIndex, val);
						((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<PathInformation>(unfilteredChunkIndex, val);
						((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<PathElement>(unfilteredChunkIndex, val);
					}
				}
				else
				{
					FindTarget(unfilteredChunkIndex, val, resourceExporter.m_Resource, resourceExporter.m_Amount);
				}
			}
		}

		private void FindTarget(int chunkIndex, Entity exporter, Resource resource, int amount)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			((ParallelWriter)(ref m_CommandBuffer)).AddComponent<PathInformation>(chunkIndex, exporter, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			((ParallelWriter)(ref m_CommandBuffer)).AddBuffer<PathElement>(chunkIndex, exporter);
			float transportCost = EconomyUtils.GetTransportCost(1f, amount, m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = float2.op_Implicit(111.111115f),
				m_WalkSpeed = float2.op_Implicit(5.555556f),
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
			SetupQueueItem setupQueueItem = new SetupQueueItem(exporter, parameters, origin, destination);
			m_PathfindQueue.Enqueue(setupQueueItem);
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

		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

		public BufferLookup<TradeCost> m_TradeCosts;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public NativeQueue<ExportEvent> m_ExportQueue;

		public Random m_Random;

		public void Execute()
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_0396: Unknown result type (might be due to invalid IL or missing references)
			//IL_039b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0265: Unknown result type (might be due to invalid IL or missing references)
			//IL_026a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0285: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_029a: Unknown result type (might be due to invalid IL or missing references)
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0321: Unknown result type (might be due to invalid IL or missing references)
			//IL_0353: Unknown result type (might be due to invalid IL or missing references)
			//IL_0358: Unknown result type (might be due to invalid IL or missing references)
			//IL_0375: Unknown result type (might be due to invalid IL or missing references)
			//IL_0377: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_021e: Unknown result type (might be due to invalid IL or missing references)
			ExportEvent exportEvent = default(ExportEvent);
			while (m_ExportQueue.TryDequeue(ref exportEvent))
			{
				int resources = EconomyUtils.GetResources(exportEvent.m_Resource, m_Resources[exportEvent.m_Seller]);
				if (exportEvent.m_Amount <= 0 || resources <= 0)
				{
					continue;
				}
				float industrialPrice = EconomyUtils.GetIndustrialPrice(exportEvent.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
				int num = (int)(2.1474836E+09f / industrialPrice) - 1000;
				exportEvent.m_Amount = math.min(math.min(exportEvent.m_Amount, resources), num);
				int num2 = Mathf.RoundToInt(industrialPrice * (float)exportEvent.m_Amount);
				float weight = EconomyUtils.GetWeight(exportEvent.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
				bool flag = weight == 0f;
				if (!flag && m_Storages.HasComponent(exportEvent.m_Buyer))
				{
					float num3 = (float)EconomyUtils.GetTransportCost(exportEvent.m_Distance, exportEvent.m_Resource, exportEvent.m_Amount, weight) / (float)exportEvent.m_Amount;
					if (m_TradeCosts.HasBuffer(exportEvent.m_Buyer) && m_TradeCosts.HasBuffer(exportEvent.m_Seller))
					{
						DynamicBuffer<TradeCost> costs = m_TradeCosts[exportEvent.m_Buyer];
						TradeCost tradeCost = EconomyUtils.GetTradeCost(exportEvent.m_Resource, costs);
						Assert.IsTrue(exportEvent.m_Amount != 0 && !float.IsNaN(tradeCost.m_BuyCost), $"NaN error of Entity:{exportEvent.m_Buyer.Index}");
						tradeCost.m_BuyCost = math.lerp(tradeCost.m_BuyCost, num3, 0.5f);
						Assert.IsTrue(!float.IsNaN(tradeCost.m_BuyCost), $"NaN error of Entity:{exportEvent.m_Buyer.Index}");
						EconomyUtils.SetTradeCost(exportEvent.m_Resource, tradeCost, costs, keepLastTime: true);
						DynamicBuffer<TradeCost> costs2 = m_TradeCosts[exportEvent.m_Seller];
						TradeCost tradeCost2 = EconomyUtils.GetTradeCost(exportEvent.m_Resource, costs2);
						tradeCost2.m_SellCost = math.lerp(tradeCost2.m_SellCost, num3, 0.5f);
						EconomyUtils.SetTradeCost(exportEvent.m_Resource, tradeCost2, costs2, keepLastTime: true);
					}
					num2 -= Mathf.RoundToInt(num3);
				}
				else if (flag)
				{
					Entity val = m_OutsideConnectionEntities[((Random)(ref m_Random)).NextInt(0, m_OutsideConnectionEntities.Length)];
					if (m_Storages.HasComponent(val) && m_TradeCosts.HasBuffer(exportEvent.m_Seller))
					{
						DynamicBuffer<TradeCost> costs3 = m_TradeCosts[val];
						TradeCost tradeCost3 = EconomyUtils.GetTradeCost(exportEvent.m_Resource, costs3);
						tradeCost3.m_BuyCost = math.lerp(tradeCost3.m_BuyCost, 0f, 0.75f);
						EconomyUtils.SetTradeCost(exportEvent.m_Resource, tradeCost3, costs3, keepLastTime: true);
						DynamicBuffer<TradeCost> costs4 = m_TradeCosts[exportEvent.m_Seller];
						tradeCost3.m_SellCost = math.lerp(tradeCost3.m_SellCost, 0f, 0.75f);
						EconomyUtils.SetTradeCost(exportEvent.m_Resource, tradeCost3, costs4, keepLastTime: true);
						num2 += (int)((float)exportEvent.m_Amount * tradeCost3.m_BuyCost);
						EconomyUtils.AddResources(Resource.Money, num2, m_Resources[exportEvent.m_Seller]);
						EconomyUtils.AddResources(exportEvent.m_Resource, exportEvent.m_Amount, m_Resources[val]);
					}
				}
				EconomyUtils.AddResources(exportEvent.m_Resource, -exportEvent.m_Amount, m_Resources[exportEvent.m_Seller]);
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

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;

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
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Companies_ResourceExporter_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ResourceExporter>(true);
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<TripNeeded>(false);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PathInformation>(true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Game.Companies.StorageCompany>(true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Game.Objects.OutsideConnection>(true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ResourceData>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<OutsideConnectionData>(true);
			__Game_Economy_Resources_RW_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Resources>(false);
			__Game_Companies_TradeCost_RW_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<TradeCost>(false);
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
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_PathfindSetupSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_EndFrameBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TaxSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<TaxSystem>();
		m_VehicleCapacitySystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		EntityQueryDesc[] array = new EntityQueryDesc[2];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[5]
		{
			ComponentType.ReadOnly<ResourceExporter>(),
			ComponentType.ReadOnly<TaxPayer>(),
			ComponentType.ReadOnly<PropertyRenter>(),
			ComponentType.ReadOnly<Resources>(),
			ComponentType.ReadWrite<TripNeeded>()
		};
		val.None = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.Exclude<ResourceBuyer>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		};
		array[0] = val;
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[5]
		{
			ComponentType.ReadOnly<ResourceExporter>(),
			ComponentType.ReadOnly<Resources>(),
			ComponentType.ReadWrite<TripNeeded>(),
			ComponentType.ReadOnly<Game.Buildings.ResourceProducer>(),
			ComponentType.ReadOnly<CityServiceUpkeep>()
		};
		val.None = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		};
		array[1] = val;
		m_ExporterQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		m_OutsideConnectionQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[7]
		{
			ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
			ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
			ComponentType.ReadOnly<PrefabRef>(),
			ComponentType.ReadWrite<Resources>(),
			ComponentType.ReadWrite<TradeCost>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		m_EconomyParameterQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<EconomyParameterData>() });
		m_ExportQueue = new NativeQueue<ExportEvent>(AllocatorHandle.op_Implicit((Allocator)4));
		((ComponentSystemBase)this).RequireForUpdate(m_ExporterQuery);
		((ComponentSystemBase)this).RequireForUpdate(m_EconomyParameterQuery);
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
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		ExportJob exportJob = new ExportJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ResourceExporterType = InternalCompilerInterface.GetComponentTypeHandle<ResourceExporter>(ref __TypeHandle.__Game_Companies_ResourceExporter_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_TripType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PathInformation = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_StorageCompanies = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup<OutsideConnectionData>(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ExportQueue = m_ExportQueue.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		};
		EntityCommandBuffer val = m_EndFrameBarrier.CreateCommandBuffer();
		exportJob.m_CommandBuffer = ((EntityCommandBuffer)(ref val)).AsParallelWriter();
		exportJob.m_FrameIndex = m_SimulationSystem.frameIndex;
		exportJob.m_RandomSeed = RandomSeed.Next();
		ExportJob exportJob2 = exportJob;
		((SystemBase)this).Dependency = JobChunkExtensions.ScheduleParallel<ExportJob>(exportJob2, m_ExporterQuery, ((SystemBase)this).Dependency);
		m_ResourceSystem.AddPrefabsReader(((SystemBase)this).Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(((SystemBase)this).Dependency);
		m_PathfindSetupSystem.AddQueueWriter(((SystemBase)this).Dependency);
		NativeArray<Entity> outsideConnectionEntities = ((EntityQuery)(ref m_OutsideConnectionQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
		HandleExportsJob handleExportsJob = new HandleExportsJob
		{
			m_Resources = InternalCompilerInterface.GetBufferLookup<Resources>(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_Storages = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_TradeCosts = InternalCompilerInterface.GetBufferLookup<TradeCost>(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_OutsideConnectionEntities = outsideConnectionEntities,
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ExportQueue = m_ExportQueue,
			m_Random = RandomSeed.Next().GetRandom(Random.Range(0, 2147483647))
		};
		((SystemBase)this).Dependency = IJobExtensions.Schedule<HandleExportsJob>(handleExportsJob, ((SystemBase)this).Dependency);
		m_ResourceSystem.AddPrefabsReader(((SystemBase)this).Dependency);
		m_TaxSystem.AddReader(((SystemBase)this).Dependency);
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
	public ResourceExporterSystem()
	{
	}
}
