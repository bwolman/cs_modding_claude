using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
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
public class FindJobSystem : GameSystemBase
{
	[BurstCompile]
	private struct CalculateFreeWorkplaceJob : IJob
	{
		[ReadOnly]
		public NativeList<FreeWorkplaces> m_FreeWorkplaces;

		public NativeArray<int> m_FreeCache;

		public void Execute()
		{
			for (int i = 0; i < m_FreeCache.Length; i++)
			{
				m_FreeCache[i] = 0;
			}
			for (int j = 0; j < m_FreeWorkplaces.Length; j++)
			{
				FreeWorkplaces freeWorkplaces = m_FreeWorkplaces[j];
				m_FreeCache[0] += freeWorkplaces.m_Uneducated;
				m_FreeCache[1] += freeWorkplaces.m_PoorlyEducated;
				m_FreeCache[2] += freeWorkplaces.m_Educated;
				m_FreeCache[3] += freeWorkplaces.m_WellEducated;
				m_FreeCache[4] += freeWorkplaces.m_HighlyEducated;
			}
		}
	}

	[BurstCompile]
	private struct FindJobJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<JobSeeker> m_JobSeekerType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenDatas;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public NativeArray<int> m_FreeCache;

		[ReadOnly]
		public NativeArray<int> m_EmployableByEducation;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<JobSeeker> nativeArray3 = chunk.GetNativeArray(ref m_JobSeekerType);
			NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity owner = nativeArray2[i].m_Owner;
				if (m_Deleteds.HasComponent(owner) || !m_CitizenDatas.HasComponent(owner))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(Deleted));
					continue;
				}
				Entity household = m_HouseholdMembers[owner].m_Household;
				Citizen citizen = m_CitizenDatas[owner];
				Entity entity = Entity.Null;
				if (m_PropertyRenters.HasComponent(household))
				{
					entity = m_PropertyRenters[household].m_Property;
				}
				else if (chunk.Has(ref m_CurrentBuildingType))
				{
					if ((citizen.m_State & CitizenFlags.Commuter) != CitizenFlags.None)
					{
						entity = nativeArray4[i].m_CurrentBuilding;
					}
				}
				else if (m_HomelessHouseholds.HasComponent(household))
				{
					entity = m_HomelessHouseholds[household].m_TempHome;
				}
				Entity entity2 = nativeArray[i];
				if (entity != Entity.Null)
				{
					int level = nativeArray3[i].m_Level;
					int num = level;
					int num2 = -1;
					bool flag = m_Workers.HasComponent(owner) && m_OutsideConnections.HasComponent(m_Workers[owner].m_Workplace);
					if (m_Workers.HasComponent(owner) && !flag)
					{
						num2 = m_Workers[owner].m_Level;
					}
					if (num2 >= 0 && num > level && num <= num2)
					{
						m_CommandBuffer.SetComponentEnabled<HasJobSeeker>(unfilteredChunkIndex, owner, value: false);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, default(Deleted));
						continue;
					}
					while (num > num2 && m_FreeCache[num] <= 0)
					{
						num--;
					}
					if (num == -1)
					{
						m_CommandBuffer.SetComponentEnabled<HasJobSeeker>(unfilteredChunkIndex, owner, value: false);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, default(Deleted));
						continue;
					}
					float num3 = m_FreeCache[num];
					float num4 = (float)m_EmployableByEducation[num] / num3;
					if (num2 >= 0 && random.NextFloat(num4) > 2f)
					{
						m_CommandBuffer.SetComponentEnabled<HasJobSeeker>(unfilteredChunkIndex, owner, value: false);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, default(Deleted));
						continue;
					}
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, new PathInformation
					{
						m_State = PathFlags.Pending
					});
					Household household2 = m_Households[household];
					DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
					PathfindParameters parameters = new PathfindParameters
					{
						m_MaxSpeed = 111.111115f,
						m_WalkSpeed = 1.6666667f,
						m_Weights = CitizenUtils.GetPathfindWeights(citizen, household2, dynamicBuffer.Length),
						m_Methods = (PathMethod.Pedestrian | PathMethod.PublicTransportDay | PathMethod.PublicTransportNight),
						m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost,
						m_PathfindFlags = (PathfindFlags.Simplified | PathfindFlags.IgnorePath)
					};
					SetupQueueTarget origin = new SetupQueueTarget
					{
						m_Type = SetupTargetType.CurrentLocation,
						m_Methods = PathMethod.Pedestrian
					};
					SetupQueueTarget destination = new SetupQueueTarget
					{
						m_Type = SetupTargetType.JobSeekerTo,
						m_Methods = PathMethod.Pedestrian,
						m_Value = level + 5 * (num + 1),
						m_Value2 = (flag ? 0f : num4)
					};
					if (nativeArray3[i].m_Outside > 0)
					{
						destination.m_Flags |= SetupTargetFlags.Export;
					}
					if (flag)
					{
						destination.m_Flags |= SetupTargetFlags.Import;
					}
					PathUtils.UpdateOwnedVehicleMethods(household, ref m_OwnedVehicles, ref parameters, ref origin, ref destination);
					SetupQueueItem value = new SetupQueueItem(entity2, parameters, origin, destination);
					m_PathfindQueue.Enqueue(value);
				}
				else
				{
					m_CommandBuffer.SetComponentEnabled<HasJobSeeker>(unfilteredChunkIndex, owner, value: false);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, default(Deleted));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct StartWorkingJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<JobSeeker> m_JobSeekerType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInfoType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		public BufferLookup<Employee> m_EmployeeBuffers;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		public ComponentLookup<FreeWorkplaces> m_FreeWorkplaces;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviders;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		public EntityCommandBuffer m_CommandBuffer;

		public uint m_SimulationFrame;

		public NativeValue<int> m_StartedWorking;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Owner> nativeArray = archetypeChunk.GetNativeArray(ref m_OwnerType);
				NativeArray<PathInformation> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PathInfoType);
				NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<JobSeeker> nativeArray4 = archetypeChunk.GetNativeArray(ref m_JobSeekerType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_State & PathFlags.Pending) != 0)
					{
						continue;
					}
					Entity e = nativeArray3[j];
					Entity owner = nativeArray[j].m_Owner;
					if (m_Citizens.HasComponent(owner) && !m_Deleteds.HasComponent(owner))
					{
						Entity destination = nativeArray2[j].m_Destination;
						if (m_Prefabs.HasComponent(destination) && m_EmployeeBuffers.HasBuffer(destination))
						{
							DynamicBuffer<Employee> employees = m_EmployeeBuffers[destination];
							WorkProvider workProvider = m_WorkProviders[destination];
							Entity entity = (m_PropertyRenters.HasComponent(destination) ? m_PropertyRenters[destination].m_Property : destination);
							Entity prefab = m_Prefabs[entity].m_Prefab;
							int level = ((!m_SpawnableBuildings.HasComponent(prefab)) ? 1 : m_SpawnableBuildings[prefab].m_Level);
							if (m_Prefabs.HasComponent(destination) && (!m_Workers.HasComponent(owner) || destination != m_Workers[owner].m_Workplace))
							{
								Entity prefab2 = m_Prefabs[destination].m_Prefab;
								if (m_WorkplaceDatas.HasComponent(prefab2))
								{
									if (m_FreeWorkplaces.HasComponent(destination) && m_FreeWorkplaces[destination].Count > 0)
									{
										WorkplaceData workplaceData = m_WorkplaceDatas[prefab2];
										Citizen citizen = m_Citizens[owner];
										Workshift shift = Workshift.Day;
										FreeWorkplaces value = m_FreeWorkplaces[destination];
										value.Refresh(employees, workProvider.m_MaxWorkers, workplaceData.m_Complexity, level);
										byte level2 = nativeArray4[j].m_Level;
										int bestFor = value.GetBestFor(level2);
										if (bestFor >= 0)
										{
											float num2 = new Random(1 + (m_SimulationFrame ^ citizen.m_PseudoRandom)).NextFloat();
											if (num2 < workplaceData.m_EveningShiftProbability)
											{
												shift = Workshift.Evening;
											}
											else if (num2 < workplaceData.m_EveningShiftProbability + workplaceData.m_NightShiftProbability)
											{
												shift = Workshift.Night;
											}
											employees.Add(new Employee
											{
												m_Worker = owner,
												m_Level = (byte)bestFor
											});
											if (m_Workers.HasComponent(owner))
											{
												m_CommandBuffer.RemoveComponent<Worker>(owner);
											}
											m_CommandBuffer.AddComponent(owner, new Worker
											{
												m_Workplace = destination,
												m_Level = (byte)bestFor,
												m_LastCommuteTime = nativeArray2[j].m_Duration,
												m_Shift = shift
											});
											num++;
											m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenStartedWorking, Entity.Null, owner, destination));
											value.Refresh(employees, workProvider.m_MaxWorkers, workplaceData.m_Complexity, level);
											m_FreeWorkplaces[destination] = value;
										}
									}
									else if (!m_Workers.HasComponent(owner))
									{
									}
								}
							}
						}
						else if (CitizenUtils.IsCommuter(owner, ref m_Citizens))
						{
							m_CommandBuffer.AddComponent(owner, default(Deleted));
							continue;
						}
						m_CommandBuffer.SetComponentEnabled<HasJobSeeker>(owner, value: false);
					}
					m_CommandBuffer.AddComponent(e, default(Deleted));
				}
			}
			m_StartedWorking.value += num;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<JobSeeker> __Game_Agents_JobSeeker_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Owner> __Game_Common_Owner_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<JobSeeker> __Game_Agents_JobSeeker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public BufferLookup<Employee> __Game_Companies_Employee_RW_BufferLookup;

		public ComponentLookup<FreeWorkplaces> __Game_Companies_FreeWorkplaces_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Agents_JobSeeker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<JobSeeker>();
			__Game_Common_Owner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Agents_JobSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<JobSeeker>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Companies_Employee_RW_BufferLookup = state.GetBufferLookup<Employee>();
			__Game_Companies_FreeWorkplaces_RW_ComponentLookup = state.GetComponentLookup<FreeWorkplaces>();
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
		}
	}

	private const int UPDATE_INTERVAL = 16;

	private EntityQuery m_JobSeekerQuery;

	private EntityQuery m_ResultsQuery;

	private EntityQuery m_FreeQuery;

	private SimulationSystem m_SimulationSystem;

	private TriggerSystem m_TriggerSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private NativeArray<int> m_FreeCache;

	[DebugWatchValue]
	private NativeValue<int> m_StartedWorking;

	[DebugWatchDeps]
	private JobHandle m_WriteDeps;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_FreeQuery = GetEntityQuery(ComponentType.ReadOnly<FreeWorkplaces>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_StartedWorking = new NativeValue<int>(Allocator.Persistent);
		m_JobSeekerQuery = GetEntityQuery(ComponentType.ReadWrite<JobSeeker>(), ComponentType.ReadOnly<Owner>(), ComponentType.Exclude<PathInformation>(), ComponentType.Exclude<Deleted>());
		m_ResultsQuery = GetEntityQuery(ComponentType.ReadWrite<JobSeeker>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PathInformation>(), ComponentType.Exclude<Deleted>());
		m_FreeCache = new NativeArray<int>(5, Allocator.Persistent);
		RequireAnyForUpdate(m_JobSeekerQuery, m_ResultsQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_StartedWorking.Dispose();
		m_FreeCache.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_JobSeekerQuery.IsEmptyIgnoreFilter && !m_CountHouseholdDataSystem.IsCountDataNotReady())
		{
			JobHandle job = IJobExtensions.Schedule(new CalculateFreeWorkplaceJob
			{
				m_FreeWorkplaces = m_FreeQuery.ToComponentDataListAsync<FreeWorkplaces>(base.World.UpdateAllocator.ToAllocator, out job),
				m_FreeCache = m_FreeCache
			}, JobHandle.CombineDependencies(job, base.Dependency));
			FindJobJob jobData = new FindJobJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_JobSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_JobSeeker_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 80, 16).AsParallelWriter(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_FreeCache = m_FreeCache,
				m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables(),
				m_RandomSeed = RandomSeed.Next()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_JobSeekerQuery, JobHandle.CombineDependencies(job, base.Dependency));
			m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_ResultsQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			StartWorkingJob jobData2 = new StartWorkingJob
			{
				m_Chunks = m_ResultsQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_JobSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_JobSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EmployeeBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RW_BufferLookup, ref base.CheckedStateRef),
				m_FreeWorkplaces = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_FreeWorkplaces_RW_ComponentLookup, ref base.CheckedStateRef),
				m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer(),
				m_SimulationFrame = m_SimulationSystem.frameIndex,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
				m_StartedWorking = m_StartedWorking
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
			m_TriggerSystem.AddActionBufferWriter(base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
			m_WriteDeps = JobHandle.CombineDependencies(base.Dependency, m_WriteDeps);
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
	public FindJobSystem()
	{
	}
}
