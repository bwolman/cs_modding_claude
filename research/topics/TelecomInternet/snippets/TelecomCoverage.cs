// Decompiled from Game.dll — Game.Simulation.TelecomCoverage

public struct TelecomCoverage : IStrideSerializable, ISerializable
{
    public byte m_SignalStrength;   // 0-255, signal strength at this cell
    public byte m_NetworkLoad;      // 0-255, network congestion at this cell

    // Network quality formula (per-cell, integer):
    //   quality = signalStrength * 510 / (255 + networkLoad * 2)
    // When networkLoad=0: quality = signalStrength * 2 (max ~510)
    // When networkLoad=255: quality = signalStrength * 510 / 765 (~0.67x)

    // SampleNetworkQuality (bilinear interpolation):
    //   Samples 4 neighboring cells, bilinear lerp
    //   Per-cell: min(1, signalStrength / (127.5 + networkLoad))
    //   Returns 0.0 to 1.0
}

// TelecomStatus (city-level summary):
public struct TelecomStatus
{
    public float m_Capacity;   // Total network capacity across all facilities
    public float m_Load;       // Total users (population weighted by signal)
    public float m_Quality;    // Weighted average quality across populated cells
}
