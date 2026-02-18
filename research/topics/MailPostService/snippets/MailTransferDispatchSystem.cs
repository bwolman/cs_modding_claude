using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Pathfind;
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
public class MailTransferDispatchSystem : GameSystemBase
{
	private struct DispatchAction
	{
		public Entity m_Request;

		public Entity m_DispatchSource;

		public Entity m_DeliverFacility;

		public Entity m_ReceiveFacility;

		public DispatchAction(Entity request, Entity dispatchSource, Entity deliverFacility, Entity receiveFacility)
		{
			m_Request = request;
			m_DispatchSource = dispatchSource;
			m_DeliverFacility = deliverFacility;
			m_ReceiveFacility = receiveFacility;
		}
	}

	[BurstCompile]
	private struct MailTransferDispatchJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<MailTransferRequest> m_MailTransferRequestType;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> m_DispatchedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentLookup<MailTransferRequest> m_MailTransferRequestData;

		[ReadOnly]
		public ComponentLookup<PostFacility> m_PostFacilityData;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[ReadOnly]
		public BufferLookup<TripNeeded> m_TripNeededs;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_NextUpdateFrameIndex;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<DispatchAction>.ParallelWriter m_DispatchActions;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			if (index == m_NextUpdateFrameIndex && !chunk.Has(ref m_DispatchedType) && !chunk.Has(ref m_PathInformationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<MailTransferRequest> nativeArray2 = chunk.GetNativeArray(ref m_MailTransferRequestType);
				NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref m_ServiceRequestType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					MailTransferRequest requestData = nativeArray2[i];
					ServiceRequest serviceRequest = nativeArray3[i];
					if (!ValidateTarget(entity, requestData))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
						continue;
					}
					if (SimulationUtils.TickServiceRequest(ref serviceRequest))
					{
						FindVehicleSource(unfilteredChunkIndex, entity, requestData);
					}
					nativeArray3[i] = serviceRequest;
				}
			}
			if (index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref m_DispatchedType);
			NativeArray<MailTransferRequest> nativeArray5 = chunk.GetNativeArray(ref m_MailTransferRequestType);
			NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref m_ServiceRequestType);
			if (nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity2 = nativeArray7[j];
					Dispatched dispatched = nativeArray4[j];
					MailTransferRequest requestData2 = nativeArray5[j];
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
						if (!ValidateTarget(entity2, requestData2))
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
				MailTransferRequest requestData3 = nativeArray5[k];
				PathInformation pathInformation = nativeArray8[k];
				ServiceRequest serviceRequest3 = nativeArray6[k];
				if (!ValidateTarget(entity3, requestData3))
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
			if (m_ServiceDispatches.HasBuffer(handler))
			{
				DynamicBuffer<ServiceDispatch> dynamicBuffer = m_ServiceDispatches[handler];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (dynamicBuffer[i].m_Request == entity)
					{
						return true;
					}
				}
			}
			if (m_TripNeededs.HasBuffer(handler))
			{
				DynamicBuffer<TripNeeded> dynamicBuffer2 = m_TripNeededs[handler];
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					if (dynamicBuffer2[j].m_TargetAgent == entity)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ValidateTarget(Entity entity, MailTransferRequest requestData)
		{
			if (!m_PostFacilityData.HasComponent(requestData.m_Facility))
			{
				return false;
			}
			PostFacility postFacility = m_PostFacilityData[requestData.m_Facility];
			if ((requestData.m_Flags & MailTransferRequestFlags.Deliver) != 0)
			{
				if (postFacility.m_AcceptMailPriority <= 0f)
				{
					return false;
				}
				if (postFacility.m_MailDeliverRequest != entity)
				{
					if (m_MailTransferRequestData.HasComponent(postFacility.m_MailDeliverRequest))
					{
						return false;
					}
					m_DispatchActions.Enqueue(new DispatchAction(entity, Entity.Null, requestData.m_Facility, Entity.Null));
				}
				return true;
			}
			if ((requestData.m_Flags & MailTransferRequestFlags.Receive) != 0)
			{
				if (postFacility.m_DeliverMailPriority <= 0f)
				{
					return false;
				}
				if (postFacility.m_MailReceiveRequest != entity)
				{
					if (m_MailTransferRequestData.HasComponent(postFacility.m_MailReceiveRequest))
					{
						return false;
					}
					m_DispatchActions.Enqueue(new DispatchAction(entity, Entity.Null, Entity.Null, requestData.m_Facility));
				}
				return true;
			}
			return false;
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
			m_DispatchActions.Enqueue(new DispatchAction(entity, pathInformation.m_Origin, Entity.Null, Entity.Null));
			m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(pathInformation.m_Origin));
		}

		private void FindVehicleSource(int jobIndex, Entity requestEntity, MailTransferRequest requestData)
		{
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_IgnoredRules = (RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget a = new SetupQueueTarget
			{
				m_Type = SetupTargetType.MailTransfer,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car,
				m_Value = requestData.m_Amount,
				m_RandomCost = 30f
			};
			SetupQueueTarget b = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car,
				m_Entity = requestData.m_Facility,
				m_RandomCost = 30f
			};
			if ((requestData.m_Flags & MailTransferRequestFlags.Receive) != 0)
			{
				a.m_Flags |= SetupTargetFlags.Import;
			}
			if ((requestData.m_Flags & MailTransferRequestFlags.Deliver) != 0)
			{
				a.m_Flags |= SetupTargetFlags.Export;
			}
			if ((requestData.m_Flags & MailTransferRequestFlags.UnsortedMail) != 0)
			{
				a.m_Resource |= Resource.UnsortedMail;
			}
			if ((requestData.m_Flags & MailTransferRequestFlags.LocalMail) != 0)
			{
				a.m_Resource |= Resource.LocalMail;
			}
			if ((requestData.m_Flags & MailTransferRequestFlags.OutgoingMail) != 0)
			{
				a.m_Resource |= Resource.OutgoingMail;
			}
			if ((requestData.m_Flags & MailTransferRequestFlags.RequireTransport) != 0)
			{
				a.m_Flags |= SetupTargetFlags.RequireTransport;
			}
			else
			{
				CommonUtils.Swap(ref a, ref b);
			}
			SetupQueueItem value = new SetupQueueItem(requestEntity, parameters, a, b);
			m_PathfindQueue.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DispatchActionJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<MailTransferRequest> m_RequestData;

		public ComponentLookup<PostFacility> m_PostFacilityData;

		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		public BufferLookup<TripNeeded> m_TripNeededs;

		public BufferLookup<Resources> m_Resources;

		public NativeQueue<DispatchAction> m_DispatchActions;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			DispatchAction item;
			while (m_DispatchActions.TryDequeue(out item))
			{
				if (item.m_DispatchSource != Entity.Null)
				{
					if (m_PostFacilityData.HasComponent(item.m_DispatchSource))
					{
						if (m_ServiceDispatches.HasBuffer(item.m_DispatchSource))
						{
							m_ServiceDispatches[item.m_DispatchSource].Add(new ServiceDispatch(item.m_Request));
						}
					}
					else if (m_TripNeededs.HasBuffer(item.m_DispatchSource))
					{
						MailTransferRequest mailTransferRequest = m_RequestData[item.m_Request];
						DynamicBuffer<TripNeeded> dynamicBuffer = m_TripNeededs[item.m_DispatchSource];
						TripNeeded elem = new TripNeeded
						{
							m_TargetAgent = item.m_Request
						};
						if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.UnsortedMail) != 0)
						{
							elem.m_Resource = Resource.UnsortedMail;
						}
						if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.LocalMail) != 0)
						{
							elem.m_Resource = Resource.LocalMail;
						}
						if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.OutgoingMail) != 0)
						{
							elem.m_Resource = Resource.OutgoingMail;
						}
						if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.RequireTransport) != 0)
						{
							if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Deliver) != 0)
							{
								elem.m_Purpose = Purpose.Exporting;
							}
							if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Receive) != 0)
							{
								elem.m_Purpose = Purpose.Collect;
							}
						}
						else
						{
							if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Deliver) != 0)
							{
								elem.m_Purpose = Purpose.Collect;
							}
							if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Receive) != 0)
							{
								elem.m_Purpose = Purpose.Exporting;
							}
						}
						elem.m_Data = mailTransferRequest.m_Amount;
						if (elem.m_Purpose == Purpose.Exporting)
						{
							if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnUnsortedMail) != 0)
							{
								elem.m_Purpose = Purpose.ReturnUnsortedMail;
							}
							else if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnLocalMail) != 0)
							{
								elem.m_Purpose = Purpose.ReturnLocalMail;
							}
							else if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnOutgoingMail) != 0)
							{
								elem.m_Purpose = Purpose.ReturnOutgoingMail;
							}
							DynamicBuffer<Resources> resources = m_Resources[item.m_DispatchSource];
							int resources2 = EconomyUtils.GetResources(elem.m_Resource, resources);
							elem.m_Data = math.min(elem.m_Data, resources2);
							if (elem.m_Data <= 0)
							{
								continue;
							}
							EconomyUtils.AddResources(elem.m_Resource, -elem.m_Data, resources);
						}
						else
						{
							if (elem.m_Resource == Resource.UnsortedMail)
							{
								elem.m_Purpose = Purpose.ReturnUnsortedMail;
							}
							else if (elem.m_Resource == Resource.LocalMail)
							{
								elem.m_Purpose = Purpose.ReturnLocalMail;
							}
							else if (elem.m_Resource == Resource.OutgoingMail)
							{
								elem.m_Purpose = Purpose.ReturnOutgoingMail;
							}
							else
							{
								elem.m_Purpose = Purpose.Exporting;
							}
							if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnUnsortedMail) != 0)
							{
								elem.m_Resource = Resource.UnsortedMail;
							}
							else if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnLocalMail) != 0)
							{
								elem.m_Resource = Resource.LocalMail;
							}
							else if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnOutgoingMail) != 0)
							{
								elem.m_Resource = Resource.OutgoingMail;
							}
							else
							{
								elem.m_Resource = Resource.NoResource;
							}
							if (elem.m_Resource != Resource.NoResource)
							{
								DynamicBuffer<Resources> resources3 = m_Resources[item.m_DispatchSource];
								int resources4 = EconomyUtils.GetResources(elem.m_Resource, resources3);
								int num = math.min(elem.m_Data, resources4);
								if (num > 0)
								{
									elem.m_Data = num;
									EconomyUtils.AddResources(elem.m_Resource, -elem.m_Data, resources3);
								}
								else
								{
									elem.m_Resource = Resource.NoResource;
								}
							}
						}
						dynamicBuffer.Add(elem);
					}
				}
				if (item.m_DeliverFacility != Entity.Null)
				{
					PostFacility value = m_PostFacilityData[item.m_DeliverFacility];
					value.m_MailDeliverRequest = item.m_Request;
					m_PostFacilityData[item.m_DeliverFacility] = value;
				}
				if (item.m_ReceiveFacility != Entity.Null)
				{
					PostFacility value2 = m_PostFacilityData[item.m_ReceiveFacility];
					value2.m_MailReceiveRequest = item.m_Request;
					m_PostFacilityData[item.m_ReceiveFacility] = value2;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MailTransferRequest> __Game_Simulation_MailTransferRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<MailTransferRequest> __Game_Simulation_MailTransferRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostFacility> __Game_Buildings_PostFacility_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TripNeeded> __Game_Citizens_TripNeeded_RO_BufferLookup;

		public ComponentLookup<PostFacility> __Game_Buildings_PostFacility_RW_ComponentLookup;

		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

		public BufferLookup<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_MailTransferRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MailTransferRequest>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
			__Game_Simulation_MailTransferRequest_RO_ComponentLookup = state.GetComponentLookup<MailTransferRequest>(isReadOnly: true);
			__Game_Buildings_PostFacility_RO_ComponentLookup = state.GetComponentLookup<PostFacility>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RO_BufferLookup = state.GetBufferLookup<TripNeeded>(isReadOnly: true);
			__Game_Buildings_PostFacility_RW_ComponentLookup = state.GetComponentLookup<PostFacility>();
			__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
			__Game_Citizens_TripNeeded_RW_BufferLookup = state.GetBufferLookup<TripNeeded>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_RequestQuery;

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
		m_RequestQuery = GetEntityQuery(ComponentType.ReadOnly<MailTransferRequest>(), ComponentType.ReadOnly<UpdateFrame>());
		RequireForUpdate(m_RequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = (m_SimulationSystem.frameIndex >> 4) & 7;
		uint nextUpdateFrameIndex = (num + 4) & 7;
		NativeQueue<DispatchAction> dispatchActions = new NativeQueue<DispatchAction>(Allocator.TempJob);
		MailTransferDispatchJob jobData = new MailTransferDispatchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_MailTransferRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_MailTransferRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DispatchedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MailTransferRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MailTransferRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PostFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_TripNeededs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_TripNeeded_RO_BufferLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_NextUpdateFrameIndex = nextUpdateFrameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_DispatchActions = dispatchActions.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		};
		DispatchActionJob jobData2 = new DispatchActionJob
		{
			m_RequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MailTransferRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PostFacility_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup, ref base.CheckedStateRef),
			m_TripNeededs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_DispatchActions = dispatchActions,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_RequestQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		dispatchActions.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
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
	public MailTransferDispatchSystem()
	{
	}
}
