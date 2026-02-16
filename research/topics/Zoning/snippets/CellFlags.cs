using System;

namespace Game.Zones;

[Flags]
public enum CellFlags : ushort
{
	None = 0,
	Blocked = 1,
	Shared = 2,
	Roadside = 4,
	Visible = 8,
	Overridden = 0x10,
	Occupied = 0x20,
	Selected = 0x40,
	Redundant = 0x80,
	Updating = 0x100,
	RoadLeft = 0x200,
	RoadRight = 0x400,
	RoadBack = 0x800
}
