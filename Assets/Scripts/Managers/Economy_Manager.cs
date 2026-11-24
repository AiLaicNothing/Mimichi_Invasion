using System;
using UnityEngine;

/// <summary>
/// Run currency. Every enemy kill adds that enemy's MoneyReward.
/// Spend with TrySpend (crates, shops...).
/// </summary>
public class Economy_Manager : MonoBehaviour
{
    public static Economy_Manager Instance;

    [SerializeField] private int startingMoney = 0;

    public int Money { get; private set; }
    /// <summary>Total earned this run (for the death screen / stats).</summary>
    public int TotalEarned { get; private set; }

    /// <summary>(newAmount, delta) — delta is negative when spending.</summary>
    public event Action<int, int> OnMoneyChanged;
    /// <summary>(cost) — tried to buy something without enough money. Hook a "not enough money" feedback here.</summary>
    public event Action<int> OnPurchaseFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Money = startingMoney;
    }

    private void OnEnable() => Enemy.OnAnyEnemyDied += HandleEnemyDied;
    private void OnDisable() => Enemy.OnAnyEnemyDied -= HandleEnemyDied;

    private void HandleEnemyDied(Enemy enemy) => AddMoney(enemy.MoneyReward);

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        Money += amount;
        TotalEarned += amount;
        OnMoneyChanged?.Invoke(Money, amount);
    }

    public bool CanAfford(int cost) => Money >= cost;

    /// <summary>Spends the money if there is enough. Returns false (and spends nothing) otherwise.</summary>
    public bool TrySpend(int cost)
    {
        if (cost < 0) return false;

        if (Money < cost)
        {
            OnPurchaseFailed?.Invoke(cost);
            return false;
        }

        Money -= cost;
        OnMoneyChanged?.Invoke(Money, -cost);
        return true;
    }
}
