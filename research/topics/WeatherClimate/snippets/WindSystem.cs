// Decompiled from Game.dll — Game.Simulation.WindSystem
// Provides 2D wind data as a 64x64 cell map. Extends CellMapSystem<Wind>.
// Updates every 512 frames during game simulation.

namespace Game.Simulation;

public class WindSystem : CellMapSystem<Wind>, IJobSerializable
{
    public static readonly int kTextureSize = 64;
    public static readonly int kUpdateInterval = 512;

    // WindCopyJob: copies from 3D WindSimulationSystem cells to 2D wind map
    //   For each cell: sample terrain height, find vertical layer,
    //   bilinear interpolate velocity from the two nearest Z layers
    //   Store as Wind { m_Wind = float2 }

    // GetWind: static method to sample wind at any world position
    //   Bilinear interpolation across the 64x64 grid

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        if (phase != SystemUpdatePhase.GameSimulation) return 1;
        return kUpdateInterval;  // 512 frames
    }
}

// Wind struct (Game.Simulation):
// public struct Wind : IStrideSerializable, ISerializable
// {
//     public float2 m_Wind;  // XZ wind velocity
//     public static float2 SampleWind(CellMapData<Wind> wind, float3 position)
//         // Bilinear interpolation of wind at world position
// }
