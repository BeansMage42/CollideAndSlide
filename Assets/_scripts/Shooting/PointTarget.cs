using UnityEngine;

public class PointTarget : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Shootable>().triggered += HitEvent;
    }

    public void HitEvent(RaycastHit hit)
    {
        float points = 10 - Mathf.Floor(Vector3.Distance(transform.position,hit.point)*10 );
        print($"distance from center = {Vector3.Distance(transform.position, hit.point)}, scaled:{ points} ");
        Destroy(gameObject);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
