using System;

namespace Game.Buildings;

[Flags]
public enum WaterConsumerFlags : byte
{
	None = 0,
	WaterConnected = 1,
	SewageConnected = 2
}
