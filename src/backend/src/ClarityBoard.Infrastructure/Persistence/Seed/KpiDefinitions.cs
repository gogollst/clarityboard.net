namespace ClarityBoard.Infrastructure.Persistence.Seed;

public static class KpiDefinitionsSeed
{
    public record KpiSeed(string Id, string Domain, string Name, string Formula, string Unit, string Direction, string Category, string? CalculationClass = null, int DisplayOrder = 0);

    public static readonly KpiSeed[] All = [
        // Financial - Profitability (14)
        new("financial.gross_margin", "financial", "Gross Margin", "(Revenue - COGS) / Revenue * 100", "percentage", "higher_better", "profitability", "FinancialKpiCalculator", 1),
        new("financial.ebitda", "financial", "EBITDA", "Operating Income + Depreciation + Amortization", "currency", "higher_better", "profitability", "FinancialKpiCalculator", 2),
        new("financial.ebitda_margin", "financial", "EBITDA Margin", "EBITDA / Revenue * 100", "percentage", "higher_better", "profitability", "FinancialKpiCalculator", 3),
        new("financial.ebit", "financial", "EBIT", "Revenue - COGS - OpEx", "currency", "higher_better", "profitability", "FinancialKpiCalculator", 4),
        new("financial.ebit_margin", "financial", "EBIT Margin", "EBIT / Revenue * 100", "percentage", "higher_better", "profitability", "FinancialKpiCalculator", 5),
        new("financial.net_income", "financial", "Net Income", "EBIT - Taxes - Interest", "currency", "higher_better", "profitability", "FinancialKpiCalculator", 6),
        new("financial.net_margin", "financial", "Net Margin", "Net Income / Revenue * 100", "percentage", "higher_better", "profitability", "FinancialKpiCalculator", 7),
        new("financial.operating_expense_ratio", "financial", "Operating Expense Ratio", "OpEx / Revenue * 100", "percentage", "lower_better", "profitability", "FinancialKpiCalculator", 8),
        new("financial.cost_income_ratio", "financial", "Cost-Income Ratio", "Total Costs / Revenue * 100", "percentage", "lower_better", "profitability", "FinancialKpiCalculator", 9),
        new("financial.revenue", "financial", "Revenue", "Sum(Revenue Accounts Class 8)", "currency", "higher_better", "profitability", "FinancialKpiCalculator", 10),
        new("financial.cogs", "financial", "Cost of Goods Sold", "Sum(COGS Accounts)", "currency", "lower_better", "profitability", "FinancialKpiCalculator", 11),
        new("financial.personnel_expense_ratio", "financial", "Personnel Expense Ratio", "Personnel Costs / Revenue * 100", "percentage", "lower_better", "profitability", "FinancialKpiCalculator", 12),
        new("financial.material_expense_ratio", "financial", "Material Expense Ratio", "Material Costs / Revenue * 100", "percentage", "lower_better", "profitability", "FinancialKpiCalculator", 13),
        new("financial.break_even_revenue", "financial", "Break-Even Revenue", "Fixed Costs / (1 - Variable Costs / Revenue)", "currency", "lower_better", "profitability", "FinancialKpiCalculator", 14),

        // Financial - Liquidity (10)
        new("financial.current_ratio", "financial", "Current Ratio", "Current Assets / Current Liabilities", "ratio", "higher_better", "liquidity", "FinancialKpiCalculator", 20),
        new("financial.quick_ratio", "financial", "Quick Ratio", "(Current Assets - Inventory) / Current Liabilities", "ratio", "higher_better", "liquidity", "FinancialKpiCalculator", 21),
        new("financial.cash_ratio", "financial", "Cash Ratio", "Cash / Current Liabilities", "ratio", "higher_better", "liquidity", "FinancialKpiCalculator", 22),
        new("financial.operating_cash_flow", "financial", "Operating Cash Flow", "OCF from Cash Flow Statement", "currency", "higher_better", "liquidity", "FinancialKpiCalculator", 23),
        new("financial.free_cash_flow", "financial", "Free Cash Flow", "OCF - CapEx", "currency", "higher_better", "liquidity", "FinancialKpiCalculator", 24),
        new("financial.cash_runway_months", "financial", "Cash Runway", "Cash / Monthly Burn Rate", "count", "higher_better", "liquidity", "FinancialKpiCalculator", 25),
        new("financial.working_capital", "financial", "Working Capital", "Current Assets - Current Liabilities", "currency", "higher_better", "liquidity", "FinancialKpiCalculator", 26),
        new("financial.dso", "financial", "Days Sales Outstanding", "Accounts Receivable / (Revenue / 365)", "days", "lower_better", "liquidity", "WorkingCapitalCalculator", 27),
        new("financial.dio", "financial", "Days Inventory Outstanding", "Inventory / (COGS / 365)", "days", "lower_better", "liquidity", "WorkingCapitalCalculator", 28),
        new("financial.dpo", "financial", "Days Payable Outstanding", "Accounts Payable / (COGS / 365)", "days", "higher_better", "liquidity", "WorkingCapitalCalculator", 29),

        // Financial - Returns (5)
        new("financial.roe", "financial", "Return on Equity", "Net Income / Shareholders Equity * 100", "percentage", "higher_better", "returns", "FinancialKpiCalculator", 30),
        new("financial.roa", "financial", "Return on Assets", "Net Income / Total Assets * 100", "percentage", "higher_better", "returns", "FinancialKpiCalculator", 31),
        new("financial.roi", "financial", "Return on Investment", "(Gain - Cost) / Cost * 100", "percentage", "higher_better", "returns", "FinancialKpiCalculator", 32),
        new("financial.roce", "financial", "Return on Capital Employed", "EBIT / (Total Assets - Current Liabilities) * 100", "percentage", "higher_better", "returns", "FinancialKpiCalculator", 33),
        new("financial.ccc", "financial", "Cash Conversion Cycle", "DSO + DIO - DPO", "days", "lower_better", "returns", "WorkingCapitalCalculator", 34),

        // Financial - Tax (4)
        new("financial.effective_tax_rate", "financial", "Effective Tax Rate", "Total Taxes / Pre-Tax Income * 100", "percentage", "target", "tax", "TaxCalculator", 40),
        new("financial.kst_amount", "financial", "Körperschaftsteuer Amount", "KSt + Soli", "currency", "lower_better", "tax", "TaxCalculator", 41),
        new("financial.gewst_amount", "financial", "Gewerbesteuer Amount", "Messbetrag * Hebesatz", "currency", "lower_better", "tax", "TaxCalculator", 42),
        new("financial.tax_shield", "financial", "Tax Shield", "Interest Expense * Tax Rate", "currency", "higher_better", "tax", "TaxCalculator", 43),

        // Sales (12)
        new("sales.mrr", "sales", "Monthly Recurring Revenue", "Sum(Active Subscriptions)", "currency", "higher_better", "recurring", "SalesKpiCalculator", 50),
        new("sales.arr", "sales", "Annual Recurring Revenue", "MRR * 12", "currency", "higher_better", "recurring", "SalesKpiCalculator", 51),
        new("sales.mrr_growth", "sales", "MRR Growth Rate", "(MRR - Previous MRR) / Previous MRR * 100", "percentage", "higher_better", "recurring", "SalesKpiCalculator", 52),
        new("sales.clv", "sales", "Customer Lifetime Value", "ARPA * Gross Margin * (1 / Churn Rate)", "currency", "higher_better", "customer", "SalesKpiCalculator", 53),
        new("sales.cac", "sales", "Customer Acquisition Cost", "Sales & Marketing Cost / New Customers", "currency", "lower_better", "customer", "SalesKpiCalculator", 54),
        new("sales.ltv_cac_ratio", "sales", "LTV:CAC Ratio", "CLV / CAC", "ratio", "higher_better", "customer", "SalesKpiCalculator", 55),
        new("sales.arpa", "sales", "Average Revenue Per Account", "MRR / Active Customers", "currency", "higher_better", "customer", "SalesKpiCalculator", 56),
        new("sales.churn_rate", "sales", "Customer Churn Rate", "Lost Customers / Total Customers * 100", "percentage", "lower_better", "retention", "SalesKpiCalculator", 57),
        new("sales.net_revenue_retention", "sales", "Net Revenue Retention", "(MRR + Expansion - Contraction - Churn) / Starting MRR * 100", "percentage", "higher_better", "retention", "SalesKpiCalculator", 58),
        new("sales.pipeline_value", "sales", "Pipeline Value", "Sum(Opportunities * Probability)", "currency", "higher_better", "pipeline", "SalesKpiCalculator", 59),
        new("sales.win_rate", "sales", "Win Rate", "Won Deals / Total Deals * 100", "percentage", "higher_better", "pipeline", "SalesKpiCalculator", 60),
        new("sales.avg_deal_size", "sales", "Average Deal Size", "Revenue / Number of Deals", "currency", "higher_better", "pipeline", "SalesKpiCalculator", 61),

        // Marketing (6)
        new("marketing.cpl", "marketing", "Cost Per Lead", "Marketing Spend / Leads Generated", "currency", "lower_better", "acquisition", "MarketingKpiCalculator", 70),
        new("marketing.marketing_roi", "marketing", "Marketing ROI", "(Revenue from Marketing - Marketing Cost) / Marketing Cost * 100", "percentage", "higher_better", "performance", "MarketingKpiCalculator", 71),
        new("marketing.lead_conversion_rate", "marketing", "Lead Conversion Rate", "Customers / Leads * 100", "percentage", "higher_better", "conversion", "MarketingKpiCalculator", 72),
        new("marketing.website_conversion", "marketing", "Website Conversion Rate", "Conversions / Visitors * 100", "percentage", "higher_better", "conversion", "MarketingKpiCalculator", 73),
        new("marketing.email_open_rate", "marketing", "Email Open Rate", "Opens / Sent * 100", "percentage", "higher_better", "engagement", "MarketingKpiCalculator", 74),
        new("marketing.cpa", "marketing", "Cost Per Acquisition", "Marketing Spend / New Customers", "currency", "lower_better", "acquisition", "MarketingKpiCalculator", 75),

        // HR (8)
        new("hr.headcount", "hr", "Headcount", "Count(Active Employees)", "count", "target", "workforce", "HrKpiCalculator", 80),
        new("hr.turnover_rate", "hr", "Employee Turnover Rate", "Departures / Avg Headcount * 100", "percentage", "lower_better", "retention", "HrKpiCalculator", 81),
        new("hr.retention_rate", "hr", "Employee Retention Rate", "100 - Turnover Rate", "percentage", "higher_better", "retention", "HrKpiCalculator", 82),
        new("hr.cost_per_hire", "hr", "Cost Per Hire", "Total Recruiting Cost / New Hires", "currency", "lower_better", "recruitment", "HrKpiCalculator", 83),
        new("hr.time_to_hire", "hr", "Time to Hire", "Avg Days from Posting to Acceptance", "days", "lower_better", "recruitment", "HrKpiCalculator", 84),
        new("hr.revenue_per_employee", "hr", "Revenue Per Employee", "Revenue / Headcount", "currency", "higher_better", "productivity", "HrKpiCalculator", 85),
        new("hr.absence_rate", "hr", "Absence Rate", "Absent Days / Working Days * 100", "percentage", "lower_better", "workforce", "HrKpiCalculator", 86),
        new("hr.training_cost_per_employee", "hr", "Training Cost Per Employee", "Training Budget / Headcount", "currency", "target", "development", "HrKpiCalculator", 87),

        // General Business (5)
        new("general.rule_of_40", "general", "Rule of 40", "Revenue Growth % + Profit Margin %", "percentage", "higher_better", "growth", "GeneralKpiCalculator", 90),
        new("general.burn_rate", "general", "Monthly Burn Rate", "Total Monthly Expenses", "currency", "lower_better", "efficiency", "GeneralKpiCalculator", 91),
        new("general.revenue_growth_yoy", "general", "Revenue Growth YoY", "(Current Revenue - Previous Revenue) / Previous Revenue * 100", "percentage", "higher_better", "growth", "GeneralKpiCalculator", 92),
        new("general.opex_growth_yoy", "general", "OpEx Growth YoY", "(Current OpEx - Previous OpEx) / Previous OpEx * 100", "percentage", "lower_better", "efficiency", "GeneralKpiCalculator", 93),
        new("general.debt_to_equity", "general", "Debt-to-Equity Ratio", "Total Debt / Shareholders Equity", "ratio", "lower_better", "risk", "GeneralKpiCalculator", 94),
    ];
}
