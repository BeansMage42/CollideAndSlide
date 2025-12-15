using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI magazineUI;

    private void Awake()
    {
        if (Instance != null) 
        {
            if(Instance != this)
            {
                Destroy(this);
            }
        }
        else
        {
            Instance = this;
        }

    }
    
    public void UpdateMagazineUI(int bulletInMag, int totalBulletRemaining)
    {
        magazineUI.text = $"{totalBulletRemaining}/{bulletInMag}";
    }
}
