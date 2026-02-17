namespace Colossal.Serialization.Entities;

public interface IDefaultSerializable : ISerializable
{
	void SetDefaults(Context context);
}
