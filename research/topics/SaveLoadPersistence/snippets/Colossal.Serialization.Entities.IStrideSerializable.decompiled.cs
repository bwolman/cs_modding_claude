namespace Colossal.Serialization.Entities;

public interface IStrideSerializable : ISerializable
{
	int GetStride(Context context);
}
