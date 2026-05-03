using System.Collections;
using UnityEngine;
using TMPro;

public class HippoFoodSortingManager : MonoBehaviour
{
    private enum SortingState
    {
        WaitingForSwipe,
        MovingItem,
        Complete
    }

    private enum FoodCategory
    {
        Approved,
        NotSuitable
    }

    [System.Serializable]
    private class FoodItemData
    {
        public string foodName;
        public FoodCategory correctCategory;
        public Color displayColor = Color.white;
    }

    [Header("References")]
    [SerializeField] private HippoSwipeInput swipeInput;

    [Header("Scene Objects")]
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Transform approvedZone;
    [SerializeField] private Transform notSuitableZone;
    [SerializeField] private GameObject foodItemPrefab;
    [SerializeField] private Transform hippoVisual;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text foodNameText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Food Items")]
    [SerializeField]
    private FoodItemData[] foodItems =
    {
        new FoodItemData { foodName = "Watermelon", correctCategory = FoodCategory.Approved, displayColor = Color.green },
        new FoodItemData { foodName = "Lettuce", correctCategory = FoodCategory.Approved, displayColor = Color.green },
        new FoodItemData { foodName = "Cabbage", correctCategory = FoodCategory.Approved, displayColor = Color.green },
        new FoodItemData { foodName = "Apples", correctCategory = FoodCategory.Approved, displayColor = Color.red },

        new FoodItemData { foodName = "Candy", correctCategory = FoodCategory.NotSuitable, displayColor = Color.magenta },
        new FoodItemData { foodName = "Chocolate", correctCategory = FoodCategory.NotSuitable, displayColor = new Color(0.35f, 0.18f, 0.08f) },
        new FoodItemData { foodName = "Chips", correctCategory = FoodCategory.NotSuitable, displayColor = Color.yellow },
        new FoodItemData { foodName = "Bread", correctCategory = FoodCategory.NotSuitable, displayColor = new Color(0.8f, 0.55f, 0.25f) },
    };

    [Header("Settings")]
    [SerializeField] private float moveDuration = 0.45f;
    [SerializeField] private float nextItemDelay = 0.35f;

    private SortingState currentState;
    private int currentItemIndex;
    private GameObject currentFoodObject;
    private FoodItemData currentFoodData;

    private void Start()
    {
        if (swipeInput == null)
            swipeInput = FindFirstObjectByType<HippoSwipeInput>();

        StartMinigame();
    }

    private void Update()
    {
        if (currentState != SortingState.WaitingForSwipe)
            return;

        if (swipeInput == null || currentFoodObject == null)
            return;

        if (swipeInput.IsDragging)
        {
            Vector3 targetPosition = swipeInput.DragWorldPosition;
            targetPosition.y = itemSpawnPoint.position.y;
            currentFoodObject.transform.position = Vector3.Lerp(
                currentFoodObject.transform.position,
                targetPosition,
                Time.deltaTime * 18f
            );
        }

        if (swipeInput.ReleasedLeftThisFrame)
            SortCurrentItem(FoodCategory.Approved);

        if (swipeInput.ReleasedRightThisFrame)
            SortCurrentItem(FoodCategory.NotSuitable);
    }

    public void StartMinigame()
    {
        currentState = SortingState.WaitingForSwipe;
        currentItemIndex = 0;

        SetInstruction("Swipe left = Approved | Swipe right = Not suitable");
        SetFeedback("");

        SpawnCurrentItem();
    }

    private void SpawnCurrentItem()
    {
        if (currentItemIndex >= foodItems.Length)
        {
            CompleteMinigame();
            return;
        }

        currentFoodData = foodItems[currentItemIndex];

        if (currentFoodObject != null)
            Destroy(currentFoodObject);

        currentFoodObject = Instantiate(foodItemPrefab, itemSpawnPoint.position, Quaternion.identity);

        HippoFoodItemVisual visual = currentFoodObject.GetComponent<HippoFoodItemVisual>();
        if (visual != null)
            visual.SetColor(currentFoodData.displayColor);

        SetFoodName(currentFoodData.foodName);
        currentState = SortingState.WaitingForSwipe;
    }

    private void SortCurrentItem(FoodCategory chosenCategory)
    {
        if (currentFoodObject == null)
            return;

        currentState = SortingState.MovingItem;

        bool correct = chosenCategory == currentFoodData.correctCategory;

        Transform targetZone = GetTargetZone(correct ? chosenCategory : currentFoodData.correctCategory);

        if (correct)
            SetFeedback("Correct!");
        else
            SetFeedback("Not quite! Sending it to the correct side.");

        StartCoroutine(MoveItemToZone(currentFoodObject.transform, targetZone.position, correct));
    }

    private Transform GetTargetZone(FoodCategory category)
    {
        return category == FoodCategory.Approved ? approvedZone : notSuitableZone;
    }

    private IEnumerator MoveItemToZone(Transform item, Vector3 targetPosition, bool correct)
    {
        Vector3 startPosition = item.position;
        float t = 0f;

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / moveDuration);
            item.position = Vector3.Lerp(startPosition, targetPosition, p);
            yield return null;
        }

        item.position = targetPosition;

        if (correct)
            StartCoroutine(HippoReactHappy());
        else
            StartCoroutine(HippoReactSmallShake());

        yield return new WaitForSeconds(nextItemDelay);

        Destroy(currentFoodObject);
        currentFoodObject = null;

        currentItemIndex++;
        SetFeedback("");

        SpawnCurrentItem();
    }

    private void CompleteMinigame()
    {
        currentState = SortingState.Complete;

        SetInstruction("");
        SetFoodName("");
        SetFeedback("Hippo food sorted!");

        StartCoroutine(HippoReactHappy());

        Debug.Log("[HippoFoodSorting] Minigame complete. Reward system connects here.");
    }

    private IEnumerator HippoReactHappy()
    {
        if (hippoVisual == null)
            yield break;

        Vector3 baseScale = hippoVisual.localScale;
        float t = 0f;

        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 20f) * 0.07f;
            hippoVisual.localScale = baseScale * s;
            yield return null;
        }

        hippoVisual.localScale = baseScale;
    }

    private IEnumerator HippoReactSmallShake()
    {
        if (hippoVisual == null)
            yield break;

        Vector3 basePos = hippoVisual.localPosition;
        float t = 0f;

        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float x = Mathf.Sin(t * 35f) * 0.06f;
            hippoVisual.localPosition = basePos + new Vector3(x, 0f, 0f);
            yield return null;
        }

        hippoVisual.localPosition = basePos;
    }

    private void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    private void SetFoodName(string text)
    {
        if (foodNameText != null)
            foodNameText.text = text;
    }

    private void SetFeedback(string text)
    {
        if (feedbackText != null)
            feedbackText.text = text;
    }
}