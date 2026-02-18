using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class XPBuiltSystem : GameSystemBase
{
	[BurstCompile]
	public struct XPBuiltJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectDatas;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> m_SignatureBuildingDatas;

		[ReadOnly]
		public ComponentLookup<PlacedSignatureBuildingData> m_PlacedSignatureBuildingDatas;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_ServiceUpgradeDatas;

		public NativeQueue<XPGain> m_XPQueue;

		public ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<PrefabRef> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			NativeArray<PrefabRef> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity prefab = nativeArray2[i].m_Prefab;
				if (m_PlaceableObjectDatas.HasComponent(prefab) && !m_PlacedSignatureBuildingDatas.HasComponent(prefab))
				{
					PlaceableObjectData placeableObjectData = m_PlaceableObjectDatas[prefab];
					if (placeableObjectData.m_XPReward > 0)
					{
						m_XPQueue.Enqueue(new XPGain
						{
							amount = placeableObjectData.m_XPReward,
							entity = nativeArray[i],
							reason = XPReason.ServiceBuilding
						});
					}
					if (m_SignatureBuildingDatas.HasComponent(prefab))
					{
						((ParallelWriter)(ref m_CommandBuffer)).AddComponent<PlacedSignatureBuildingData>(unfilteredChunkIndex, prefab);
					}
				}
				if (m_ServiceUpgradeDatas.HasComponent(prefab))
				{
					ServiceUpgradeData serviceUpgradeData = m_ServiceUpgradeDatas[prefab];
					if (serviceUpgradeData.m_XPReward > 0)
					{
						m_XPQueue.Enqueue(new XPGain
						{
							amount = serviceUpgradeData.m_XPReward,
							entity = nativeArray[i],
							reason = XPReason.ServiceUpgrade
						});
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
	public struct XPElectricityJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ElectricityConsumer> m_ElectricityConsumers;

		public NativeQueue<XPGain> m_XPQueue;

		[ReadOnly]
		public Entity m_City;

		public ComponentLookup<XP> m_CityXPs;

		public void Execute()
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < m_ElectricityConsumers.Length; i++)
			{
				if (m_ElectricityConsumers[i].m_FulfilledConsumption > 0)
				{
					m_XPQueue.Enqueue(new XPGain
					{
						amount = kElectricityGridXPBonus,
						entity = Entity.Null,
						reason = XPReason.ElectricityNetwork
					});
					XP xP = m_CityXPs[m_City];
					xP.m_XPRewardRecord |= XPRewardFlags.ElectricityGridBuilt;
					m_CityXPs[m_City] = xP;
					break;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlacedSignatureBuildingData> __Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		public ComponentLookup<XP> __Game_City_XP_RW_ComponentLookup;

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
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabRef>(false);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PlaceableObjectData>(true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SignatureBuildingData>(true);
			__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PlacedSignatureBuildingData>(true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ServiceUpgradeData>(true);
			__Game_City_XP_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<XP>(false);
		}
	}

	private EntityQuery m_BuiltGroup;

	private EntityQuery m_ElectricityGroup;

	private XPSystem m_XPSystem;

	private ToolSystem m_ToolSystem;

	private CitySystem m_CitySystem;

	private ModificationEndBarrier m_ModificationEndBarrier;

	private static readonly int kElectricityGridXPBonus = 25;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_XPSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<XPSystem>();
		m_CitySystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CitySystem>();
		m_ToolSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ToolSystem>();
		m_ModificationEndBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_BuiltGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<PrefabRef>(),
			ComponentType.ReadOnly<Created>(),
			ComponentType.Exclude<Temp>()
		});
		m_ElectricityGroup = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<ElectricityConsumer>(),
			ComponentType.Exclude<Temp>()
		});
		((ComponentSystemBase)this).RequireAnyForUpdate((EntityQuery[])(object)new EntityQuery[2] { m_BuiltGroup, m_ElectricityGroup });
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		if (m_ToolSystem.actionMode.IsGame())
		{
			JobHandle deps;
			NativeQueue<XPGain> queue = m_XPSystem.GetQueue(out deps);
			if (!((EntityQuery)(ref m_BuiltGroup)).IsEmptyIgnoreFilter)
			{
				XPBuiltJob xPBuiltJob = new XPBuiltJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
					m_PlaceableObjectDatas = InternalCompilerInterface.GetComponentLookup<PlaceableObjectData>(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
					m_SignatureBuildingDatas = InternalCompilerInterface.GetComponentLookup<SignatureBuildingData>(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
					m_PlacedSignatureBuildingDatas = InternalCompilerInterface.GetComponentLookup<PlacedSignatureBuildingData>(ref __TypeHandle.__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
					m_ServiceUpgradeDatas = InternalCompilerInterface.GetComponentLookup<ServiceUpgradeData>(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
					m_XPQueue = queue
				};
				EntityCommandBuffer val = m_ModificationEndBarrier.CreateCommandBuffer();
				xPBuiltJob.m_CommandBuffer = ((EntityCommandBuffer)(ref val)).AsParallelWriter();
				XPBuiltJob xPBuiltJob2 = xPBuiltJob;
				((SystemBase)this).Dependency = JobChunkExtensions.Schedule<XPBuiltJob>(xPBuiltJob2, m_BuiltGroup, JobHandle.CombineDependencies(deps, ((SystemBase)this).Dependency));
			}
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			if ((((EntityManager)(ref entityManager)).GetComponentData<XP>(m_CitySystem.City).m_XPRewardRecord & XPRewardFlags.ElectricityGridBuilt) == 0 && !((EntityQuery)(ref m_ElectricityGroup)).IsEmptyIgnoreFilter)
			{
				XPElectricityJob xPElectricityJob = new XPElectricityJob
				{
					m_ElectricityConsumers = ((EntityQuery)(ref m_ElectricityGroup)).ToComponentDataArray<ElectricityConsumer>(AllocatorHandle.op_Implicit((Allocator)3)),
					m_City = m_CitySystem.City,
					m_CityXPs = InternalCompilerInterface.GetComponentLookup<XP>(ref __TypeHandle.__Game_City_XP_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
					m_XPQueue = queue
				};
				((SystemBase)this).Dependency = IJobExtensions.Schedule<XPElectricityJob>(xPElectricityJob, ((SystemBase)this).Dependency);
			}
			m_XPSystem.AddQueueWriter(((SystemBase)this).Dependency);
		}
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
	public XPBuiltSystem()
	{
	}
}
