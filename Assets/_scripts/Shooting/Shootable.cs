using System;
using UnityEngine;

public class Shootable : MonoBehaviour
{
    public Action<RaycastHit> triggered;

    public void HitTarget(RaycastHit hit)
    {
        triggered?.Invoke(hit);
    }
    
}
