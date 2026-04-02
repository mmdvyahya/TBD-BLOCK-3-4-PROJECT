using System.Collections;
using UnityEngine;

public class HabitatPrototype : MonoBehaviour
{
    [Header("Habitat Parts")]
    [SerializeField] private GameObject habitatVisualRoot;
    [SerializeField] private GameObject fence;
    [SerializeField] private GameObject grass;
    [SerializeField] private GameObject burrow;
    [SerializeField] private GameObject prairieDog;
    [SerializeField] private GameObject highlight;

    [Header("Coin")]
    [SerializeField] private GameObject coinObject;
    [SerializeField] private float coinDelay = 2f;
    [SerializeField] private int firstCoinValue = 10;

    private bool isBuilt = false;

    private void Start()
    {
        if (habitatVisualRoot != null) habitatVisualRoot.SetActive(false);
        if (fence != null) fence.SetActive(false);
        if (grass != null) grass.SetActive(false);
        if (burrow != null) burrow.SetActive(false);
        if (prairieDog != null) prairieDog.SetActive(false);
        if (highlight != null) highlight.SetActive(false);
        if (coinObject != null) coinObject.SetActive(false);
    }

    public void BuildHabitat()
    {
        Debug.Log("BuildHabitat called");

        if (isBuilt) return;

        isBuilt = true;
        StartCoroutine(BuildSequence());
    }

    private IEnumerator BuildSequence()
    {
        if (habitatVisualRoot != null)
            habitatVisualRoot.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        if (fence != null) fence.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        if (grass != null) grass.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        if (burrow != null) burrow.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        if (prairieDog != null) prairieDog.SetActive(true);

        yield return new WaitForSeconds(coinDelay);

        if (coinObject != null)
        {
            coinObject.SetActive(true);

            CoinPrototype coin = coinObject.GetComponent<CoinPrototype>();
            if (coin != null)
                coin.Setup(true, firstCoinValue);
        }
    }

    public void SetHighlight(bool value)
    {
        if (highlight != null)
            highlight.SetActive(value);
    }
}