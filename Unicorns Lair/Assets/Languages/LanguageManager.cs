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
                [Language.Nederlands] = "Kantel de tablet!",
                [Language.Deutsch] = "Tablet kippen!",
                [Language.English] = "Tilt the tablet!",
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
            ["minigame_prairiedog_title"] = new()
            {
                [Language.Nederlands] = "Prairiehond Spel",
                [Language.Deutsch] = "Präriehund-Spiel",
                [Language.English] = "Prairie Dog Game",
            },
            ["minigame_prairiedog_shake_pc"] = new()
            {
                [Language.Nederlands] = "Schud de tablet!",
                [Language.Deutsch] = "Schüttle das Tablet!",
                [Language.English] = "Shake the tablet!",
            },
            ["minigame_prairiedog_shake_tablet"] = new()
            {
                [Language.Nederlands] = "Schud de tablet!",
                [Language.Deutsch] = "Schüttle das Tablet!",
                [Language.English] = "Shake the tablet!",
            },
            ["minigame_prairiedog_watch"] = new()
            {
                [Language.Nederlands] = "Kijk goed!",
                [Language.Deutsch] = "Pass auf!",
                [Language.English] = "Watch carefully!",
            },
            ["minigame_prairiedog_tap"] = new()
            {
                [Language.Nederlands] = "Tik op het juiste hol!",
                [Language.Deutsch] = "Tippe das richtige Loch!",
                [Language.English] = "Tap the correct hole!",
            },
            ["minigame_prairiedog_correct"] = new()
            {
                [Language.Nederlands] = "Goed gedaan!",
                [Language.Deutsch] = "Sehr gut!",
                [Language.English] = "Correct!",
            },
            ["minigame_prairiedog_wrong"] = new()
            {
                [Language.Nederlands] = "Niet helemaal! Hier was hij.",
                [Language.Deutsch] = "Nicht ganz! Hier war er.",
                [Language.English] = "Not quite! It was here.",
            },
            ["minigame_prairiedog_success_title"] = new()
            {
                [Language.Nederlands] = "Goed gespot!",
                [Language.Deutsch] = "Gut erkannt!",
                [Language.English] = "Good spotting!",
            },
            ["minigame_prairiedog_success_desc"] = new()
            {
                [Language.Nederlands] = "Goed gedaan, dierenoppasser!",
                [Language.Deutsch] = "Gut gemacht, Tierpfleger!",
                [Language.English] = "Well done, zookeeper!",
            },
            ["minigame_baboon_title"] = new()
            {
                [Language.Nederlands] = "Baviaan Patroon",
                [Language.Deutsch] = "Pavian-Muster",
                [Language.English] = "Baboon Pattern",
            },
            ["minigame_baboon_watch"] = new()
            {
                [Language.Nederlands] = "Kijk naar de baviaan!",
                [Language.Deutsch] = "Schau dem Pavian zu!",
                [Language.English] = "Watch the baboon!",
            },
            ["minigame_baboon_your_turn"] = new()
            {
                [Language.Nederlands] = "Jouw beurt!",
                [Language.Deutsch] = "Du bist dran!",
                [Language.English] = "Your turn!",
            },
            ["minigame_baboon_correct"] = new()
            {
                [Language.Nederlands] = "Goed!",
                [Language.Deutsch] = "Gut!",
                [Language.English] = "Good!",
            },
            ["minigame_baboon_wrong"] = new()
            {
                [Language.Nederlands] = "Probeer opnieuw! Kijk goed.",
                [Language.Deutsch] = "Versuch's nochmal! Pass auf.",
                [Language.English] = "Try again! Watch carefully.",
            },
            ["minigame_baboon_success_title"] = new()
            {
                [Language.Nederlands] = "Geweldig gedaan!",
                [Language.Deutsch] = "Super gemacht!",
                [Language.English] = "Awesome job!",
            },
            ["minigame_baboon_success_desc"] = new()
            {
                [Language.Nederlands] = "De baviaan is trots op je!",
                [Language.Deutsch] = "Der Pavian ist stolz auf dich!",
                [Language.English] = "The baboon is proud of you!",
            },
            ["minigame_hippo_title"] = new()
            {
                [Language.Nederlands] = "Nijlpaard Eten Sorteren",
                [Language.Deutsch] = "Nilpferd-Futter sortieren",
                [Language.English] = "Hippo Food Sorting",
            },
            ["minigame_hippo_instruction"] = new()
            {
                [Language.Nederlands] = "Swipe links = Lekker  |  Swipe rechts = Niet lekker",
                [Language.Deutsch] = "Wische links = Lecker  |  Wische rechts = Nicht geeignet",
                [Language.English] = "Swipe left = Yummy  |  Swipe right = Not suitable",
            },
            ["minigame_hippo_correct"] = new()
            {
                [Language.Nederlands] = "Goed gedaan!",
                [Language.Deutsch] = "Gut gemacht!",
                [Language.English] = "Well done!",
            },
            ["minigame_hippo_wrong"] = new()
            {
                [Language.Nederlands] = "Niet helemaal!",
                [Language.Deutsch] = "Nicht ganz!",
                [Language.English] = "Not quite!",
            },
            ["minigame_hippo_success_title"] = new()
            {
                [Language.Nederlands] = "Goed gesorteerd!",
                [Language.Deutsch] = "Toll sortiert!",
                [Language.English] = "Great sorting!",
            },
            ["minigame_hippo_success_desc"] = new()
            {
                [Language.Nederlands] = "De nijlpaard eet nu lekker en gezond!",
                [Language.Deutsch] = "Das Nilpferd isst jetzt gesund und lecker!",
                [Language.English] = "The hippo is eating healthy and happy now!",
            },

            ["food_watermelon"] = new() { [Language.Nederlands] = "Watermeloen", [Language.Deutsch] = "Wassermelone", [Language.English] = "Watermelon" },
            ["food_lettuce"] = new() { [Language.Nederlands] = "Sla", [Language.Deutsch] = "Salat", [Language.English] = "Lettuce" },
            ["food_cabbage"] = new() { [Language.Nederlands] = "Kool", [Language.Deutsch] = "Kohl", [Language.English] = "Cabbage" },
            ["food_apples"] = new() { [Language.Nederlands] = "Appels", [Language.Deutsch] = "Äpfel", [Language.English] = "Apples" },
            ["food_candy"] = new() { [Language.Nederlands] = "Snoep", [Language.Deutsch] = "Süßigkeiten", [Language.English] = "Candy" },
            ["food_chocolate"] = new() { [Language.Nederlands] = "Chocolade", [Language.Deutsch] = "Schokolade", [Language.English] = "Chocolate" },
            ["food_chips"] = new() { [Language.Nederlands] = "Chips", [Language.Deutsch] = "Chips", [Language.English] = "Chips" },
            ["food_bread"] = new() { [Language.Nederlands] = "Brood", [Language.Deutsch] = "Brot", [Language.English] = "Bread" },
            ["intro_title"] = new()
            {
                [Language.Nederlands] = "Welkom bij Wildlands\nLittle Explorers!",
                [Language.Deutsch] = "Willkommen bei Wildlands\nLittle Explorers!",
                [Language.English] = "Welcome to Wildlands\nLittle Explorers!",
            },
            ["intro_line1"] = new()
            {
                [Language.Nederlands] = "Bouw je eigen dierentuin!",
                [Language.Deutsch] = "Bau deinen eigenen Zoo!",
                [Language.English] = "Build your own zoo!",
            },
            ["intro_line2"] = new()
            {
                [Language.Nederlands] = "Speel leuke minigames met de dieren om munten te verdienen.",
                [Language.Deutsch] = "Spiele lustige Minispiele mit den Tieren, um Münzen zu verdienen.",
                [Language.English] = "Play fun minigames with the animals to earn coins.",
            },
            ["intro_line3"] = new()
            {
                [Language.Nederlands] = "Geef je munten uit aan nieuwe verblijven!",
                [Language.Deutsch] = "Gib deine Münzen für neue Gehege aus!",
                [Language.English] = "Spend your coins on new habitats!",
            },
            ["intro_line4"] = new()
            {
                [Language.Nederlands] = "Tik op verblijven om de dieren te ontmoeten!",
                [Language.Deutsch] = "Tippe auf Gehege, um die Tiere zu treffen!",
                [Language.English] = "Tap habitats to meet the animals!",
            },
            ["intro_ready"] = new()
            {
                [Language.Nederlands] = "Klaar om te beginnen?",
                [Language.Deutsch] = "Bereit anzufangen?",
                [Language.English] = "Ready to start?",
            },
            ["btn_lets_go"] = new()
            {
                [Language.Nederlands] = "Laten we beginnen!",
                [Language.Deutsch] = "Los geht's!",
                [Language.English] = "Let's go!",
            },
            // Tutorial Dialog
            ["tutorial_first_buy"] = new()
            {
                [Language.Nederlands] = "Tik op de groene knop om je allereerste verblijf te bouwen!",
                [Language.Deutsch] = "Tippe auf den grünen Knopf, um dein allererstes Gehege zu bauen!",
                [Language.English] = "Tap the green button to build your very first habitat!",
            },
            ["tutorial_building"] = new()
            {
                [Language.Nederlands] = "Daar komt 'ie... je verblijf wordt gebouwd!",
                [Language.Deutsch] = "Da kommt es schon... dein Gehege wird gebaut!",
                [Language.English] = "Here it comes... your habitat is being built!",
            },
            ["tutorial_tap_habitat"] = new()
            {
                [Language.Nederlands] = "Tik nu op het verblijf om je nieuwe dier te ontmoeten!",
                [Language.Deutsch] = "Tippe jetzt auf das Gehege, um dein neues Tier kennenzulernen!",
                [Language.English] = "Now tap the habitat to go and meet your new animal!",
            },
            ["tutorial_press_inspect"] = new()
            {
                [Language.Nederlands] = "Druk op de knop Inspecteren om alles van dichtbij te bekijken!",
                [Language.Deutsch] = "Drück auf den Knopf Untersuchen, um alles aus der Nähe zu sehen!",
                [Language.English] = "Press the Inspect button to get a closer look!",
            },
            ["tutorial_inspecting"] = new()
            {
                [Language.Nederlands] = "Kijk maar goed rond! Kantel je tablet om overal naar te kijken.",
                [Language.Deutsch] = "Schau dich ruhig um! Neig dein Tablet, um überall hinzuschauen.",
                [Language.English] = "Have a good look! Tilt your tablet to look all around.",
            },
            ["tutorial_press_back"] = new()
            {
                [Language.Nederlands] = "Klaar met kijken? Druk op de knop Terug om verder te gaan!",
                [Language.Deutsch] = "Fertig mit Schauen? Drück auf den Knopf Zurück, um weiterzumachen!",
                [Language.English] = "All done looking? Press the back button to keep going!",
            },
            ["tutorial_press_minigame"] = new()
            {
                [Language.Nederlands] = "Speel nu de minigame om munten te verdienen!",
                [Language.Deutsch] = "Spiel jetzt das Minispiel, um Münzen zu verdienen!",
                [Language.English] = "Now play the minigame to earn some coins!",
            },
            ["tutorial_next_buy"] = new()
            {
                [Language.Nederlands] = "Geweldig! Laten we een huis bouwen voor het volgende dier!",
                [Language.Deutsch] = "Wunderbar! Lass uns ein Zuhause für das nächste Tier bauen!",
                [Language.English] = "Wonderful! Let's build a home for the next animal!",
            },
            ["tutorial_complete"] = new()
            {
                [Language.Nederlands] = "Het is je gelukt! Nu heeft elk dier een huis. De dierentuin is van jou om te ontdekken!",
                [Language.Deutsch] = "Du hast es geschafft! Jetzt hat jedes Tier ein Zuhause. Der Zoo gehört dir zum Entdecken!",
                [Language.English] = "You did it! Every animal has a home now. The zoo is yours to explore!",
            },
            // Intro Dialog

            ["intro_speaker"] = new()
            {
                [Language.Nederlands] = "Dierenverzorger",
                [Language.Deutsch] = "Tierpfleger",
                [Language.English] = "Zookeeper",
            },
            ["intro_tap_continue"] = new()
            {
                [Language.Nederlands] = "Tik om verder ▶",
                [Language.Deutsch] = "Tippe weiter ▶",
                [Language.English] = "Tap to continue ▶",
            },
            ["intro_dlg_0"] = new()
            {
                [Language.Nederlands] = "Hé, hallo daar, ontdekker!",
                [Language.Deutsch] = "Na, hallo da, Entdecker!",
                [Language.English] = "Well, hello there, explorer!",
            },
            ["intro_dlg_1"] = new()
            {
                [Language.Nederlands] = "Welkom in Wildlands, wat fijn dat je er bent!",
                [Language.Deutsch] = "Willkommen in Wildlands, schön, dass du da bist!",
                [Language.English] = "Welcome to Wildlands, I'm so glad you're here!",
            },
            ["intro_dlg_2"] = new()
            {
                [Language.Nederlands] = "Weet je, we kunnen jouw hulp goed gebruiken. Onze dierentuin heeft namelijk nog geen verblijven…",
                [Language.Deutsch] = "Weißt du, wir brauchen wirklich deine Hilfe. Unser Zoo hat nämlich noch keine Gehege…",
                [Language.English] = "You see, we really need your help. Right now our zoo doesn't have any habitats yet…",
            },
            ["intro_dlg_3"] = new()
            {
                [Language.Nederlands] = "…en daardoor hebben de dieren nog geen fijne plek om te wonen.",
                [Language.Deutsch] = "…und deshalb haben die Tiere noch kein gemütliches Zuhause.",
                [Language.English] = "…so the animals have nowhere cozy to live.",
            },
            ["intro_dlg_4"] = new()
            {
                [Language.Nederlands] = "Help jij ons om voor ieder dier een huis te bouwen?",
                [Language.Deutsch] = "Hilfst du uns, für jedes Tier ein Zuhause zu bauen?",
                [Language.English] = "Will you help us build a home for every one of them?",
            },
            ["intro_dlg_5"] = new()
            {
                [Language.Nederlands] = "Zo werkt het: je verdient munten door de minigame van elk dier te spelen.",
                [Language.Deutsch] = "So funktioniert's: Du verdienst Münzen, indem du die Minispiele der Tiere spielst.",
                [Language.English] = "Here's the trick: you earn coins by playing each animal's minigame.",
            },
            ["intro_dlg_6"] = new()
            {
                [Language.Nederlands] = "Met die munten bouw je daarna hun verblijf. Makkelijk toch?",
                [Language.Deutsch] = "Mit diesen Münzen baust du dann ihr Gehege. Ganz einfach!",
                [Language.English] = "Then you spend those coins to build their habitat. Easy!",
            },
            ["minigame_hippo_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_hippo_howto_intro"] = new()
            {
                [Language.Nederlands] = "Het nijlpaard heeft honger! Sommig eten is gezond, en sommig eten is niet goed voor nijlpaarden.",
                [Language.Deutsch] = "Das Nilpferd hat Hunger! Manches Essen ist gesund, und manches ist nicht gut für Nilpferde.",
                [Language.English] = "The hippo is hungry! Some food is healthy, and some is not good for hippos.",
            },
            ["minigame_hippo_howto_left"] = new()
            {
                [Language.Nederlands] = "Veeg naar LINKS voor eten dat goed is voor het nijlpaard.",
                [Language.Deutsch] = "Wisch nach LINKS für Essen, das gut für das Nilpferd ist.",
                [Language.English] = "Swipe LEFT for food that is good for the hippo.",
            },
            ["minigame_hippo_howto_right"] = new()
            {
                [Language.Nederlands] = "Veeg naar RECHTS voor eten dat NIET goed is.",
                [Language.Deutsch] = "Wisch nach RECHTS für Essen, das NICHT gut ist.",
                [Language.English] = "Swipe RIGHT for food that is NOT good.",
            },
            ["minigame_hippo_zone_good"] = new()
            {
                [Language.Nederlands] = "Lekker!",
                [Language.Deutsch] = "Lecker!",
                [Language.English] = "Yummy!",
            },
            ["minigame_hippo_zone_bad"] = new()
            {
                [Language.Nederlands] = "Niet lekker",
                [Language.Deutsch] = "Nicht lecker",
                [Language.English] = "Not good",
            },
            ["minigame_hippo_instruction"] = new()
            {
                [Language.Nederlands] = "Veeg naar links = Lekker  |  Veeg naar rechts = Niet lekker",
                [Language.Deutsch] = "Links wischen = Lecker  |  Rechts wischen = Nicht lecker",
                [Language.English] = "Swipe left = Yummy  |  Swipe right = Not good",
            },
            ["parrot_name"] = new()
            {
                [Language.Nederlands] = "Papegaai",
                [Language.Deutsch] = "Papagei",
                [Language.English] = "Parrot",
            },
            ["parrot_desc"] = new()
            {
                [Language.Nederlands] = "De papegaai is het kleurrijkste en kletsigste dier van Wildlands. Hij houdt van klimmen, kraken en kletsen!",
                [Language.Deutsch] = "Der Papagei ist das bunteste und plapprigste Tier in Wildlands. Er liebt es zu klettern, zu knacken und zu plappern!",
                [Language.English] = "The parrot is the most colorful and chattiest animal in Wildlands. It loves to climb, crack nuts, and chatter away!",
            },
            ["parrot_fact"] = new()
            {
                [Language.Nederlands] = "Wist je dat sommige papegaaien woorden kunnen nadoen die mensen zeggen? Misschien leren ze zelfs jouw naam!",
                [Language.Deutsch] = "Wusstest du, dass manche Papageien Wörter nachmachen können, die Menschen sagen? Vielleicht lernen sie sogar deinen Namen!",
                [Language.English] = "Did you know some parrots can copy the words people say? They might even learn your name!",
            },

            ["habitat_parrot_name"] = new()
            {
                [Language.Nederlands] = "Papegaai",
                [Language.Deutsch] = "Papagei",
                [Language.English] = "Parrot",
            },
            ["minigame_parrot_title"] = new()
            {
                [Language.Nederlands] = "Papegaai Voeren",
                [Language.Deutsch] = "Papagei füttern",
                [Language.English] = "Parrot Feeding",
            },
            ["minigame_parrot_instruction"] = new()
            {
                [Language.Nederlands] = "Kantel om zaadjes te strooien!",
                [Language.Deutsch] = "Kipp das Tablet, um Körner zu streuen!",
                [Language.English] = "Tilt to pour seeds!",
            },
            ["minigame_parrot_pour"] = new()
            {
                [Language.Nederlands] = "Strooien!",
                [Language.Deutsch] = "Streuen!",
                [Language.English] = "Pouring!",
            },
            ["minigame_parrot_no_seeds"] = new()
            {
                [Language.Nederlands] = "Geen zaadjes meer!",
                [Language.Deutsch] = "Keine Körner mehr!",
                [Language.English] = "No seeds left!",
            },
            ["minigame_parrot_checking"] = new()
            {
                [Language.Nederlands] = "Zaadjes tellen...",
                [Language.Deutsch] = "Körner werden gezählt...",
                [Language.English] = "Counting seeds...",
            },
            ["minigame_parrot_retry_aim"] = new()
            {
                [Language.Nederlands] = "Probeer opnieuw! Mik op de bak!",
                [Language.Deutsch] = "Versuch's nochmal! Ziel auf die Schale!",
                [Language.English] = "Try again! Aim for the tray!",
            },
            ["minigame_parrot_retry_tilt"] = new()
            {
                [Language.Nederlands] = "Probeer opnieuw! Kantel de tablet!",
                [Language.Deutsch] = "Versuch's nochmal! Kipp das Tablet!",
                [Language.English] = "Try again! Tilt the tablet!",
            },
            ["minigame_parrot_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_parrot_howto_intro"] = new()
            {
                [Language.Nederlands] = "De papegaai heeft honger! Vul de bak met lekkere zaadjes.",
                [Language.Deutsch] = "Der Papagei hat Hunger! Füll die Schale mit leckeren Körnern.",
                [Language.English] = "The parrot is hungry! Fill the tray with tasty seeds.",
            },
            ["minigame_parrot_howto_line1"] = new()
            {
                [Language.Nederlands] = "Kantel je tablet om zaadjes uit de zak te strooien.",
                [Language.Deutsch] = "Kipp dein Tablet, um Körner aus dem Sack zu streuen.",
                [Language.English] = "Tilt your tablet to pour seeds from the sack.",
            },
            ["minigame_parrot_howto_line2"] = new()
            {
                [Language.Nederlands] = "Mik de zaadjes in de bak eronder om hem te vullen!",
                [Language.Deutsch] = "Ziel die Körner in die Schale darunter, um sie zu füllen!",
                [Language.English] = "Aim the seeds into the tray below to fill it up!",
            },
            ["minigame_parrot_success_title"] = new()
            {
                [Language.Nederlands] = "Lekkere zaadjes!",
                [Language.Deutsch] = "Leckere Körner!",
                [Language.English] = "Tasty seeds!",
            },
            ["minigame_parrot_success_desc"] = new()
            {
                [Language.Nederlands] = "De buik van de papegaai is vol en blij!",
                [Language.Deutsch] = "Der Bauch des Papageis ist voll und glücklich!",
                [Language.English] = "The parrot's tummy is full and happy!",
            },
            ["minigame_parrot_aim"] = new()
            {
                [Language.Nederlands] = "Mik op de bak!",
                [Language.Deutsch] = "Ziel auf die Schale!",
                [Language.English] = "Aim for the tray!",
            },
            // === OTTER — inspection card (animal name / description / fun fact) ===

            ["otter_name"] = new()
            {
                [Language.Nederlands] = "Otter",
                [Language.Deutsch] = "Otter",
                [Language.English] = "Otter",
            },
            ["otter_desc"] = new()
            {
                [Language.Nederlands] = "De otter is het speelste dier van Wildlands.",
                [Language.Deutsch] = "Der Otter ist das verspielteste Tier in Wildlands.",
                [Language.English] = "The otter is the most playful animal in Wildlands.",
            },
            ["otter_fact"] = new()
            {
                [Language.Nederlands] = "Wist je dat otters elkaars pootjes vasthouden als ze slapen?",
                [Language.Deutsch] = "Wusstest du, dass Otter sich beim Schlafen an den Pfoten halten?",
                [Language.English] = "Did you know otters hold hands while they sleep?",
            },
            ["habitat_otter_name"] = new()
            {
                [Language.Nederlands] = "Otter",
                [Language.Deutsch] = "Otter",
                [Language.English] = "Otter",
            },
            ["minigame_raccoon_title"] = new()
            {
                [Language.Nederlands] = "Draai de Pot Open!",
                [Language.Deutsch] = "Dreh das Glas auf!",
                [Language.English] = "Open the Jar!",
            },
            ["minigame_raccoon_instruction"] = new()
            {
                [Language.Nederlands] = "Draai de tablet heen en weer om de pot te openen!",
                [Language.Deutsch] = "Dreh das Tablet hin und her, um das Glas zu öffnen!",
                [Language.English] = "Twist the tablet back and forth to open the jar!",
            },
            ["minigame_raccoon_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_raccoon_howto_intro"] = new()
            {
                [Language.Nederlands] = "De wasbeer heeft een pot met snoepjes gevonden, maar de deksel zit muurvast!",
                [Language.Deutsch] = "Der Waschbär hat ein Glas mit Leckerlis gefunden, aber der Deckel klemmt total!",
                [Language.English] = "The raccoon found a jar of treats, but the lid is stuck tight!",
            },
            ["minigame_raccoon_howto_line1"] = new()
            {
                [Language.Nederlands] = "Draai de tablet een kant op, en dan de andere kant.",
                [Language.Deutsch] = "Dreh das Tablet erst in die eine, dann in die andere Richtung.",
                [Language.English] = "Twist the tablet one way, then the other.",
            },
            ["minigame_raccoon_howto_line2"] = new()
            {
                [Language.Nederlands] = "Blijf heen en weer draaien tot de deksel eraf ploft!",
                [Language.Deutsch] = "Dreh weiter hin und her, bis der Deckel abspringt!",
                [Language.English] = "Keep twisting until the lid pops off!",
            },
            ["minigame_raccoon_success_title"] = new()
            {
                [Language.Nederlands] = "Lekkere snoepjes!",
                [Language.Deutsch] = "Leckere Leckerlis!",
                [Language.English] = "Tasty treats!",
            },
            ["minigame_raccoon_success_desc"] = new()
            {
                [Language.Nederlands] = "De wasbeer heeft de pot opengekregen!",
                [Language.Deutsch] = "Der Waschbär hat das Glas aufbekommen!",
                [Language.English] = "The raccoon got the jar open!",
            },
            ["minigame_otter_title"] = new()
            {
                [Language.Nederlands] = "Kraak de Schelp!",
                [Language.Deutsch] = "Knack die Muschel!",
                [Language.English] = "Crack the Shell!",
            },
            ["minigame_otter_instruction"] = new()
            {
                [Language.Nederlands] = "Schud de tablet om de schelp te kraken!",
                [Language.Deutsch] = "Schüttel das Tablet, um die Muschel zu knacken!",
                [Language.English] = "Shake the tablet to crack the shell open!",
            },
            ["minigame_otter_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_otter_howto_intro"] = new()
            {
                [Language.Nederlands] = "De otter heeft een schelp met een lekker hapje erin gevonden, maar hij zit potdicht!",
                [Language.Deutsch] = "Der Otter hat eine Muschel mit einem leckeren Happen darin gefunden, aber sie ist fest verschlossen!",
                [Language.English] = "The otter found a shell with a tasty snack inside, but it's shut tight!",
            },
            ["minigame_otter_howto_line1"] = new()
            {
                [Language.Nederlands] = "Schud de tablet om de schelp te kraken.",
                [Language.Deutsch] = "Schüttel das Tablet, um die Muschel zu knacken.",
                [Language.English] = "Shake the tablet to crack the shell.",
            },
            ["minigame_otter_howto_line2"] = new()
            {
                [Language.Nederlands] = "Blijf schudden tot de schelp openbreekt!",
                [Language.Deutsch] = "Schüttel weiter, bis die Muschel aufbricht!",
                [Language.English] = "Keep shaking until it breaks open!",
            },
            ["minigame_otter_success_title"] = new()
            {
                [Language.Nederlands] = "Smikkelen maar!",
                [Language.Deutsch] = "Schmaus-Zeit!",
                [Language.English] = "Snack time!",
            },
            ["minigame_otter_success_desc"] = new()
            {
                [Language.Nederlands] = "De otter heeft de schelp gekraakt!",
                [Language.Deutsch] = "Der Otter hat die Muschel geknackt!",
                [Language.English] = "The otter cracked the shell open!",
            },
            ["minigame_beaver_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_beaver_howto_intro"] = new()
            {
                [Language.Nederlands] = "De bever balanceert een stok. Help hem om hem recht te houden!",
                [Language.Deutsch] = "Der Biber balanciert einen Stock. Hilf ihm, ihn gerade zu halten!",
                [Language.English] = "The beaver is balancing a stick. Help him keep it steady!",
            },
            ["minigame_beaver_howto_line1"] = new()
            {
                [Language.Nederlands] = "Kantel de tablet zachtjes naar links en rechts.",
                [Language.Deutsch] = "Kipp das Tablet sanft nach links und rechts.",
                [Language.English] = "Tilt the tablet gently left and right.",
            },
            ["minigame_beaver_howto_line2"] = new()
            {
                [Language.Nederlands] = "Houd de stok in het midden tot de tijd op is!",
                [Language.Deutsch] = "Halt den Stock in der Mitte, bis die Zeit um ist!",
                [Language.English] = "Keep the stick centered until the time runs out!",
            },
            ["minigame_polarbear_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_polarbear_howto_intro"] = new()
            {
                [Language.Nederlands] = "De ijsbeer heeft het veel te warm! Help hem afkoelen.",
                [Language.Deutsch] = "Dem Eisbären ist viel zu heiß! Hilf ihm, sich abzukühlen.",
                [Language.English] = "The polar bear is far too hot! Help him cool down.",
            },
            ["minigame_polarbear_howto_line1"] = new()
            {
                [Language.Nederlands] = "Blaas in de microfoon van je tablet, net als een koude wind.",
                [Language.Deutsch] = "Blas in das Mikrofon deines Tablets, wie ein kalter Wind.",
                [Language.English] = "Blow into your tablet's microphone, like a cold wind.",
            },
            ["minigame_polarbear_howto_line2"] = new()
            {
                [Language.Nederlands] = "Blijf blazen tot de ijsbeer helemaal is afgekoeld!",
                [Language.Deutsch] = "Blas weiter, bis der Eisbär ganz abgekühlt ist!",
                [Language.English] = "Keep blowing until the polar bear is all cooled down!",
            },
            ["minigame_baboon_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_baboon_howto_intro"] = new()
            {
                [Language.Nederlands] = "De baviaan laat een patroon van lichtjes zien. Kun jij het nadoen?",
                [Language.Deutsch] = "Der Pavian zeigt ein Muster aus Lichtern. Kannst du es nachmachen?",
                [Language.English] = "The baboon shows a pattern of lights. Can you copy it?",
            },
            ["minigame_baboon_howto_line1"] = new()
            {
                [Language.Nederlands] = "Kijk goed welke knoppen oplichten, en in welke volgorde.",
                [Language.Deutsch] = "Schau genau, welche Knöpfe aufleuchten und in welcher Reihenfolge.",
                [Language.English] = "Watch which buttons light up, and in what order.",
            },
            ["minigame_baboon_howto_line2"] = new()
            {
                [Language.Nederlands] = "Tik de knoppen daarna in precies dezelfde volgorde!",
                [Language.Deutsch] = "Tippe die Knöpfe danach in genau derselben Reihenfolge!",
                [Language.English] = "Then tap the buttons back in the exact same order!",
            },
            ["minigame_prairiedog_howto_title"] = new()
            {
                [Language.Nederlands] = "Hoe speel je?",
                [Language.Deutsch] = "So wird gespielt",
                [Language.English] = "How to Play",
            },
            ["minigame_prairiedog_howto_intro"] = new()
            {
                [Language.Nederlands] = "De prairiehonden verstoppen zich in hun holen. Kun jij ze vinden?",
                [Language.Deutsch] = "Die Präriehunde verstecken sich in ihren Höhlen. Kannst du sie finden?",
                [Language.English] = "The prairie dogs are hiding in their holes. Can you find them?",
            },
            ["minigame_prairiedog_howto_line1"] = new()
            {
                [Language.Nederlands] = "Schud de tablet en kijk goed welk hol de prairiehond kiest.",
                [Language.Deutsch] = "Schüttel das Tablet und schau genau, welche Höhle der Präriehund wählt.",
                [Language.English] = "Shake the tablet and watch which hole the prairie dog picks.",
            },
            ["minigame_prairiedog_howto_line2"] = new()
            {
                [Language.Nederlands] = "Tik daarna op het juiste hol om hem te vinden!",
                [Language.Deutsch] = "Tippe danach auf die richtige Höhle, um ihn zu finden!",
                [Language.English] = "Then tap the right hole to find him!",
            },
            ["tutorial_meet_beaver"] = new()
            {
                [Language.Nederlands] = "Zeg hallo tegen onze bever! Bevers zijn de echte bouwmeesters van de natuur, net als jij vandaag!",
                [Language.Deutsch] = "Sag mal Hallo zu unserem Biber! Biber sind die echten Baumeister der Natur, genau wie du heute!",
                [Language.English] = "Say hello to our beaver! Beavers are nature's master builders, just like you today!",
            },
            ["tutorial_fact_beaver"] = new()
            {
                [Language.Nederlands] = "De voortanden van een bever blijven altijd groeien, dus knaagt hij de hele dag op hout om ze precies goed te houden.",
                [Language.Deutsch] = "Die Vorderzähne eines Bibers hören nie auf zu wachsen, deshalb knabbert er den ganzen Tag an Holz, damit sie genau richtig bleiben.",
                [Language.English] = "A beaver's front teeth never stop growing, so it chews on wood all day to keep them just right.",
            },
            ["tutorial_meet_polarbear"] = new()
            {
                [Language.Nederlands] = "Brrr! Maak kennis met onze ijsbeer. Hij houdt meer van de kou dan wie dan ook in de hele dierentuin.",
                [Language.Deutsch] = "Brrr! Lern unseren Eisbären kennen. Er mag die Kälte lieber als alle anderen im ganzen Zoo.",
                [Language.English] = "Brrr! Meet our polar bear. They love the cold more than anyone in the whole zoo.",
            },
            ["tutorial_fact_polarbear"] = new()
            {
                [Language.Nederlands] = "De vacht van een ijsbeer lijkt wit, maar is eigenlijk doorzichtig! Daaronder is zijn huid zwart.",
                [Language.Deutsch] = "Das Fell eines Eisbären sieht weiß aus, ist aber eigentlich durchsichtig! Darunter ist seine Haut schwarz.",
                [Language.English] = "A polar bear's fur looks white, but it's really see-through! Underneath, its skin is black.",
            },
            ["tutorial_meet_raccoon"] = new()
            {
                [Language.Nederlands] = "Hier zijn onze slimme wasberen. Let goed op je snacks als die in de buurt zijn!",
                [Language.Deutsch] = "Hier sind unsere schlauen Waschbären. Pass gut auf deine Snacks auf, wenn die in der Nähe sind!",
                [Language.English] = "Here's our clever little raccoons. Watch your snacks around these ones!",
            },
            ["tutorial_fact_raccoon"] = new()
            {
                [Language.Nederlands] = "Wasberen dopen hun eten graag in water voordat ze het opeten, bijna alsof ze het wassen.",
                [Language.Deutsch] = "Waschbären tunken ihr Essen gern ins Wasser, bevor sie es fressen, fast so, als würden sie es waschen.",
                [Language.English] = "Raccoons love to dip their food in water before they eat, almost like they're washing it.",
            },
            ["tutorial_meet_prairiedog"] = new()
            {
                [Language.Nederlands] = "Plop! Daar is onze prairiehond. Laat de naam je niet voor de gek houden, het is helemaal geen hond!",
                [Language.Deutsch] = "Plopp! Da ist unser Präriehund. Lass dich vom Namen nicht täuschen, das ist überhaupt kein Hund!",
                [Language.English] = "Pop! There's our prairie dog. Don't let the name fool you, it's not a dog at all!",
            },
            ["tutorial_fact_prairiedog"] = new()
            {
                [Language.Nederlands] = "Prairiehonden zijn eigenlijk een soort eekhoorn, en ze wonen in enorme ondergrondse steden vol tunnels.",
                [Language.Deutsch] = "Präriehunde sind eigentlich eine Art Eichhörnchen, und sie wohnen in riesigen unterirdischen Städten voller Tunnel.",
                [Language.English] = "Prairie dogs are actually a kind of squirrel, and they live in huge underground towns full of tunnels.",
            },
            ["tutorial_meet_baboon"] = new()
            {
                [Language.Nederlands] = "Maak kennis met onze bavianen! Zij zijn een van de slimste dieren in de hele dierentuin.",
                [Language.Deutsch] = "Lern unsere Paviane kennen! Sie sind eins der schlausten Tiere im ganzen Zoo.",
                [Language.English] = "Meet our baboons! They are one of the smartest animals in the whole zoo.",
            },
            ["tutorial_fact_baboon"] = new()
            {
                [Language.Nederlands] = "Bavianen leven in grote families die troepen heten, en ze praten met elkaar met geluiden en gekke gezichten.",
                [Language.Deutsch] = "Paviane leben in großen Familien, die man Horden nennt, und sie reden miteinander mit Lauten und lustigen Gesichtern.",
                [Language.English] = "Baboons live in big family groups called troops, and they talk to each other with sounds and funny faces.",
            },
            ["tutorial_meet_hippo"] = new()
            {
                [Language.Nederlands] = "Plons! Daar komt ons nijlpaard. Hij blijft graag lekker koel in het water.",
                [Language.Deutsch] = "Platsch! Da kommt unser Nilpferd. Es bleibt gern schön kühl im Wasser.",
                [Language.English] = "Splash! Here comes our hippo. He likes to keep nice and cool in the water.",
            },
            ["tutorial_fact_hippo"] = new()
            {
                [Language.Nederlands] = "Een nijlpaard kan wel vijf hele minuten zijn adem onder water inhouden!",
                [Language.Deutsch] = "Ein Nilpferd kann etwa fünf ganze Minuten lang unter Wasser die Luft anhalten!",
                [Language.English] = "A hippo can hold its breath underwater for about five whole minutes!",
            },
            ["tutorial_meet_parrot"] = new()
            {
                [Language.Nederlands] = "En hier is onze papegaai, het kletsende dier van Wildlands!",
                [Language.Deutsch] = "Und hier ist unser Papagei, das plapprigste Tier in Wildlands!",
                [Language.English] = "And here's our parrot, the chattiest animal in Wildlands!",
            },
            ["tutorial_fact_parrot"] = new()
            {
                [Language.Nederlands] = "Sommige papegaaien kunnen de woorden die mensen zeggen nadoen. Misschien leren ze zelfs jouw naam!",
                [Language.Deutsch] = "Manche Papageien können die Wörter nachmachen, die Menschen sagen. Vielleicht lernen sie sogar deinen Namen!",
                [Language.English] = "Some parrots can copy the words people say. They might even learn your name!",
            },
            ["tutorial_meet_otter"] = new()
            {
                [Language.Nederlands] = "Hier is onze speelse otter! Otters houden van spetteren, glijden en de hele dag spelen.",
                [Language.Deutsch] = "Hier ist unser verspielter Otter! Otter lieben es, zu plantschen, zu rutschen und den ganzen Tag zu spielen.",
                [Language.English] = "Here's our playful otter! Otters love to splash, slide, and play all day long.",
            },
            ["tutorial_fact_otter"] = new()
            {
                [Language.Nederlands] = "Otters houden elkaars pootjes vast als ze slapen, zodat ze niet van elkaar wegdrijven!",
                [Language.Deutsch] = "Otter halten sich beim Schlafen an den Pfoten, damit sie nicht voneinander wegtreiben!",
                [Language.English] = "Otters hold hands while they sleep so they don't float away from each other!",
            },
            ["rfact_beaver_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat de tanden van een bever oranje zijn? Die kleur komt door ijzer, en dat maakt ze supersterk.",
                [Language.Deutsch] = "Wusstest du, dass die Zähne eines Bibers orange sind? Diese Farbe kommt vom Eisen, und das macht sie superstark.",
                [Language.English] = "Did you know a beaver's teeth are orange? That color comes from iron, which makes them super strong.",
            },
            ["rfact_beaver_1"] = new()
            {
                [Language.Nederlands] = "Wist je dat bevers wel 15 minuten lang hun adem onder water kunnen inhouden? Dat is hartstikke lang!",
                [Language.Deutsch] = "Wusstest du, dass Biber bis zu 15 Minuten lang unter Wasser die Luft anhalten können? Das ist richtig lang!",
                [Language.English] = "Did you know beavers can hold their breath underwater for up to 15 minutes? That's a long time!",
            },
            ["rfact_beaver_2"] = new()
            {
                [Language.Nederlands] = "Een bever slaat met zijn platte staart op het water, met een grote PLONS, om zijn familie voor gevaar te waarschuwen.",
                [Language.Deutsch] = "Ein Biber schlägt mit seinem flachen Schwanz aufs Wasser, mit einem großen PLATSCH, um seine Familie vor Gefahr zu warnen.",
                [Language.English] = "A beaver slaps its flat tail on the water with a big SPLASH to warn its family of danger.",
            },
            ["rfact_polarbear_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat een ijsbeer een zeehond kan ruiken van meer dan een kilometer ver weg? Wat een neus!",
                [Language.Deutsch] = "Wusstest du, dass ein Eisbär eine Robbe aus mehr als einem Kilometer Entfernung riechen kann? Was für eine Nase!",
                [Language.English] = "Did you know a polar bear can smell a seal from over a kilometer away? What a nose!",
            },
            ["rfact_polarbear_1"] = new()
            {
                [Language.Nederlands] = "IJsberen zijn geweldige zwemmers en kunnen urenlang doorzwemmen zonder te stoppen.",
                [Language.Deutsch] = "Eisbären sind tolle Schwimmer und können stundenlang schwimmen, ohne anzuhalten.",
                [Language.English] = "Polar bears are amazing swimmers and can paddle for hours without stopping.",
            },
            ["rfact_polarbear_2"] = new()
            {
                [Language.Nederlands] = "Wist je dat een ijsbeerbaby een welp heet? Bij de geboorte is hij ongeveer zo groot als een cavia!",
                [Language.Deutsch] = "Wusstest du, dass ein Eisbärbaby Jungtier heißt? Bei der Geburt ist es ungefähr so groß wie ein Meerschweinchen!",
                [Language.English] = "Did you know a baby polar bear is called a cub? It's about the size of a guinea pig when it's born!",
            },
            ["rfact_raccoon_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat de pootjes van een wasbeer zo handig zijn dat ze bijna als kleine handjes werken?",
                [Language.Deutsch] = "Wusstest du, dass die Pfoten eines Waschbären so geschickt sind, dass sie fast wie kleine Hände funktionieren?",
                [Language.English] = "Did you know a raccoon's paws are so clever they work almost like tiny hands?",
            },
            ["rfact_raccoon_1"] = new()
            {
                [Language.Nederlands] = "De donkere vacht rond de ogen van een wasbeer lijkt net een klein maskertje.",
                [Language.Deutsch] = "Das dunkle Fell rund um die Augen eines Waschbären sieht aus wie eine kleine Maske.",
                [Language.English] = "The dark fur around a raccoon's eyes looks just like a little mask.",
            },
            ["rfact_raccoon_2"] = new()
            {
                [Language.Nederlands] = "Wist je dat wasberen meestal 's nachts wakker zijn? Zulke dieren noemen we nachtdieren.",
                [Language.Deutsch] = "Wusstest du, dass Waschbären meistens nachts wach sind? Solche Tiere nennen wir nachtaktiv.",
                [Language.English] = "Did you know raccoons are mostly awake at night? We call animals like that nocturnal.",
            },
            ["rfact_prairiedog_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat prairiehonden elkaar begroeten met iets dat net op een kusje lijkt?",
                [Language.Deutsch] = "Wusstest du, dass Präriehunde sich mit etwas begrüßen, das aussieht wie ein kleines Küsschen?",
                [Language.English] = "Did you know prairie dogs greet each other with something that looks just like a little kiss?",
            },
            ["rfact_prairiedog_1"] = new()
            {
                [Language.Nederlands] = "Prairiehonden hebben verschillende geluiden voor verschillend gevaar: een blafje voor een havik, een ander voor een coyote.",
                [Language.Deutsch] = "Präriehunde haben verschiedene Laute für verschiedene Gefahren: ein Bellen für einen Falken, ein anderes für einen Kojoten.",
                [Language.English] = "Prairie dogs have different sounds for different dangers, one bark for a hawk, another for a coyote.",
            },
            ["rfact_prairiedog_2"] = new()
            {
                [Language.Nederlands] = "Wist je dat hun ondergrondse steden wel honderden tunnels kunnen hebben? Net een gigantisch doolhof!",
                [Language.Deutsch] = "Wusstest du, dass ihre unterirdischen Städte Hunderte von Tunneln haben können? Wie ein riesiges Labyrinth!",
                [Language.English] = "Did you know their underground towns can have hundreds of tunnels? Like a giant maze!",
            },
            ["rfact_baboon_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat bavianen elkaar schoonhouden door voorzichtig door elkaars vacht te kammen?",
                [Language.Deutsch] = "Wusstest du, dass Paviane sich gegenseitig sauber halten, indem sie behutsam durch das Fell des anderen kämmen?",
                [Language.English] = "Did you know baboons keep each other clean by gently brushing through one another's fur?",
            },
            ["rfact_baboon_1"] = new()
            {
                [Language.Nederlands] = "Bavianen zijn echte probleemoplossers en kunnen zelfs simpele puzzels uitvogelen.",
                [Language.Deutsch] = "Paviane sind echte Problemlöser und können sogar einfache Rätsel knacken.",
                [Language.English] = "Baboons are great problem-solvers and can even figure out simple puzzles.",
            },
            ["rfact_baboon_2"] = new()
            {
                [Language.Nederlands] = "Een baviaan loopt op alle vier zijn poten, maar kan rechtop gaan staan om rond te kijken.",
                [Language.Deutsch] = "Ein Pavian läuft auf allen vier Beinen, kann sich aber aufrichten, um sich umzuschauen.",
                [Language.English] = "A baboon walks on all four legs, but it can stand up tall to look around.",
            },
            ["rfact_hippo_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat een nijlpaard zijn eigen roze zonnebrand maakt? Zijn huid geeft een speciale olie af die de zon tegenhoudt.",
                [Language.Deutsch] = "Wusstest du, dass ein Nilpferd seine eigene rosa Sonnencreme macht? Seine Haut gibt ein besonderes Öl ab, das die Sonne abhält.",
                [Language.English] = "Did you know a hippo makes its own pinkish sunscreen? Its skin oozes a special oil to block the sun.",
            },
            ["rfact_hippo_1"] = new()
            {
                [Language.Nederlands] = "Hoewel nijlpaarden enorm groot zijn, kunnen ze verrassend snel rennen op het land.",
                [Language.Deutsch] = "Obwohl Nilpferde riesig sind, können sie an Land überraschend schnell rennen.",
                [Language.English] = "Even though hippos are huge, they can run surprisingly fast on land.",
            },
            ["rfact_hippo_2"] = new()
            {
                [Language.Nederlands] = "Wist je dat een nijlpaardbaby onder water melk kan drinken bij zijn moeder? Wat een knappe truc!",
                [Language.Deutsch] = "Wusstest du, dass ein Nilpferdbaby unter Wasser Milch bei seiner Mama trinken kann? Das ist mal ein cleverer Trick!",
                [Language.English] = "Did you know a baby hippo can drink its mother's milk underwater? Now that's a clever trick!",
            },
            ["rfact_otter_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat otters elkaars pootjes vasthouden als ze slapen? Zo drijven ze in het water niet uit elkaar!",
                [Language.Deutsch] = "Wusstest du, dass Otter sich beim Schlafen an den Pfoten halten? So treiben sie im Wasser nicht auseinander!",
                [Language.English] = "Did you know otters hold hands while they sleep? That way they don't drift apart in the water!",
            },
            ["rfact_otter_1"] = new()
            {
                [Language.Nederlands] = "Een otter heeft een speciaal zakje van huid onder zijn arm om zijn lievelingssteentje veilig te bewaren.",
                [Language.Deutsch] = "Ein Otter hat eine besondere Hauttasche unter dem Arm, um seinen Lieblingsstein sicher aufzubewahren.",
                [Language.English] = "An otter has a special pocket of skin under its arm to keep its favorite rock safe.",
            },
            ["rfact_otter_2"] = new()
            {
                [Language.Nederlands] = "Wist je dat otters de dikste vacht van alle dieren hebben? Wel een miljoen haartjes op een klein plekje!",
                [Language.Deutsch] = "Wusstest du, dass Otter das dichteste Fell aller Tiere haben? Bis zu eine Million Härchen auf einem einzigen kleinen Fleck!",
                [Language.English] = "Did you know otters have the thickest fur of any animal? Up to a million hairs in one tiny patch!",
            },
            ["rfact_parrot_0"] = new()
            {
                [Language.Nederlands] = "Wist je dat sommige papegaaien meer dan 50 jaar oud kunnen worden? Dat is langer dan een hond of een kat!",
                [Language.Deutsch] = "Wusstest du, dass manche Papageien über 50 Jahre alt werden können? Das ist länger als ein Hund oder eine Katze!",
                [Language.English] = "Did you know some parrots can live for more than 50 years? Longer than a dog or a cat!",
            },
            ["rfact_parrot_1"] = new()
            {
                [Language.Nederlands] = "Papegaaien gebruiken hun sterke snavel als gereedschap om harde noten open te kraken.",
                [Language.Deutsch] = "Papageien benutzen ihren starken Schnabel wie ein Werkzeug, um harte Nüsse zu knacken.",
                [Language.English] = "Parrots use their strong beaks like a tool to crack open hard nuts.",
            },
            ["rfact_parrot_2"] = new()
            {
                [Language.Nederlands] = "Een papegaai heeft twee tenen die naar voren wijzen en twee naar achteren, perfect om aan takken vast te houden.",
                [Language.Deutsch] = "Ein Papagei hat zwei Zehen, die nach vorne zeigen, und zwei nach hinten, perfekt zum Festhalten an Ästen.",
                [Language.English] = "A parrot has two toes pointing forward and two pointing back, perfect for gripping branches.",
            },
            ["welcome_back_0"] = new()
            {
                [Language.Nederlands] = "Welkom terug, ontdekker! De dieren hebben je gemist.",
                [Language.Deutsch] = "Willkommen zurück, Entdecker! Die Tiere haben dich vermisst.",
                [Language.English] = "Welcome back, explorer! The animals missed you.",
            },
            ["welcome_back_1"] = new()
            {
                [Language.Nederlands] = "Welkom terug! Kijk eens hoe ver onze dierentuin al is gekomen. Daar mag je trots op zijn!",
                [Language.Deutsch] = "Willkommen zurück! Schau mal, wie weit unser Zoo schon gekommen ist. Darauf darfst du stolz sein!",
                [Language.English] = "Welcome back! Look how far our zoo has come. You should be proud!",
            },
            ["welcome_back_2"] = new()
            {
                [Language.Nederlands] = "Welkom terug! Neem rustig de tijd om even rond te kijken.",
                [Language.Deutsch] = "Willkommen zurück! Lass dir ruhig Zeit und schau dich um.",
                [Language.English] = "Welcome back! Take your time and look around.",
            },
            ["welcome_back_3"] = new()
            {
                [Language.Nederlands] = "Welkom terug! Wanneer je er klaar voor bent, is er altijd nog een verblijf om te bouwen.",
                [Language.Deutsch] = "Willkommen zurück! Wann immer du bereit bist, gibt es noch ein Gehege zu bauen.",
                [Language.English] = "Welcome back! Whenever you're ready, there's always another habitat to build.",
            },
            ["tutorial_done_0"] = new()
            {
                [Language.Nederlands] = "Het is je gelukt, ontdekker! Nu heeft elk dier een heerlijk thuis.",
                [Language.Deutsch] = "Du hast es geschafft, Entdecker! Jetzt hat jedes Tier ein wunderbares Zuhause.",
                [Language.English] = "You did it, explorer! Every single animal has a wonderful home now.",
            },
            ["tutorial_done_1"] = new()
            {
                [Language.Nederlands] = "Wildlands zit vol leven, allemaal dankzij jou. De dieren zullen het nooit vergeten!",
                [Language.Deutsch] = "Wildlands steckt voller Leben, alles dank dir. Die Tiere werden es nie vergessen!",
                [Language.English] = "Wildlands is full of life, all thanks to you. The animals will never forget it!",
            },
            ["tutorial_done_2"] = new()
            {
                [Language.Nederlands] = "De dierentuin is van jou om van te genieten. Kom je vrienden gerust opzoeken, en blijf nieuwsgierig!",
                [Language.Deutsch] = "Der Zoo gehört dir zum Genießen. Besuch deine Freunde, wann immer du willst, und bleib neugierig!",
                [Language.English] = "The zoo is yours to enjoy. Visit your friends anytime and keep being curious!",
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