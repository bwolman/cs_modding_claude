// Game.Simulation.TransportBoardingHelpers -- Key boarding logic excerpts

namespace Game.Simulation;

public static class TransportBoardingHelpers
{
    // BoardingData.Concurrent provides thread-safe boarding operations:
    //   BeginBoarding(vehicle, route, stop, waypoint, currentStation, nextStation, refuel)
    //   EndBoarding(vehicle, route, stop, waypoint, currentStation, nextStation)
    //   BeginTesting(vehicle, route, stop, waypoint)
    //   EndTesting(vehicle, route, stop, waypoint)

    // TransportBoardingJob.BeginBoarding:
    // 1. Check if stop already has a boarding vehicle (skip if so)
    // 2. Get TransportLine + TransportLineData from route prefab
    // 3. Get VehicleTiming from waypoint
    // 4. Calculate departure frame using RouteUtils.CalculateDepartureFrame
    //    (based on vehicle interval, unbunching factor, stop duration)
    // 5. Set boarding vehicle on stop (BoardingVehicle.m_Vehicle = vehicle)
    // 6. Update VehicleTiming.m_AverageTravelTime
    // 7. Set PublicTransportFlags.Boarding on vehicle
    // 8. For cargo: UnloadResources at current station, LoadResources for next station

    // TransportBoardingJob.EndBoarding:
    // 1. Clear BoardingVehicle.m_Vehicle on stop
    // 2. Final LoadResources attempt
    // 3. Clear Boarding/Refueling flags on vehicle

    // GetVehicleType determines TransportType from vehicle entity:
    //   Aircraft -> Airplane, Train -> Train, Watercraft -> Ship,
    //   DeliveryTruck -> Bus, else -> None

    // Cargo statistics tracked per TransportType:
    //   Train -> StatisticType.CargoCountTrain
    //   Ship -> StatisticType.CargoCountShip
    //   Airplane -> StatisticType.CargoCountAirplane
}
