using UnityEngine;

public static class VoiceLocalizer
{
    public static SoundData Resolve(LocalizedSoundData localized, SoundData fallback = null)
    {
        if (localized == null) return fallback;
        var manager = LanguageManager.Instance;
        if (manager == null) { LanguageManager.Ensure(); manager = LanguageManager.Instance; }
        return localized.GetFor(manager != null ? manager.CurrentLanguage : Language.English) ?? fallback;
    }

    public static void PlayLocalized(LocalizedSoundData localized, int clipIndex = -1)
    {
        var data = Resolve(localized);
        if (data == null || SoundManager.Instance == null) return;
        SoundManager.Instance.FadeOut(data);
        if (clipIndex >= 0) SoundManager.Instance.PlayClipAt(data, clipIndex);
        else SoundManager.Instance.Play(data);
    }
}
