using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class ServiceUpgradeSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			__Game_Common_Deleted_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Deleted>(true);
			__Game_Common_Owner_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Owner>(true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ServiceUpgrade>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabRef>(true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<InstalledUpgrade>(true);
		}
	}

	private EntityQuery m_UpgradeQuery;

	private EntityQuery m_UpgradePrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private ModificationBarrier4 m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_PrefabSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ModificationBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ModificationBarrier4>();
		EntityQueryDesc[] array = new EntityQueryDesc[2];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<ServiceUpgrade>(),
			ComponentType.ReadOnly<Object>()
		};
		val.Any = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Created>(),
			ComponentType.ReadOnly<Deleted>()
		};
		val.None = (ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<Temp>() };
		array[0] = val;
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<InstalledUpgrade>(),
			ComponentType.ReadOnly<Deleted>()
		};
		val.None = (ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<Temp>() };
		array[1] = val;
		m_UpgradeQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		m_UpgradePrefabQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<ServiceUpgradeData>(),
			ComponentType.ReadOnly<PrefabData>()
		});
		((ComponentSystemBase)this).RequireForUpdate(m_UpgradeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		EntityCommandBuffer commandBuffer = m_ModificationBarrier.CreateCommandBuffer();
		ComponentTypeHandle<Deleted> componentTypeHandle = InternalCompilerInterface.GetComponentTypeHandle<Deleted>(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<Owner> componentTypeHandle2 = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<ServiceUpgrade> componentTypeHandle3 = InternalCompilerInterface.GetComponentTypeHandle<ServiceUpgrade>(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		ComponentTypeHandle<PrefabRef> componentTypeHandle4 = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		BufferTypeHandle<InstalledUpgrade> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle<InstalledUpgrade>(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef);
		NativeArray<ArchetypeChunk> val = ((EntityQuery)(ref m_UpgradeQuery)).ToArchetypeChunkArray(AllocatorHandle.op_Implicit((Allocator)3));
		try
		{
			((SystemBase)this).CompleteDependency();
			for (int i = 0; i < val.Length; i++)
			{
				ArchetypeChunk val2 = val[i];
				if (((ArchetypeChunk)(ref val2)).Has<ServiceUpgrade>(ref componentTypeHandle3))
				{
					NativeArray<Owner> nativeArray = ((ArchetypeChunk)(ref val2)).GetNativeArray<Owner>(ref componentTypeHandle2);
					NativeArray<PrefabRef> nativeArray2 = ((ArchetypeChunk)(ref val2)).GetNativeArray<PrefabRef>(ref componentTypeHandle4);
					if (((ArchetypeChunk)(ref val2)).Has<Deleted>(ref componentTypeHandle))
					{
						for (int j = 0; j < nativeArray.Length; j++)
						{
							UpgradeRemoved(commandBuffer, nativeArray[j], nativeArray2[j]);
						}
					}
					else
					{
						for (int k = 0; k < nativeArray.Length; k++)
						{
							UpgradeInstalled(commandBuffer, nativeArray[k], nativeArray2[k]);
						}
					}
				}
				else
				{
					BufferAccessor<InstalledUpgrade> bufferAccessor = ((ArchetypeChunk)(ref val2)).GetBufferAccessor<InstalledUpgrade>(ref bufferTypeHandle);
					for (int l = 0; l < bufferAccessor.Length; l++)
					{
						OwnerDeleted(commandBuffer, bufferAccessor[l]);
					}
				}
			}
		}
		finally
		{
			val.Dispose();
		}
	}

	private void UpgradeInstalled(EntityCommandBuffer commandBuffer, Owner owner, PrefabRef prefabRef)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		List<ComponentBase> components = m_PrefabSystem.GetPrefab<PrefabBase>(prefabRef).components;
		HashSet<ComponentType> val = new HashSet<ComponentType>();
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] is IServiceUpgrade serviceUpgrade)
			{
				serviceUpgrade.GetUpgradeComponents(val);
			}
		}
		Enumerator<ComponentType> enumerator = val.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				ComponentType current = enumerator.Current;
				EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
				if (!((EntityManager)(ref entityManager)).HasComponent(owner.m_Owner, current))
				{
					((EntityCommandBuffer)(ref commandBuffer)).AddComponent(owner.m_Owner, current);
				}
			}
		}
		finally
		{
			((System.IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
		}
	}

	private void UpgradeRemoved(EntityCommandBuffer commandBuffer, Owner owner, PrefabRef prefabRef)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		if (((EntityManager)(ref entityManager)).HasComponent<Deleted>(owner.m_Owner))
		{
			return;
		}
		HashSet<ComponentType> val = new HashSet<ComponentType>();
		HashSet<ComponentType> val2 = new HashSet<ComponentType>();
		HashSet<IServiceUpgrade> val3 = new HashSet<IServiceUpgrade>();
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabRef, out var prefab))
		{
			List<ComponentBase> components = prefab.components;
			for (int i = 0; i < components.Count; i++)
			{
				if (components[i] is IServiceUpgrade serviceUpgrade)
				{
					serviceUpgrade.GetUpgradeComponents(val);
					val3.Add(serviceUpgrade);
				}
			}
		}
		else
		{
			NativeArray<PrefabData> val4 = ((EntityQuery)(ref m_UpgradePrefabQuery)).ToComponentDataArray<PrefabData>(AllocatorHandle.op_Implicit((Allocator)2));
			Enumerator<PrefabData> enumerator = val4.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					PrefabData current = enumerator.Current;
					List<ComponentBase> components2 = m_PrefabSystem.GetPrefab<PrefabBase>(current).components;
					for (int j = 0; j < components2.Count; j++)
					{
						if (components2[j] is IServiceUpgrade serviceUpgrade2)
						{
							serviceUpgrade2.GetUpgradeComponents(val);
							val3.Add(serviceUpgrade2);
						}
					}
				}
			}
			finally
			{
				((System.IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
			}
			val4.Dispose();
		}
		entityManager = ((ComponentSystemBase)this).EntityManager;
		PrefabRef componentData = ((EntityManager)(ref entityManager)).GetComponentData<PrefabRef>(owner.m_Owner);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		DynamicBuffer<InstalledUpgrade> buffer = ((EntityManager)(ref entityManager)).GetBuffer<InstalledUpgrade>(owner.m_Owner, true);
		if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(componentData, out var prefab2))
		{
			return;
		}
		List<ComponentBase> components3 = prefab2.components;
		for (int k = 0; k < components3.Count; k++)
		{
			components3[k].GetArchetypeComponents(val2);
		}
		Enumerator<InstalledUpgrade> enumerator2 = buffer.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				InstalledUpgrade current2 = enumerator2.Current;
				entityManager = ((ComponentSystemBase)this).EntityManager;
				PrefabRef componentData2 = ((EntityManager)(ref entityManager)).GetComponentData<PrefabRef>(current2.m_Upgrade);
				if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(componentData2, out var prefab3))
				{
					continue;
				}
				List<ComponentBase> components4 = prefab3.components;
				for (int l = 0; l < components4.Count; l++)
				{
					if (components4[l] is IServiceUpgrade serviceUpgrade3)
					{
						serviceUpgrade3.GetUpgradeComponents(val2);
						val3.Add(serviceUpgrade3);
					}
				}
			}
		}
		finally
		{
			((System.IDisposable)enumerator2/*cast due to .constrained prefix*/).Dispose();
		}
		Enumerator<ComponentType> enumerator3 = val.GetEnumerator();
		try
		{
			while (enumerator3.MoveNext())
			{
				ComponentType current3 = enumerator3.Current;
				if (!val2.Contains(current3))
				{
					entityManager = ((ComponentSystemBase)this).EntityManager;
					if (((EntityManager)(ref entityManager)).HasComponent(owner.m_Owner, current3))
					{
						((EntityCommandBuffer)(ref commandBuffer)).RemoveComponent(owner.m_Owner, current3);
					}
				}
			}
		}
		finally
		{
			((System.IDisposable)enumerator3/*cast due to .constrained prefix*/).Dispose();
		}
		Enumerator<IServiceUpgrade> enumerator4 = val3.GetEnumerator();
		try
		{
			while (enumerator4.MoveNext())
			{
				enumerator4.Current.DoActionWithOwnerAfterRemove(((ComponentSystemBase)this).EntityManager, owner.m_Owner);
			}
		}
		finally
		{
			((System.IDisposable)enumerator4/*cast due to .constrained prefix*/).Dispose();
		}
	}

	private void OwnerDeleted(EntityCommandBuffer commandBuffer, DynamicBuffer<InstalledUpgrade> installedUpgrades)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < installedUpgrades.Length; i++)
		{
			Entity upgrade = installedUpgrades[i].m_Upgrade;
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasComponent<Object>(upgrade))
			{
				entityManager = ((ComponentSystemBase)this).EntityManager;
				if (!((EntityManager)(ref entityManager)).HasComponent<Deleted>(upgrade))
				{
					((EntityCommandBuffer)(ref commandBuffer)).AddComponent<Deleted>(upgrade, default(Deleted));
				}
			}
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
	public ServiceUpgradeSystem()
	{
	}
}
