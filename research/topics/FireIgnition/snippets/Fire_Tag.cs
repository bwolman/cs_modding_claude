using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Fire : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
