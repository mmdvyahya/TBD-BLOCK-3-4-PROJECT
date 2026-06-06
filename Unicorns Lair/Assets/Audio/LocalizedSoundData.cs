using UnityEngine;

[CreateAssetMenu(fileName = "LocalizedSoundData", menuName = "Audio/Localized Sound Data")]
public class LocalizedSoundData : ScriptableObject
{
    public SoundData dutch;
    public SoundData german;
    public SoundData english;

    public SoundData GetFor(Language language)
    {
        switch (language)
        {
            case Language.Nederlands: return dutch ?? english ?? german;
            case Language.Deutsch: return german ?? english ?? dutch;
            case Language.English: return english ?? dutch ?? german;
            default: return english ?? dutch ?? german;
        }
    }
}
