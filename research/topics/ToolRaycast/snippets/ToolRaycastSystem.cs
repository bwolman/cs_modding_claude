using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

public class ToolRaycastSystem : GameSystemBase
{
	private ToolSystem m_ToolSystem;

	private RaycastSystem m_RaycastSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	public RaycastFlags raycastFlags { get; set; }

	public TypeMask typeMask { get; set; }

	public CollisionMask collisionMask { get; set; }

	public Layer netLayerMask { get; set; }

	public AreaTypeMask areaTypeMask { get; set; }

	public RouteType routeType { get; set; }

	public TransportType transportType { get; set; }

	public IconLayerMask iconLayerMask { get; set; }

	public UtilityTypes utilityTypeMask { get; set; }

	public float3 rayOffset { get; set; }

	public Entity owner { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_RaycastSystem = base.World.GetOrCreateSystemManaged<RaycastSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
	}

	public bool GetRaycastResult(out RaycastResult result)
	{
		NativeArray<RaycastResult> result2 = m_RaycastSystem.GetResult(this);
		if (result2.Length != 0)
		{
			result = result2[0];
			return result.m_Owner != Entity.Null;
		}
		result = default(RaycastResult);
		return false;
	}

	public static Line3.Segment CalculateRaycastLine(Camera mainCamera)
	{
		Ray ray = mainCamera.ScreenPointToRay(InputManager.instance.mousePosition);
		float3 @float = ray.direction;
		float3 y = mainCamera.transform.forward;
		Line3.Segment result = default(Line3.Segment);
		result.a = ray.origin;
		result.b = result.a + @float * (mainCamera.farClipPlane / math.clamp(math.dot(@float, y), 0.25f, 1f));
		return result;
	}

	public static float3 CameraRayPlaneIntersect(Camera mainCamera, in float3 planeOrigin)
	{
		Ray ray = mainCamera.ScreenPointToRay(InputManager.instance.mousePosition);
		float3 @float = ray.origin;
		float3 float2 = ray.direction;
		float num = math.dot(mainCamera.transform.forward, planeOrigin - @float) / math.dot(mainCamera.transform.forward, float2);
		return @float + float2 * num;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool != null)
		{
			m_ToolSystem.activeTool.InitializeRaycast();
		}
		if (m_CameraUpdateSystem.TryGetViewer(out var viewer))
		{
			if (m_ToolSystem.fullUpdateRequired)
			{
				raycastFlags |= RaycastFlags.ToolDisable;
			}
			else
			{
				raycastFlags &= ~RaycastFlags.ToolDisable;
			}
			if (InputManager.instance.controlOverWorld)
			{
				raycastFlags &= ~RaycastFlags.UIDisable;
			}
			else
			{
				raycastFlags |= RaycastFlags.UIDisable;
			}
			RaycastInput input = new RaycastInput
			{
				m_Line = CalculateRaycastLine(viewer.camera),
				m_Offset = rayOffset,
				m_Owner = owner,
				m_TypeMask = typeMask,
				m_Flags = raycastFlags,
				m_CollisionMask = collisionMask,
				m_NetLayerMask = netLayerMask,
				m_AreaTypeMask = areaTypeMask,
				m_RouteType = routeType,
				m_TransportType = transportType,
				m_IconLayerMask = iconLayerMask,
				m_UtilityTypeMask = utilityTypeMask
			};
			m_RaycastSystem.AddInput(this, input);
		}
	}

	[Preserve]
	public ToolRaycastSystem()
	{
	}
}
