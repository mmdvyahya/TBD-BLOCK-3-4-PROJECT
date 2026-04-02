using UnityEngine;

public class CoinPrototype : MonoBehaviour
{
    private bool isFirstCoin;
    private int value;

    public void Setup(bool firstCoin, int coinValue)
    {
        isFirstCoin = firstCoin;
        value = coinValue;
    }

    public void OnCoinClicked()
    {
        Debug.Log("Coin clicked via raycast!");

        if (isFirstCoin)
            PrototypeGameManager.Instance.NotifyFirstCoinCollected(value);
        else
            PrototypeGameManager.Instance.NotifyExtraCoinCollected(value);

        gameObject.SetActive(false);
    }
}