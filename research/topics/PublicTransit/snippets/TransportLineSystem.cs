// Game.Simulation.TransportLineSystem -- Key logic excerpts
// Full class is GameSystemBase, IDefaultSerializable, ISerializable
// UPDATE_INTERVAL = 256

namespace Game.Simulation;

public class TransportLineSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
    // Line query: Route + TransportLine + RouteWaypoint + PrefabRef, exclude Temp/Deleted
    // Vehicle request archetype: ServiceRequest + TransportVehicleRequest + RequestGroup

    // Night determination: normalizedTime < 0.25 or >= 11/12
    // isNight used for Day/Night route scheduling

    // --- TransportLineTickJob.Execute (per transport line entity) ---
    // 1. Apply RouteModifier for VehicleInterval
    // 2. Check RouteOption.PaidTicket -> apply TicketPrice modifier
    // 3. Determine isActive based on:
    //    - RouteOption.Inactive -> always inactive
    //    - No active buildings on line -> inactive
    //    - RouteOption.Day -> active during daytime only
    //    - RouteOption.Night -> active during nighttime only
    //    - Otherwise -> always active
    // 4. RefreshLineSegments: compute lineDuration and stableDuration
    //    from PathInformation on each segment + VehicleTiming + StopDuration
    // 5. CalculateVehicleCount = round(lineDuration / vehicleInterval), min 1
    // 6. CalculateVehicleInterval = lineDuration / vehicleCount
    // 7. If vehicleInterval or ticketPrice changed, trigger PathfindUpdated on stops
    // 8. CheckVehicles: count total and continuing vehicles on route
    //    - Vehicles with AbandonRoute flag are not counted as continuing
    //    - Vehicles with wrong model are flagged for AbandonRoute
    // 9. If continuingCount < targetCount: CancelAbandon on some vehicles
    //    If continuingCount > targetCount: AbandonVehicles (furthest first)
    // 10. If totalCount < targetCount: request new vehicles
    //     via TransportVehicleRequest entity

    public static int CalculateVehicleCount(float vehicleInterval, float lineDuration)
    {
        return math.max(1, (int)math.round(lineDuration / math.max(1f, vehicleInterval)));
    }

    public static float CalculateVehicleInterval(float lineDuration, int vehicleCount)
    {
        return lineDuration / (float)math.max(1, vehicleCount);
    }

    // MaxTransportSpeed tracked per frame (passenger [0] and cargo [1])
    // Default: 277.77777 m/s (~1000 km/h)
}
