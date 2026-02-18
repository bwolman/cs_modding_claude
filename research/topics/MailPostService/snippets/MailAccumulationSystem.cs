#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class MailAccumulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct MailAccumulationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		public ComponentTypeHandle<MailProducer> m_MailProducerType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<MailAccumulationData> m_MailAccumulationData;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectData;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_PostVanRequestArchetype;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		[ReadOnly]
		public BuildingEfficiencyParameterData m_EfficiencyParameters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[NativeDisableParallelForRestriction]
		public NativeReference<int> m_AccumulatedMail;

		[NativeDisableParallelForRestriction]
		public NativeReference<int> m_ProcessedMail;

		public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			float num = 0.28444445f;
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			NativeArray<MailProducer> nativeArray3 = chunk.GetNativeArray(ref m_MailProducerType);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			int2 @int = default(int2);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				bool requireCollect;
				float2 baseAccumulationRate = GetBaseAccumulationRate(nativeArray2[i].m_Prefab, out requireCollect);
				if (!math.all(baseAccumulationRate == 0f))
				{
					if (bufferAccessor.Length != 0)
					{
						GetCitizenCounts(bufferAccessor[i], out var residentCount, out var workerCount);
						baseAccumulationRate *= (float)(residentCount + workerCount);
					}
					else
					{
						GetCitizenCounts(entity, out var residentCount2, out var workerCount2);
						baseAccumulationRate *= (float)(residentCount2 + workerCount2);
					}
					@int.x = MathUtils.RoundToIntRandom(ref random, baseAccumulationRate.x * num);
					@int.y = MathUtils.RoundToIntRandom(ref random, baseAccumulationRate.y * num);
					ref MailProducer reference = ref nativeArray3.ElementAt(i);
					int value = math.max(0, reference.m_LastUpdateTotalMail - (reference.m_SendingMail + reference.receivingMail));
					int2 int2 = new int2(reference.m_SendingMail, reference.receivingMail);
					int2 int3 = int2;
					int2 = math.min(int2 + @int, m_PostConfigurationData.m_MaxMailAccumulation);
					reference.m_SendingMail = (ushort)int2.x;
					reference.receivingMail = int2.y;
					reference.m_LastUpdateTotalMail = (ushort)(reference.m_SendingMail + reference.receivingMail);
					Interlocked.Add(ref UnsafeUtility.AsRef<int>(m_AccumulatedMail.GetUnsafePtr()), @int.x + @int.y);
					Interlocked.Add(ref UnsafeUtility.AsRef<int>(m_ProcessedMail.GetUnsafePtr()), value);
					RequestPostVanIfNeeded(unfilteredChunkIndex, entity, ref reference, requireCollect);
					if (int3.y >= m_PostConfigurationData.m_MailAccumulationTolerance != int2.y >= m_PostConfigurationData.m_MailAccumulationTolerance)
					{
						QuantityUpdated(unfilteredChunkIndex, entity);
					}
					if (bufferAccessor2.Length != 0)
					{
						BuildingUtils.SetEfficiencyFactor(bufferAccessor2[i], EfficiencyFactor.Mail, GetMailEfficiencyFactor(reference));
					}
				}
			}
		}

		private void QuantityUpdated(int jobIndex, Entity buildingEntity)
		{
			if (!m_SubObjects.HasBuffer(buildingEntity))
			{
				return;
			}
			DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[buildingEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subObject = dynamicBuffer[i].m_SubObject;
				if (m_QuantityData.HasComponent(subObject))
				{
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
				}
			}
		}

		private void GetCitizenCounts(DynamicBuffer<Renter> renters, out int residentCount, out int workerCount)
		{
			residentCount = 0;
			workerCount = 0;
			for (int i = 0; i < renters.Length; i++)
			{
				GetCitizenCounts(renters[i].m_Renter, out var residentCount2, out var workerCount2);
				residentCount += residentCount2;
				workerCount += workerCount2;
			}
		}

		private void GetCitizenCounts(Entity entity, out int residentCount, out int workerCount)
		{
			if (m_HouseholdCitizens.HasBuffer(entity))
			{
				residentCount = m_HouseholdCitizens[entity].Length;
			}
			else
			{
				residentCount = 0;
			}
			if (m_Employees.HasBuffer(entity))
			{
				workerCount = m_Employees[entity].Length;
			}
			else
			{
				workerCount = 0;
			}
		}

		private float2 GetBaseAccumulationRate(Entity prefab, out bool requireCollect)
		{
			if (m_SpawnableBuildingData.HasComponent(prefab))
			{
				SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingData[prefab];
				if (m_MailAccumulationData.HasComponent(spawnableBuildingData.m_ZonePrefab))
				{
					MailAccumulationData mailAccumulationData = m_MailAccumulationData[spawnableBuildingData.m_ZonePrefab];
					requireCollect = mailAccumulationData.m_RequireCollect;
					return mailAccumulationData.m_AccumulationRate;
				}
			}
			else if (m_ServiceObjectData.HasComponent(prefab))
			{
				ServiceObjectData serviceObjectData = m_ServiceObjectData[prefab];
				if (m_MailAccumulationData.HasComponent(serviceObjectData.m_Service))
				{
					MailAccumulationData mailAccumulationData2 = m_MailAccumulationData[serviceObjectData.m_Service];
					requireCollect = mailAccumulationData2.m_RequireCollect;
					return mailAccumulationData2.m_AccumulationRate;
				}
			}
			requireCollect = false;
			return default(float2);
		}

		private void RequestPostVanIfNeeded(int jobIndex, Entity entity, ref MailProducer producer, bool requireCollect)
		{
			int num = ((!requireCollect) ? producer.receivingMail : math.max(producer.m_SendingMail, producer.receivingMail));
			if (num >= m_PostConfigurationData.m_MailAccumulationTolerance && (!m_PostVanRequestData.TryGetComponent(producer.m_MailRequest, out var componentData) || (!(componentData.m_Target == entity) && componentData.m_DispatchIndex != producer.m_DispatchIndex)))
			{
				producer.m_MailRequest = Entity.Null;
				producer.m_DispatchIndex = 0;
				PostVanRequestFlags postVanRequestFlags = PostVanRequestFlags.Deliver | PostVanRequestFlags.BuildingTarget;
				if (requireCollect)
				{
					postVanRequestFlags |= PostVanRequestFlags.Collect;
				}
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PostVanRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PostVanRequest(entity, postVanRequestFlags, (ushort)num));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
			}
		}

		private float GetMailEfficiencyFactor(MailProducer producer)
		{
			int num = math.max(0, math.max(producer.m_SendingMail, producer.receivingMail) - m_EfficiencyParameters.m_NegligibleMail);
			float num2 = 0f;
			if (num > 25)
			{
				int num3 = math.min(50, num - 25);
				num2 = ((float)(num3 * num3) + 125f) / 2625f;
			}
			return 1f - m_EfficiencyParameters.m_MailEfficiencyPenalty * num2;
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

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		public ComponentTypeHandle<MailProducer> __Game_Buildings_MailProducer_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> __Game_Simulation_PostVanRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailAccumulationData> __Game_Prefabs_MailAccumulationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Buildings_MailProducer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<MailProducer>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Simulation_PostVanRequest_RO_ComponentLookup = state.GetComponentLookup<PostVanRequest>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_MailAccumulationData_RO_ComponentLookup = state.GetComponentLookup<MailAccumulationData>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private const int kUpdatesPerDay = 256;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private NativeReference<int> m_AccumulatedMailRef;

	private NativeReference<int> m_ProcessedMailRef;

	private int m_LastAccumulatedMail;

	private int m_LastProcessedMail;

	private EntityQuery m_MailProducerQuery;

	private EntityArchetype m_PostVanRequestArchetype;

	private uint m_PreviousFrameIndex;

	private int m_CycleCounter;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_890676537_0;

	private EntityQuery __query_890676537_1;

	public int LastAccumulatedMail => m_LastAccumulatedMail;

	public int LastProcessedMail => m_LastProcessedMail;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_LastAccumulatedMail = 0;
		m_LastProcessedMail = 0;
		m_CycleCounter = 0;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_AccumulatedMailRef.Dispose();
		m_ProcessedMailRef.Dispose();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_AccumulatedMailRef = new NativeReference<int>(Allocator.Persistent);
		m_ProcessedMailRef = new NativeReference<int>(Allocator.Persistent);
		m_MailProducerQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<MailProducer>() },
			Any = new ComponentType[0],
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_PostVanRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PostVanRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_MailProducerQuery);
		RequireForUpdate<PostConfigurationData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PostConfigurationData singleton = __query_890676537_0.GetSingleton<PostConfigurationData>();
		if (base.EntityManager.HasEnabledComponent<Locked>(singleton.m_PostServicePrefab))
		{
			return;
		}
		uint num = (m_SimulationSystem.frameIndex / 64) & 0xF;
		if (num == 0 && m_PreviousFrameIndex != 0)
		{
			m_CycleCounter++;
			if (m_CycleCounter >= 4)
			{
				m_CycleCounter = 0;
				m_LastAccumulatedMail = math.abs((int)((float)(256 * m_AccumulatedMailRef.Value) / 4f));
				m_LastProcessedMail = math.abs((int)((float)(256 * m_ProcessedMailRef.Value) * 1.4f / 4f));
				m_AccumulatedMailRef.Value = 0;
				m_ProcessedMailRef.Value = 0;
			}
		}
		m_PreviousFrameIndex = num;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new MailAccumulationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_MailProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_MailProducer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PostVanRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PostVanRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailAccumulationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_RandomSeed = RandomSeed.Next(),
			m_PostVanRequestArchetype = m_PostVanRequestArchetype,
			m_PostConfigurationData = singleton,
			m_EfficiencyParameters = __query_890676537_1.GetSingleton<BuildingEfficiencyParameterData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_AccumulatedMail = m_AccumulatedMailRef,
			m_ProcessedMail = m_ProcessedMailRef
		}, m_MailProducerQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PostConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_890676537_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_890676537_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public MailAccumulationSystem()
	{
	}
}
