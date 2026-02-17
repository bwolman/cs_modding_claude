using Game.City;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct OutsideTradeParameterData : IComponentData, IQueryTypeParameter
{
	public float m_ElectricityImportPrice;

	public float m_ElectricityExportPrice;

	public float m_WaterImportPrice;

	public float m_WaterExportPrice;

	public float m_WaterExportPollutionTolerance;

	public float m_SewageExportPrice;

	public float m_AirWeightMultiplier;

	public float m_RoadWeightMultiplier;

	public float m_TrainWeightMultiplier;

	public float m_ShipWeightMultiplier;

	public float m_AirDistanceMultiplier;

	public float m_RoadDistanceMultiplier;

	public float m_TrainDistanceMultiplier;

	public float m_ShipDistanceMultiplier;

	public float m_AmbulanceImportServiceFee;

	public float m_HearseImportServiceFee;

	public float m_FireEngineImportServiceFee;

	public float m_GarbageImportServiceFee;

	public float m_PoliceImportServiceFee;

	public int m_OCServiceTradePopulationRange;

	public float GetDistanceCostSingle(OutsideConnectionTransferType type)
	{
		return type switch
		{
			OutsideConnectionTransferType.Air => m_AirDistanceMultiplier, 
			OutsideConnectionTransferType.Road => m_RoadDistanceMultiplier, 
			OutsideConnectionTransferType.Train => m_TrainDistanceMultiplier, 
			OutsideConnectionTransferType.Ship => m_ShipDistanceMultiplier, 
			_ => 0f, 
		};
	}

	public float GetBestDistanceCostAmongTypes(OutsideConnectionTransferType types)
	{
		float num = float.MaxValue;
		for (int num2 = 1; num2 < 32; num2 <<= 1)
		{
			if ((num2 & 0x17) != 0 && ((uint)num2 & (uint)types) != 0)
			{
				num = math.min(num, GetDistanceCostSingle((OutsideConnectionTransferType)num2));
			}
		}
		return num;
	}

	public float GetWeightCostSingle(OutsideConnectionTransferType type)
	{
		return type switch
		{
			OutsideConnectionTransferType.Air => m_AirWeightMultiplier, 
			OutsideConnectionTransferType.Road => m_RoadWeightMultiplier, 
			OutsideConnectionTransferType.Train => m_TrainWeightMultiplier, 
			OutsideConnectionTransferType.Ship => m_ShipWeightMultiplier, 
			_ => 0f, 
		};
	}

	public float GetBestWeightCostAmongTypes(OutsideConnectionTransferType types)
	{
		float num = float.MaxValue;
		for (int num2 = 1; num2 < 32; num2 <<= 1)
		{
			if ((num2 & 0x17) != 0 && ((uint)num2 & (uint)types) != 0)
			{
				num = math.min(num, GetWeightCostSingle((OutsideConnectionTransferType)num2));
			}
		}
		return num;
	}

	public float GetFee(PlayerResource resource, bool export = false)
	{
		switch (resource)
		{
		case PlayerResource.Electricity:
			if (!export)
			{
				return m_ElectricityImportPrice;
			}
			return m_ElectricityExportPrice;
		case PlayerResource.Water:
			if (!export)
			{
				return m_WaterImportPrice;
			}
			return m_WaterExportPrice;
		case PlayerResource.Sewage:
			if (!export)
			{
				return m_SewageExportPrice;
			}
			return 0f;
		default:
			return 0f;
		}
	}

	public bool Importable(PlayerResource resource)
	{
		return GetFee(resource) != 0f;
	}

	public bool Exportable(PlayerResource resource)
	{
		return GetFee(resource, export: true) != 0f;
	}
}
