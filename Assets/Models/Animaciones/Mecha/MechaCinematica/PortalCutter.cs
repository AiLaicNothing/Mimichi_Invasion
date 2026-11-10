using UnityEngine;

public class PortalCutter : MonoBehaviour
{
    public Material[] portalMaterials;

    void Update()
    {
        foreach (Material mat in portalMaterials)
        {
            if (!mat) continue;

            mat.SetVector("_PortalPos", transform.position);
            mat.SetVector("_PortalNormal", transform.forward.normalized);
        }
    }
}