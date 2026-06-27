using UnityEngine;

public class StructureInvestmentStateBootstrap : MonoBehaviour
{
    [SerializeField] private string seoulRootName = "Seoul";

    private void Awake()
    {
        EnsureInvestmentStates();
    }

    public int EnsureInvestmentStates()
    {
        GameObject seoulObject = GameObject.Find(seoulRootName);
        if (seoulObject == null)
        {
            return 0;
        }

        int configuredCount = 0;
        Transform[] transforms = seoulObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i += 1)
        {
            Transform target = transforms[i];
            if (!StructureInvestmentState.IsInvestmentTargetName(target.name))
            {
                continue;
            }

            StructureInvestmentState state = target.GetComponent<StructureInvestmentState>();
            if (state == null)
            {
                state = target.gameObject.AddComponent<StructureInvestmentState>();
            }

            state.ConfigureForStructureName(target.name);
            configuredCount += 1;
        }

        return configuredCount;
    }
}
