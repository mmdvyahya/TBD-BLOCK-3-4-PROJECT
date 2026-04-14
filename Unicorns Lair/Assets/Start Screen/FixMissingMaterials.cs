using UnityEngine;

public class FixMissingMaterials : MonoBehaviour
{
    void Start()
    {
        var whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        whiteMat.color = Color.white;
        if (whiteMat.HasProperty("_BaseColor"))
            whiteMat.SetColor("_BaseColor", Color.white);

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                Color col = mats[i].HasProperty("_BaseColor")
                    ? mats[i].GetColor("_BaseColor")
                    : mats[i].color;

                if (col.r > 0.7f && col.g < 0.3f && col.b < 0.3f)
                {
                    mats[i] = whiteMat;
                    changed = true;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }
}