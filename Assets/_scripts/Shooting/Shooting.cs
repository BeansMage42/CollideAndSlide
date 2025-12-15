using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class Shooting : MonoBehaviour
{
    [field:Header("FIRING BEHAVIOUR")]
    [SerializeField] private float shootDelay;
    [SerializeField] private bool isFullyAuto;
    [SerializeField] private Vector2 bulletSpreadVariance;
    [SerializeField] private int numBulletsPerShot;
    [SerializeField] private Transform firePoint;

    [Header("GUN STATS")]
    [SerializeField] private int magSize;
    private int bulletsRemaining;
    [SerializeField] private int magCount;
    [SerializeField] private float reloadTime;
    [SerializeField] private float range;
    private int totalBulletRemaining;


    private bool onDelay = false;
    private bool isFiring;
    private bool isReloading;

    
    private void Start()
    {
        bulletsRemaining = magSize ;
        totalBulletRemaining = magCount * magSize;
        UIManager.Instance.UpdateMagazineUI(bulletsRemaining, totalBulletRemaining);
    }
    public void ShootInput(InputAction.CallbackContext context)
    {
        if (context.started) 
        {
            StartShooting();
        }
        if(context.canceled) 
        {
           StopShooting();
        }
    }
    public void ReloadInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            if (!isReloading && totalBulletRemaining > 0)
            {
              //  StopShooting();

                StartCoroutine(ReloadDelay());

            }

        }
    }

    private void StartShooting()
    {
        isFiring = true;
        Shoot();
    }
    
    private void StopShooting()
    {
        isFiring = false;
    }

    private bool CanShoot()
    {
        
        if(bulletsRemaining > 0)
        {
            return !onDelay && !isReloading;
        }
        else
        {
            if(!isReloading && totalBulletRemaining > 0)
            {
                StartCoroutine(ReloadDelay());
                
            }
            return false;
        }
           
    }

    private void Shoot()
    {
        if (CanShoot())
        {
            
          //  print("can shoot shoot");
            for(int i = 0; i < numBulletsPerShot; i++)
            {
                if(bulletsRemaining > 0)
                {
                    SendShot();
                }
                else
                {
                    break;
                }
            }
            
            StartCoroutine(RefireTimer());
        }

    }

    private void SendShot()
    {
        bulletsRemaining--;
        UIManager.Instance.UpdateMagazineUI(bulletsRemaining, totalBulletRemaining);
        float xVariance = Random.Range(-bulletSpreadVariance.x,bulletSpreadVariance.x);
        float yVariance = Random.Range(-bulletSpreadVariance.y,bulletSpreadVariance.y);

        Vector3 fireDir = Camera.main.transform.forward + new Vector3(xVariance,yVariance,0f);
        Vector3 origin = Camera.main.transform.position;

        RaycastHit hit;
        Debug.DrawLine(origin, fireDir.normalized * range, Color.black,0.2f);
        if(Physics.Raycast(origin,fireDir.normalized, out hit, range ))
        {
            if(hit.collider.gameObject.TryGetComponent(out Shootable shot))
            {
                shot.HitTarget(hit);
            }
         //   print("hit: " + hit.collider.gameObject.name);

        }

    }

    private IEnumerator RefireTimer()
    {
       // print("start refireTimer" + bulletsRemaining);
        onDelay = true;
        yield return new WaitForSeconds(shootDelay);
        onDelay = false;
        if (isFiring && isFullyAuto)
        {
           // print("fully auto try shooting again");
            Shoot();
        }
    }
    private IEnumerator ReloadDelay()
    {
        //print("start reloading");
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
       // print("stop reloading");
        isReloading = false;
        if (totalBulletRemaining < (magSize - bulletsRemaining))
        {
            bulletsRemaining += totalBulletRemaining;
            totalBulletRemaining = 0;
        }
        else
        {
            totalBulletRemaining -= (magSize - bulletsRemaining);
            bulletsRemaining = magSize;
        }
        UIManager.Instance.UpdateMagazineUI(bulletsRemaining, totalBulletRemaining);
        if(isFullyAuto && isFiring)
        {
            StartCoroutine (RefireTimer());
        }

    }

}
