using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] public string key;

    private Text _text;

    void Awake()
    {
        _text = GetComponent<Text>();
    }

    void OnEnable()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_text == null) _text = GetComponent<Text>();
        if (_text == null) return;
        if (string.IsNullOrEmpty(key)) return;
        if (LanguageManager.Instance == null) { LanguageManager.Ensure(); }
        if (LanguageManager.Instance == null) return;
        _text.text = LanguageManager.Instance.Get(key);
    }
}