using System;

namespace Game.Companies;

[Flags]
public enum StorageTransferFlags : byte
{
	Car = 1,
	Transport = 2,
	Track = 4,
	Incoming = 8
}
