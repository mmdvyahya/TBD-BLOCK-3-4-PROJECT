using UnityEngine;

public class HippoFoodItemVisual : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer itemRenderer;

    public void SetColor(Color color)
    {
        if (itemRenderer == null)
            itemRenderer = GetComponentInChildren<Renderer>();

        if (itemRenderer != null)
            itemRenderer.material.color = color;
    }
}