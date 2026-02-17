using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[StructLayout((LayoutKind)0, Size = 1)]
public struct ResourceProducer : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
