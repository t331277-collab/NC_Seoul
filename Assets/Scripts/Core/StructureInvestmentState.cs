using UnityEngine;

[DisallowMultipleComponent]
public class StructureInvestmentState : MonoBehaviour
{
    public enum InvestmentStructureKind
    {
        Unknown,
        House,
        CommonFacility,
        UniqueStructure
    }

    public int totalInvestmentAttemptCount;
    public int successfulInvestmentCount;
    public int failedInvestmentCount;

    public bool hasPendingInvestment;
    public int pendingResolveYear;
    public int pendingCost;
    public float pendingSuccessChance;
    public string pendingRegionName;
    public string pendingRegionDisplayName;
    public string pendingStructureDisplayName;

    public bool lastInvestmentSucceeded;
    public int lastResolvedYear;

    public int modelStage;
    public int maxSuccessfulInvestments;
    public float currentStatMultiplier = 1f;
    public int permanentMilestoneStage;
    public GameObject activeVisualInstance;
    public GameObject pendingWorkVisualInstance;

    [SerializeField] private InvestmentStructureKind structureKind = InvestmentStructureKind.Unknown;

    public InvestmentStructureKind StructureKind { get { return structureKind; } }
    public bool IsAtSuccessLimit { get { return maxSuccessfulInvestments > 0 && successfulInvestmentCount >= maxSuccessfulInvestments; } }

    public void ConfigureForStructureName(string structureName)
    {
        structureKind = ResolveStructureKind(structureName);
        maxSuccessfulInvestments = ResolveMaxSuccessfulInvestments(structureKind);
        RefreshCurrentStatMultiplier();
    }

    public float RefreshCurrentStatMultiplier()
    {
        currentStatMultiplier = CalculateStatMultiplier(structureKind, successfulInvestmentCount);
        permanentMilestoneStage = ResolvePermanentMilestoneStage(structureKind, successfulInvestmentCount);
        return currentStatMultiplier;
    }

    public static float CalculateStatMultiplier(InvestmentStructureKind kind, int successCount)
    {
        int clampedSuccessCount = Mathf.Max(0, successCount);
        if (kind == InvestmentStructureKind.UniqueStructure)
        {
            return 1f + 0.1f * Mathf.Min(clampedSuccessCount, 3);
        }

        if (kind == InvestmentStructureKind.House || kind == InvestmentStructureKind.CommonFacility)
        {
            if (clampedSuccessCount >= 10)
            {
                return 4f * (1f + 0.1f * (clampedSuccessCount - 10));
            }

            if (clampedSuccessCount >= 5)
            {
                return 2f * (1f + 0.1f * (clampedSuccessCount - 5));
            }

            return 1f + 0.1f * clampedSuccessCount;
        }

        return 1f;
    }

    private static int ResolvePermanentMilestoneStage(InvestmentStructureKind kind, int successCount)
    {
        if (kind != InvestmentStructureKind.House && kind != InvestmentStructureKind.CommonFacility)
        {
            return 0;
        }

        if (successCount >= 10)
        {
            return 2;
        }

        if (successCount >= 5)
        {
            return 1;
        }

        return 0;
    }

    public static InvestmentStructureKind ResolveStructureKind(string structureName)
    {
        if (IsHouseName(structureName))
        {
            return InvestmentStructureKind.House;
        }

        if (IsCommonFacilityName(structureName))
        {
            return InvestmentStructureKind.CommonFacility;
        }

        if (!string.IsNullOrEmpty(structureName) && structureName.StartsWith("Stru_") && structureName != "Stru_CommonSense")
        {
            return InvestmentStructureKind.UniqueStructure;
        }

        return InvestmentStructureKind.Unknown;
    }

    public static int ResolveMaxSuccessfulInvestments(InvestmentStructureKind kind)
    {
        if (kind == InvestmentStructureKind.House)
        {
            return 15;
        }

        if (kind == InvestmentStructureKind.CommonFacility)
        {
            return 10;
        }

        if (kind == InvestmentStructureKind.UniqueStructure)
        {
            return 3;
        }

        return 0;
    }

    public static bool IsInvestmentTargetName(string structureName)
    {
        return ResolveStructureKind(structureName) != InvestmentStructureKind.Unknown;
    }

    public static bool IsHouseName(string structureName)
    {
        return structureName == "House1" ||
               structureName == "House2" ||
               structureName == "House3" ||
               structureName == "House4";
    }

    public static bool IsCommonFacilityName(string structureName)
    {
        return structureName == "DistrictOffice" ||
               structureName == "School" ||
               structureName == "University";
    }
}
