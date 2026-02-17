using System;

namespace Game.Prefabs;

[Flags]
public enum OutsideConnectionTransferType
{
	None = 0,
	Road = 1,
	Train = 2,
	Air = 4,
	Ship = 0x10,
	Last = 0x20,
	All = 0x17
}
