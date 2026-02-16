using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class EducationInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		Uneducated,
		ElementaryEducated,
		HighSchoolEducated,
		CollegeEducated,
		UniversityEducated,
		ElementaryEligible,
		HighSchoolEligible,
		CollegeEligible,
		UniversityEligible,
		ElementaryStudents,
		HighSchoolStudents,
		CollegeStudents,
		UniversityStudents,
		ElementaryCapacity,
		HighSchoolCapacity,
		CollegeCapacity,
		UniversityCapacity,
		Count
	}

	[BurstCompile]
	private struct UpdateEducationDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdHandle;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Household> nativeArray = chunk.GetNativeArray(ref m_HouseholdHandle);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenHandle);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				Household household = nativeArray[i];
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
				if ((household.m_Flags & HouseholdFlags.MovedIn) == 0)
				{
					continue;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity citizen = dynamicBuffer[j].m_Citizen;
					if (!CitizenUtils.IsDead(citizen, ref m_HealthProblemFromEntity) && m_CitizenFromEntity.TryGetComponent(citizen, out var componentData))
					{
						switch (componentData.GetEducationLevel())
						{
						case 0:
							num++;
							break;
						case 1:
							num2++;
							break;
						case 2:
							num3++;
							break;
						case 3:
							num4++;
							break;
						case 4:
							num5++;
							break;
						}
					}
				}
			}
			m_Results[0] += num;
			m_Results[1] += num2;
			m_Results[2] += num3;
			m_Results[3] += num4;
			m_Results[4] += num5;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateStudentCountsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Buildings.Student> m_StudentHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolDataFromEntity;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgradeFromEntity;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefTypeHandle);
			BufferAccessor<Game.Buildings.Student> bufferAccessor = chunk.GetBufferAccessor(ref m_StudentHandle);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (BuildingUtils.GetEfficiency(bufferAccessor2, i) != 0f)
				{
					DynamicBuffer<Game.Buildings.Student> dynamicBuffer = bufferAccessor[i];
					m_SchoolDataFromEntity.TryGetComponent(prefab, out var componentData);
					if (m_InstalledUpgradeFromEntity.TryGetBuffer(entity, out var bufferData))
					{
						UpgradeUtils.CombineStats(ref componentData, bufferData, ref m_PrefabRefFromEntity, ref m_SchoolDataFromEntity);
					}
					switch (componentData.m_EducationLevel)
					{
					case 1:
						m_Results[9] += dynamicBuffer.Length;
						m_Results[13] += componentData.m_StudentCapacity;
						break;
					case 2:
						m_Results[10] += dynamicBuffer.Length;
						m_Results[14] += componentData.m_StudentCapacity;
						break;
					case 3:
						m_Results[11] += dynamicBuffer.Length;
						m_Results[15] += componentData.m_StudentCapacity;
						break;
					case 4:
						m_Results[12] += dynamicBuffer.Length;
						m_Results[16] += componentData.m_StudentCapacity;
						break;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateEligibilityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Citizens.Student> m_StudentHandle;

		[ReadOnly]
		public ComponentTypeHandle<Worker> m_WorkerHandle;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdFromEntity;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMemberFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifierFromEntity;

		public Entity m_City;

		public EducationParameterData m_EducationParameterData;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenHandle);
			NativeArray<Game.Citizens.Student> nativeArray3 = chunk.GetNativeArray(ref m_StudentHandle);
			NativeArray<Worker> nativeArray4 = chunk.GetNativeArray(ref m_WorkerHandle);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifierFromEntity[m_City];
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (CitizenUtils.IsDead(entity, ref m_HealthProblemFromEntity) || !m_HouseholdMemberFromEntity.TryGetComponent(entity, out var componentData) || !m_HouseholdFromEntity.TryGetComponent(componentData.m_Household, out var componentData2) || (componentData2.m_Flags & HouseholdFlags.MovedIn) == 0 || (componentData2.m_Flags & HouseholdFlags.Tourist) != HouseholdFlags.None || m_MovingAways.HasComponent(componentData.m_Household) || !m_PropertyRenters.HasComponent(componentData.m_Household))
				{
					continue;
				}
				if (chunk.Has(ref m_StudentHandle))
				{
					switch (nativeArray3[i].m_Level)
					{
					case 1:
						num += 1f;
						break;
					case 2:
						num2 += 1f;
						break;
					case 3:
						num3 += 1f;
						break;
					case 4:
						num4 += 1f;
						break;
					}
					continue;
				}
				Citizen citizen = nativeArray2[i];
				CitizenAge age = citizen.GetAge();
				float willingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
				if (age == CitizenAge.Child)
				{
					num += 1f;
					continue;
				}
				if (citizen.GetEducationLevel() == 1 && age <= CitizenAge.Adult)
				{
					num2 += ApplyToSchoolSystem.GetEnteringProbability(age, nativeArray4.IsCreated, 2, citizen.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
					continue;
				}
				int failedEducationCount = citizen.GetFailedEducationCount();
				if (citizen.GetEducationLevel() == 2 && failedEducationCount < 3)
				{
					float enteringProbability = ApplyToSchoolSystem.GetEnteringProbability(age, nativeArray4.IsCreated, 4, citizen.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
					num4 += enteringProbability;
					num3 += (1f - enteringProbability) * ApplyToSchoolSystem.GetEnteringProbability(age, nativeArray4.IsCreated, 3, citizen.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
				}
			}
			m_Results[5] += Mathf.CeilToInt(num);
			m_Results[6] += Mathf.CeilToInt(num2);
			m_Results[7] += Mathf.CeilToInt(num3);
			m_Results[8] += Mathf.CeilToInt(num4);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Household> __Game_Citizens_Household_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Household_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Household>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Student_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Buildings.Student>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private const string kGroup = "educationInfo";

	private SimulationSystem m_SimulationSystem;

	private CitySystem m_CitySystem;

	private RawValueBinding m_EducationData;

	private ValueBinding<int> m_ElementaryStudents;

	private ValueBinding<int> m_HighSchoolStudents;

	private ValueBinding<int> m_CollegeStudents;

	private ValueBinding<int> m_UniversityStudents;

	private ValueBinding<int> m_ElementaryEligible;

	private ValueBinding<int> m_HighSchoolEligible;

	private ValueBinding<int> m_CollegeEligible;

	private ValueBinding<int> m_UniversityEligible;

	private ValueBinding<int> m_ElementaryCapacity;

	private ValueBinding<int> m_HighSchoolCapacity;

	private ValueBinding<int> m_CollegeCapacity;

	private ValueBinding<int> m_UniversityCapacity;

	private GetterValueBinding<IndicatorValue> m_ElementaryAvailability;

	private GetterValueBinding<IndicatorValue> m_HighSchoolAvailability;

	private GetterValueBinding<IndicatorValue> m_CollegeAvailability;

	private GetterValueBinding<IndicatorValue> m_UniversityAvailability;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_SchoolQuery;

	private EntityQuery m_SchoolModifiedQuery;

	private EntityQuery m_EligibleQuery;

	private EntityQuery m_TimeDataQuery;

	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_607537787_0;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_EducationData.active && !m_ElementaryStudents.active && !m_ElementaryCapacity.active && !m_ElementaryEligible.active && !m_ElementaryAvailability.active && !m_HighSchoolStudents.active && !m_HighSchoolCapacity.active && !m_HighSchoolEligible.active && !m_HighSchoolAvailability.active && !m_CollegeStudents.active && !m_CollegeCapacity.active && !m_CollegeEligible.active && !m_CollegeAvailability.active && !m_UniversityStudents.active && !m_UniversityCapacity.active && !m_UniversityEligible.active)
			{
				return m_UniversityAvailability.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_SchoolModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		RequireForUpdate<EconomyParameterData>();
		RequireForUpdate<TimeData>();
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>());
		m_SchoolQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.School>(), ComponentType.ReadOnly<Game.Buildings.Student>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_SchoolModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.School>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_EligibleQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadWrite<Citizen>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<HasJobSeeker>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		AddBinding(m_EducationData = new RawValueBinding("educationInfo", "educationData", UpdateEducationData));
		AddBinding(m_ElementaryStudents = new ValueBinding<int>("educationInfo", "elementaryStudentCount", 0));
		AddBinding(m_HighSchoolStudents = new ValueBinding<int>("educationInfo", "highSchoolStudentCount", 0));
		AddBinding(m_CollegeStudents = new ValueBinding<int>("educationInfo", "collegeStudentCount", 0));
		AddBinding(m_UniversityStudents = new ValueBinding<int>("educationInfo", "universityStudentCount", 0));
		AddBinding(m_ElementaryEligible = new ValueBinding<int>("educationInfo", "elementaryEligible", 0));
		AddBinding(m_HighSchoolEligible = new ValueBinding<int>("educationInfo", "highSchoolEligible", 0));
		AddBinding(m_CollegeEligible = new ValueBinding<int>("educationInfo", "collegeEligible", 0));
		AddBinding(m_UniversityEligible = new ValueBinding<int>("educationInfo", "universityEligible", 0));
		AddBinding(m_ElementaryCapacity = new ValueBinding<int>("educationInfo", "elementaryCapacity", 0));
		AddBinding(m_HighSchoolCapacity = new ValueBinding<int>("educationInfo", "highSchoolCapacity", 0));
		AddBinding(m_CollegeCapacity = new ValueBinding<int>("educationInfo", "collegeCapacity", 0));
		AddBinding(m_UniversityCapacity = new ValueBinding<int>("educationInfo", "universityCapacity", 0));
		AddBinding(m_ElementaryAvailability = new GetterValueBinding<IndicatorValue>("educationInfo", "elementaryAvailability", UpdateElementaryAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_HighSchoolAvailability = new GetterValueBinding<IndicatorValue>("educationInfo", "highSchoolAvailability", UpdateHighSchoolAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_CollegeAvailability = new GetterValueBinding<IndicatorValue>("educationInfo", "collegeAvailability", UpdateCollegeAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_UniversityAvailability = new GetterValueBinding<IndicatorValue>("educationInfo", "universityAvailability", UpdateUniversityAvailability, new ValueWriter<IndicatorValue>()));
		m_Results = new NativeArray<int>(17, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		ResetResults();
		UpdateStudentCounts();
		UpdateEligibility();
		m_EducationData.Update();
		m_ElementaryStudents.Update(m_Results[9]);
		m_ElementaryCapacity.Update(m_Results[13]);
		m_ElementaryEligible.Update(m_Results[5]);
		m_HighSchoolStudents.Update(m_Results[10]);
		m_HighSchoolCapacity.Update(m_Results[14]);
		m_HighSchoolEligible.Update(m_Results[6]);
		m_CollegeStudents.Update(m_Results[11]);
		m_CollegeCapacity.Update(m_Results[15]);
		m_CollegeEligible.Update(m_Results[7]);
		m_UniversityStudents.Update(m_Results[12]);
		m_UniversityCapacity.Update(m_Results[16]);
		m_UniversityEligible.Update(m_Results[8]);
		m_ElementaryAvailability.Update();
		m_HighSchoolAvailability.Update();
		m_CollegeAvailability.Update();
		m_UniversityAvailability.Update();
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0;
		}
	}

	private void UpdateEducationData(IJsonWriter binder)
	{
		JobChunkExtensions.Schedule(new UpdateEducationDataJob
		{
			m_HouseholdHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CitizenFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_HouseholdQuery, base.Dependency).Complete();
		InfoviewsUIUtils.UpdateFiveSlicePieChartData(binder, m_Results[0], m_Results[1], m_Results[2], m_Results[3], m_Results[4]);
	}

	private void UpdateStudentCounts()
	{
		JobChunkExtensions.Schedule(new UpdateStudentCountsJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_StudentHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Student_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SchoolDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgradeFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_SchoolQuery, base.Dependency).Complete();
	}

	private void UpdateEligibility()
	{
		JobChunkExtensions.Schedule(new UpdateEligibilityJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StudentHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkerHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifierFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_EducationParameterData = __query_607537787_0.GetSingleton<EducationParameterData>(),
			m_Results = m_Results
		}, m_EligibleQuery, base.Dependency).Complete();
	}

	private IndicatorValue UpdateElementaryAvailability()
	{
		return IndicatorValue.Calculate(m_ElementaryCapacity.value, m_ElementaryEligible.value);
	}

	private IndicatorValue UpdateHighSchoolAvailability()
	{
		return IndicatorValue.Calculate(m_HighSchoolCapacity.value, m_HighSchoolEligible.value);
	}

	private IndicatorValue UpdateCollegeAvailability()
	{
		return IndicatorValue.Calculate(m_CollegeCapacity.value, m_CollegeEligible.value);
	}

	private IndicatorValue UpdateUniversityAvailability()
	{
		return IndicatorValue.Calculate(m_UniversityCapacity.value, m_UniversityEligible.value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EducationParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_607537787_0 = entityQueryBuilder2.Build(ref state);
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
	public EducationInfoviewUISystem()
	{
	}
}
