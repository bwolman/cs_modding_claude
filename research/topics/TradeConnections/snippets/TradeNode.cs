using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct TradeNode : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
