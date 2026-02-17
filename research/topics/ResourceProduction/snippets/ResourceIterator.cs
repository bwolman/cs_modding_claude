using System;

namespace Game.Economy;

public struct ResourceIterator
{
	public Resource resource;

	public static ResourceIterator GetIterator()
	{
		return new ResourceIterator
		{
			resource = Resource.NoResource
		};
	}

	public bool Next()
	{
		resource = (Resource)Math.Max(1uL, (ulong)resource << 1);
		return resource != Resource.Last;
	}
}
