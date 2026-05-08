using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingCoins = 100;

    public int Coins { get; private set; }

    private readonly HashSet<string> _boughtItems = new();
    private readonly HashSet<string> _builtItems = new();

    public delegate void OnCoinsChanged(int newAmount);
    public event OnCoinsChanged CoinsChanged;

    public delegate void OnItemStateChanged(string itemId);
    public event OnItemStateChanged ItemBought;
    public event OnItemStateChanged ItemBuilt;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadState();
    }

    public static GameStateManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameStateManager");
        return go.AddComponent<GameStateManager>();
    }

    public bool TrySpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        SaveState();
        CoinsChanged?.Invoke(Coins);
        return true;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        SaveState();
        CoinsChanged?.Invoke(Coins);
    }

    public void NotifyItemBought(string itemId)
    {
        if (_boughtItems.Contains(itemId)) return;
        _boughtItems.Add(itemId);
        SaveState();
        ItemBought?.Invoke(itemId);
    }

    public void NotifyItemBuilt(string itemId)
    {
        if (_builtItems.Contains(itemId)) return;
        _builtItems.Add(itemId);
        SaveState();
        ItemBuilt?.Invoke(itemId);
    }

    public bool IsBought(string itemId) => _boughtItems.Contains(itemId);
    public bool IsBuilt(string itemId) => _builtItems.Contains(itemId);

    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        Coins = startingCoins;
        _boughtItems.Clear();
        _builtItems.Clear();
        CoinsChanged?.Invoke(Coins);
        Debug.Log("[GameStateManager] Progress reset!");
    }

    void Update()
    {
#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetAllProgress();
        }
#endif
    }

    void SaveState()
    {
        PlayerPrefs.SetInt("coins", Coins);
        PlayerPrefs.SetString("bought", string.Join(",", _boughtItems));
        PlayerPrefs.SetString("built", string.Join(",", _builtItems));
        PlayerPrefs.Save();
    }

    void LoadState()
    {
        Coins = PlayerPrefs.GetInt("coins", startingCoins);

        var bought = PlayerPrefs.GetString("bought", "");
        if (!string.IsNullOrEmpty(bought))
            foreach (var id in bought.Split(','))
                if (!string.IsNullOrEmpty(id)) _boughtItems.Add(id);

        var built = PlayerPrefs.GetString("built", "");
        if (!string.IsNullOrEmpty(built))
            foreach (var id in built.Split(','))
                if (!string.IsNullOrEmpty(id)) _builtItems.Add(id);
    }


}