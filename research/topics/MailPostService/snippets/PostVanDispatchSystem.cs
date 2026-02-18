using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PostVanDispatchSystem : GameSystemBase
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
	private struct PostVanDispatchJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> m_DispatchedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<PostVanRequest> m_PostVanRequestType;

		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<MailProducer> m_MailProducerData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Routes.MailBox> m_MailBoxData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Buildings.PostFacility> m_PostFacilityData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Vehicles.PostVan> m_PostVanData;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_NextUpdateFrameIndex;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<VehicleDispatch>.ParallelWriter m_VehicleDispatches;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			if (index == m_NextUpdateFrameIndex && !chunk.Has(ref m_DispatchedType) && !chunk.Has(ref m_PathInformationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<PostVanRequest> nativeArray2 = chunk.GetNativeArray(ref m_PostVanRequestType);
				NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref m_ServiceRequestType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					PostVanRequest postVanRequest = nativeArray2[i];
					ServiceRequest serviceRequest = nativeArray3[i];
					if ((serviceRequest.m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						if (!ValidateReversed(entity, postVanRequest.m_Target))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
							continue;
						}
						if (SimulationUtils.TickServiceRequest(ref serviceRequest))
						{
							FindVehicleTarget(unfilteredChunkIndex, entity, postVanRequest.m_Target);
						}
					}
					else
					{
						if (!ValidateTarget(entity, postVanRequest))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
							continue;
						}
						if (SimulationUtils.TickServiceRequest(ref serviceRequest))
						{
							FindVehicleSource(unfilteredChunkIndex, entity, postVanRequest);
						}
					}
					nativeArray2[i] = postVanRequest;
					nativeArray3[i] = serviceRequest;
				}
			}
			if (index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref m_DispatchedType);
			NativeArray<PostVanRequest> nativeArray5 = chunk.GetNativeArray(ref m_PostVanRequestType);
			NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref m_ServiceRequestType);
			if (nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity2 = nativeArray7[j];
					Dispatched dispatched = nativeArray4[j];
					PostVanRequest postVanRequest2 = nativeArray5[j];
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
						if (!ValidateTarget(entity2, postVanRequest2))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity2);
							continue;
						}
						ResetFailedRequest(unfilteredChunkIndex, entity2, dispatched: true, ref postVanRequest2, ref serviceRequest2);
					}
					nativeArray5[j] = postVanRequest2;
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
				PostVanRequest postVanRequest3 = nativeArray5[k];
				PathInformation pathInformation = nativeArray8[k];
				ServiceRequest serviceRequest3 = nativeArray6[k];
				if ((serviceRequest3.m_Flags & ServiceRequestFlags.Reversed) != 0)
				{
					if (!ValidateReversed(entity3, postVanRequest3.m_Target))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
						continue;
					}
					if (pathInformation.m_Destination != Entity.Null)
					{
						ResetReverseRequest(unfilteredChunkIndex, entity3, pathInformation, ref serviceRequest3);
					}
					else
					{
						ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref postVanRequest3, ref serviceRequest3);
					}
				}
				else
				{
					if (!ValidateTarget(entity3, postVanRequest3))
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
						ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref postVanRequest3, ref serviceRequest3);
					}
				}
				nativeArray5[k] = postVanRequest3;
				nativeArray6[k] = serviceRequest3;
			}
		}

		private bool ValidateReversed(Entity entity, Entity source)
		{
			if (m_PostFacilityData.TryGetComponent(source, out var componentData))
			{
				if ((componentData.m_Flags & (PostFacilityFlags.CanDeliverMailWithVan | PostFacilityFlags.CanCollectMailWithVan)) == 0)
				{
					return false;
				}
				if (componentData.m_TargetRequest != entity)
				{
					if (m_PostVanRequestData.HasComponent(componentData.m_TargetRequest))
					{
						return false;
					}
					componentData.m_TargetRequest = entity;
					m_PostFacilityData[source] = componentData;
				}
				return true;
			}
			if (m_PostVanData.TryGetComponent(source, out var componentData2))
			{
				if ((componentData2.m_State & PostVanFlags.Disabled) != 0 || componentData2.m_RequestCount > 1 || (componentData2.m_State & (PostVanFlags.EstimatedEmpty | PostVanFlags.EstimatedFull)) == (PostVanFlags.EstimatedEmpty | PostVanFlags.EstimatedFull) || m_ParkedCarData.HasComponent(source))
				{
					return false;
				}
				if (componentData2.m_TargetRequest != entity)
				{
					if (m_PostVanRequestData.HasComponent(componentData2.m_TargetRequest))
					{
						return false;
					}
					componentData2.m_TargetRequest = entity;
					m_PostVanData[source] = componentData2;
				}
				return true;
			}
			return false;
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

		private bool ValidateTarget(Entity entity, PostVanRequest requestData)
		{
			if ((requestData.m_Flags & PostVanRequestFlags.BuildingTarget) != 0)
			{
				if (!m_MailProducerData.TryGetComponent(requestData.m_Target, out var componentData))
				{
					return false;
				}
				int num = 0;
				if ((requestData.m_Flags & PostVanRequestFlags.Collect) != 0)
				{
					num = math.max(num, componentData.m_SendingMail);
				}
				if ((requestData.m_Flags & PostVanRequestFlags.Deliver) != 0)
				{
					num = math.max(num, componentData.receivingMail);
				}
				if (num < m_PostConfigurationData.m_MailAccumulationTolerance)
				{
					return false;
				}
				if (componentData.m_MailRequest != entity)
				{
					if (m_PostVanRequestData.HasComponent(componentData.m_MailRequest))
					{
						return false;
					}
					componentData.m_MailRequest = entity;
					m_MailProducerData[requestData.m_Target] = componentData;
				}
			}
			else if ((requestData.m_Flags & PostVanRequestFlags.MailBoxTarget) != 0)
			{
				if (!m_MailBoxData.TryGetComponent(requestData.m_Target, out var componentData2))
				{
					return false;
				}
				if (componentData2.m_MailAmount < m_PostConfigurationData.m_MailAccumulationTolerance)
				{
					return false;
				}
				if (componentData2.m_CollectRequest != entity)
				{
					if (m_PostVanRequestData.HasComponent(componentData2.m_CollectRequest))
					{
						return false;
					}
					componentData2.m_CollectRequest = entity;
					m_MailBoxData[requestData.m_Target] = componentData2;
				}
			}
			return true;
		}

		private void ResetReverseRequest(int jobIndex, Entity entity, PathInformation pathInformation, ref ServiceRequest serviceRequest)
		{
			VehicleDispatch value = new VehicleDispatch(entity, pathInformation.m_Destination);
			m_VehicleDispatches.Enqueue(value);
			SimulationUtils.ResetReverseRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
		}

		private void ResetFailedRequest(int jobIndex, Entity entity, bool dispatched, ref PostVanRequest postVanRequest, ref ServiceRequest serviceRequest)
		{
			SimulationUtils.ResetFailedRequest(ref serviceRequest);
			postVanRequest.m_DispatchIndex++;
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<PathElement>(jobIndex, entity);
			if (dispatched)
			{
				m_CommandBuffer.RemoveComponent<Dispatched>(jobIndex, entity);
			}
		}

		private void DispatchVehicle(int jobIndex, Entity entity, PathInformation pathInformation)
		{
			Entity entity2 = pathInformation.m_Origin;
			if (m_ParkedCarData.HasComponent(entity2) && m_OwnerData.TryGetComponent(entity2, out var componentData))
			{
				entity2 = componentData.m_Owner;
			}
			VehicleDispatch value = new VehicleDispatch(entity, entity2);
			m_VehicleDispatches.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(entity2));
		}

		private void FindVehicleSource(int jobIndex, Entity requestEntity, PostVanRequest requestData)
		{
			Entity entity = Entity.Null;
			if (m_CurrentDistrictData.HasComponent(requestData.m_Target))
			{
				entity = m_CurrentDistrictData[requestData.m_Target].m_District;
			}
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = PathMethod.Road,
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.PostVan,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = entity
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = requestData.m_Target
			};
			if ((requestData.m_Flags & PostVanRequestFlags.Collect) != 0)
			{
				origin.m_Flags |= SetupTargetFlags.Import;
			}
			if ((requestData.m_Flags & PostVanRequestFlags.Deliver) != 0)
			{
				origin.m_Flags |= SetupTargetFlags.Export;
			}
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		private void FindVehicleTarget(int jobIndex, Entity requestEntity, Entity vehicleSource)
		{
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = PathMethod.Road,
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = vehicleSource
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.PostVanRequest,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car
			};
			if (m_PostVanData.TryGetComponent(vehicleSource, out var componentData) && (componentData.m_State & PostVanFlags.Returning) == 0)
			{
				origin.m_Flags |= SetupTargetFlags.PathEnd;
			}
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
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

		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		public void Execute()
		{
			VehicleDispatch item;
			while (m_VehicleDispatches.TryDequeue(out item))
			{
				ServiceRequest componentData;
				if (m_ServiceDispatches.TryGetBuffer(item.m_Source, out var bufferData))
				{
					bufferData.Add(new ServiceDispatch(item.m_Request));
				}
				else if (m_ServiceRequestData.TryGetComponent(item.m_Source, out componentData))
				{
					componentData.m_Flags |= ServiceRequestFlags.SkipCooldown;
					m_ServiceRequestData[item.m_Source] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<PostVanRequest> __Game_Simulation_PostVanRequest_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> __Game_Simulation_PostVanRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RW_ComponentLookup;

		public ComponentLookup<Game.Routes.MailBox> __Game_Routes_MailBox_RW_ComponentLookup;

		public ComponentLookup<Game.Buildings.PostFacility> __Game_Buildings_PostFacility_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.PostVan> __Game_Vehicles_PostVan_RW_ComponentLookup;

		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentLookup;

		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Simulation_PostVanRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PostVanRequest>();
			__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
			__Game_Simulation_PostVanRequest_RO_ComponentLookup = state.GetComponentLookup<PostVanRequest>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Buildings_MailProducer_RW_ComponentLookup = state.GetComponentLookup<MailProducer>();
			__Game_Routes_MailBox_RW_ComponentLookup = state.GetComponentLookup<Game.Routes.MailBox>();
			__Game_Buildings_PostFacility_RW_ComponentLookup = state.GetComponentLookup<Game.Buildings.PostFacility>();
			__Game_Vehicles_PostVan_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PostVan>();
			__Game_Simulation_ServiceRequest_RW_ComponentLookup = state.GetComponentLookup<ServiceRequest>();
			__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_RequestQuery;

	private EntityQuery m_PostConfigurationQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_RequestQuery = GetEntityQuery(ComponentType.ReadOnly<PostVanRequest>(), ComponentType.ReadOnly<UpdateFrame>());
		m_PostConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PostConfigurationData>());
		RequireForUpdate(m_RequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = (m_SimulationSystem.frameIndex >> 4) & 0x1F;
		uint nextUpdateFrameIndex = (num + 4) & 0x1F;
		NativeQueue<VehicleDispatch> vehicleDispatches = new NativeQueue<VehicleDispatch>(Allocator.TempJob);
		PostVanDispatchJob jobData = new PostVanDispatchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DispatchedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PostVanRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_PostVanRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PostVanRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PostVanRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_MailProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MailBoxData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_MailBox_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PostFacility_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PostVanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PostVan_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_NextUpdateFrameIndex = nextUpdateFrameIndex,
			m_PostConfigurationData = m_PostConfigurationQuery.GetSingleton<PostConfigurationData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_VehicleDispatches = vehicleDispatches.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		};
		DispatchVehiclesJob jobData2 = new DispatchVehiclesJob
		{
			m_VehicleDispatches = vehicleDispatches,
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentLookup, ref base.CheckedStateRef),
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
	public PostVanDispatchSystem()
	{
	}
}
