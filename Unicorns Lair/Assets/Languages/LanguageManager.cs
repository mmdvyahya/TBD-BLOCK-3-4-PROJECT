using System.Collections.Generic;
using UnityEngine;

public enum Language { Nederlands, Deutsch, English }

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public Language CurrentLanguage { get; private set; } = Language.Nederlands;

    public delegate void OnLanguageChanged();
    public event OnLanguageChanged LanguageChanged;

    private static readonly Dictionary<string, Dictionary<Language, string>> _strings
        = new()
        {
            ["btn_spelen"] = new()
            {
                [Language.Nederlands] = "Spelen  ▶",
                [Language.Deutsch] = "Spielen  ▶",
                [Language.English] = "Play  ▶",
            },
            ["title_wildlands"] = new()
            {
                [Language.Nederlands] = "Wildlands Game",
                [Language.Deutsch] = "Wildlands Spiel",
                [Language.English] = "Wildlands Game",
            },
            ["btn_settings"] = new()
            {
                [Language.Nederlands] = "⚙",
                [Language.Deutsch] = "⚙",
                [Language.English] = "⚙",
            },
            ["settings_title"] = new()
            {
                [Language.Nederlands] = "Instellingen",
                [Language.Deutsch] = "Einstellungen",
                [Language.English] = "Settings",
            },
            ["settings_language"] = new()
            {
                [Language.Nederlands] = "Taal",
                [Language.Deutsch] = "Sprache",
                [Language.English] = "Language",
            },
            ["btn_close"] = new()
            {
                [Language.Nederlands] = "Sluiten",
                [Language.Deutsch] = "Schließen",
                [Language.English] = "Close",
            },
            ["shop_title"] = new()
            {
                [Language.Nederlands] = "🏗  Koop een Verblijf",
                [Language.Deutsch] = "🏗  Gehege kaufen",
                [Language.English] = "🏗  Buy a Habitat",
            },
            ["shop_currency"] = new()
            {
                [Language.Nederlands] = "💰  {0} munten",
                [Language.Deutsch] = "💰  {0} Münzen",
                [Language.English] = "💰  {0} coins",
            },
            ["shop_currency_short"] = new()
            {
                [Language.Nederlands] = "💰 {0} munten",
                [Language.Deutsch] = "💰 {0} Münzen",
                [Language.English] = "💰 {0} coins",
            },
            ["btn_buy"] = new()
            {
                [Language.Nederlands] = "Kopen voor 💰 {0}",
                [Language.Deutsch] = "Kaufen für 💰 {0}",
                [Language.English] = "Buy for 💰 {0}",
            },
            ["btn_plaatsen"] = new()
            {
                [Language.Nederlands] = "Plaatsen",
                [Language.Deutsch] = "Platzieren",
                [Language.English] = "Place",
            },
            ["bought"] = new()
            {
                [Language.Nederlands] = "✓  Gekocht!",
                [Language.Deutsch] = "✓  Gekauft!",
                [Language.English] = "✓  Bought!",
            },
            ["not_enough"] = new()
            {
                [Language.Nederlands] = "Niet genoeg munten! 💸",
                [Language.Deutsch] = "Nicht genug Münzen! 💸",
                [Language.English] = "Not enough coins! 💸",
            },
            ["place_instruction"] = new()
            {
                [Language.Nederlands] = "Tik op het raster om het verblijf te plaatsen",
                [Language.Deutsch] = "Tippe auf das Raster, um das Gehege zu platzieren",
                [Language.English] = "Tap the grid to place the habitat",
            },
            ["well_done"] = new()
            {
                [Language.Nederlands] = "Goed gedaan! 🎉",
                [Language.Deutsch] = "Gut gemacht! 🎉",
                [Language.English] = "Well done! 🎉",
            },
            ["building_label"] = new()
            {
                [Language.Nederlands] = "In aanbouw! 🔨",
                [Language.Deutsch] = "Im Bau! 🔨",
                [Language.English] = "Building! 🔨",
            },
            ["building_label"] = new()
            {
                [Language.Nederlands] = "In aanbouw! 🔨",
                [Language.Deutsch] = "Im Bau! 🔨",
                [Language.English] = "Under construction! 🔨",
            },
            ["btn_continue"] = new()
            {
                [Language.Nederlands] = "Doorgaan",
                [Language.Deutsch] = "Weiter",
                [Language.English] = "Continue",
            },
            ["pb_btn_blow"] = new()
            {
                [Language.Nederlands] = "💨 Blaas",
                [Language.Deutsch] = "💨 Pusten",
                [Language.English] = "💨 Blow",
            },
            ["pb_splash"] = new()
            {
                [Language.Nederlands] = "Splash! 🌊",
                [Language.Deutsch] = "Platsch! 🌊",
                [Language.English] = "Splash! 🌊",
            },
            ["pb_retry_text"] = new()
            {
                [Language.Nederlands] = "Splash! 🌊\nProbeer opnieuw!",
                [Language.Deutsch] = "Platsch! 🌊\nNochmal versuchen!",
                [Language.English] = "Splash! 🌊\nTry again!",
            },
            ["pb_retry_btn"] = new()
            {
                [Language.Nederlands] = "🐻‍❄️  Start!",
                [Language.Deutsch] = "🐻‍❄️  Los!",
                [Language.English] = "🐻‍❄️  Go!",
            },
            ["pb_reached_end"] = new()
            {
                [Language.Nederlands] = "Gelukt! 🐟 Voer de ijsbeer!",
                [Language.Deutsch] = "Geschafft! 🐟 Füttere den Eisbären!",
                [Language.English] = "Made it! 🐟 Feed the polar bear!",
            },
            ["pb_complete"] = new()
            {
                [Language.Nederlands] = "Lekker! 🎉",
                [Language.Deutsch] = "Super! 🎉",
                [Language.English] = "Yummy! 🎉",
            },
            ["pb_blow_hit"] = new()
            {
                [Language.Nederlands] = "Poof! 💨 Sneeuw Weggeblazen!",
                [Language.Deutsch] = "Puff! 💨 Schnee weggeblasen!",
                [Language.English] = "Poof! 💨 Snow blown away!",
            },
            ["pb_blow_miss"] = new()
            {
                [Language.Nederlands] = "Er is niks om weg te blazen!",
                [Language.Deutsch] = "Nichts zum Wegblasen!",
                [Language.English] = "Nothing to blow away!",
            },
            ["pb_missed"] = new()
            {
                [Language.Nederlands] = "Te ver! ❄️ Wacht op de ijsplaat!",
                [Language.Deutsch] = "Zu weit! ❄️ Warte auf die Eisscholle!",
                [Language.English] = "Too far! ❄️ Wait for the ice sheet!",
            },
            ["pb_blocked"] = new()
            {
                [Language.Nederlands] = "Geblockeerd! 🧊  Blaas eerst!",
                [Language.Deutsch] = "Blockiert! 🧊  Erst pusten!",
                [Language.English] = "Blocked! 🧊  Blow it away first!",
            },
            ["pb_feed_btn"] = new()
            {
                [Language.Nederlands] = "🐟  Voer IJsbeer!",
                [Language.Deutsch] = "🐟  Eisbär füttern!",
                [Language.English] = "🐟  Feed Bear!",
            },
            ["habitat_beaver_name"] = new()
            {
                [Language.Nederlands] = "Bever Verblijf",
                [Language.Deutsch] = "Biber-Gehege",
                [Language.English] = "Beaver Habitat",
            },
            ["habitat_beaver_desc"] = new()
            {
                [Language.Nederlands] = "Een rustige waterplas\nvoor vrolijke bevers.",
                [Language.Deutsch] = "Ein ruhiger Teich\nfür fröhliche Biber.",
                [Language.English] = "A calm pond\nfor happy beavers.",
            },
            ["habitat_polarbear_name"] = new()
            {
                [Language.Nederlands] = "IJsbeer Verblijf",
                [Language.Deutsch] = "Eisbär-Gehege",
                [Language.English] = "Polar Bear Habitat",
            },
            ["habitat_polarbear_desc"] = new()
            {
                [Language.Nederlands] = "Een ijskoud paradijs\nvoor de ijsbeer.",
                [Language.Deutsch] = "Ein eiskaltes Paradies\nfür den Eisbären.",
                [Language.English] = "An icy paradise\nfor the polar bear.",
            },
        };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        string saved = PlayerPrefs.GetString("language", "Nederlands");
        if (System.Enum.TryParse(saved, out Language parsed))
            CurrentLanguage = parsed;
    }

    public void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        PlayerPrefs.SetString("language", lang.ToString());
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public string Get(string key)
    {
        if (_strings.TryGetValue(key, out var dict))
            if (dict.TryGetValue(CurrentLanguage, out var val))
                return val;
        return $"[{key}]";
    }

    public string Get(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    public static LanguageManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("LanguageManager");
        return go.AddComponent<LanguageManager>();
    }
}