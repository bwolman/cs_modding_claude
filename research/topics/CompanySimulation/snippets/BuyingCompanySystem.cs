using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BuyingCompanySystem : GameSystemBase
{
	[BurstCompile]
	private struct CompanyBuyJob : IJobChunk
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_VehicleBufType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourceBufType;

		[ReadOnly]
		public BufferTypeHandle<TripNeeded> m_TripNeededBufType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCostBufType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		public ComponentTypeHandle<CompanyNotifications> m_CompanyNotificationsType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimits;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_Trucks;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_Layouts;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public CompanyNotificationParameterData m_CompanyNotificationParameters;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<OwnedVehicle> bufferAccessor = chunk.GetBufferAccessor(ref m_VehicleBufType);
			BufferAccessor<TripNeeded> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TripNeededBufType);
			BufferAccessor<Resources> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ResourceBufType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<TradeCost> bufferAccessor4 = chunk.GetBufferAccessor(ref m_TradeCostBufType);
			NativeArray<CompanyNotifications> nativeArray3 = chunk.GetNativeArray(ref m_CompanyNotificationsType);
			NativeArray<PropertyRenter> nativeArray4 = chunk.GetNativeArray(ref m_PropertyRenterType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				CompanyNotifications value = nativeArray3[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor[i];
				DynamicBuffer<TradeCost> tradeCosts = bufferAccessor4[i];
				DynamicBuffer<Resources> resourceBuffers = bufferAccessor3[i];
				DynamicBuffer<TripNeeded> trips = bufferAccessor2[i];
				int num = int.MaxValue;
				Entity prefab = nativeArray2[i].m_Prefab;
				if (m_StorageLimits.HasComponent(prefab))
				{
					num = m_StorageLimits[prefab].m_Limit;
				}
				IndustrialProcessData industrialProcessData = m_IndustrialProcessDatas[prefab];
				Entity entity2 = entity;
				if (nativeArray4.Length > 0)
				{
					entity2 = nativeArray4[i].m_Property;
				}
				Resource needResource = Resource.NoResource;
				int needResourceLeft = 0;
				int storageLeft = num;
				bool expensive = false;
				bool flag = industrialProcessData.m_Input2.m_Resource != Resource.NoResource;
				bool flag2 = !flag && (industrialProcessData.m_Output.m_Resource == industrialProcessData.m_Input1.m_Resource || m_ResourceDatas[m_ResourcePrefabs[industrialProcessData.m_Output.m_Resource]].m_Weight <= 0f);
				int num2 = num;
				int num3 = ((!flag) ? 1 : 2);
				if (industrialProcessData.m_Output.m_Resource != industrialProcessData.m_Input1.m_Resource && m_ResourceDatas[m_ResourcePrefabs[industrialProcessData.m_Output.m_Resource]].m_Weight > 0f)
				{
					num3++;
				}
				num2 /= num3;
				CalculateResourceNeeded(isInput: true, industrialProcessData.m_Input1.m_Resource, num2, ref needResource, ref needResourceLeft, ref storageLeft, ref expensive, tradeCosts, resourceBuffers, vehicles, trips);
				if (flag)
				{
					CalculateResourceNeeded(isInput: true, industrialProcessData.m_Input2.m_Resource, num2, ref needResource, ref needResourceLeft, ref storageLeft, ref expensive, tradeCosts, resourceBuffers, vehicles, trips);
				}
				if (industrialProcessData.m_Output.m_Resource != industrialProcessData.m_Input1.m_Resource)
				{
					CalculateResourceNeeded(isInput: false, industrialProcessData.m_Output.m_Resource, num2, ref needResource, ref needResourceLeft, ref storageLeft, ref expensive, tradeCosts, resourceBuffers, vehicles, trips);
				}
				if (value.m_NoInputEntity == default(Entity) && expensive)
				{
					m_IconCommandBuffer.Add(entity2, m_CompanyNotificationParameters.m_NoInputsNotificationPrefab, IconPriority.Problem);
					value.m_NoInputEntity = entity2;
					nativeArray3[i] = value;
				}
				else if (value.m_NoInputEntity != default(Entity))
				{
					if (!expensive)
					{
						m_IconCommandBuffer.Remove(value.m_NoInputEntity, m_CompanyNotificationParameters.m_NoInputsNotificationPrefab);
						value.m_NoInputEntity = Entity.Null;
						nativeArray3[i] = value;
					}
					else if (entity2 != value.m_NoInputEntity)
					{
						m_IconCommandBuffer.Remove(value.m_NoInputEntity, m_CompanyNotificationParameters.m_NoInputsNotificationPrefab);
						m_IconCommandBuffer.Add(entity2, m_CompanyNotificationParameters.m_NoInputsNotificationPrefab, IconPriority.Problem);
						value.m_NoInputEntity = entity2;
						nativeArray3[i] = value;
					}
				}
				if (needResource == Resource.NoResource)
				{
					continue;
				}
				m_DeliveryTruckSelectData.GetCapacityRange(needResource, out var _, out var max);
				int num4 = kResourceMinimumRequestAmount;
				DeliveryTruckSelectItem item = default(DeliveryTruckSelectItem);
				ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[needResource]];
				int num5 = 0;
				if (resourceData.m_Weight > 0f)
				{
					num5 = ((!flag) ? (flag2 ? num : (num / 2)) : (num / 3));
					num4 = math.min(num5 - needResourceLeft, math.min(storageLeft, max));
					if (num4 <= kResourceMinimumRequestAmount)
					{
						continue;
					}
					m_DeliveryTruckSelectData.TrySelectItem(ref random, needResource, num4, out item);
				}
				else
				{
					item.m_Capacity = num4;
				}
				if (m_PropertyRenters.HasComponent(entity))
				{
					Entity property = m_PropertyRenters[entity].m_Property;
					if (m_Transforms.HasComponent(property))
					{
						ResourceBuyer component = new ResourceBuyer
						{
							m_Payer = entity,
							m_AmountNeeded = math.min(num4, item.m_Capacity),
							m_Flags = (SetupTargetFlags.Industrial | SetupTargetFlags.Import),
							m_Location = m_Transforms[property].m_Position,
							m_ResourceNeeded = needResource
						};
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, component);
					}
				}
			}
		}

		private void CalculateResourceNeeded(bool isInput, Resource resource, int maxCapacity, ref Resource needResource, ref int needResourceLeft, ref int storageLeft, ref bool expensive, DynamicBuffer<TradeCost> tradeCosts, DynamicBuffer<Resources> resourceBuffers, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<TripNeeded> trips)
		{
			int num = EconomyUtils.GetResources(resource, resourceBuffers);
			if (isInput)
			{
				if (EconomyUtils.GetTradeCost(resource, tradeCosts).m_BuyCost > kNotificationCostLimit)
				{
					expensive = true;
				}
				for (int i = 0; i < vehicles.Length; i++)
				{
					Entity vehicle = vehicles[i].m_Vehicle;
					num += VehicleUtils.GetBuyingTrucksLoad(vehicle, resource, ref m_Trucks, ref m_Layouts);
				}
				for (int j = 0; j < trips.Length; j++)
				{
					TripNeeded tripNeeded = trips[j];
					if (tripNeeded.m_Purpose == Purpose.Shopping && tripNeeded.m_Resource == resource)
					{
						num += tripNeeded.m_Data;
					}
				}
				int num2 = (int)math.max(kResourceLowStockAmount, (float)maxCapacity * 0.25f);
				if (needResource == Resource.NoResource && num < num2)
				{
					needResource = resource;
					needResourceLeft = num;
				}
			}
			if (EconomyUtils.IsResourceHasWeight(resource, m_ResourcePrefabs, ref m_ResourceDatas))
			{
				storageLeft -= num;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		public ComponentTypeHandle<CompanyNotifications> __Game_Companies_CompanyNotifications_RW_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RO_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>(isReadOnly: true);
			__Game_Companies_TradeCost_RO_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Companies_CompanyNotifications_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyNotifications>();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	private static readonly float kNotificationCostLimit = 5f;

	private static readonly int kResourceLowStockAmount = 4000;

	private static readonly int kResourceMinimumRequestAmount = 2000;

	private SimulationSystem m_SimulationSystem;

	private ResourceSystem m_ResourceSystem;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_CompanyNotificationParameterQuery;

	private EntityQuery m_CompanyGroup;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_CompanyGroup = GetEntityQuery(ComponentType.ReadOnly<BuyingCompany>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadWrite<OwnedVehicle>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<TradeCost>(), ComponentType.ReadWrite<CompanyNotifications>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.Exclude<Deleted>(), ComponentType.ReadOnly<UpdateFrame>());
		m_CompanyNotificationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyNotificationParameterData>());
		RequireForUpdate(m_CompanyGroup);
		RequireForUpdate(m_CompanyNotificationParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		CompanyBuyJob jobData = new CompanyBuyJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ResourceBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TripNeededBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TradeCostBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyNotificationsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyNotifications_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageLimits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Trucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Layouts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyNotificationParameters = m_CompanyNotificationParameterQuery.GetSingleton<CompanyNotificationParameterData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_RandomSeed = RandomSeed.Next()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CompanyGroup, base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public BuyingCompanySystem()
	{
	}
}
