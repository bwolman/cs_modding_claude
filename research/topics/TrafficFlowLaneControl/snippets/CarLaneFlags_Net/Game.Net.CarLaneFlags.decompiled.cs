using System;

namespace Game.Net;

[Flags]
public enum CarLaneFlags : uint
{
	Unsafe = 1u,
	UTurnLeft = 2u,
	Invert = 4u,
	SideConnection = 8u,
	TurnLeft = 0x10u,
	TurnRight = 0x20u,
	LevelCrossing = 0x40u,
	Twoway = 0x80u,
	IsSecured = 0x100u,
	Runway = 0x200u,
	Yield = 0x400u,
	Stop = 0x800u,
	SecondaryStart = 0x1000u,
	SecondaryEnd = 0x2000u,
	ForbidBicycles = 0x4000u,
	PublicOnly = 0x8000u,
	Highway = 0x10000u,
	UTurnRight = 0x20000u,
	GentleTurnLeft = 0x40000u,
	GentleTurnRight = 0x80000u,
	Forward = 0x100000u,
	Approach = 0x200000u,
	Roundabout = 0x400000u,
	RightLimit = 0x800000u,
	LeftLimit = 0x1000000u,
	ForbidPassing = 0x2000000u,
	RightOfWay = 0x4000000u,
	TrafficLights = 0x8000000u,
	ParkingLeft = 0x10000000u,
	ParkingRight = 0x20000000u,
	Forbidden = 0x40000000u,
	AllowEnter = 0x80000000u
}
