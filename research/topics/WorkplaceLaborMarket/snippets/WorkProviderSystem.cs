using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
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
public class WorkProviderSystem : GameSystemBase
{
	private enum LayOffReason
	{
		Unknown,
		MovingAway,
		TooMany,
		NoBuilding,
		Count
	}

	[BurstCompile]
	private struct WorkProviderTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposes;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolDatas;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		[ReadOnly]
		public BufferLookup<Game.Buildings.Student> m_StudentBufs;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Efficiency> m_Efficiencies;

		public ParallelWriter m_CommandBuffer;

		public ParallelWriter<LayOffReason> m_LayOffQueue;

		public IconCommandBuffer m_IconCommandBuffer;

		public ParallelWriter<TriggerAction> m_TriggerBuffer;

		public WorkProviderParameterData m_WorkProviderParameterData;

		public BuildingEfficiencyParameterData m_BuildingEfficiencyParameterData;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_026c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0275: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_022b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0245: Unknown result type (might be due to invalid IL or missing references)
			//IL_024d: Unknown result type (might be due to invalid IL or missing references)
			if (!((ArchetypeChunk)(ref chunk)).Has<Game.Objects.OutsideConnection>() && ((ArchetypeChunk)(ref chunk)).GetSharedComponent<UpdateFrame>(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<WorkProvider>(ref m_WorkProviderType);
			BufferAccessor<Employee> bufferAccessor = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<Employee>(ref m_EmployeeType);
			NativeArray<PropertyRenter> nativeArray3 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PropertyRenter>(ref m_PropertyRenterType);
			bool isDestroyed = ((ArchetypeChunk)(ref chunk)).Has<Destroyed>(ref m_DestroyedType);
			bool flag = ((ArchetypeChunk)(ref chunk)).Has<CompanyData>();
			bool flag2 = !flag && ((ArchetypeChunk)(ref chunk)).Has<Game.Objects.OutsideConnection>();
			bool flag3 = !flag && !flag2 && ((ArchetypeChunk)(ref chunk)).Has<Game.Buildings.School>();
			WorkplaceData data = default(WorkplaceData);
			DynamicBuffer<InstalledUpgrade> upgrades = default(DynamicBuffer<InstalledUpgrade>);
			for (int i = 0; i < ((ArchetypeChunk)(ref chunk)).Count; i++)
			{
				if (m_WorkplaceDatas.TryGetComponent((Entity)m_PrefabRefs[nativeArray[i]], ref data))
				{
					ref WorkProvider reference = ref CollectionUtils.ElementAt<WorkProvider>(nativeArray2, i);
					Entity val = Entity.Null;
					if (flag)
					{
						if (nativeArray3.Length <= 0)
						{
							continue;
						}
						val = nativeArray3[i].m_Property;
						if (val == Entity.Null)
						{
							Liquidate(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i]);
							continue;
						}
					}
					else
					{
						val = nativeArray[i];
						if (flag2)
						{
							Workplaces workplaces = new Workplaces
							{
								m_Uneducated = 0,
								m_PoorlyEducated = 0,
								m_Educated = 200,
								m_WellEducated = 200,
								m_HighlyEducated = 200
							};
							reference.m_MaxWorkers = workplaces.TotalCount;
						}
						else if (flag3)
						{
							UpdateSchoolMaxWorkers(ref reference, nativeArray[i]);
						}
						if (!flag2 && m_InstalledUpgrades.TryGetBuffer(nativeArray[i], ref upgrades))
						{
							UpgradeUtils.CombineStats<WorkplaceData>(ref data, upgrades, ref m_PrefabRefs, ref m_WorkplaceDatas);
						}
					}
					if (val != Entity.Null && m_PrefabRefs.HasComponent(val))
					{
						int buildingLevel = PropertyUtils.GetBuildingLevel(m_PrefabRefs[val], m_SpawnableBuildingDatas);
						Workplaces workplaces2 = EconomyUtils.CalculateNumberOfWorkplaces(reference.m_MaxWorkers, data.m_Complexity, buildingLevel);
						Workplaces freeWorkplaces = workplaces2;
						RefreshFreeWorkplace(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i], ref freeWorkplaces);
						if (!flag2)
						{
							UpdateNotificationAndEfficiency(val, ref reference, bufferAccessor[i], workplaces2, freeWorkplaces, data.m_WorkConditions, isDestroyed);
						}
					}
				}
				else
				{
					Liquidate(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i]);
				}
			}
		}

		private void UpdateSchoolMaxWorkers(ref WorkProvider workProvider, Entity schoolEntity)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			int cityServiceWorkplaceMaxWorkers = CityUtils.GetCityServiceWorkplaceMaxWorkers(schoolEntity, ref m_PrefabRefs, ref m_InstalledUpgrades, ref m_Deleteds, ref m_WorkplaceDatas, ref m_SchoolDatas, ref m_StudentBufs);
			workProvider.m_MaxWorkers = cityServiceWorkplaceMaxWorkers;
		}

		private void RefreshFreeWorkplace(int sortKey, Entity workplaceEntity, DynamicBuffer<Employee> employeeBuf, ref Workplaces freeWorkplaces)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			Worker worker = default(Worker);
			for (int i = 0; i < employeeBuf.Length; i++)
			{
				Employee employee = employeeBuf[i];
				if (!m_Citizens.HasComponent(employee.m_Worker) || CitizenUtils.IsDead(employee.m_Worker, ref m_HealthProblems) || !m_Workers.TryGetComponent(employee.m_Worker, ref worker) || worker.m_Workplace != workplaceEntity || m_MovingAways.HasComponent(m_HouseholdMembers[employee.m_Worker].m_Household))
				{
					employeeBuf.RemoveAtSwapBack(i);
					i--;
					m_LayOffQueue.Enqueue(LayOffReason.MovingAway);
				}
				else if (freeWorkplaces[employee.m_Level] <= 0)
				{
					RemoveWorker(sortKey, employee.m_Worker, workplaceEntity);
					employeeBuf.RemoveAtSwapBack(i);
					i--;
					m_LayOffQueue.Enqueue(LayOffReason.TooMany);
				}
				else
				{
					freeWorkplaces[employee.m_Level]--;
				}
			}
			if (freeWorkplaces.TotalCount > 0)
			{
				((ParallelWriter)(ref m_CommandBuffer)).AddComponent<FreeWorkplaces>(sortKey, workplaceEntity, new FreeWorkplaces(freeWorkplaces));
			}
			else
			{
				((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<FreeWorkplaces>(sortKey, workplaceEntity);
			}
		}

		private void UpdateNotificationAndEfficiency(Entity buildingEntity, ref WorkProvider workProvider, DynamicBuffer<Employee> employees, Workplaces maxWorkplaces, Workplaces freeWorkplaces, int workConditions, bool isDestroyed)
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_021b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_022f: Unknown result type (might be due to invalid IL or missing references)
			int num = maxWorkplaces.m_Uneducated + maxWorkplaces.m_PoorlyEducated;
			int num2 = freeWorkplaces.m_Uneducated + freeWorkplaces.m_PoorlyEducated;
			bool enabled = false;
			if (!isDestroyed)
			{
				enabled = num > 0 && (float)num2 / (float)num >= m_WorkProviderParameterData.m_UneducatedNotificationLimit;
			}
			UpdateCooldown(ref workProvider.m_UneducatedCooldown, enabled);
			UpdateNotification(buildingEntity, m_WorkProviderParameterData.m_UneducatedNotificationPrefab, workProvider.m_UneducatedCooldown >= m_WorkProviderParameterData.m_UneducatedNotificationDelay, ref workProvider.m_UneducatedNotificationEntity);
			int num3 = maxWorkplaces.m_Educated + 2 * maxWorkplaces.m_WellEducated + 2 * maxWorkplaces.m_HighlyEducated;
			int num4 = freeWorkplaces.m_Educated + 2 * freeWorkplaces.m_WellEducated + 2 * freeWorkplaces.m_HighlyEducated;
			bool enabled2 = false;
			if (!isDestroyed)
			{
				enabled2 = num3 > 0 && (float)(num4 / num3) >= m_WorkProviderParameterData.m_EducatedNotificationLimit;
			}
			UpdateCooldown(ref workProvider.m_EducatedCooldown, enabled2);
			UpdateNotification(buildingEntity, m_WorkProviderParameterData.m_EducatedNotificationPrefab, workProvider.m_EducatedCooldown >= m_WorkProviderParameterData.m_EducatedNotificationDelay, ref workProvider.m_EducatedNotificationEntity);
			DynamicBuffer<Efficiency> buffer = default(DynamicBuffer<Efficiency>);
			if (m_Efficiencies.TryGetBuffer(buildingEntity, ref buffer))
			{
				float averageWorkforce = EconomyUtils.GetAverageWorkforce(maxWorkplaces);
				float efficiency;
				float efficiency2;
				float num6;
				if (averageWorkforce > 0f)
				{
					CalculateCurrentWorkforce(employees, maxWorkplaces.TotalCount, out var currentWorkforce, out var averageWorkforce2, out var sickWorkforce);
					float num5 = averageWorkforce - averageWorkforce2 - sickWorkforce;
					UpdateCooldown(ref workProvider.m_EfficiencyCooldown, num5 > 0.001f);
					num5 *= math.saturate((float)workProvider.m_EfficiencyCooldown / m_BuildingEfficiencyParameterData.m_MissingEmployeesEfficiencyDelay);
					num5 *= m_BuildingEfficiencyParameterData.m_MissingEmployeesEfficiencyPenalty;
					sickWorkforce *= m_BuildingEfficiencyParameterData.m_SickEmployeesEfficiencyPenalty;
					float2 val = BuildingUtils.ApproximateEfficiencyFactors((averageWorkforce - num5 - sickWorkforce) / averageWorkforce, new float2(num5, sickWorkforce));
					efficiency = val.x;
					efficiency2 = val.y;
					num6 = ((averageWorkforce2 > 0f) ? (currentWorkforce / averageWorkforce2) : 1f);
					num6 += (float)workConditions * 0.01f;
				}
				else
				{
					workProvider.m_EfficiencyCooldown = 0;
					efficiency = 1f;
					efficiency2 = 1f;
					num6 = 1f;
				}
				BuildingUtils.SetEfficiencyFactor(buffer, EfficiencyFactor.NotEnoughEmployees, efficiency);
				BuildingUtils.SetEfficiencyFactor(buffer, EfficiencyFactor.SickEmployees, efficiency2);
				BuildingUtils.SetEfficiencyFactor(buffer, EfficiencyFactor.EmployeeHappiness, num6);
			}
		}

		private void UpdateCooldown(ref short cooldown, bool enabled)
		{
			if (!enabled)
			{
				if (cooldown > 0)
				{
					cooldown = 0;
				}
			}
			else if (cooldown < 32767)
			{
				cooldown++;
			}
		}

		private void UpdateNotification(Entity building, Entity notificationPrefab, bool enabled, ref Entity currentTarget)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			if (currentTarget != Entity.Null && (!enabled || currentTarget != building))
			{
				m_IconCommandBuffer.Remove(currentTarget, notificationPrefab);
				currentTarget = Entity.Null;
			}
			if (enabled && currentTarget == Entity.Null)
			{
				m_IconCommandBuffer.Add(building, notificationPrefab, IconPriority.Problem);
				currentTarget = building;
			}
		}

		private void CalculateCurrentWorkforce(DynamicBuffer<Employee> employees, int maxCount, out float currentWorkforce, out float averageWorkforce, out float sickWorkforce)
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			currentWorkforce = 0f;
			averageWorkforce = 0f;
			sickWorkforce = 0f;
			int num = math.min(employees.Length, maxCount);
			for (int i = 0; i < num; i++)
			{
				Employee employee = employees[i];
				Citizen citizen = m_Citizens[employee.m_Worker];
				if (!m_HealthProblems.HasComponent(employee.m_Worker))
				{
					currentWorkforce += EconomyUtils.GetWorkerWorkforce(citizen.Happiness, employee.m_Level);
					averageWorkforce += EconomyUtils.GetWorkerWorkforce(50, employee.m_Level);
				}
				else
				{
					sickWorkforce += EconomyUtils.GetWorkerWorkforce(50, employee.m_Level);
				}
			}
		}

		private void Liquidate(int sortKey, Entity provider, DynamicBuffer<Employee> employees)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < employees.Length; i++)
			{
				Entity worker = employees[i].m_Worker;
				if (m_Workers.HasComponent(worker))
				{
					m_LayOffQueue.Enqueue(LayOffReason.NoBuilding);
					RemoveWorker(sortKey, worker, provider);
				}
			}
			employees.Clear();
			((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<FreeWorkplaces>(sortKey, provider);
		}

		private void RemoveWorker(int sortKey, Entity worker, Entity workplace)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			TravelPurpose travelPurpose = default(TravelPurpose);
			if (m_TravelPurposes.TryGetComponent(worker, ref travelPurpose))
			{
				Purpose purpose = travelPurpose.m_Purpose;
				if (purpose == Purpose.GoingToWork || purpose == Purpose.Working || purpose == Purpose.GoingToSchool || purpose == Purpose.Studying)
				{
					purpose = travelPurpose.m_Purpose;
					if (purpose == Purpose.GoingToSchool || purpose == Purpose.Studying)
					{
						Debug.LogWarning((object)$"Worker {worker.Index} had incorrect TravelPurpose {(int)travelPurpose.m_Purpose}!");
					}
					((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<TravelPurpose>(sortKey, worker);
				}
			}
			((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<Worker>(sortKey, worker);
			m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, worker, workplace));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct LayOffCountJob : IJob
	{
		public NativeQueue<LayOffReason> m_LayOffQueue;

		public NativeArray<int> m_LayOffs;

		public void Execute()
		{
			LayOffReason layOffReason = default(LayOffReason);
			while (m_LayOffQueue.TryDequeue(ref layOffReason))
			{
				ref NativeArray<int> reference = ref m_LayOffs;
				int num = (int)layOffReason;
				int num2 = reference[num];
				reference[num] = num2 + 1;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		public BufferTypeHandle<Employee> __Game_Companies_Employee_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = ((SystemState)(ref state)).GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PropertyRenter>(true);
			__Game_Companies_WorkProvider_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WorkProvider>(false);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Destroyed>(true);
			__Game_Companies_Employee_RW_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<Employee>(false);
			__Game_Citizens_Citizen_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Citizen>(true);
			__Game_Citizens_Worker_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Worker>(true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SpawnableBuildingData>(true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<WorkplaceData>(true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<HealthProblem>(true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<TravelPurpose>(true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<HouseholdMember>(true);
			__Game_Agents_MovingAway_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<MovingAway>(true);
			__Game_Buildings_Efficiency_RW_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Efficiency>(false);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SchoolData>(true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<InstalledUpgrade>(true);
			__Game_Common_Deleted_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Deleted>(true);
			__Game_Buildings_Student_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Buildings.Student>(true);
		}
	}

	private const int kUpdatesPerDay = 512;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private IconCommandSystem m_IconCommandSystem;

	private TriggerSystem m_TriggerSystem;

	private EntityQuery m_WorkProviderGroup;

	private NativeQueue<LayOffReason> m_LayOffQueue;

	[DebugWatchValue]
	private NativeArray<int> m_LayOffs;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_543653706_0;

	private EntityQuery __query_543653706_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 32;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_IconCommandSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<TriggerSystem>();
		EntityQueryDesc[] array = new EntityQueryDesc[1];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<WorkProvider>(),
			ComponentType.ReadOnly<PrefabRef>(),
			ComponentType.ReadWrite<Employee>()
		};
		val.Any = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<CompanyData>(),
			ComponentType.ReadOnly<Building>(),
			ComponentType.ReadOnly<Game.Objects.OutsideConnection>()
		};
		val.None = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Temp>()
		};
		array[0] = val;
		m_WorkProviderGroup = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		m_LayOffQueue = new NativeQueue<LayOffReason>(AllocatorHandle.op_Implicit((Allocator)4));
		m_LayOffs = new NativeArray<int>(4, (Allocator)4, (NativeArrayOptions)1);
		((ComponentSystemBase)this).RequireForUpdate(m_WorkProviderGroup);
		((ComponentSystemBase)this).RequireForUpdate<WorkProviderParameterData>();
		((ComponentSystemBase)this).RequireForUpdate<BuildingEfficiencyParameterData>();
		Assert.IsTrue(true);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LayOffs.Dispose();
		m_LayOffQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 512, 16);
		WorkProviderTickJob workProviderTickJob = new WorkProviderTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle<PropertyRenter>(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle<WorkProvider>(ref __TypeHandle.__Game_Companies_WorkProvider_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle<Destroyed>(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle<Employee>(ref __TypeHandle.__Game_Companies_Employee_RW_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup<Citizen>(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup<WorkplaceData>(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup<HealthProblem>(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_TravelPurposes = InternalCompilerInterface.GetComponentLookup<TravelPurpose>(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup<HouseholdMember>(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup<MovingAway>(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_Efficiencies = InternalCompilerInterface.GetBufferLookup<Efficiency>(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SchoolDatas = InternalCompilerInterface.GetComponentLookup<SchoolData>(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup<InstalledUpgrade>(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_Deleteds = InternalCompilerInterface.GetComponentLookup<Deleted>(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_StudentBufs = InternalCompilerInterface.GetBufferLookup<Game.Buildings.Student>(ref __TypeHandle.__Game_Buildings_Student_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef)
		};
		EntityCommandBuffer val = m_EndFrameBarrier.CreateCommandBuffer();
		workProviderTickJob.m_CommandBuffer = ((EntityCommandBuffer)(ref val)).AsParallelWriter();
		workProviderTickJob.m_LayOffQueue = m_LayOffQueue.AsParallelWriter();
		workProviderTickJob.m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
		workProviderTickJob.m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter();
		workProviderTickJob.m_WorkProviderParameterData = ((EntityQuery)(ref __query_543653706_0)).GetSingleton<WorkProviderParameterData>();
		workProviderTickJob.m_BuildingEfficiencyParameterData = ((EntityQuery)(ref __query_543653706_1)).GetSingleton<BuildingEfficiencyParameterData>();
		workProviderTickJob.m_UpdateFrameIndex = updateFrame;
		WorkProviderTickJob workProviderTickJob2 = workProviderTickJob;
		((SystemBase)this).Dependency = JobChunkExtensions.ScheduleParallel<WorkProviderTickJob>(workProviderTickJob2, m_WorkProviderGroup, ((SystemBase)this).Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(((SystemBase)this).Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(((SystemBase)this).Dependency);
		m_TriggerSystem.AddActionBufferWriter(((SystemBase)this).Dependency);
		LayOffCountJob layOffCountJob = new LayOffCountJob
		{
			m_LayOffs = m_LayOffs,
			m_LayOffQueue = m_LayOffQueue
		};
		((SystemBase)this).Dependency = IJobExtensions.Schedule<LayOffCountJob>(layOffCountJob, ((SystemBase)this).Dependency);
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
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		EntityQueryBuilder val2 = ((EntityQueryBuilder)(ref val)).WithAll<WorkProviderParameterData>();
		val2 = ((EntityQueryBuilder)(ref val2)).WithOptions((EntityQueryOptions)16);
		__query_543653706_0 = ((EntityQueryBuilder)(ref val2)).Build(ref state);
		((EntityQueryBuilder)(ref val)).Reset();
		val2 = ((EntityQueryBuilder)(ref val)).WithAll<BuildingEfficiencyParameterData>();
		val2 = ((EntityQueryBuilder)(ref val2)).WithOptions((EntityQueryOptions)16);
		__query_543653706_1 = ((EntityQueryBuilder)(ref val2)).Build(ref state);
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
	public WorkProviderSystem()
	{
	}
}
