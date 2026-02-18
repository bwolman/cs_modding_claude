using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Notifications;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class NotificationsSection : InfoSectionBase
{
	[BurstCompile]
	private struct CheckAndCacheNotificationsJob : IJob
	{
		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconDataFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public BufferLookup<IconElement> m_IconElementBufferFromEntity;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeBufferFromEntity;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufferFromEntity;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenBufferFromEntity;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypointBufferFromEntity;

		public NativeArray<bool> m_DisplayResult;

		public NativeList<Notification> m_NotificationResult;

		public void Execute()
		{
			if (m_IconDataFromEntity.HasComponent(m_Entity) || m_CitizenDataFromEntity.HasComponent(m_Entity))
			{
				return;
			}
			if (HasNotifications(m_Entity, m_IconElementBufferFromEntity, m_IconDataFromEntity))
			{
				m_DisplayResult[0] = true;
				m_NotificationResult = GetNotifications(m_Entity, m_PrefabRefDataFromEntity, m_IconDataFromEntity, m_IconElementBufferFromEntity, m_NotificationResult);
			}
			if (m_EmployeeBufferFromEntity.TryGetBuffer(m_Entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (HasNotifications(bufferData[i].m_Worker, m_IconElementBufferFromEntity, m_IconDataFromEntity))
					{
						m_DisplayResult[0] = true;
						m_NotificationResult = GetNotifications(bufferData[i].m_Worker, m_PrefabRefDataFromEntity, m_IconDataFromEntity, m_IconElementBufferFromEntity, m_NotificationResult);
					}
				}
			}
			if (m_RenterBufferFromEntity.TryGetBuffer(m_Entity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					if (m_HouseholdCitizenBufferFromEntity.TryGetBuffer(bufferData2[j].m_Renter, out var bufferData3))
					{
						for (int k = 0; k < bufferData3.Length; k++)
						{
							if (HasNotifications(bufferData3[k].m_Citizen, m_IconElementBufferFromEntity, m_IconDataFromEntity))
							{
								m_DisplayResult[0] = true;
								m_NotificationResult = GetNotifications(bufferData3[k].m_Citizen, m_PrefabRefDataFromEntity, m_IconDataFromEntity, m_IconElementBufferFromEntity, m_NotificationResult);
							}
						}
					}
					if (!m_EmployeeBufferFromEntity.TryGetBuffer(bufferData2[j].m_Renter, out var bufferData4))
					{
						continue;
					}
					for (int l = 0; l < bufferData4.Length; l++)
					{
						if (HasNotifications(bufferData4[l].m_Worker, m_IconElementBufferFromEntity, m_IconDataFromEntity))
						{
							m_DisplayResult[0] = true;
							m_NotificationResult = GetNotifications(bufferData4[l].m_Worker, m_PrefabRefDataFromEntity, m_IconDataFromEntity, m_IconElementBufferFromEntity, m_NotificationResult);
						}
					}
				}
			}
			if (!m_RouteWaypointBufferFromEntity.TryGetBuffer(m_Entity, out var bufferData5))
			{
				return;
			}
			for (int m = 0; m < bufferData5.Length; m++)
			{
				if (HasNotifications(bufferData5[m].m_Waypoint, m_IconElementBufferFromEntity, m_IconDataFromEntity))
				{
					m_DisplayResult[0] = true;
					m_NotificationResult = GetNotifications(bufferData5[m].m_Waypoint, m_PrefabRefDataFromEntity, m_IconDataFromEntity, m_IconElementBufferFromEntity, m_NotificationResult);
				}
			}
		}
	}

	[BurstCompile]
	private struct CheckAndCacheVisitorNotificationsJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingTypeHandle;

		[ReadOnly]
		public BufferLookup<IconElement> m_IconElementBufferFromEntity;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconDataFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		public NativeArray<bool> m_DisplayResult;

		public NativeList<Notification> m_NotificationResult;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentBuilding> nativeArray2 = chunk.GetNativeArray(ref m_CurrentBuildingTypeHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray2[i].m_CurrentBuilding == m_Entity && HasNotifications(nativeArray[i], m_IconElementBufferFromEntity, m_IconDataFromEntity))
				{
					m_DisplayResult[0] = true;
					m_NotificationResult = GetNotifications(nativeArray[i], m_PrefabRefDataFromEntity, m_IconDataFromEntity, m_IconElementBufferFromEntity, m_NotificationResult);
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
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<IconElement> __Game_Notifications_IconElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferLookup = state.GetBufferLookup<IconElement>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
		}
	}

	private ImageSystem m_ImageSystem;

	private EntityQuery m_CitizenQuery;

	private NativeList<Notification> m_NotificationsResult;

	private NativeArray<bool> m_DisplayResult;

	private TypeHandle __TypeHandle;

	protected override string group => "NotificationsSection";

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUnderConstruction => true;

	protected override bool displayForUpgrades => true;

	private List<NotificationInfo> notifications { get; set; }

	protected override void Reset()
	{
		notifications.Clear();
		m_NotificationsResult.Clear();
		m_DisplayResult[0] = false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CurrentBuilding>(),
				ComponentType.ReadOnly<IconElement>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_DisplayResult = new NativeArray<bool>(1, Allocator.Persistent);
		m_NotificationsResult = new NativeList<Notification>(10, Allocator.Persistent);
		notifications = new List<NotificationInfo>(10);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_NotificationsResult.Dispose();
		m_DisplayResult.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		IJobExtensions.Schedule(new CheckAndCacheNotificationsJob
		{
			m_Entity = selectedEntity,
			m_CitizenDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconElementBufferFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_EmployeeBufferFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterBufferFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizenBufferFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteWaypointBufferFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_DisplayResult = m_DisplayResult,
			m_NotificationResult = m_NotificationsResult
		}, base.Dependency).Complete();
		JobChunkExtensions.Schedule(new CheckAndCacheVisitorNotificationsJob
		{
			m_Entity = selectedEntity,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconElementBufferFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_IconDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DisplayResult = m_DisplayResult,
			m_NotificationResult = m_NotificationsResult
		}, m_CitizenQuery, base.Dependency).Complete();
		base.visible = m_DisplayResult[0];
	}

	protected override void OnProcess()
	{
		for (int i = 0; i < m_NotificationsResult.Length; i++)
		{
			NotificationInfo notificationInfo = new NotificationInfo(m_NotificationsResult[i]);
			bool flag = false;
			for (int j = 0; j < notifications.Count; j++)
			{
				if (notifications[j].entity == notificationInfo.entity)
				{
					if (base.EntityManager.HasComponent<Building>(selectedEntity))
					{
						notifications[j].AddTarget(notificationInfo.target);
					}
					flag = true;
				}
			}
			if (!flag)
			{
				notifications.Add(notificationInfo);
			}
		}
		notifications.Sort();
	}

	public static bool HasNotifications(Entity entity, BufferLookup<IconElement> iconBuffer, ComponentLookup<Icon> iconDataFromEntity)
	{
		if (iconBuffer.HasBuffer(entity))
		{
			DynamicBuffer<IconElement> dynamicBuffer = iconBuffer[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity icon = dynamicBuffer[i].m_Icon;
				if (iconDataFromEntity.HasComponent(icon) && iconDataFromEntity[icon].m_ClusterLayer != IconClusterLayer.Marker)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static NativeList<Notification> GetNotifications(EntityManager EntityManager, Entity entity, NativeList<Notification> notifications)
	{
		if (EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<IconElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity icon = buffer[i].m_Icon;
				if (EntityManager.TryGetComponent<Icon>(icon, out var component) && component.m_ClusterLayer != IconClusterLayer.Marker && EntityManager.TryGetComponent<PrefabRef>(icon, out var component2))
				{
					notifications.Add(new Notification(component2.m_Prefab, entity, component.m_Priority));
				}
			}
		}
		return notifications;
	}

	public static NativeList<Notification> GetNotifications(Entity entity, ComponentLookup<PrefabRef> prefabRefDataFromEntity, ComponentLookup<Icon> iconDataFromEntity, BufferLookup<IconElement> iconBufferFromEntity, NativeList<Notification> notifications)
	{
		if (iconBufferFromEntity.HasBuffer(entity))
		{
			DynamicBuffer<IconElement> dynamicBuffer = iconBufferFromEntity[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity icon = dynamicBuffer[i].m_Icon;
				if (iconDataFromEntity.HasComponent(icon))
				{
					Icon icon2 = iconDataFromEntity[icon];
					if (icon2.m_ClusterLayer != IconClusterLayer.Marker && prefabRefDataFromEntity.HasComponent(icon))
					{
						notifications.Add(new Notification(prefabRefDataFromEntity[icon].m_Prefab, entity, icon2.m_Priority));
					}
				}
			}
		}
		return notifications;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("notifications");
		writer.ArrayBegin(notifications.Count);
		for (int i = 0; i < notifications.Count; i++)
		{
			Entity entity = notifications[i].entity;
			writer.TypeBegin(typeof(Notification).FullName);
			writer.PropertyName("key");
			writer.Write(m_PrefabSystem.GetPrefabName(entity));
			writer.PropertyName("count");
			writer.Write(notifications[i].count);
			writer.PropertyName("iconPath");
			if (m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab))
			{
				writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
			}
			else
			{
				writer.Write(m_ImageSystem.placeholderIcon);
			}
			writer.TypeEnd();
		}
		writer.ArrayEnd();
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
	public NotificationsSection()
	{
	}
}
