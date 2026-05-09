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
                [Language.Nederlands] = "Spelen ▶",
                [Language.Deutsch] = "Spielen ▶",
                [Language.English] = "Play ▶",
            },
            ["title_wildlands"] = new()
            {
                [Language.Nederlands] = "Wildlands Game",
                [Language.Deutsch] = "Wildlands Spiel",
                [Language.English] = "Wildlands Game",
            },
            ["btn_settings"] = new()
            {
                [Language.Nederlands] = "",
                [Language.Deutsch] = "",
                [Language.English] = "",
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
                [Language.Nederlands] = "Koop een Verblijf",
                [Language.Deutsch] = "Gehege kaufen",
                [Language.English] = "Buy a Habitat",
            },
            ["shop_currency"] = new()
            {
                [Language.Nederlands] = "{0} munten",
                [Language.Deutsch] = "{0} Münzen",
                [Language.English] = "{0} coins",
            },
            ["shop_currency_short"] = new()
            {
                [Language.Nederlands] = "{0} munten",
                [Language.Deutsch] = "{0} Münzen",
                [Language.English] = "{0} coins",
            },
            ["btn_buy"] = new()
            {
                [Language.Nederlands] = "Kopen voor {0}",
                [Language.Deutsch] = "Kaufen für {0}",
                [Language.English] = "Buy for {0}",
            },
            ["btn_plaatsen"] = new()
            {
                [Language.Nederlands] = "Plaatsen",
                [Language.Deutsch] = "Platzieren",
                [Language.English] = "Place",
            },
            ["bought"] = new()
            {
                [Language.Nederlands] = "Gekocht!",
                [Language.Deutsch] = "Gekauft!",
                [Language.English] = "Bought!",
            },
            ["not_enough"] = new()
            {
                [Language.Nederlands] = "Niet genoeg munten!",
                [Language.Deutsch] = "Nicht genug Münzen!",
                [Language.English] = "Not enough coins!",
            },
            ["place_instruction"] = new()
            {
                [Language.Nederlands] = "Tik op het raster om het verblijf te plaatsen",
                [Language.Deutsch] = "Tippe auf das Raster, um das Gehege zu platzieren",
                [Language.English] = "Tap the grid to place the habitat",
            },
            ["well_done"] = new()
            {
                [Language.Nederlands] = "Goed gedaan!",
                [Language.Deutsch] = "Gut gemacht!",
                [Language.English] = "Well done!",
            },
            ["building_label"] = new()
            {
                [Language.Nederlands] = "In aanbouw!",
                [Language.Deutsch] = "Im Bau!",
                [Language.English] = "Building!",
            },
            ["building_label"] = new()
            {
                [Language.Nederlands] = "In aanbouw!",
                [Language.Deutsch] = "Im Bau!",
                [Language.English] = "Under construction!",
            },
            ["building_title"] = new()
            {
                [Language.Nederlands] = "Verblijf bouwen!",
                [Language.Deutsch] = "Gehege wird gebaut!",
                [Language.English] = "Building habitat!",
            },
            ["building_fun_0"] = new()
            {
                [Language.Nederlands] = "De dieren komen eraan!",
                [Language.Deutsch] = "Die Tiere kommen!",
                [Language.English] = "The animals are coming!",
            },
            ["building_fun_1"] = new()
            {
                [Language.Nederlands] = "Materialen ophalen...",
                [Language.Deutsch] = "Materialien werden geholt...",
                [Language.English] = "Fetching materials...",
            },
            ["building_fun_2"] = new()
            {
                [Language.Nederlands] = "Spullen verzamelen!",
                [Language.Deutsch] = "Sachen werden gesammelt!",
                [Language.English] = "Gathering supplies!",
            },
            ["building_fun_3"] = new()
            {
                [Language.Nederlands] = "Hamer hamer hamer!",
                [Language.Deutsch] = "Hammer hammer hammer!",
                [Language.English] = "Hammer hammer hammer!",
            },
            ["building_fun_4"] = new()
            {
                [Language.Nederlands] = "Het verblijf wordt gebouwd!",
                [Language.Deutsch] = "Das Gehege wird gebaut!",
                [Language.English] = "The habitat is being built!",
            },
            ["building_fun_5"] = new()
            {
                [Language.Nederlands] = "Bijna klaar... nog even!",
                [Language.Deutsch] = "Fast fertig... noch kurz!",
                [Language.English] = "Almost done... hang tight!",
            },
            ["building_fun_6"] = new()
            {
                [Language.Nederlands] = "De bouwers zijn superdruk!",
                [Language.Deutsch] = "Die Bauarbeiter sind super beschäftigt!",
                [Language.English] = "The builders are super busy!",
            },
            ["building_fun_7"] = new()
            {
                [Language.Nederlands] = "Een perfect thuis voor de dieren!",
                [Language.Deutsch] = "Ein perfektes Zuhause für die Tiere!",
                [Language.English] = "A perfect home for the animals!",
            },
            ["building_fun_8"] = new()
            {
                [Language.Nederlands] = "Bijna af!",
                [Language.Deutsch] = "Fast fertig!",
                [Language.English] = "Nearly done!",
            },
            ["building_fun_9"] = new()
            {
                [Language.Nederlands] = "De laatste hand wordt gelegd!",
                [Language.Deutsch] = "Der letzte Schliff wird angebracht!",
                [Language.English] = "Putting on the finishing touches!",
            },
            ["btn_back"] = new()
            {
                [Language.Nederlands] = "◀ Terug",
                [Language.Deutsch] = "◀ Zurück",
                [Language.English] = "◀ Back",
            },
            ["btn_inspect"] = new()
            {
                [Language.Nederlands] = "Inspecteren",
                [Language.Deutsch] = "Inspizieren",
                [Language.English] = "Inspect",
            },
            ["btn_minigame"] = new()
            {
                [Language.Nederlands] = "Minigame",
                [Language.Deutsch] = "Minispiel",
                [Language.English] = "Minigame",
            },
            ["minigames_title"] = new()
            {
                [Language.Nederlands] = "Minigames",
                [Language.Deutsch] = "Minispiele",
                [Language.English] = "Minigames",
            },
            ["codes_title"] = new()
            {
                [Language.Nederlands] = "Codes",
                [Language.Deutsch] = "Codes",
                [Language.English] = "Codes",
            },
            ["codes_subtitle"] = new()
            {
                [Language.Nederlands] = "Voer een 4-cijferige code in",
                [Language.Deutsch] = "Gib einen 4-stelligen Code ein",
                [Language.English] = "Enter a 4-digit code",
            },
            ["codes_enter"] = new()
            {
                [Language.Nederlands] = "Bevestig",
                [Language.Deutsch] = "Bestätigen",
                [Language.English] = "Enter",
            },
            ["codes_invalid"] = new()
            {
                [Language.Nederlands] = "Ongeldige code",
                [Language.Deutsch] = "Ungültiger Code",
                [Language.English] = "Invalid code",
            },
            ["codes_too_short"] = new()
            {
                [Language.Nederlands] = "Code moet 4 cijfers zijn",
                [Language.Deutsch] = "Code muss 4 Ziffern lang sein",
                [Language.English] = "Code must be 4 digits",
            },
            ["codes_all_skins"] = new()
            {
                [Language.Nederlands] = "Alle Skins",
                [Language.Deutsch] = "Alle Skins",
                [Language.English] = "All Skins",
            },
            ["codes_unlocked"] = new()
            {
                [Language.Nederlands] = "ontgrendeld",
                [Language.Deutsch] = "freigeschaltet",
                [Language.English] = "unlocked",
            },
            ["codes_locked"] = new()
            {
                [Language.Nederlands] = "??? Vergrendeld",
                [Language.Deutsch] = "??? Gesperrt",
                [Language.English] = "??? Locked",
            },
            ["codes_already"] = new()
            {
                [Language.Nederlands] = "Skin al ontgrendeld",
                [Language.Deutsch] = "Skin schon freigeschaltet",
                [Language.English] = "Skin already unlocked",
            },
            ["codes_info_body"] = new()
            {
                [Language.Nederlands] = "Ga naar Wildlands Zoo om codes te vinden!\n\nBij elk dierenverblijf staat een infobord met een geheime code. Voer de code hier in om een nieuwe skin voor dat dier te ontgrendelen!",
                [Language.Deutsch] = "Geh in den Wildlands Zoo, um Codes zu finden!\n\nAn jedem Tiergehege steht ein Infoschild mit einem geheimen Code. Gib den Code hier ein, um einen neuen Skin für dieses Tier freizuschalten!",
                [Language.English] = "Visit Wildlands Zoo to find codes!\n\nEach animal exhibit has an info sign with a secret code. Enter the code here to unlock a new skin for that animal!",
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
                [Language.Nederlands] = "Gefeliciteerd!",
                [Language.Deutsch] = "Glückwunsch!",
                [Language.English] = "Congratulations!",
            },
            ["minigame_beaver_title"] = new()
            {
                [Language.Nederlands] = "Bever Balans!",
                [Language.Deutsch] = "Biber-Balance!",
                [Language.English] = "Beaver Balance!",
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
                [Language.Nederlands] = "Je hebt 100 munten verdiend!",
                [Language.Deutsch] = "Du hast 100 Münzen verdient!",
                [Language.English] = "You earned 100 coins!",
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
                [Language.Nederlands] = "Bever Verblijf",
                [Language.Deutsch] = "Biber-Gehege",
                [Language.English] = "Beaver Habitat",
            },
            ["beaver_desc"] = new()
            {
                [Language.Nederlands] = "Een rustige waterplas voor vrolijke bevers.",
                [Language.Deutsch] = "Ein ruhiger Teich für fröhliche Biber.",
                [Language.English] = "A calm pond for happy beavers.",
            },
            ["beaver_fact"] = new()
            {
                [Language.Nederlands] = "Bevers bouwen dammen van takken en modder!",
                [Language.Deutsch] = "Biber bauen Dämme aus Ästen und Schlamm!",
                [Language.English] = "Beavers build dams from sticks and mud!",
            },
            ["btn_continue"] = new()
            {
                [Language.Nederlands] = "Doorgaan",
                [Language.Deutsch] = "Weiter",
                [Language.English] = "Continue",
            },
            ["pb_btn_blow"] = new()
            {
                [Language.Nederlands] = "Blaas",
                [Language.Deutsch] = "Pusten",
                [Language.English] = "Blow",
            },
            ["pb_splash"] = new()
            {
                [Language.Nederlands] = "Splash!",
                [Language.Deutsch] = "Platsch!",
                [Language.English] = "Splash!",
            },
            ["pb_retry_text"] = new()
            {
                [Language.Nederlands] = "Splash! \nProbeer opnieuw!",
                [Language.Deutsch] = "Platsch! \nNochmal versuchen!",
                [Language.English] = "Splash! \nTry again!",
            },
            ["pb_retry_btn"] = new()
            {
                [Language.Nederlands] = "Start!",
                [Language.Deutsch] = "Los!",
                [Language.English] = "Go!",
            },
            ["pb_reached_end"] = new()
            {
                [Language.Nederlands] = "Gelukt! Voer de ijsbeer!",
                [Language.Deutsch] = "Geschafft! Füttere den Eisbären!",
                [Language.English] = "Made it! Feed the polar bear!",
            },
            ["pb_complete"] = new()
            {
                [Language.Nederlands] = "Lekker!",
                [Language.Deutsch] = "Super!",
                [Language.English] = "Yummy!",
            },
            ["pb_blow_hit"] = new()
            {
                [Language.Nederlands] = "Poof! Sneeuw Weggeblazen!",
                [Language.Deutsch] = "Puff! Schnee weggeblasen!",
                [Language.English] = "Poof! Snow blown away!",
            },
            ["pb_blow_miss"] = new()
            {
                [Language.Nederlands] = "Er is niks om weg te blazen!",
                [Language.Deutsch] = "Nichts zum Wegblasen!",
                [Language.English] = "Nothing to blow away!",
            },
            ["pb_missed"] = new()
            {
                [Language.Nederlands] = "Te ver! Wacht op de ijsplaat!",
                [Language.Deutsch] = "Zu weit! Warte auf die Eisscholle!",
                [Language.English] = "Too far! Wait for the ice sheet!",
            },
            ["pb_blocked"] = new()
            {
                [Language.Nederlands] = "Geblockeerd! Blaas eerst!",
                [Language.Deutsch] = "Blockiert! Erst pusten!",
                [Language.English] = "Blocked! Blow it away first!",
            },
            ["pb_feed_btn"] = new()
            {
                [Language.Nederlands] = "Voer IJsbeer!",
                [Language.Deutsch] = "Eisbär füttern!",
                [Language.English] = "Feed Bear!",
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
            ["habitat_baboon_name"] = new()
            {
                [Language.Nederlands] = "Baviaan Verblijf",
                [Language.Deutsch] = "Pavian-Gehege",
                [Language.English] = "Baboon Habitat",
            },
            ["habitat_polarbear_name"] = new()
            {
                [Language.Nederlands] = "IJsbeer Verblijf",
                [Language.Deutsch] = "Eisbär-Gehege",
                [Language.English] = "Polar Bear Habitat",
            },
            ["habitat_racoon_name"] = new()
            {
                [Language.Nederlands] = "Wasbeer Verblijf",
                [Language.Deutsch] = "Waschbär-Gehege",
                [Language.English] = "Raccoon Habitat",
            },
            ["habitat_prairiedog_name"] = new()
            {
                [Language.Nederlands] = "Prairiehond Verblijf",
                [Language.Deutsch] = "Präriehund-Gehege",
                [Language.English] = "Prairie Dog Habitat",
            },
            ["build_mode_label"] = new()
            {
                [Language.Nederlands] = "Bouw Modus",
                [Language.Deutsch] = "Baumodus",
                [Language.English] = "Build Mode",
            },
            ["build_mode_on"] = new()
            {
                [Language.Nederlands] = "AAN",
                [Language.Deutsch] = "AN",
                [Language.English] = "ON",
            },
            ["build_mode_off"] = new()
            {
                [Language.Nederlands] = "UIT",
                [Language.Deutsch] = "AUS",
                [Language.English] = "OFF",
            },
            ["baboon_name"] = new()
            {
                [Language.Nederlands] = "Baviaan Verblijf",
                [Language.Deutsch] = "Pavian-Gehege",
                [Language.English] = "Baboon Habitat",
            },
            ["baboon_desc"] = new()
            {
                [Language.Nederlands] = "Een rotsachtig speelterrein vol slimme bavianen.",
                [Language.Deutsch] = "Ein felsiger Spielplatz voller cleverer Paviane.",
                [Language.English] = "A rocky playground full of clever baboons.",
            },
            ["baboon_fact"] = new()
            {
                [Language.Nederlands] = "Bavianen leven in groepen die soms wel 100 dieren tellen!",
                [Language.Deutsch] = "Paviane leben in Gruppen mit bis zu 100 Tieren!",
                [Language.English] = "Baboons live in troops that can have up to 100 members!",
            },

            ["racoon_name"] = new()
            {
                [Language.Nederlands] = "Wasbeer Verblijf",
                [Language.Deutsch] = "Waschbär-Gehege",
                [Language.English] = "Raccoon Habitat",
            },
            ["racoon_desc"] = new()
            {
                [Language.Nederlands] = "Een gezellig bos met klimboomstammen voor stoute wasberen.",
                [Language.Deutsch] = "Ein gemütlicher Wald mit Klettermöglichkeiten für freche Waschbären.",
                [Language.English] = "A cozy forest with climbing logs for mischievous raccoons.",
            },
            ["racoon_fact"] = new()
            {
                [Language.Nederlands] = "Wasberen wassen hun eten voor ze het opeten!",
                [Language.Deutsch] = "Waschbären waschen ihr Essen, bevor sie es fressen!",
                [Language.English] = "Raccoons wash their food before eating it!",
            },

            ["prairiedog_name"] = new()
            {
                [Language.Nederlands] = "Prairiehond Verblijf",
                [Language.Deutsch] = "Präriehund-Gehege",
                [Language.English] = "Prairie Dog Habitat",
            },
            ["prairiedog_desc"] = new()
            {
                [Language.Nederlands] = "Een grasveld vol tunnels en piepende prairiehondjes.",
                [Language.Deutsch] = "Eine Wiese voller Tunnel und quiekender Präriehunde.",
                [Language.English] = "A grassy field full of tunnels and squeaking prairie dogs.",
            },
            ["prairiedog_fact"] = new()
            {
                [Language.Nederlands] = "Prairiehonden geven elkaar een kus om hallo te zeggen!",
                [Language.Deutsch] = "Präriehunde küssen sich zur Begrüßung!",
                [Language.English] = "Prairie dogs kiss each other to say hello!",
            },
            ["polarbear_name"] = new()
            {
                [Language.Nederlands] = "IJsbeer Verblijf",
                [Language.Deutsch] = "Eisbär-Gehege",
                [Language.English] = "Polar Bear Habitat",
            },
            ["polarbear_desc"] = new()
            {
                [Language.Nederlands] = "Een ijskoud paradijs met sneeuw en water voor grote ijsberen.",
                [Language.Deutsch] = "Ein eiskaltes Paradies mit Schnee und Wasser für große Eisbären.",
                [Language.English] = "An icy paradise with snow and water for big polar bears.",
            },
            ["polarbear_fact"] = new()
            {
                [Language.Nederlands] = "IJsberen hebben zwarte huid onder hun witte vacht!",
                [Language.Deutsch] = "Eisbären haben schwarze Haut unter ihrem weißen Fell!",
                [Language.English] = "Polar bears have black skin under their white fur!",
            },
            ["minigame_polarbear_title"] = new()
            {
                [Language.Nederlands] = "IJsbeer Verkoeling",
                [Language.Deutsch] = "Eisbär-Abkühlung",
                [Language.English] = "Polar Bear Cooldown",
            },
            ["minigame_polarbear_instruction"] = new()
            {
                [Language.Nederlands] = "Blaas in de microfoon om de ijsbeer af te koelen!",
                [Language.Deutsch] = "Blas ins Mikrofon, um den Eisbär abzukühlen!",
                [Language.English] = "Blow into the microphone to cool the polar bear down!",
            },
            ["minigame_polarbear_listening"] = new()
            {
                [Language.Nederlands] = "Aan het luisteren...",
                [Language.Deutsch] = "Hört zu...",
                [Language.English] = "Listening...",
            },
            ["minigame_polarbear_blowing"] = new()
            {
                [Language.Nederlands] = "Blazen!",
                [Language.Deutsch] = "Pusten!",
                [Language.English] = "Blowing!",
            },
            ["minigame_polarbear_success_title"] = new()
            {
                [Language.Nederlands] = "Lekker koel!",
                [Language.Deutsch] = "Schön kühl!",
                [Language.English] = "Nice and cool!",
            },
            ["minigame_polarbear_success_desc"] = new()
            {
                [Language.Nederlands] = "De ijsbeer voelt zich weer happy!",
                [Language.Deutsch] = "Der Eisbär ist wieder glücklich!",
                [Language.English] = "The polar bear is happy again!",
            },
            ["habitat_hippo_name"] = new()
            {
                [Language.Nederlands] = "Nijlpaard Verblijf",
                [Language.Deutsch] = "Nilpferd-Gehege",
                [Language.English] = "Hippo Habitat",
            },
            ["hippo_name"] = new()
            {
                [Language.Nederlands] = "Nijlpaard Verblijf",
                [Language.Deutsch] = "Nilpferd-Gehege",
                [Language.English] = "Hippo Habitat",
            },
            ["hippo_desc"] = new()
            {
                [Language.Nederlands] = "Een modderig waterpoeltje voor lekkere luie nijlpaarden.",
                [Language.Deutsch] = "Eine schlammige Wasserstelle für richtig faule Nilpferde.",
                [Language.English] = "A muddy waterhole for nice lazy hippos.",
            },
            ["hippo_fact"] = new()
            {
                [Language.Nederlands] = "Nijlpaarden zweten roze om hun huid te beschermen!",
                [Language.Deutsch] = "Nilpferde schwitzen rosa, um ihre Haut zu schützen!",
                [Language.English] = "Hippos sweat pink to protect their skin!",
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