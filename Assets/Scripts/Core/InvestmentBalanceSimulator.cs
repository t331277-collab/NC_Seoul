using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class InvestmentBalanceSimulator : MonoBehaviour
{
    private static readonly int[] HouseInvestmentCosts = new int[] { 80, 100, 125, 155, 195, 250, 320, 410, 520, 660, 850, 1100, 1400, 1800, 2300 };

    [SerializeField] private string structDefinitionRelativePath = "Data/StructDefinition.csv";
    [SerializeField] private int simulationRuns = 1000;
    [SerializeField] private int startYear = 1945;
    [SerializeField] private int maxYear = 2050;
    [SerializeField] private int randomSeed = 1945;

    [ContextMenu("Run Investment Balance Simulation")]
    public void RunInvestmentBalanceSimulation()
    {
        Debug.Log(RunSimulationToString());
    }

    public string RunSimulationToString()
    {
        Dictionary<string, StructDefinitionData> definitions = StructDefinitionDatabase.Load(structDefinitionRelativePath);
        ProductionValues baseline = CalculateBaselineProduction(definitions);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[InvestmentBalanceSimulator] runs=" + simulationRuns + ", years=" + startYear + "-" + maxYear);
        builder.AppendLine("Baseline annual production: money=" + baseline.Money + ", science=" + baseline.Science + ", people=" + baseline.People + ", convenience=" + baseline.Convenience + ", love=" + baseline.Love);
        AppendSimulation(builder, definitions, baseline, "House1", 5, 10);
        AppendSimulation(builder, definitions, baseline, "School", 5, 10);
        AppendSimulation(builder, definitions, baseline, "DistrictOffice", 5, 10);
        AppendSimulation(builder, definitions, baseline, "University", 5, 10);
        AppendSimulation(builder, definitions, baseline, PickUniqueStructure(definitions, false), 3, 0);
        AppendSimulation(builder, definitions, baseline, PickUniqueStructure(definitions, true), 3, 0);
        return builder.ToString();
    }

    private void AppendSimulation(StringBuilder builder, Dictionary<string, StructDefinitionData> definitions, ProductionValues baseline, string structureName, int firstGoal, int secondGoal)
    {
        if (string.IsNullOrEmpty(structureName))
        {
            builder.AppendLine("Unique structure target not found.");
            return;
        }

        StructDefinitionData definition;
        if (!definitions.TryGetValue(structureName, out definition))
        {
            builder.AppendLine(structureName + " definition not found.");
            return;
        }

        SimulationSummary summary = Simulate(definition, baseline, firstGoal, secondGoal);
        builder.AppendLine("Target=" + structureName + " (" + definition.DisplayName + ") kind=" + StructureInvestmentState.ResolveStructureKind(structureName));
        builder.AppendLine("  goal " + firstGoal + " success: avgYear=" + FormatAverageYear(summary.FirstGoalYearTotal, summary.FirstGoalReached) + ", reached=" + summary.FirstGoalReached + "/" + simulationRuns);
        if (secondGoal > 0)
        {
            builder.AppendLine("  goal " + secondGoal + " success: avgYear=" + FormatAverageYear(summary.SecondGoalYearTotal, summary.SecondGoalReached) + ", reached=" + summary.SecondGoalReached + "/" + simulationRuns);
        }

        builder.AppendLine("  avgAttempts=" + ((float)summary.AttemptTotal / simulationRuns).ToString("F1") + ", avgSuccess=" + ((float)summary.SuccessTotal / simulationRuns).ToString("F1") + ", avgFinalMoney=" + ((float)summary.FinalMoneyTotal / simulationRuns).ToString("F1") + ", avgFinalScience=" + ((float)summary.FinalScienceTotal / simulationRuns).ToString("F1"));
    }

    private SimulationSummary Simulate(StructDefinitionData definition, ProductionValues baseline, int firstGoal, int secondGoal)
    {
        SimulationSummary summary = new SimulationSummary();
        int runs = Mathf.Max(1, simulationRuns);
        for (int run = 0; run < runs; run += 1)
        {
            System.Random random = new System.Random(randomSeed + run);
            int money = 0;
            int science = 0;
            int successCount = 0;
            int attempts = 0;
            bool pending = false;
            float pendingChance = 0f;
            int firstGoalYear = 0;
            int secondGoalYear = 0;

            for (int year = startYear; year <= maxYear; year += 1)
            {
                if (pending)
                {
                    attempts += 1;
                    if (random.NextDouble() <= pendingChance)
                    {
                        successCount += 1;
                        if (firstGoalYear == 0 && successCount >= firstGoal)
                        {
                            firstGoalYear = year;
                        }

                        if (secondGoal > 0 && secondGoalYear == 0 && successCount >= secondGoal)
                        {
                            secondGoalYear = year;
                        }
                    }

                    pending = false;
                }

                ProductionValues annualProduction = CalculateAnnualProductionWithTargetMultiplier(baseline, definition, successCount);
                money += annualProduction.Money;
                science += annualProduction.Science;

                if (successCount >= StructureInvestmentState.ResolveMaxSuccessfulInvestments(StructureInvestmentState.ResolveStructureKind(definition.Name)))
                {
                    continue;
                }

                int cost = GetInvestmentCost(definition, successCount);
                if (!pending && money >= cost)
                {
                    money -= cost;
                    pendingChance = GetInvestmentSuccessChance(definition, successCount, science);
                    pending = true;
                }
            }

            if (firstGoalYear > 0)
            {
                summary.FirstGoalReached += 1;
                summary.FirstGoalYearTotal += firstGoalYear;
            }

            if (secondGoal > 0 && secondGoalYear > 0)
            {
                summary.SecondGoalReached += 1;
                summary.SecondGoalYearTotal += secondGoalYear;
            }

            summary.AttemptTotal += attempts;
            summary.SuccessTotal += successCount;
            summary.FinalMoneyTotal += money;
            summary.FinalScienceTotal += science;
        }

        return summary;
    }

    private ProductionValues CalculateBaselineProduction(Dictionary<string, StructDefinitionData> definitions)
    {
        ProductionValues values = new ProductionValues();
        GameObject seoul = GameObject.Find("Seoul");
        if (seoul == null)
        {
            return values;
        }

        AddActiveStructureProduction(seoul.transform, definitions, ref values);
        return values;
    }

    private void AddActiveStructureProduction(Transform parent, Dictionary<string, StructDefinitionData> definitions, ref ProductionValues values)
    {
        foreach (Transform child in parent)
        {
            StructDefinitionData definition;
            if (child.gameObject.activeInHierarchy && child.name != "Stru_CommonSense" && definitions.TryGetValue(child.name, out definition))
            {
                values.Money += definition.MoneyProduction;
                values.Science += definition.ScienceIncrease;
                values.People += definition.PeopleIncrease;
                values.Convenience += definition.ConvenienceIncrease;
                values.Love += definition.LoveIncrease;
            }

            AddActiveStructureProduction(child, definitions, ref values);
        }
    }

    private ProductionValues CalculateAnnualProductionWithTargetMultiplier(ProductionValues baseline, StructDefinitionData targetDefinition, int successCount)
    {
        ProductionValues values = baseline;
        float multiplier = StructureInvestmentState.CalculateStatMultiplier(StructureInvestmentState.ResolveStructureKind(targetDefinition.Name), successCount);
        values.Money += Mathf.CeilToInt(targetDefinition.MoneyProduction * multiplier) - targetDefinition.MoneyProduction;
        values.Science += Mathf.CeilToInt(targetDefinition.ScienceIncrease * multiplier) - targetDefinition.ScienceIncrease;
        values.People += Mathf.CeilToInt(targetDefinition.PeopleIncrease * multiplier) - targetDefinition.PeopleIncrease;
        values.Convenience += Mathf.CeilToInt(targetDefinition.ConvenienceIncrease * multiplier) - targetDefinition.ConvenienceIncrease;
        values.Love += Mathf.CeilToInt(targetDefinition.LoveIncrease * multiplier) - targetDefinition.LoveIncrease;
        return values;
    }

    private int GetInvestmentCost(StructDefinitionData definition, int successCount)
    {
        StructureInvestmentState.InvestmentStructureKind kind = StructureInvestmentState.ResolveStructureKind(definition.Name);
        int successIndex = Mathf.Max(0, successCount);
        if (kind == StructureInvestmentState.InvestmentStructureKind.House)
        {
            return HouseInvestmentCosts[Mathf.Clamp(successIndex, 0, HouseInvestmentCosts.Length - 1)];
        }

        if (kind == StructureInvestmentState.InvestmentStructureKind.CommonFacility)
        {
            int costIndex = Mathf.Clamp(successIndex, 0, Mathf.Min(9, HouseInvestmentCosts.Length - 1));
            return Mathf.Max(definition.InvestCost, HouseInvestmentCosts[costIndex]);
        }

        if (kind == StructureInvestmentState.InvestmentStructureKind.UniqueStructure)
        {
            int resourceTotal = GetResourceTotal(definition);
            int scaledCost = definition.InvestCost + resourceTotal * 20 * (successIndex + 1);
            return Mathf.Max(definition.InvestCost, scaledCost);
        }

        return definition.InvestCost;
    }

    private float GetInvestmentSuccessChance(StructDefinitionData definition, int successCount, int science)
    {
        StructureInvestmentState.InvestmentStructureKind kind = StructureInvestmentState.ResolveStructureKind(definition.Name);
        if (kind == StructureInvestmentState.InvestmentStructureKind.House)
        {
            if (successCount < StructureInvestmentState.FirstUpgradeSuccessCount)
            {
                return Mathf.Clamp(0.30f + science * 0.00028f, 0.30f, 0.75f);
            }

            if (successCount < StructureInvestmentState.SecondUpgradeSuccessCount)
            {
                return Mathf.Clamp(0.08f + science * 0.00036f, 0.10f, 0.88f);
            }

            return Mathf.Clamp(0.05f + science * 0.00022f, 0.08f, 0.65f);
        }

        if (kind == StructureInvestmentState.InvestmentStructureKind.CommonFacility)
        {
            int resourceTotal = GetResourceTotal(definition);
            float resourcePenalty = Mathf.Clamp(resourceTotal * 0.01f, 0f, 0.20f);
            if (successCount < StructureInvestmentState.FirstUpgradeSuccessCount)
            {
                return Mathf.Clamp(0.25f - resourcePenalty + science * 0.00024f, 0.12f, 0.70f);
            }

            return Mathf.Clamp(0.06f - resourcePenalty * 0.5f + science * 0.00030f, 0.08f, 0.82f);
        }

        if (kind == StructureInvestmentState.InvestmentStructureKind.UniqueStructure)
        {
            int resourceTotal = GetResourceTotal(definition);
            float baseChance = Mathf.Clamp(0.28f - resourceTotal * 0.015f, 0.06f, 0.25f);
            float requiredScience = 400f + resourceTotal * 260f;
            float scienceBonus = Mathf.Clamp01(science / requiredScience) * 0.65f;
            return Mathf.Clamp(baseChance + scienceBonus, 0.05f, 0.85f);
        }

        return 0f;
    }

    private string PickUniqueStructure(Dictionary<string, StructDefinitionData> definitions, bool pickHighestResourceTotal)
    {
        string selectedName = string.Empty;
        int selectedResourceTotal = pickHighestResourceTotal ? -1 : int.MaxValue;
        foreach (KeyValuePair<string, StructDefinitionData> pair in definitions)
        {
            if (StructureInvestmentState.ResolveStructureKind(pair.Key) != StructureInvestmentState.InvestmentStructureKind.UniqueStructure)
            {
                continue;
            }

            int resourceTotal = GetResourceTotal(pair.Value);
            if ((pickHighestResourceTotal && resourceTotal > selectedResourceTotal) || (!pickHighestResourceTotal && resourceTotal < selectedResourceTotal))
            {
                selectedResourceTotal = resourceTotal;
                selectedName = pair.Key;
            }
        }

        return selectedName;
    }

    private int GetResourceTotal(StructDefinitionData definition)
    {
        if (definition == null)
        {
            return 0;
        }

        return Mathf.Max(0, definition.MoneyProduction)
            + Mathf.Max(0, definition.PeopleIncrease)
            + Mathf.Max(0, definition.ScienceIncrease)
            + Mathf.Max(0, definition.LoveIncrease)
            + Mathf.Max(0, definition.ConvenienceIncrease);
    }

    private string FormatAverageYear(int totalYear, int reachedCount)
    {
        if (reachedCount <= 0)
        {
            return "not reached";
        }

        return ((float)totalYear / reachedCount).ToString("F1");
    }

    private struct ProductionValues
    {
        public int Money;
        public int Science;
        public int People;
        public int Convenience;
        public int Love;
    }

    private struct SimulationSummary
    {
        public int FirstGoalReached;
        public int SecondGoalReached;
        public int FirstGoalYearTotal;
        public int SecondGoalYearTotal;
        public int AttemptTotal;
        public int SuccessTotal;
        public int FinalMoneyTotal;
        public int FinalScienceTotal;
    }
}
