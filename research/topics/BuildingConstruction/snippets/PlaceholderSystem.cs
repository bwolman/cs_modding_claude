using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class PlaceholderSystem : GameSystemBase
{
	[BurstCompile]
	private struct PlaceholderJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_AreaNodeType;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_PrefabRequirementElements;

		[ReadOnly]
		public Entity m_Theme;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			if (nativeArray.Length != 0 && nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Owner owner = nativeArray[i];
					Owner componentData;
					while (m_OwnerData.TryGetComponent(owner.m_Owner, out componentData))
					{
						owner = componentData;
					}
					if (!m_PlaceholderData.HasComponent(owner.m_Owner))
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, owner.m_Owner, default(Updated));
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_AreaNodeType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				Entity e = nativeArray3[j];
				PrefabRef prefabRef = nativeArray4[j];
				Entity entity = Entity.Null;
				if (m_PrefabPlaceholderElements.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					int num = 0;
					for (int k = 0; k < bufferData.Length; k++)
					{
						PlaceholderObjectElement placeholder = bufferData[k];
						if (GetVariationProbability(placeholder, out var probability))
						{
							num += probability;
							if (random.NextInt(num) < probability)
							{
								entity = placeholder.m_Object;
							}
						}
					}
				}
				CreationDefinition component;
				if (entity != Entity.Null)
				{
					component = new CreationDefinition
					{
						m_Prefab = entity
					};
					component.m_Flags |= CreationFlags.Permanent | CreationFlags.Native;
					component.m_RandomSeed = random.NextInt();
					if (CollectionUtils.TryGet(nativeArray, j, out var value))
					{
						component.m_Owner = value.m_Owner;
						while (!m_PlaceholderData.HasComponent(value.m_Owner))
						{
							Entity owner2 = value.m_Owner;
							if (m_OwnerData.TryGetComponent(owner2, out value))
							{
								continue;
							}
							goto IL_01dc;
						}
						continue;
					}
					goto IL_01dc;
				}
				goto IL_02ca;
				IL_01dc:
				Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, component);
				if (CollectionUtils.TryGet(nativeArray2, j, out var value2))
				{
					ObjectDefinition component2 = new ObjectDefinition
					{
						m_ParentMesh = -1,
						m_Position = value2.m_Position,
						m_Rotation = value2.m_Rotation,
						m_LocalPosition = value2.m_Position,
						m_LocalRotation = value2.m_Rotation
					};
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, component2);
				}
				if (CollectionUtils.TryGet(bufferAccessor, j, out var value3))
				{
					DynamicBuffer<Node> dynamicBuffer = m_CommandBuffer.AddBuffer<Node>(unfilteredChunkIndex, e2);
					if (value3.Length != 0)
					{
						dynamicBuffer.Capacity = value3.Length + 1;
						dynamicBuffer.AddRange(value3.AsNativeArray());
						dynamicBuffer.Add(value3[0]);
					}
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, default(Updated));
				goto IL_02ca;
				IL_02ca:
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Deleted));
			}
		}

		private bool GetVariationProbability(PlaceholderObjectElement placeholder, out int probability)
		{
			probability = 100;
			if (m_PrefabRequirementElements.TryGetBuffer(placeholder.m_Object, out var bufferData))
			{
				int num = -1;
				bool flag = true;
				for (int i = 0; i < bufferData.Length; i++)
				{
					ObjectRequirementElement objectRequirementElement = bufferData[i];
					if (objectRequirementElement.m_Group != num)
					{
						if (!flag)
						{
							break;
						}
						num = objectRequirementElement.m_Group;
						flag = false;
					}
					flag |= m_Theme == objectRequirementElement.m_Requirement;
				}
				if (!flag)
				{
					return false;
				}
			}
			if (m_PrefabSpawnableObjectData.TryGetComponent(placeholder.m_Object, out var componentData))
			{
				probability = componentData.m_Probability;
			}
			return true;
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_EntityQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EntityQuery = GetEntityQuery(ComponentType.ReadOnly<Placeholder>());
		RequireForUpdate(m_EntityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LoadGameSystem.context.purpose == Purpose.NewGame)
		{
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			JobChunkExtensions.ScheduleParallel(new PlaceholderJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AreaNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabRequirementElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Theme = m_CityConfigurationSystem.defaultTheme,
				m_RandomSeed = RandomSeed.Next(),
				m_CommandBuffer = entityCommandBuffer.AsParallelWriter()
			}, m_EntityQuery, base.Dependency).Complete();
			entityCommandBuffer.Playback(base.EntityManager);
			entityCommandBuffer.Dispose();
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
	public PlaceholderSystem()
	{
	}
}
