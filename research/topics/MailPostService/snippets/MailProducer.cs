using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct MailProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_MailRequest;

	public ushort m_SendingMail;

	public ushort m_ReceivingMail;

	public byte m_DispatchIndex;

	public ushort m_LastUpdateTotalMail;

	public int receivingMail
	{
		get
		{
			return m_ReceivingMail & 0x7FFF;
		}
		set
		{
			m_ReceivingMail = (ushort)((m_ReceivingMail & 0x8000) | value);
		}
	}

	public bool mailDelivered
	{
		get
		{
			return (m_ReceivingMail & 0x8000) != 0;
		}
		set
		{
			if (value)
			{
				m_ReceivingMail |= 32768;
			}
			else
			{
				m_ReceivingMail &= 32767;
			}
		}
	}
}
