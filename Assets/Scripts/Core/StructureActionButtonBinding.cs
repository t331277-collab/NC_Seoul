using UnityEngine;

public class StructureActionButtonBinding : MonoBehaviour
{
    public enum ActionKind
    {
        Invest,
        Repair,
        Destroy
    }

    private StructureActionManager actionManager;
    private DistrictStructurePanelManager sourcePanelManager;
    private GameObject targetObject;
    private StructDefinitionData definition;
    private string displayName;
    private ActionKind actionKind;

    public void Configure(StructureActionManager manager, DistrictStructurePanelManager panelManager, GameObject target, StructDefinitionData structDefinition, string structDisplayName, ActionKind kind)
    {
        actionManager = manager;
        sourcePanelManager = panelManager;
        targetObject = target;
        definition = structDefinition;
        displayName = structDisplayName;
        actionKind = kind;
    }

    public void InvokeAction()
    {
        if (actionManager == null || targetObject == null || definition == null)
        {
            return;
        }

        if (actionKind == ActionKind.Invest)
        {
            actionManager.OpenInvestPanel(targetObject, definition, displayName, sourcePanelManager);
        }
        else if (actionKind == ActionKind.Repair)
        {
            actionManager.OpenRepairPanel(targetObject, definition, displayName, sourcePanelManager);
        }
        else if (actionKind == ActionKind.Destroy)
        {
            actionManager.OpenDestroyPanel(targetObject, definition, displayName, sourcePanelManager);
        }
    }
}
