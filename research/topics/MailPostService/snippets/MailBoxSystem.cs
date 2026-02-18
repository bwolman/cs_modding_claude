#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class MailBoxSystem : GameSystemBase
{
	[BurstCompile]
	private struct MailBoxTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.MailBox> m_MailBoxType;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		[ReadOnly]
		public EntityArchetype m_PostVanRequestArchetype;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Routes.MailBox> nativeArray2 = chunk.GetNativeArray(ref m_MailBoxType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Game.Routes.MailBox mailBox = nativeArray2[i];
				RequestPostVanIfNeeded(unfilteredChunkIndex, entity, mailBox);
			}
		}

		private void RequestPostVanIfNeeded(int jobIndex, Entity entity, Game.Routes.MailBox mailBox)
		{
			if (mailBox.m_MailAmount >= m_PostConfigurationData.m_MailAccumulationTolerance && !m_PostVanRequestData.HasComponent(mailBox.m_CollectRequest))
			{
				PostVanRequestFlags flags = PostVanRequestFlags.Collect | PostVanRequestFlags.MailBoxTarget;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PostVanRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PostVanRequest(entity, flags, (ushort)math.min(65535, mailBox.m_MailAmount)));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
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
		public ComponentTypeHandle<Game.Routes.MailBox> __Game_Routes_MailBox_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> __Game_Simulation_PostVanRequest_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_MailBox_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.MailBox>(isReadOnly: true);
			__Game_Simulation_PostVanRequest_RO_ComponentLookup = state.GetComponentLookup<PostVanRequest>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 512u;

	private EntityQuery m_MailBoxQuery;

	private EntityQuery m_PostConfigurationQuery;

	private EntityArchetype m_PostVanRequestArchetype;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_MailBoxQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Routes.MailBox>(), ComponentType.ReadOnly<Game.Routes.TransportStop>(), ComponentType.Exclude<Game.Buildings.PostFacility>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>());
		m_PostConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PostConfigurationData>());
		m_PostVanRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PostVanRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_MailBoxQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new MailBoxTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_MailBoxType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_MailBox_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PostVanRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PostVanRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostVanRequestArchetype = m_PostVanRequestArchetype,
			m_PostConfigurationData = m_PostConfigurationQuery.GetSingleton<PostConfigurationData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_MailBoxQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public MailBoxSystem()
	{
	}
}
