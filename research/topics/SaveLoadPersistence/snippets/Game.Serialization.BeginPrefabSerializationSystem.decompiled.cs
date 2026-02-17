using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class BeginPrefabSerializationSystem : GameSystemBase
{
	[BurstCompile]
	private struct BeginPrefabSerializationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		[NativeDisableParallelForRestriction]
		public NativeArray<Entity> m_PrefabArray;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabDataType);
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_PrefabDataType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				PrefabData prefabData = nativeArray2[i];
				enabledMask[i] = false;
				m_PrefabArray[math.select(prefabData.m_Index, m_PrefabArray.Length + prefabData.m_Index, prefabData.m_Index < 0)] = nativeArray[i];
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckSavedPrefabsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public ComponentTypeHandle<SignatureBuildingData> m_SignatureBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<PlacedSignatureBuildingData> m_PlacedSignatureBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<CollectedCityServiceBudgetData> m_CollectedCityServiceBudgetType;

		[ReadOnly]
		public BufferTypeHandle<CollectedCityServiceFeeData> m_CollectedCityServiceFeeType;

		[ReadOnly]
		public BufferTypeHandle<CollectedCityServiceUpkeepData> m_CollectedCityServiceUpkeepType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			PrefabComponents prefabComponents = (PrefabComponents)0u;
			if (chunk.Has(ref m_SignatureBuildingType))
			{
				prefabComponents |= PrefabComponents.PlacedSignatureBuilding;
			}
			PrefabComponents prefabComponents2 = (PrefabComponents)0u;
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_LockedType);
			if (chunk.Has(ref m_PlacedSignatureBuildingType))
			{
				prefabComponents2 |= PrefabComponents.PlacedSignatureBuilding;
			}
			bool flag = prefabComponents != prefabComponents2 || chunk.Has(ref m_CollectedCityServiceBudgetType) || chunk.Has(ref m_CollectedCityServiceFeeType) || chunk.Has(ref m_CollectedCityServiceUpkeepType);
			if (!flag && !enabledMask.EnableBit.IsValid)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (flag || (enabledMask.EnableBit.IsValid && !enabledMask[i]))
				{
					m_PrefabReferences.SetDirty(nativeArray[i]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetPrefabDataIndexJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_PrefabChunks;

		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		public BufferTypeHandle<LoadedIndex> m_LoadedIndexType;

		public void Execute()
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < m_PrefabChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_PrefabChunks[i];
				EnabledMask enabledMask = archetypeChunk.GetEnabledMask(ref m_PrefabDataType);
				for (int j = 0; j < archetypeChunk.Count; j++)
				{
					num += math.select(0, 1, enabledMask[j]);
				}
			}
			NativeArray<int> array = new NativeArray<int>(num, Allocator.Temp);
			num = 0;
			for (int k = 0; k < m_PrefabChunks.Length; k++)
			{
				ArchetypeChunk archetypeChunk2 = m_PrefabChunks[k];
				NativeArray<PrefabData> nativeArray = archetypeChunk2.GetNativeArray(ref m_PrefabDataType);
				BufferAccessor<LoadedIndex> bufferAccessor = archetypeChunk2.GetBufferAccessor(ref m_LoadedIndexType);
				EnabledMask enabledMask2 = archetypeChunk2.GetEnabledMask(ref m_PrefabDataType);
				for (int l = 0; l < archetypeChunk2.Count; l++)
				{
					if (enabledMask2[l])
					{
						PrefabData prefabData = nativeArray[l];
						DynamicBuffer<LoadedIndex> dynamicBuffer = bufferAccessor[l];
						dynamicBuffer.ResizeUninitialized(1);
						dynamicBuffer[0] = new LoadedIndex
						{
							m_Index = prefabData.m_Index
						};
						array[num++] = prefabData.m_Index;
						num2 += math.select(0, 1, prefabData.m_Index < 0);
					}
				}
			}
			array.Sort();
			for (int m = 0; m < m_PrefabChunks.Length; m++)
			{
				ArchetypeChunk archetypeChunk3 = m_PrefabChunks[m];
				NativeArray<PrefabData> nativeArray2 = archetypeChunk3.GetNativeArray(ref m_PrefabDataType);
				EnabledMask enabledMask3 = archetypeChunk3.GetEnabledMask(ref m_PrefabDataType);
				for (int n = 0; n < archetypeChunk3.Count; n++)
				{
					if (!enabledMask3[n])
					{
						continue;
					}
					PrefabData value = nativeArray2[n];
					int num3 = 0;
					int num4 = num;
					while (num3 < num4)
					{
						int num5 = num3 + num4 >> 1;
						int num6 = array[num5];
						if (num6 < value.m_Index)
						{
							num3 = num5 + 1;
							continue;
						}
						if (num6 > value.m_Index)
						{
							num4 = num5;
							continue;
						}
						num3 = num5;
						break;
					}
					value.m_Index = num3 - num2;
					nativeArray2[n] = value;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PlacedSignatureBuildingData> __Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CollectedCityServiceBudgetData> __Game_Simulation_CollectedCityServiceBudgetData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<CollectedCityServiceFeeData> __Game_Simulation_CollectedCityServiceFeeData_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<CollectedCityServiceUpkeepData> __Game_Simulation_CollectedCityServiceUpkeepData_RO_BufferTypeHandle;

		public BufferTypeHandle<LoadedIndex> __Game_Prefabs_LoadedIndex_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>();
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(isReadOnly: true);
			__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PlacedSignatureBuildingData>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceBudgetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedCityServiceBudgetData>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceFeeData_RO_BufferTypeHandle = state.GetBufferTypeHandle<CollectedCityServiceFeeData>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceUpkeepData_RO_BufferTypeHandle = state.GetBufferTypeHandle<CollectedCityServiceUpkeepData>(isReadOnly: true);
			__Game_Prefabs_LoadedIndex_RW_BufferTypeHandle = state.GetBufferTypeHandle<LoadedIndex>();
		}
	}

	private SaveGameSystem m_SaveGameSystem;

	private CheckPrefabReferencesSystem m_CheckPrefabReferencesSystem;

	private UpdateSystem m_UpdateSystem;

	private EntityQuery m_EnabledPrefabsQuery;

	private EntityQuery m_LoadedPrefabsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SaveGameSystem = base.World.GetOrCreateSystemManaged<SaveGameSystem>();
		m_CheckPrefabReferencesSystem = base.World.GetOrCreateSystemManaged<CheckPrefabReferencesSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_EnabledPrefabsQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>());
		m_LoadedPrefabsQuery = GetEntityQuery(ComponentType.ReadOnly<LoadedIndex>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		int length = m_LoadedPrefabsQuery.CalculateEntityCountWithoutFiltering();
		NativeArray<Entity> nativeArray = new NativeArray<Entity>(length, Allocator.TempJob);
		JobHandle dependencies = JobChunkExtensions.ScheduleParallel(new BeginPrefabSerializationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabArray = nativeArray
		}, m_LoadedPrefabsQuery, base.Dependency);
		m_CheckPrefabReferencesSystem.BeginPrefabCheck(nativeArray, isLoading: false, dependencies);
		m_UpdateSystem.Update(SystemUpdatePhase.PrefabReferences);
		if (m_SaveGameSystem.context.purpose == Purpose.SaveGame)
		{
			JobHandle dependencies3;
			JobHandle dependencies2 = JobChunkExtensions.ScheduleParallel(new CheckSavedPrefabsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SignatureBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlacedSignatureBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CollectedCityServiceBudgetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedCityServiceBudgetData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CollectedCityServiceFeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedCityServiceFeeData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CollectedCityServiceUpkeepType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedCityServiceUpkeepData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabReferences = m_CheckPrefabReferencesSystem.GetPrefabReferences(this, out dependencies3)
			}, m_LoadedPrefabsQuery, dependencies3);
			m_CheckPrefabReferencesSystem.AddPrefabReferencesUser(dependencies2);
			m_CheckPrefabReferencesSystem.Update();
		}
		m_CheckPrefabReferencesSystem.EndPrefabCheck(out var dependencies4);
		nativeArray.Dispose(dependencies4);
		dependencies4.Complete();
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> prefabChunks = m_EnabledPrefabsQuery.ToArchetypeChunkListAsync(Allocator.TempJob, dependencies4, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new SetPrefabDataIndexJob
		{
			m_PrefabChunks = prefabChunks,
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LoadedIndexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LoadedIndex_RW_BufferTypeHandle, ref base.CheckedStateRef)
		}, outJobHandle);
		prefabChunks.Dispose(jobHandle);
		base.Dependency = jobHandle;
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
	public BeginPrefabSerializationSystem()
	{
	}
}
