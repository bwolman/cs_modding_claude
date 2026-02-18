using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Notifications;

[CompilerGenerated]
public class IconCommandSystem : GameSystemBase
{
	[BurstCompile]
	private struct IconCommandPlaybackJob : IJob
	{
		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NotificationIconData> m_NotificationIconData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<IconAnimationElement> m_IconAnimations;

		public ComponentLookup<Icon> m_IconData;

		public BufferLookup<IconElement> m_IconElements;

		[ReadOnly]
		public Entity m_ConfigurationEntity;

		[ReadOnly]
		public float m_DeltaTime;

		[DeallocateOnJobCompletion]
		public NativeArray<IconCommandBuffer.Command> m_Commands;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int length = m_Commands.Length;
			int num = 0;
			m_Commands.Sort();
			while (num < length)
			{
				Entity owner = m_Commands[num].m_Owner;
				int num2 = num;
				while (++num2 < length && !(m_Commands[num2].m_Owner != owner))
				{
				}
				if (m_EntityLookup.Exists(owner))
				{
					ProcessCommands(owner, num, num2);
				}
				num = num2;
			}
		}

		private unsafe void ProcessCommands(Entity owner, int startIndex, int endIndex)
		{
			DynamicBuffer<IconElement> dynamicBuffer = default(DynamicBuffer<IconElement>);
			m_IconElements.TryGetBuffer(owner, out var bufferData);
			bool flag = m_DeletedData.HasComponent(owner);
			int2* ptr = stackalloc int2[16];
			int num = 0;
			for (int i = startIndex; i < endIndex; i++)
			{
				IconCommandBuffer.Command command = m_Commands[i];
				if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.All) != 0)
				{
					int num2 = i + 1;
					while (num2 < endIndex)
					{
						IconCommandBuffer.Command command2 = m_Commands[num2];
						if ((command2.m_CommandFlags & IconCommandBuffer.CommandFlags.All) == 0 || command2.m_Priority != command.m_Priority)
						{
							num2++;
							continue;
						}
						goto IL_08e3;
					}
				}
				else
				{
					int num3 = i + 1;
					while (num3 < endIndex)
					{
						IconCommandBuffer.Command command3 = m_Commands[num3];
						if (!(command3.m_Prefab == command.m_Prefab) || ((command.m_Flags ^ command3.m_Flags) & IconFlags.SecondaryLocation) != 0 || (((command.m_Flags | command3.m_Flags) & IconFlags.IgnoreTarget) == 0 && !(command3.m_Target == command.m_Target)) || (flag && (command3.m_CommandFlags & IconCommandBuffer.CommandFlags.Remove) == 0))
						{
							num3++;
							continue;
						}
						goto IL_08e3;
					}
				}
				if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Add) != 0)
				{
					if (flag && command.m_ClusterLayer != IconClusterLayer.Transaction)
					{
						continue;
					}
					int num4 = 0;
					while (true)
					{
						if (num4 < num)
						{
							if ((int)command.m_Priority == ptr[num4].x && command.m_BufferIndex != ptr[num4].y)
							{
								break;
							}
							num4++;
							continue;
						}
						Icon iconData = GetIconData(command);
						if (command.m_ClusterLayer != IconClusterLayer.Transaction)
						{
							if (dynamicBuffer.IsCreated)
							{
								int num5 = FindIcon(dynamicBuffer, command);
								if (num5 >= 0)
								{
									Entity icon = dynamicBuffer[num5].m_Icon;
									if (m_DeletedData.HasComponent(icon))
									{
										m_CommandBuffer.RemoveComponent<Deleted>(icon);
									}
									Icon other = m_IconData[icon];
									iconData.m_ClusterIndex = other.m_ClusterIndex;
									if (!iconData.Equals(other))
									{
										m_IconData[icon] = iconData;
										m_CommandBuffer.AddComponent(icon, default(Updated));
									}
									if (command.m_Target != Entity.Null)
									{
										if (!m_TargetData.HasComponent(icon))
										{
											m_CommandBuffer.AddComponent(icon, new Target(command.m_Target));
										}
										else if (m_TargetData[icon].m_Target != command.m_Target)
										{
											m_CommandBuffer.SetComponent(icon, new Target(command.m_Target));
										}
									}
									else
									{
										m_CommandBuffer.RemoveComponent<Target>(icon);
									}
									if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Temp) != 0)
									{
										Temp tempData = GetTempData(command);
										if (tempData.m_Flags != m_TempData[icon].m_Flags)
										{
											m_CommandBuffer.SetComponent(icon, tempData);
										}
									}
									if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Hidden) != 0)
									{
										if (!m_HiddenData.HasComponent(icon))
										{
											m_CommandBuffer.AddComponent(icon, default(Hidden));
										}
									}
									else if (m_HiddenData.HasComponent(icon))
									{
										m_CommandBuffer.RemoveComponent<Hidden>(icon);
									}
									break;
								}
							}
							else if (bufferData.IsCreated)
							{
								int num6 = FindIcon(bufferData, command);
								if (num6 >= 0)
								{
									Entity icon2 = bufferData[num6].m_Icon;
									if (m_DeletedData.HasComponent(icon2))
									{
										m_CommandBuffer.RemoveComponent<Deleted>(icon2);
									}
									Icon other2 = m_IconData[icon2];
									iconData.m_ClusterIndex = other2.m_ClusterIndex;
									if (!iconData.Equals(other2))
									{
										m_IconData[icon2] = iconData;
										m_CommandBuffer.AddComponent(icon2, default(Updated));
									}
									if (command.m_Target != Entity.Null)
									{
										if (!m_TargetData.HasComponent(icon2))
										{
											m_CommandBuffer.AddComponent(icon2, new Target(command.m_Target));
										}
										else if (m_TargetData[icon2].m_Target != command.m_Target)
										{
											m_CommandBuffer.SetComponent(icon2, new Target(command.m_Target));
										}
									}
									else
									{
										m_CommandBuffer.RemoveComponent<Target>(icon2);
									}
									if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Temp) != 0)
									{
										Temp tempData2 = GetTempData(command);
										if (tempData2.m_Flags != m_TempData[icon2].m_Flags)
										{
											m_CommandBuffer.SetComponent(icon2, tempData2);
										}
									}
									if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Hidden) != 0)
									{
										if (!m_HiddenData.HasComponent(icon2))
										{
											m_CommandBuffer.AddComponent(icon2, default(Hidden));
										}
									}
									else if (m_HiddenData.HasComponent(icon2))
									{
										m_CommandBuffer.RemoveComponent<Hidden>(icon2);
									}
									break;
								}
								dynamicBuffer = m_CommandBuffer.SetBuffer<IconElement>(owner);
								dynamicBuffer.CopyFrom(bufferData);
							}
							else
							{
								dynamicBuffer = m_CommandBuffer.AddBuffer<IconElement>(owner);
							}
						}
						NotificationIconData notificationIconData = m_NotificationIconData[command.m_Prefab];
						Entity entity = m_CommandBuffer.CreateEntity(notificationIconData.m_Archetype);
						m_CommandBuffer.SetComponent(entity, new PrefabRef(command.m_Prefab));
						m_CommandBuffer.SetComponent(entity, iconData);
						if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Temp) != 0)
						{
							Temp tempData3 = GetTempData(command);
							if (tempData3.m_Original != Entity.Null || (command.m_CommandFlags & IconCommandBuffer.CommandFlags.DisallowCluster) != 0)
							{
								m_CommandBuffer.AddComponent(entity, default(DisallowCluster));
							}
							m_CommandBuffer.AddComponent(entity, tempData3);
						}
						else
						{
							if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.DisallowCluster) != 0)
							{
								m_CommandBuffer.AddComponent(entity, default(DisallowCluster));
							}
							DynamicBuffer<IconAnimationElement> dynamicBuffer2 = m_IconAnimations[m_ConfigurationEntity];
							AnimationType appearAnimation = GetAppearAnimation(command.m_ClusterLayer);
							float duration = dynamicBuffer2[(int)appearAnimation].m_Duration;
							m_CommandBuffer.AddComponent(entity, new Animation(appearAnimation, m_DeltaTime - command.m_Delay, duration));
						}
						if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Hidden) != 0)
						{
							m_CommandBuffer.AddComponent(entity, default(Hidden));
						}
						if (command.m_Target != Entity.Null)
						{
							m_CommandBuffer.AddComponent(entity, new Target(command.m_Target));
						}
						if (command.m_ClusterLayer != IconClusterLayer.Transaction)
						{
							m_CommandBuffer.AddComponent(entity, new Owner(owner));
							dynamicBuffer.Add(new IconElement(entity));
						}
						break;
					}
				}
				else if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Remove) != 0)
				{
					DynamicBuffer<IconElement> iconElements = (dynamicBuffer.IsCreated ? dynamicBuffer : bufferData);
					if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.All) != 0)
					{
						if (iconElements.IsCreated)
						{
							for (int j = 0; j < iconElements.Length; j++)
							{
								Entity icon3 = iconElements[j].m_Icon;
								if (icon3.Index < 0 || m_IconData[icon3].m_Priority != command.m_Priority)
								{
									DeleteIcon(icon3);
									iconElements.RemoveAt(j--);
								}
							}
						}
						if (num < 16)
						{
							ptr[num++] = new int2((int)command.m_Priority, command.m_BufferIndex);
						}
					}
					else if (iconElements.IsCreated)
					{
						int num7 = FindIcon(iconElements, command);
						if (num7 != -1)
						{
							DeleteIcon(iconElements[num7].m_Icon);
							iconElements.RemoveAt(num7);
						}
					}
				}
				else
				{
					if ((command.m_CommandFlags & IconCommandBuffer.CommandFlags.Update) == 0)
					{
						continue;
					}
					DynamicBuffer<IconElement> dynamicBuffer3 = (dynamicBuffer.IsCreated ? dynamicBuffer : bufferData);
					if (!dynamicBuffer3.IsCreated)
					{
						continue;
					}
					for (int k = 0; k < dynamicBuffer3.Length; k++)
					{
						Entity icon4 = dynamicBuffer3[k].m_Icon;
						if (icon4.Index < 0)
						{
							continue;
						}
						Icon value = m_IconData[icon4];
						if ((value.m_Flags & IconFlags.CustomLocation) == 0)
						{
							float3 location = value.m_Location;
							if ((command.m_Flags & IconFlags.TargetLocation) != 0)
							{
								value.m_Location = FindLocation(command.m_Target);
							}
							else
							{
								value.m_Location = FindLocation(command.m_Owner);
							}
							if (!location.Equals(value.m_Location))
							{
								m_IconData[icon4] = value;
								m_CommandBuffer.AddComponent(icon4, default(Updated));
							}
						}
					}
				}
				IL_08e3:;
			}
			if (bufferData.IsCreated && !dynamicBuffer.IsCreated && bufferData.Length == 0)
			{
				m_CommandBuffer.RemoveComponent<IconElement>(owner);
			}
		}

		private void DeleteIcon(Entity entity)
		{
			if (entity.Index < 0 || m_TempData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(entity, default(Deleted));
				return;
			}
			DynamicBuffer<IconAnimationElement> dynamicBuffer = m_IconAnimations[m_ConfigurationEntity];
			AnimationType resolveAnimation = GetResolveAnimation(m_IconData[entity].m_ClusterLayer);
			float duration = dynamicBuffer[(int)resolveAnimation].m_Duration;
			m_CommandBuffer.AddComponent(entity, new Animation(resolveAnimation, m_DeltaTime, duration));
			m_CommandBuffer.AddComponent(entity, default(Updated));
		}

		private AnimationType GetAppearAnimation(IconClusterLayer layer)
		{
			return layer switch
			{
				IconClusterLayer.Default => AnimationType.WarningAppear, 
				IconClusterLayer.Marker => AnimationType.MarkerAppear, 
				IconClusterLayer.Transaction => AnimationType.Transaction, 
				_ => AnimationType.WarningAppear, 
			};
		}

		private AnimationType GetResolveAnimation(IconClusterLayer layer)
		{
			return layer switch
			{
				IconClusterLayer.Default => AnimationType.WarningResolve, 
				IconClusterLayer.Marker => AnimationType.MarkerDisappear, 
				_ => AnimationType.WarningResolve, 
			};
		}

		private Temp GetTempData(IconCommandBuffer.Command command)
		{
			Temp result = default(Temp);
			if (m_TempData.TryGetComponent(command.m_Owner, out var componentData))
			{
				result.m_Flags |= componentData.m_Flags;
				if (m_IconElements.TryGetBuffer(componentData.m_Original, out var bufferData))
				{
					int num = FindIcon(bufferData, command);
					if (num >= 0)
					{
						result.m_Original = bufferData[num].m_Icon;
					}
				}
			}
			return result;
		}

		private Icon GetIconData(IconCommandBuffer.Command command)
		{
			Icon result = default(Icon);
			if ((command.m_Flags & IconFlags.CustomLocation) != 0)
			{
				result.m_Location = command.m_Location;
			}
			else if ((command.m_Flags & IconFlags.TargetLocation) != 0)
			{
				result.m_Location = FindLocation(command.m_Target);
			}
			else
			{
				result.m_Location = FindLocation(command.m_Owner);
			}
			result.m_Priority = command.m_Priority;
			result.m_ClusterLayer = command.m_ClusterLayer;
			result.m_Flags = command.m_Flags;
			return result;
		}

		private float3 FindLocation(Entity entity)
		{
			float3 result = default(float3);
			if (m_ConnectedData.HasComponent(entity))
			{
				Entity connected = m_ConnectedData[entity].m_Connected;
				if (m_TransformData.HasComponent(connected))
				{
					entity = connected;
				}
			}
			else if (m_CurrentBuildingData.HasComponent(entity))
			{
				entity = m_CurrentBuildingData[entity].m_CurrentBuilding;
				if (m_OwnerData.HasComponent(entity))
				{
					entity = m_OwnerData[entity].m_Owner;
				}
			}
			else if (m_CurrentTransportData.HasComponent(entity))
			{
				entity = m_CurrentTransportData[entity].m_CurrentTransport;
			}
			if (m_CurrentVehicleData.HasComponent(entity))
			{
				entity = m_CurrentVehicleData[entity].m_Vehicle;
			}
			if (m_TransformData.TryGetComponent(entity, out var componentData))
			{
				result = componentData.m_Position;
				if (m_PrefabRefData.TryGetComponent(entity, out var componentData2) && m_ObjectGeometryData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
				{
					if ((componentData3.m_Flags & Game.Objects.GeometryFlags.Marker) == 0)
					{
						result.y = ObjectUtils.CalculateBounds(componentData.m_Position, componentData.m_Rotation, componentData3).max.y;
					}
					if ((componentData3.m_Flags & (Game.Objects.GeometryFlags.Physical | Game.Objects.GeometryFlags.HasLot)) == (Game.Objects.GeometryFlags.Physical | Game.Objects.GeometryFlags.HasLot) && m_DestroyedData.TryGetComponent(entity, out var componentData4) && componentData4.m_Cleared >= 0f)
					{
						result.y = componentData.m_Position.y + 5f;
					}
				}
			}
			else if (m_NodeData.HasComponent(entity))
			{
				result = m_NodeData[entity].m_Position;
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_NetGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					result.y += m_NetGeometryData[prefabRef.m_Prefab].m_DefaultSurfaceHeight.max;
				}
			}
			else if (m_CurveData.HasComponent(entity))
			{
				result = MathUtils.Position(m_CurveData[entity].m_Bezier, 0.5f);
			}
			else if (m_PositionData.HasComponent(entity))
			{
				result = m_PositionData[entity].m_Position;
			}
			else if (m_RouteWaypoints.HasBuffer(entity))
			{
				DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[entity];
				if (dynamicBuffer.Length != 0)
				{
					result = m_PositionData[dynamicBuffer[0].m_Waypoint].m_Position;
				}
			}
			return result;
		}

		private int FindIcon(DynamicBuffer<IconElement> iconElements, IconCommandBuffer.Command command)
		{
			for (int i = 0; i < iconElements.Length; i++)
			{
				Entity icon = iconElements[i].m_Icon;
				if (icon.Index < 0 || m_PrefabRefData[icon].m_Prefab != command.m_Prefab)
				{
					continue;
				}
				Icon icon2 = m_IconData[icon];
				if ((command.m_Flags & IconFlags.IgnoreTarget) == 0 && (icon2.m_Flags & IconFlags.IgnoreTarget) == 0)
				{
					if (m_TargetData.HasComponent(icon))
					{
						if (m_TargetData[icon].m_Target != command.m_Target)
						{
							continue;
						}
					}
					else if (command.m_Target != Entity.Null)
					{
						continue;
					}
				}
				if (((command.m_Flags ^ icon2.m_Flags) & IconFlags.SecondaryLocation) == 0)
				{
					return i;
				}
			}
			return -1;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NotificationIconData> __Game_Prefabs_NotificationIconData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<IconAnimationElement> __Game_Prefabs_IconAnimationElement_RO_BufferLookup;

		public ComponentLookup<Icon> __Game_Notifications_Icon_RW_ComponentLookup;

		public BufferLookup<IconElement> __Game_Notifications_IconElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NotificationIconData_RO_ComponentLookup = state.GetComponentLookup<NotificationIconData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Prefabs_IconAnimationElement_RO_BufferLookup = state.GetBufferLookup<IconAnimationElement>(isReadOnly: true);
			__Game_Notifications_Icon_RW_ComponentLookup = state.GetComponentLookup<Icon>();
			__Game_Notifications_IconElement_RW_BufferLookup = state.GetBufferLookup<IconElement>();
		}
	}

	private ModificationEndBarrier m_ModificationBarrier;

	private EntityQuery m_ConfigurationQuery;

	private List<NativeQueue<IconCommandBuffer.Command>> m_Queues;

	private JobHandle m_Dependencies;

	private int m_BufferIndex;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_Queues = new List<NativeQueue<IconCommandBuffer.Command>>();
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<IconConfigurationData>());
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		m_Dependencies.Complete();
		for (int i = 0; i < m_Queues.Count; i++)
		{
			m_Queues[i].Dispose();
		}
		m_Queues.Clear();
		base.OnStopRunning();
	}

	public IconCommandBuffer CreateCommandBuffer()
	{
		NativeQueue<IconCommandBuffer.Command> item = new NativeQueue<IconCommandBuffer.Command>(Allocator.TempJob);
		m_Queues.Add(item);
		return new IconCommandBuffer(item.AsParallelWriter(), m_BufferIndex++);
	}

	public void AddCommandBufferWriter(JobHandle handle)
	{
		m_Dependencies = JobHandle.CombineDependencies(m_Dependencies, handle);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_Dependencies.Complete();
		m_BufferIndex = 0;
		int num = 0;
		for (int i = 0; i < m_Queues.Count; i++)
		{
			num += m_Queues[i].Count;
		}
		if (num == 0 || m_ConfigurationQuery.IsEmptyIgnoreFilter)
		{
			for (int j = 0; j < m_Queues.Count; j++)
			{
				m_Queues[j].Dispose();
			}
			m_Queues.Clear();
			return;
		}
		NativeArray<IconCommandBuffer.Command> commands = new NativeArray<IconCommandBuffer.Command>(num, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		num = 0;
		for (int k = 0; k < m_Queues.Count; k++)
		{
			NativeQueue<IconCommandBuffer.Command> nativeQueue = m_Queues[k];
			int count = nativeQueue.Count;
			for (int l = 0; l < count; l++)
			{
				commands[num++] = nativeQueue.Dequeue();
			}
			nativeQueue.Dispose();
		}
		m_Queues.Clear();
		JobHandle jobHandle = IJobExtensions.Schedule(new IconCommandPlaybackJob
		{
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NotificationIconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NotificationIconData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_IconAnimations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_IconAnimationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RW_ComponentLookup, ref base.CheckedStateRef),
			m_IconElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Notifications_IconElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_ConfigurationEntity = m_ConfigurationQuery.GetSingletonEntity(),
			m_DeltaTime = UnityEngine.Time.deltaTime,
			m_Commands = commands,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public IconCommandSystem()
	{
	}
}
