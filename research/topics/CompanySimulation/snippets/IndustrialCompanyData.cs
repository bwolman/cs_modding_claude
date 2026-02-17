using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Prefabs;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct IndustrialCompanyData : IComponentData, IQueryTypeParameter
{
}
