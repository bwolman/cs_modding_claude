// Decompiled from Game.dll -> Game.Simulation.CollectedCityServiceFeeData
// Decompiled with ilspycmd on 2026-02-16

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

// Buffer element that tracks collected fees per service resource.
// Attached to city service entities (queried via CollectedFeeGroup).
// Used by CityServiceBudgetSystem to aggregate income/expenses.
public struct CollectedCityServiceFeeData : IBufferElementData, ISerializable
{
    public int m_PlayerResource;    // PlayerResource enum cast to int
    public float m_Export;          // Total export fee revenue (cost * 128)
    public float m_Import;          // Total import fee cost (cost * 128)
    public float m_Internal;        // Total internal fee revenue (cost * 128)
    public float m_ExportCount;     // Export service units (amount * 128)
    public float m_ImportCount;     // Import service units (amount * 128)
    public float m_InternalCount;   // Internal service units (amount * 128)
}
