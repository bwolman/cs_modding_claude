// Decompiled from Game.dll — Game.Vehicles.PoliceCarFlags
using System;

namespace Game.Vehicles;

[Flags]
public enum PoliceCarFlags : uint
{
    Returning = 1u,
    ShiftEnded = 2u,
    AccidentTarget = 4u,
    AtTarget = 8u,
    Disembarking = 0x10u,
    Cancelled = 0x20u,
    Full = 0x40u,
    Empty = 0x80u,
    EstimatedShiftEnd = 0x100u,
    Disabled = 0x200u
}
