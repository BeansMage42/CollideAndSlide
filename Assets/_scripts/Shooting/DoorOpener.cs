
using UnityEngine;
using System.Collections.Generic;
public class DoorOpener : MonoBehaviour
{
    [SerializeField] private List<Shootable> shootable = new List<Shootable>();
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        foreach (var shoot in shootable)
        { 
            shoot.triggered += OpenSesame;
        }
    }

    private void OpenSesame(RaycastHit hit)
    {
        Shootable shot = hit.collider.gameObject.GetComponent<Shootable>();
        shot.triggered -= OpenSesame;
        shootable.Remove(shot);
        if(shootable.Count <= 0 ) animator.SetBool("Opened", true);
    }
}
