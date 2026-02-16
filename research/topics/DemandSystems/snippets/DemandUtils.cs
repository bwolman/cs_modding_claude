using UnityEngine;

namespace Game.Simulation;

public static class DemandUtils
{
	public const int kUpdateInterval = 16;
	public const int kCountCompanyUpdateOffset = 1;
	public const int kCommercialUpdateOffset = 4;
	public const int kIndustrialUpdateOffset = 7;
	public const int kResidentialUpdateOffset = 10;
	public const int kZoneSpawnUpdateOffset = 13;

	public static int GetDemandFactorEffect(int total, float effect)
	{
		return Mathf.RoundToInt(100f * effect);
	}
}
