using UnityEngine;

public class WaterUVScroll : MonoBehaviour
{
    public float baseScrollX = 0.03f;
    public float baseScrollY = 0.02f;

    public float normalScrollX = -0.02f;
    public float normalScrollY = 0.04f;

    private Renderer waterRenderer;
    private Material waterMaterial;

    void Start()
    {
        waterRenderer = GetComponent<Renderer>();
        waterMaterial = waterRenderer.material;
    }

    void Update()
    {
        Vector2 baseOffset = new Vector2(
            Time.time * baseScrollX,
            Time.time * baseScrollY
        );

        Vector2 normalOffset = new Vector2(
            Time.time * normalScrollX,
            Time.time * normalScrollY
        );

        waterMaterial.SetTextureOffset("_BaseMap", baseOffset);
        waterMaterial.SetTextureOffset("_BumpMap", normalOffset);
    }
}