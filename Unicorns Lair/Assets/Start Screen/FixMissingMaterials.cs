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

                Color col;
                if (mats[i].HasProperty("_BaseColor"))
                    col = mats[i].GetColor("_BaseColor");
                else if (mats[i].HasProperty("_Color"))
                    col = mats[i].GetColor("_Color");
                else
                    continue;

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