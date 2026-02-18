using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class UnlockSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckUnlockRequirementsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<UnlockRequirement> m_UnlockRequirementType;

		[ReadOnly]
		public ComponentLookup<Locked> m_LockedData;

		public NativeQueue<Entity>.ParallelWriter m_UnlockQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<UnlockRequirement> bufferAccessor = chunk.GetBufferAccessor(ref m_UnlockRequirementType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				DynamicBuffer<UnlockRequirement> dynamicBuffer = bufferAccessor[nextIndex];
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (flag)
					{
						break;
					}
					UnlockRequirement unlockRequirement = dynamicBuffer[i];
					bool flag4 = m_LockedData.HasEnabledComponent(unlockRequirement.m_Prefab);
					bool flag5 = (unlockRequirement.m_Flags & UnlockFlags.RequireAll) != 0;
					bool flag6 = (unlockRequirement.m_Flags & UnlockFlags.RequireAny) != 0;
					flag = flag || (flag4 && flag5);
					flag2 = flag2 || (flag4 && flag6);
					flag3 = flag3 || (!flag4 && flag6);
				}
				if (!flag && (flag3 || !flag2))
				{
					Entity value = nativeArray[nextIndex];
					m_UnlockQueue.Enqueue(value);
				}
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
		public BufferTypeHandle<UnlockRequirement> __Game_Prefabs_UnlockRequirement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_UnlockRequirement_RO_BufferTypeHandle = state.GetBufferTypeHandle<UnlockRequirement>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_LockedQuery;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_EventQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private bool m_Loaded;

	private ILog m_Log;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_LockedQuery = GetEntityQuery(ComponentType.ReadOnly<Locked>(), ComponentType.ReadOnly<UnlockRequirement>());
		m_UpdatedQuery = GetEntityQuery(ComponentType.ReadOnly<Locked>(), ComponentType.ReadOnly<UnlockRequirement>(), ComponentType.ReadOnly<Updated>());
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		m_Log = LogManager.GetLogger("Unlocking");
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	protected override void OnGameLoaded(Context context)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		if (!ProcessEvents() && !loaded && m_UpdatedQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeQueue<Entity> nativeQueue = new NativeQueue<Entity>(Allocator.TempJob);
		try
		{
			while (true)
			{
				JobChunkExtensions.ScheduleParallel(new CheckUnlockRequirementsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_UnlockRequirementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_LockedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
					m_UnlockQueue = nativeQueue.AsParallelWriter()
				}, m_LockedQuery, default(JobHandle)).Complete();
				if (nativeQueue.Count == 0)
				{
					break;
				}
				Entity item;
				while (nativeQueue.TryDequeue(out item))
				{
					UnlockPrefab(item, createEvent: true);
				}
			}
		}
		finally
		{
			nativeQueue.Dispose();
		}
	}

	private bool ProcessEvents()
	{
		if (m_EventQuery.IsEmptyIgnoreFilter)
		{
			return false;
		}
		bool result = false;
		NativeArray<Unlock> nativeArray = m_EventQuery.ToComponentDataArray<Unlock>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				if (base.EntityManager.HasEnabledComponent<Locked>(prefab))
				{
					UnlockPrefab(prefab, createEvent: false);
					result = true;
				}
			}
			return result;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void UnlockPrefab(Entity unlock, bool createEvent)
	{
		base.EntityManager.SetComponentEnabled<Locked>(unlock, value: false);
		if (createEvent)
		{
			Entity entity = base.EntityManager.CreateEntity(m_UnlockEventArchetype);
			base.EntityManager.SetComponentData(entity, new Unlock(unlock));
		}
		if (!base.EntityManager.HasEnabledComponent<PrefabData>(unlock))
		{
			PrefabID obsoleteID = m_PrefabSystem.GetObsoleteID(unlock);
			m_Log.DebugFormat("Prefab unlocked: {0}", obsoleteID);
		}
		else
		{
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(unlock);
			m_Log.DebugFormat("Prefab unlocked: {0}", prefab);
		}
	}

	public bool IsLocked(PrefabBase prefab)
	{
		return m_PrefabSystem.HasEnabledComponent<Locked>(prefab);
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
	public UnlockSystem()
	{
	}
}
