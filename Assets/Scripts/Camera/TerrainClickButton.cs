using UnityEngine;

public class TerrainClickButton : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform regionTransform;
    [SerializeField] private string regionName;

    void OnMouseDown()
    {
        if (uiManager != null)
        {
            uiManager.ShowTerrainPanel(regionName, regionTransform);
        }
    }
}
