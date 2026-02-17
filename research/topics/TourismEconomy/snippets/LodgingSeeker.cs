using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct LodgingSeeker : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
