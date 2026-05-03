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
            ["building_title"] = new()
            {
                [Language.Nederlands] = "🦫  Bever Verblijf bouwen!  🪵",
                [Language.Deutsch] = "🦫  Biber-Gehege bauen!  🪵",
                [Language.English] = "🦫  Building Beaver Habitat!  🪵",
            },
            ["building_fun_0"] = new()
            {
                [Language.Nederlands] = "De bevers komen eraan! 🦫",
                [Language.Deutsch] = "Die Biber kommen! 🦫",
                [Language.English] = "The beavers are coming! 🦫",
            },
            ["building_fun_1"] = new()
            {
                [Language.Nederlands] = "Water ophalen... 💧",
                [Language.Deutsch] = "Wasser holen... 💧",
                [Language.English] = "Fetching water... 💧",
            },
            ["building_fun_2"] = new()
            {
                [Language.Nederlands] = "Takken verzamelen! 🌿",
                [Language.Deutsch] = "Äste sammeln! 🌿",
                [Language.English] = "Gathering branches! 🌿",
            },
            ["building_fun_3"] = new()
            {
                [Language.Nederlands] = "Knaag knaag knaag! 🪵",
                [Language.Deutsch] = "Nag nag nag! 🪵",
                [Language.English] = "Gnaw gnaw gnaw! 🪵",
            },
            ["building_fun_4"] = new()
            {
                [Language.Nederlands] = "De dam wordt gebouwd! 🔨",
                [Language.Deutsch] = "Der Damm wird gebaut! 🔨",
                [Language.English] = "Building the dam! 🔨",
            },
            ["building_fun_5"] = new()
            {
                [Language.Nederlands] = "Bijna klaar... nog even! ⏳",
                [Language.Deutsch] = "Fast fertig... noch kurz! ⏳",
                [Language.English] = "Almost done... hang tight! ⏳",
            },
            ["building_fun_6"] = new()
            {
                [Language.Nederlands] = "Bevers zijn superdruk! 🦫💨",
                [Language.Deutsch] = "Biber sind superbeschäftigt! 🦫💨",
                [Language.English] = "Beavers are super busy! 🦫💨",
            },
            ["building_fun_7"] = new()
            {
                [Language.Nederlands] = "Een perfect thuis! 🏠",
                [Language.Deutsch] = "Ein perfektes Zuhause! 🏠",
                [Language.English] = "A perfect home! 🏠",
            },
            ["building_fun_8"] = new()
            {
                [Language.Nederlands] = "Plons! 💦 Bijna af!",
                [Language.Deutsch] = "Plitsch! 💦 Fast fertig!",
                [Language.English] = "Splash! 💦 Nearly done!",
            },
            ["building_fun_9"] = new()
            {
                [Language.Nederlands] = "🌊 Het water stroomt al!",
                [Language.Deutsch] = "🌊 Das Wasser fließt schon!",
                [Language.English] = "🌊 The water flows already!",
            },
            ["btn_back"] = new()
            {
                [Language.Nederlands] = "◀  Terug",
                [Language.Deutsch] = "◀  Zurück",
                [Language.English] = "◀  Back",
            },
            ["btn_inspect"] = new()
            {
                [Language.Nederlands] = "🔍  Inspecteren",
                [Language.Deutsch] = "🔍  Inspizieren",
                [Language.English] = "🔍  Inspect",
            },
            ["btn_minigame"] = new()
            {
                [Language.Nederlands] = "🎮  Minigame",
                [Language.Deutsch] = "🎮  Minispiel",
                [Language.English] = "🎮  Minigame",
            },
            ["minigames_title"] = new()
            {
                [Language.Nederlands] = "Minigames",
                [Language.Deutsch] = "Minispiele",
                [Language.English] = "Minigames",
            },
            ["minigame_parrot"] = new()
            {
                [Language.Nederlands] = "Papegaai Voeren",
                [Language.Deutsch] = "Papagei Füttern",
                [Language.English] = "Feed the Parrot",
            },
            ["minigame_polarbear"] = new()
            {
                [Language.Nederlands] = "IJsbeer Avontuur",
                [Language.Deutsch] = "Eisbär Abenteuer",
                [Language.English] = "Polar Bear Adventure",
            },
            ["minigame_prairiedog"] = new()
            {
                [Language.Nederlands] = "Prairiehond Peek",
                [Language.Deutsch] = "Präriehund Gucken",
                [Language.English] = "Prairie Dog Peek",
            },
            ["minigame_complete"] = new()
            {
                [Language.Nederlands] = "Gefeliciteerd! 🎉",
                [Language.Deutsch] = "Glückwunsch! 🎉",
                [Language.English] = "Congratulations! 🎉",
            },
            ["minigame_beaver_title"] = new()
            {
                [Language.Nederlands] = "🦫  Bever Balans!",
                [Language.Deutsch] = "🦫  Biber-Balance!",
                [Language.English] = "🦫  Beaver Balance!",
            },
            ["minigame_instruction_pc"] = new()
            {
                [Language.Nederlands] = "Druk A / D om te kantelen",
                [Language.Deutsch] = "A / D drücken zum Kippen",
                [Language.English] = "Press A / D to tilt",
            },
            ["minigame_instruction_tablet"] = new()
            {
                [Language.Nederlands] = "Kantel de tablet!",
                [Language.Deutsch] = "Tablet kippen!",
                [Language.English] = "Tilt the tablet!",
            },
            ["minigame_coins_earned"] = new()
            {
                [Language.Nederlands] = "Je hebt 10 munten verdiend! 💰",
                [Language.Deutsch] = "Du hast 10 Münzen verdient! 💰",
                [Language.English] = "You earned 10 coins! 💰",
            },
            ["minigame_success_desc"] = new()
            {
                [Language.Nederlands] = "De bever heeft de stok in balans gehouden!",
                [Language.Deutsch] = "Der Biber hat den Stock ausbalanciert!",
                [Language.English] = "The beaver kept the stick balanced!",
            },
            ["inspect_hint"] = new()
            {
                [Language.Nederlands] = "Kantel tablet om rond te kijken",
                [Language.Deutsch] = "Tablet kippen zum Umschauen",
                [Language.English] = "Tilt tablet to look around",
            },
            ["beaver_name"] = new()
            {
                [Language.Nederlands] = "🦫 Bever Verblijf",
                [Language.Deutsch] = "🦫 Biber-Gehege",
                [Language.English] = "🦫 Beaver Habitat",
            },
            ["beaver_desc"] = new()
            {
                [Language.Nederlands] = "Een rustige waterplas voor vrolijke bevers.",
                [Language.Deutsch] = "Ein ruhiger Teich für fröhliche Biber.",
                [Language.English] = "A calm pond for happy beavers.",
            },
            ["beaver_fact"] = new()
            {
                [Language.Nederlands] = "Bevers bouwen dammen van takken en modder! 🌿",
                [Language.Deutsch] = "Biber bauen Dämme aus Ästen und Schlamm! 🌿",
                [Language.English] = "Beavers build dams from sticks and mud! 🌿",
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