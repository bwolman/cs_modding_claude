using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PropertyRenterRemoveSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateRentersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_PropertyDatas;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		public uint m_UpdateFrameIndex;

		public NativeQueue<RemoveData>.ParallelWriter m_RemoveData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_PropertyRenterType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity property = nativeArray2[i].m_Property;
				if (!m_Buildings.HasComponent(property))
				{
					m_RemoveData.Enqueue(new RemoveData
					{
						m_Renter = nativeArray[i]
					});
					continue;
				}
				Entity prefab = m_Prefabs[property].m_Prefab;
				if (m_PropertyDatas.HasComponent(prefab) && m_Renters.HasBuffer(property))
				{
					BuildingPropertyData buildingPropertyData = m_PropertyDatas[prefab];
					if (m_Renters[property].Length > buildingPropertyData.CountProperties())
					{
						m_RemoveData.Enqueue(new RemoveData
						{
							m_Property = property,
							m_Renter = nativeArray[i]
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

	private struct RemoveData
	{
		public Entity m_Property;

		public Entity m_Renter;
	}

	[BurstCompile]
	private struct RemoveRentersJob : IJob
	{
		[ReadOnly]
		public EntityArchetype m_RentEventArchetype;

		public BufferLookup<Renter> m_Renters;

		public NativeQueue<RemoveData> m_RemoveData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeHashSet<Entity> nativeHashSet = default(NativeHashSet<Entity>);
			RemoveData item;
			while (m_RemoveData.TryDequeue(out item))
			{
				if (item.m_Property != Entity.Null)
				{
					if (m_Renters.TryGetBuffer(item.m_Property, out var bufferData))
					{
						for (int i = 0; i < bufferData.Length; i++)
						{
							if (bufferData[i].m_Renter == item.m_Renter)
							{
								bufferData.RemoveAt(i);
								m_CommandBuffer.RemoveComponent<PropertyRenter>(item.m_Renter);
								break;
							}
						}
					}
					if (!nativeHashSet.IsCreated)
					{
						nativeHashSet = new NativeHashSet<Entity>(10, Allocator.Temp);
					}
					if (nativeHashSet.Add(item.m_Property))
					{
						Entity e = m_CommandBuffer.CreateEntity(m_RentEventArchetype);
						m_CommandBuffer.SetComponent(e, new RentersUpdated(item.m_Property));
					}
				}
				else
				{
					UnityEngine.Debug.Log($"{item.m_Renter.Index} removed renter since building does not exist");
					m_CommandBuffer.RemoveComponent<PropertyRenter>(item.m_Renter);
				}
			}
			if (nativeHashSet.IsCreated)
			{
				nativeHashSet.Dispose();
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
		}
	}

	private EntityQuery m_RenterGroup;

	private EntityArchetype m_RentEventArchetype;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_RenterGroup = GetEntityQuery(ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<UpdateFrame>());
		m_RentEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<RentersUpdated>());
		RequireForUpdate(m_RenterGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		NativeQueue<RemoveData> removeData = new NativeQueue<RemoveData>(Allocator.TempJob);
		UpdateRentersJob jobData = new UpdateRentersJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_RemoveData = removeData.AsParallelWriter()
		};
		RemoveRentersJob jobData2 = new RemoveRentersJob
		{
			m_RentEventArchetype = m_RentEventArchetype,
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref base.CheckedStateRef),
			m_RemoveData = removeData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_RenterGroup, base.Dependency);
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		removeData.Dispose(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public PropertyRenterRemoveSystem()
	{
	}
}
