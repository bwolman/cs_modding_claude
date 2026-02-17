using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct CompanyStatisticData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_MaxNumberOfCustomers;

	public int m_MonthlyCustomerCount;

	public int m_MonthlyCostBuyingResources;

	public int m_CurrentNumberOfCustomers;

	public int m_CurrentCostOfBuyingResources;

	public int m_Income;

	public int m_Worth;

	public int m_Profit;

	public int m_WagePaid;

	public int m_RentPaid;

	public int m_ElectricityPaid;

	public int m_WaterPaid;

	public int m_SewagePaid;

	public int m_GarbagePaid;

	public int m_TaxPaid;

	public int m_CostBuyResource;

	public int m_LastUpdateWorth;

	public uint m_LastFrameLowIncome;

	public int m_LastUpdateProduce;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int maxNumberOfCustomers = m_MaxNumberOfCustomers;
		writer.Write(maxNumberOfCustomers);
		int monthlyCustomerCount = m_MonthlyCustomerCount;
		writer.Write(monthlyCustomerCount);
		int monthlyCostBuyingResources = m_MonthlyCostBuyingResources;
		writer.Write(monthlyCostBuyingResources);
		int currentNumberOfCustomers = m_CurrentNumberOfCustomers;
		writer.Write(currentNumberOfCustomers);
		int currentCostOfBuyingResources = m_CurrentCostOfBuyingResources;
		writer.Write(currentCostOfBuyingResources);
		int income = m_Income;
		writer.Write(income);
		int worth = m_Worth;
		writer.Write(worth);
		int profit = m_Profit;
		writer.Write(profit);
		int wagePaid = m_WagePaid;
		writer.Write(wagePaid);
		int rentPaid = m_RentPaid;
		writer.Write(rentPaid);
		int electricityPaid = m_ElectricityPaid;
		writer.Write(electricityPaid);
		int waterPaid = m_WaterPaid;
		writer.Write(waterPaid);
		int sewagePaid = m_SewagePaid;
		writer.Write(sewagePaid);
		int garbagePaid = m_GarbagePaid;
		writer.Write(garbagePaid);
		int taxPaid = m_TaxPaid;
		writer.Write(taxPaid);
		int costBuyResource = m_CostBuyResource;
		writer.Write(costBuyResource);
		int lastUpdateWorth = m_LastUpdateWorth;
		writer.Write(lastUpdateWorth);
		uint lastFrameLowIncome = m_LastFrameLowIncome;
		writer.Write(lastFrameLowIncome);
		int lastUpdateProduce = m_LastUpdateProduce;
		writer.Write(lastUpdateProduce);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int maxNumberOfCustomers = ref m_MaxNumberOfCustomers;
		reader.Read(out maxNumberOfCustomers);
		ref int monthlyCustomerCount = ref m_MonthlyCustomerCount;
		reader.Read(out monthlyCustomerCount);
		ref int monthlyCostBuyingResources = ref m_MonthlyCostBuyingResources;
		reader.Read(out monthlyCostBuyingResources);
		ref int currentNumberOfCustomers = ref m_CurrentNumberOfCustomers;
		reader.Read(out currentNumberOfCustomers);
		ref int currentCostOfBuyingResources = ref m_CurrentCostOfBuyingResources;
		reader.Read(out currentCostOfBuyingResources);
		if (reader.context.format.Has(FormatTags.UnifyCompanyStatistics))
		{
			ref int income = ref m_Income;
			reader.Read(out income);
			ref int worth = ref m_Worth;
			reader.Read(out worth);
			ref int profit = ref m_Profit;
			reader.Read(out profit);
			ref int wagePaid = ref m_WagePaid;
			reader.Read(out wagePaid);
			ref int rentPaid = ref m_RentPaid;
			reader.Read(out rentPaid);
			ref int electricityPaid = ref m_ElectricityPaid;
			reader.Read(out electricityPaid);
			ref int waterPaid = ref m_WaterPaid;
			reader.Read(out waterPaid);
			ref int sewagePaid = ref m_SewagePaid;
			reader.Read(out sewagePaid);
			ref int garbagePaid = ref m_GarbagePaid;
			reader.Read(out garbagePaid);
			ref int taxPaid = ref m_TaxPaid;
			reader.Read(out taxPaid);
			ref int costBuyResource = ref m_CostBuyResource;
			reader.Read(out costBuyResource);
			ref int lastUpdateWorth = ref m_LastUpdateWorth;
			reader.Read(out lastUpdateWorth);
		}
		else
		{
			m_Income = 0;
			m_Worth = 0;
			m_Profit = 0;
			m_WagePaid = 0;
			m_RentPaid = 0;
			m_ElectricityPaid = 0;
			m_WaterPaid = 0;
			m_SewagePaid = 0;
			m_GarbagePaid = 0;
			m_TaxPaid = 0;
			m_CostBuyResource = 0;
			m_LastUpdateWorth = 0;
		}
		if (reader.context.format.Has(FormatTags.DelayMoveAwayCompany))
		{
			ref uint lastFrameLowIncome = ref m_LastFrameLowIncome;
			reader.Read(out lastFrameLowIncome);
		}
		else
		{
			m_LastFrameLowIncome = 0u;
		}
		if (reader.context.format.Has(FormatTags.TrackCityMaxProduction))
		{
			ref int lastUpdateProduce = ref m_LastUpdateProduce;
			reader.Read(out lastUpdateProduce);
		}
		else
		{
			m_LastUpdateProduce = 0;
		}
	}
}
