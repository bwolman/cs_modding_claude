using System;

namespace Game.Vehicles;

[Flags]
public enum CarLaneFlags : uint
{
	EndOfPath = 1u,
	EndReached = 2u,
	UpdateOptimalLane = 4u,
	TransformTarget = 8u,
	ParkingSpace = 0x10u,
	EnteringRoad = 0x20u,
	Obsolete = 0x40u,
	Reserved = 0x80u,
	FixedLane = 0x100u,
	Waypoint = 0x200u,
	Checked = 0x400u,
	GroupTarget = 0x800u,
	Queue = 0x1000u,
	IgnoreBlocker = 0x2000u,
	IsBlocked = 0x4000u,
	QueueReached = 0x8000u,
	Validated = 0x10000u,
	Interruption = 0x20000u,
	TurnLeft = 0x40000u,
	TurnRight = 0x80000u,
	PushBlockers = 0x100000u,
	HighBeams = 0x200000u,
	RequestSpace = 0x400000u,
	FixedStart = 0x800000u,
	Connection = 0x1000000u,
	ResetSpeed = 0x2000000u,
	Area = 0x4000000u,
	Roundabout = 0x8000000u,
	CanReverse = 0x10000000u,
	ClearedForPathfind = 0x20000000u
}
