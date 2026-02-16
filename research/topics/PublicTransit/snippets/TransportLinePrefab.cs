using Game.Net;
using Game.Pathfind;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { })]
public class TransportLinePrefab : RoutePrefab
{
    public RouteConnectionType m_AccessConnectionType = RouteConnectionType.Pedestrian;
    public RouteConnectionType m_RouteConnectionType = RouteConnectionType.Road;
    public TrackTypes m_AccessTrackType;
    public TrackTypes m_RouteTrackType;
    public RoadTypes m_AccessRoadType;
    public RoadTypes m_RouteRoadType;
    public TransportType m_TransportType;
    public float m_DefaultVehicleInterval = 15f;
    public float m_DefaultUnbunchingFactor = 0.75f;
    public float m_StopDuration = 1f;
    public SizeClass m_SizeClass = SizeClass.Large;
    public bool m_PassengerTransport = true;
    public bool m_CargoTransport;
    public PathfindPrefab m_PathfindPrefab;
    public NotificationIconPrefab m_VehicleNotification;

    // GetArchetypeComponents shows what ECS components are added:
    // Route entity gets: TransportLine, VehicleModel, DispatchedRequest,
    //                    RouteNumber, RouteVehicle, RouteModifier, Policy
    // Waypoint entity gets: AccessLane, RouteLane, VehicleTiming,
    //                       WaitingPassengers (if passenger transport)
    // Segment entity gets: PathTargets, RouteInfo, PathElement, PathInformation
}
