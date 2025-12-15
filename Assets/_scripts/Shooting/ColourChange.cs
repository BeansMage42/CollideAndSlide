using UnityEngine;

public class ColourChange : MonoBehaviour
{
    [SerializeField] Material material;
    private Renderer renderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       renderer = GetComponent<Renderer>();
        GetComponent<Shootable>().triggered += MaterialSwap;
    }

    private void MaterialSwap(RaycastHit hit)
    {
        renderer.material = material;
    }
}
