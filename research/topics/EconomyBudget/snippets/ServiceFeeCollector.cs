// Decompiled from Game.dll -> Game.City.ServiceFeeCollector
// Decompiled with ilspycmd on 2026-02-16

using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

// Zero-size tag component (no fields). Marks buildings that collect service fees.
// Used in ServiceFeeSystem query: entities with ServiceFeeCollector + (Patient or Student)
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct ServiceFeeCollector : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
