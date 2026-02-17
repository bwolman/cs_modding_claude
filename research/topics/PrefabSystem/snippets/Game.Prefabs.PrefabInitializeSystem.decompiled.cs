using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class PrefabInitializeSystem : GameSystemBase
{
	private struct ListItem
	{
		public readonly Entity m_Entity;

		public readonly PrefabBase m_Prefab;

		public ListItem(Entity entity, PrefabBase prefab)
		{
			m_Entity = entity;
			m_Prefab = prefab;
		}
	}

	private struct QueueItem
	{
		public readonly PrefabBase m_Prefab;

		public readonly PrefabBase m_ParentPrefab;

		public readonly ComponentBase m_ParentComponent;

		public QueueItem(PrefabBase prefab, PrefabBase parentPrefab, ComponentBase parentComponent)
		{
			m_Prefab = prefab;
			m_ParentPrefab = parentPrefab;
			m_ParentComponent = parentComponent;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
		}
	}

	private EntityQuery m_PrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<PrefabData>());
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			List<ListItem> list = new List<ListItem>();
			Queue<QueueItem> queue = new Queue<QueueItem>();
			HashSet<PrefabBase> hashSet = new HashSet<PrefabBase>();
			List<PrefabBase> dependencies = new List<PrefabBase>();
			List<ComponentBase> components = new List<ComponentBase>();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<PrefabData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(nativeArray3[j]);
					list.Add(new ListItem(nativeArray2[j], prefab));
					hashSet.Add(prefab);
				}
			}
			foreach (ListItem item in list)
			{
				InitializePrefab(item.m_Entity, item.m_Prefab, queue, hashSet, dependencies, components);
			}
			QueueItem result;
			while (queue.TryDequeue(out result))
			{
				if (m_PrefabSystem.AddPrefab(result.m_Prefab, null, result.m_ParentPrefab, result.m_ParentComponent))
				{
					Entity entity = m_PrefabSystem.GetEntity(result.m_Prefab);
					InitializePrefab(entity, result.m_Prefab, queue, hashSet, dependencies, components);
					list.Add(new ListItem(entity, result.m_Prefab));
				}
			}
			foreach (ListItem item2 in list)
			{
				LateInitializePrefab(item2.m_Entity, item2.m_Prefab, dependencies, components);
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void InitializePrefab(Entity entity, PrefabBase prefab, Queue<QueueItem> queue, HashSet<PrefabBase> prefabSet, List<PrefabBase> dependencies, List<ComponentBase> components)
	{
		prefab.GetComponents(components);
		for (int i = 0; i < components.Count; i++)
		{
			ComponentBase componentBase = components[i];
			try
			{
				componentBase.Initialize(base.EntityManager, entity);
				componentBase.GetDependencies(dependencies);
			}
			catch (Exception exception)
			{
				bool showsErrorsInUI = COSystemBase.baseLog.showsErrorsInUI;
				if (GameManager.instance.gameMode.IsEditor())
				{
					COSystemBase.baseLog.showsErrorsInUI = false;
				}
				COSystemBase.baseLog.ErrorFormat(prefab, exception, "Error when initializing prefab: {0} ({1})", prefab.name, (prefab.asset != null) ? ((object)prefab.asset) : ((object)"No asset"));
				COSystemBase.baseLog.showsErrorsInUI = showsErrorsInUI;
			}
			finally
			{
				foreach (PrefabBase dependency in dependencies)
				{
					if (dependency == null || prefabSet.Add(dependency))
					{
						queue.Enqueue(new QueueItem(dependency, prefab, componentBase));
					}
				}
				dependencies.Clear();
			}
		}
		components.Clear();
	}

	private void LateInitializePrefab(Entity entity, PrefabBase prefab, List<PrefabBase> dependencies, List<ComponentBase> components)
	{
		bool flag = m_PrefabSystem.IsUnlockable(prefab);
		bool canIgnoreUnlockDependencies = prefab.canIgnoreUnlockDependencies;
		prefab.GetComponents(components);
		UnlockableBase unlockableBase = null;
		if (flag)
		{
			unlockableBase = prefab.GetComponent<UnlockableBase>();
		}
		for (int i = 0; i < components.Count; i++)
		{
			ComponentBase componentBase = components[i];
			if (!(componentBase != unlockableBase))
			{
				continue;
			}
			try
			{
				componentBase.LateInitialize(base.EntityManager, entity);
				if (!canIgnoreUnlockDependencies || !componentBase.ignoreUnlockDependencies)
				{
					componentBase.GetDependencies(dependencies);
				}
			}
			catch (Exception exception)
			{
				bool showsErrorsInUI = COSystemBase.baseLog.showsErrorsInUI;
				if (GameManager.instance.gameMode.IsEditor())
				{
					COSystemBase.baseLog.showsErrorsInUI = false;
				}
				COSystemBase.baseLog.ErrorFormat(prefab, exception, "Error when initializing prefab: {0} ({1})", prefab.name, (prefab.asset != null) ? ((object)prefab.asset) : ((object)"No asset"));
				COSystemBase.baseLog.showsErrorsInUI = showsErrorsInUI;
			}
		}
		if (flag)
		{
			try
			{
				if (unlockableBase != null)
				{
					unlockableBase.LateInitialize(base.EntityManager, entity, dependencies);
				}
				else
				{
					UnlockableBase.DefaultLateInitialize(base.EntityManager, entity, dependencies);
				}
			}
			catch (Exception exception2)
			{
				bool showsErrorsInUI2 = COSystemBase.baseLog.showsErrorsInUI;
				if (GameManager.instance.gameMode.IsEditor())
				{
					COSystemBase.baseLog.showsErrorsInUI = false;
				}
				COSystemBase.baseLog.ErrorFormat(prefab, exception2, "Error when initializing prefab: {0} ({1})", prefab.name, (prefab.asset != null) ? ((object)prefab.asset) : ((object)"No asset"));
				COSystemBase.baseLog.showsErrorsInUI = showsErrorsInUI2;
			}
		}
		components.Clear();
		dependencies.Clear();
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
	public PrefabInitializeSystem()
	{
	}
}
