using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct ElectricityOutsideConnection : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
