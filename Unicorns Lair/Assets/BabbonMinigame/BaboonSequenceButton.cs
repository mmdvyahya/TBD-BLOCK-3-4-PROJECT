using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BaboonSequenceButton : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int buttonIndex;

    [Header("Visual")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.35f;

    private BaboonSequenceManager manager;

    public int ButtonIndex => buttonIndex;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (button != null)
            button.onClick.AddListener(PressButton);

        ApplyNormalColor();
    }

    public void Initialize(BaboonSequenceManager owner, int index)
    {
        manager = owner;
        buttonIndex = index;
    }

    private void PressButton()
    {
        manager?.NotifyButtonPressed(this);
    }

    public IEnumerator Flash()
    {
        if (buttonImage != null)
            buttonImage.color = highlightColor;

        yield return new WaitForSeconds(flashDuration);

        ApplyNormalColor();
    }

    public void ApplyNormalColor()
    {
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }
}