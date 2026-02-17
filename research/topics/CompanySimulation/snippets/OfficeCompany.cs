using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct OfficeCompany : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
