using System;

namespace Game.Prefabs;

[Flags]
public enum ZoneFlags : byte
{
	SupportNarrow = 1,
	SupportLeftCorner = 2,
	SupportRightCorner = 4,
	Office = 8
}
