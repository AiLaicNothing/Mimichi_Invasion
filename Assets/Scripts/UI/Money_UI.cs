using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the player's money. Flashes red when a purchase fails.
/// </summary>
public class Money_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [Tooltip("{0} = money")]
    [SerializeField] private string format = "$ {0}";

    [Header("Not enough money")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color deniedColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float flashTime = 0.4f;

    private Economy_Manager economy;
    private Coroutine flash;

    private IEnumerator Start()
    {
        while (Economy_Manager.Instance == null) yield return null;

        economy = Economy_Manager.Instance;
        economy.OnMoneyChanged += HandleMoneyChanged;
        economy.OnPurchaseFailed += HandlePurchaseFailed;

        moneyText.text = string.Format(format, economy.Money);
    }

    private void OnDestroy()
    {
        if (economy == null) return;
        economy.OnMoneyChanged -= HandleMoneyChanged;
        economy.OnPurchaseFailed -= HandlePurchaseFailed;
    }

    private void HandleMoneyChanged(int money, int delta)
    {
        moneyText.text = string.Format(format, money);
    }

    private void HandlePurchaseFailed(int cost)
    {
        if (flash != null) StopCoroutine(flash);
        flash = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        moneyText.color = deniedColor;
        yield return new WaitForSeconds(flashTime);
        moneyText.color = normalColor;
        flash = null;
    }
}
