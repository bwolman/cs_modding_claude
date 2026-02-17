// Decompiled from Game.dll — Game.Vehicles.CarFlags
using System;

namespace Game.Vehicles;

[Flags]
public enum CarFlags : uint
{
    Emergency = 1u,
    StayOnRoad = 2u,
    AnyLaneTarget = 4u,
    Warning = 8u,
    Queueing = 0x10u,
    UsePublicTransportLanes = 0x20u,
    PreferPublicTransportLanes = 0x40u,
    Sign = 0x80u,
    Interior = 0x100u,
    Working = 0x200u,
    SignalAnimation1 = 0x400u,
    SignalAnimation2 = 0x800u,
    CannotReverse = 0x1000u
}
