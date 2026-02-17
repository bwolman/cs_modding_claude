using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

[StructLayout((LayoutKind)0, Size = 1)]
public struct ResourceSeller : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
