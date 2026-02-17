using Colossal.Serialization.Entities;
using Game.Economy;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Companies;

public struct CurrentTrading : IBufferElementData, ISerializable
{
	public int m_TradingResourceAmount;

	public Resource m_TradingResource;

	public uint m_TradingStartFrameIndex;

	public OutsideConnectionTransferType m_OutsideConnectionType;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int tradingResourceAmount = m_TradingResourceAmount;
		writer.Write(tradingResourceAmount);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_TradingResource);
		writer.Write(value);
		uint tradingStartFrameIndex = m_TradingStartFrameIndex;
		writer.Write(tradingStartFrameIndex);
		OutsideConnectionTransferType outsideConnectionType = m_OutsideConnectionType;
		writer.Write((int)outsideConnectionType);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int tradingResourceAmount = ref m_TradingResourceAmount;
		reader.Read(out tradingResourceAmount);
		reader.Read(out sbyte value);
		m_TradingResource = EconomyUtils.GetResource(value);
		ref uint tradingStartFrameIndex = ref m_TradingStartFrameIndex;
		reader.Read(out tradingStartFrameIndex);
		reader.Read(out int value2);
		m_OutsideConnectionType = (OutsideConnectionTransferType)value2;
	}
}
