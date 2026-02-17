using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class RandomTrafficDispatchSystem : GameSystemBase
{
	private struct VehicleDispatch
	{
		public Entity m_Request;

		public Entity m_Source;

		public VehicleDispatch(Entity request, Entity source)
		{
			m_Request = request;
			m_Source = source;
		}
	}

	[BurstCompile]
	private struct RandomTrafficDispatchJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<RandomTrafficRequest> m_RandomTrafficRequestType;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> m_DispatchedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentLookup<RandomTrafficRequest> m_RandomTrafficRequestData;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TrafficSpawner> m_TrafficSpawnerData;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_NextUpdateFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<VehicleDispatch>.ParallelWriter m_VehicleDispatches;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			if (index == m_NextUpdateFrameIndex && !chunk.Has(ref m_DispatchedType) && !chunk.Has(ref m_PathInformationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<RandomTrafficRequest> nativeArray2 = chunk.GetNativeArray(ref m_RandomTrafficRequestType);
				NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref m_ServiceRequestType);
				Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					RandomTrafficRequest trafficRequest = nativeArray2[i];
					ServiceRequest serviceRequest = nativeArray3[i];
					if (!ValidateTarget(entity, trafficRequest.m_Target))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
						continue;
					}
					if (SimulationUtils.TickServiceRequest(ref serviceRequest))
					{
						FindVehicleSource(unfilteredChunkIndex, ref random, entity, trafficRequest);
					}
					nativeArray3[i] = serviceRequest;
				}
			}
			if (index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref m_DispatchedType);
			NativeArray<RandomTrafficRequest> nativeArray5 = chunk.GetNativeArray(ref m_RandomTrafficRequestType);
			NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref m_ServiceRequestType);
			if (nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity2 = nativeArray7[j];
					Dispatched dispatched = nativeArray4[j];
					RandomTrafficRequest randomTrafficRequest = nativeArray5[j];
					ServiceRequest serviceRequest2 = nativeArray6[j];
					if (ValidateHandler(entity2, dispatched.m_Handler))
					{
						serviceRequest2.m_Cooldown = 0;
					}
					else if (serviceRequest2.m_Cooldown == 0)
					{
						serviceRequest2.m_Cooldown = 1;
					}
					else
					{
						if (!ValidateTarget(entity2, randomTrafficRequest.m_Target))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity2);
							continue;
						}
						ResetFailedRequest(unfilteredChunkIndex, entity2, dispatched: true, ref serviceRequest2);
					}
					nativeArray6[j] = serviceRequest2;
				}
				return;
			}
			NativeArray<PathInformation> nativeArray8 = chunk.GetNativeArray(ref m_PathInformationType);
			if (nativeArray8.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray9 = chunk.GetNativeArray(m_EntityType);
			for (int k = 0; k < nativeArray5.Length; k++)
			{
				Entity entity3 = nativeArray9[k];
				RandomTrafficRequest randomTrafficRequest2 = nativeArray5[k];
				PathInformation pathInformation = nativeArray8[k];
				ServiceRequest serviceRequest3 = nativeArray6[k];
				if (!ValidateTarget(entity3, randomTrafficRequest2.m_Target))
				{
					m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
					continue;
				}
				if (pathInformation.m_Origin != Entity.Null)
				{
					DispatchVehicle(unfilteredChunkIndex, entity3, pathInformation);
				}
				else
				{
					ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
				}
				nativeArray6[k] = serviceRequest3;
			}
		}

		private bool ValidateHandler(Entity entity, Entity handler)
		{
			if (m_ServiceDispatches.TryGetBuffer(handler, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (bufferData[i].m_Request == entity)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ValidateTarget(Entity entity, Entity target)
		{
			if (!m_TrafficSpawnerData.TryGetComponent(target, out var componentData))
			{
				return false;
			}
			if (componentData.m_TrafficRequest != entity)
			{
				if (m_RandomTrafficRequestData.HasComponent(componentData.m_TrafficRequest))
				{
					return false;
				}
				componentData.m_TrafficRequest = entity;
				m_TrafficSpawnerData[target] = componentData;
			}
			return true;
		}

		private void ResetFailedRequest(int jobIndex, Entity entity, bool dispatched, ref ServiceRequest serviceRequest)
		{
			SimulationUtils.ResetFailedRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<PathElement>(jobIndex, entity);
			if (dispatched)
			{
				m_CommandBuffer.RemoveComponent<Dispatched>(jobIndex, entity);
			}
		}

		private void DispatchVehicle(int jobIndex, Entity entity, PathInformation pathInformation)
		{
			VehicleDispatch value = new VehicleDispatch(entity, pathInformation.m_Origin);
			m_VehicleDispatches.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(pathInformation.m_Origin));
		}

		private void FindVehicleSource(int jobIndex, ref Random random, Entity requestEntity, RandomTrafficRequest trafficRequest)
		{
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_IgnoredRules = (RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget a = new SetupQueueTarget
			{
				m_Type = SetupTargetType.RandomTraffic,
				m_RoadTypes = trafficRequest.m_RoadType,
				m_TrackTypes = trafficRequest.m_TrackType,
				m_Entity = trafficRequest.m_Target
			};
			SetupQueueTarget b = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_RoadTypes = trafficRequest.m_RoadType,
				m_TrackTypes = trafficRequest.m_TrackType,
				m_Entity = trafficRequest.m_Target
			};
			if ((trafficRequest.m_RoadType & RoadTypes.Car) != RoadTypes.None)
			{
				parameters.m_MaxSpeed = math.max(parameters.m_MaxSpeed, 111.111115f);
				parameters.m_Methods |= PathMethod.Road;
				a.m_Methods |= PathMethod.Road;
				b.m_Methods |= PathMethod.Road;
				if ((trafficRequest.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) != 0)
				{
					parameters.m_Methods |= PathMethod.CargoLoading;
					a.m_Methods |= PathMethod.CargoLoading;
					b.m_Methods |= PathMethod.CargoLoading;
				}
				if ((int)trafficRequest.m_SizeClass <= 1)
				{
					parameters.m_Methods |= PathMethod.MediumRoad;
					a.m_Methods |= PathMethod.MediumRoad;
					b.m_Methods |= PathMethod.MediumRoad;
				}
			}
			if ((trafficRequest.m_RoadType & RoadTypes.Airplane) != RoadTypes.None)
			{
				parameters.m_MaxSpeed = math.max(parameters.m_MaxSpeed, 277.77777f);
				parameters.m_Methods |= PathMethod.Road | PathMethod.Flying;
				a.m_Methods |= PathMethod.Road;
				b.m_Methods |= PathMethod.Road;
				if ((int)trafficRequest.m_SizeClass <= 1)
				{
					parameters.m_Methods |= PathMethod.MediumRoad;
					a.m_Methods |= PathMethod.MediumRoad;
					b.m_Methods |= PathMethod.MediumRoad;
				}
			}
			if ((trafficRequest.m_RoadType & RoadTypes.Watercraft) != RoadTypes.None)
			{
				parameters.m_MaxSpeed = math.max(parameters.m_MaxSpeed, 55.555557f);
				parameters.m_Methods |= PathMethod.Road;
				a.m_Methods |= PathMethod.Road;
				b.m_Methods |= PathMethod.Road;
				if ((int)trafficRequest.m_SizeClass <= 1)
				{
					parameters.m_Methods |= PathMethod.MediumRoad;
					a.m_Methods |= PathMethod.MediumRoad;
					b.m_Methods |= PathMethod.MediumRoad;
				}
			}
			if ((trafficRequest.m_TrackType & TrackTypes.Train) != TrackTypes.None)
			{
				parameters.m_MaxSpeed = math.max(parameters.m_MaxSpeed, 138.88889f);
				parameters.m_Methods |= PathMethod.Track;
				a.m_Methods |= PathMethod.Track;
				b.m_Methods |= PathMethod.Track;
			}
			if ((int)trafficRequest.m_SizeClass < 2)
			{
				parameters.m_IgnoredRules |= RuleFlags.ForbidHeavyTraffic;
			}
			if (trafficRequest.m_EnergyTypes == EnergyTypes.Electricity)
			{
				parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines;
			}
			if (random.NextBool())
			{
				CommonUtils.Swap(ref a, ref b);
			}
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, a, b));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DispatchVehiclesJob : IJob
	{
		public NativeQueue<VehicleDispatch> m_VehicleDispatches;

		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		public void Execute()
		{
			VehicleDispatch item;
			while (m_VehicleDispatches.TryDequeue(out item))
			{
				if (m_ServiceDispatches.TryGetBuffer(item.m_Source, out var bufferData))
				{
					bufferData.Add(new ServiceDispatch(item.m_Request));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RandomTrafficRequest> __Game_Simulation_RandomTrafficRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<RandomTrafficRequest> __Game_Simulation_RandomTrafficRequest_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		public ComponentLookup<TrafficSpawner> __Game_Buildings_TrafficSpawner_RW_ComponentLookup;

		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_RandomTrafficRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RandomTrafficRequest>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
			__Game_Simulation_RandomTrafficRequest_RO_ComponentLookup = state.GetComponentLookup<RandomTrafficRequest>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Buildings_TrafficSpawner_RW_ComponentLookup = state.GetComponentLookup<TrafficSpawner>();
			__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_RequestQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		if (phase == SystemUpdatePhase.LoadSimulation)
		{
			return 1;
		}
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_RequestQuery = GetEntityQuery(ComponentType.ReadOnly<RandomTrafficRequest>(), ComponentType.ReadOnly<UpdateFrame>());
		RequireForUpdate(m_RequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num;
		uint nextUpdateFrameIndex;
		int maxDelayFrames;
		if (m_SimulationSystem.loadingProgress != 1f)
		{
			num = m_SimulationSystem.frameIndex & 0xF;
			nextUpdateFrameIndex = num;
			maxDelayFrames = 16;
		}
		else
		{
			num = (m_SimulationSystem.frameIndex >> 4) & 0xF;
			nextUpdateFrameIndex = (num + 4) & 0xF;
			maxDelayFrames = 64;
		}
		NativeQueue<VehicleDispatch> vehicleDispatches = new NativeQueue<VehicleDispatch>(Allocator.TempJob);
		RandomTrafficDispatchJob jobData = new RandomTrafficDispatchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RandomTrafficRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_RandomTrafficRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DispatchedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomTrafficRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_RandomTrafficRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_TrafficSpawnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_TrafficSpawner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_NextUpdateFrameIndex = nextUpdateFrameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_VehicleDispatches = vehicleDispatches.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, maxDelayFrames).AsParallelWriter()
		};
		DispatchVehiclesJob jobData2 = new DispatchVehiclesJob
		{
			m_VehicleDispatches = vehicleDispatches,
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_RequestQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		vehicleDispatches.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle2;
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
	public RandomTrafficDispatchSystem()
	{
	}
}
