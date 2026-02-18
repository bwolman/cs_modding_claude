using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

[StructLayout((LayoutKind)0, Size = 1)]
public struct Placeholder : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
