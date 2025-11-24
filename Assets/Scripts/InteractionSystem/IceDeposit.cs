using UnityEngine;

public class IceDeposit : MonoBehaviour
{
    [Header("Настройки залежи")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private Transform spawnPoint;             

    private int currentHits = 0;

    public void Hit()
    {
        currentHits++;

        if (currentHits >= hitsRequired)
            BreakDeposit();
    }

    private void BreakDeposit()
    {
        if (mineralPrefab != null)
        {
            Instantiate(
                mineralPrefab,
                spawnPoint ? spawnPoint.position : transform.position,
                Quaternion.identity
            );
            GameDayManager.Instance.RegisterDepositBroken();
        }

        gameObject.SetActive(false);
    }
}
