using System;
using System.Collections;
using System.Collections.Generic;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

public struct TaxableResourceData : IComponentData, IQueryTypeParameter
{
	public byte m_TaxAreas;

	public bool Contains(TaxAreaType areaType)
	{
		return (m_TaxAreas & GetBit(areaType)) != 0;
	}

	public TaxableResourceData(System.Collections.Generic.IEnumerable<TaxAreaType> taxAreas)
	{
		m_TaxAreas = 0;
		System.Collections.Generic.IEnumerator<TaxAreaType> enumerator = taxAreas.GetEnumerator();
		try
		{
			while (((System.Collections.IEnumerator)enumerator).MoveNext())
			{
				TaxAreaType current = enumerator.Current;
				m_TaxAreas |= (byte)GetBit(current);
			}
		}
		finally
		{
			((System.IDisposable)enumerator)?.Dispose();
		}
	}

	private static int GetBit(TaxAreaType areaType)
	{
		return 1 << (int)(areaType - 1);
	}
}
