using System;

namespace Game.Simulation;

[Flags]
public enum WaterPipeEdgeFlags : byte
{
	None = 0,
	WaterShortage = 1,
	SewageBackup = 2,
	WaterDisconnected = 4,
	SewageDisconnected = 8
}
