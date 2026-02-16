using System;

namespace Game.Events;

[Flags]
public enum AccidentSiteFlags : uint
{
	StageAccident = 1u,
	Secured = 2u,
	CrimeScene = 4u,
	TrafficAccident = 8u,
	CrimeFinished = 0x10u,
	CrimeDetected = 0x20u,
	CrimeMonitored = 0x40u,
	RequirePolice = 0x80u,
	MovingVehicles = 0x100u
}
