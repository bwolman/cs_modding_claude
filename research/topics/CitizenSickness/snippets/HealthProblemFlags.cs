using System;

namespace Game.Citizens;

[Flags]
public enum HealthProblemFlags : byte
{
	None = 0,
	Sick = 1,
	Dead = 2,
	Injured = 4,
	RequireTransport = 8,
	InDanger = 0x10,
	Trapped = 0x20,
	NoHealthcare = 0x40
}
