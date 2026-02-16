using System;

namespace Game.Citizens;

[Flags]
public enum CriminalFlags : ushort
{
	Robber = 1,
	Prisoner = 2,
	Planning = 4,
	Preparing = 8,
	Monitored = 0x10,
	Arrested = 0x20,
	Sentenced = 0x40
}
