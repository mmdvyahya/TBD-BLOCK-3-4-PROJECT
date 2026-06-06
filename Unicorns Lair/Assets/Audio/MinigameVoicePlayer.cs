using System.Collections;
using UnityEngine;

public class MinigameVoicePlayer : MonoBehaviour
{
    static MinigameVoicePlayer instance;
    public static MinigameVoicePlayer Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("MinigameVoicePlayer");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MinigameVoicePlayer>();
            }
            return instance;
        }
    }

    Coroutine currentCoroutine;
    SoundData activeData;

    public static void Stop()
    {
        if (Instance.currentCoroutine != null)
        {
            Instance.StopCoroutine(Instance.currentCoroutine);
            Instance.currentCoroutine = null;
        }
        if (Instance.activeData != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.FadeOut(Instance.activeData);
            Instance.activeData = null;
        }
    }

    public static void PlayLocalizedForPage(LocalizedSoundData localized, int page, bool glueFirstTwo = false)
    {
        Instance.PlayLocalizedInternal(localized, page, glueFirstTwo);
    }

    void PlayLocalizedInternal(LocalizedSoundData localized, int page, bool glueFirstTwo)
    {
        Stop();
        if (localized == null) return;
        var data = VoiceLocalizer.Resolve(localized);
        if (data == null) return;
        activeData = data;

        // If requested, glue clips element 0+1 for page 0
        if (glueFirstTwo && page == 0 && data.clips != null && data.clips.Length >= 2)
        {
            currentCoroutine = StartCoroutine(PlayClipsSequential(data, 0, 1));
            return;
        }

        // Determine clip index to play based on page, accounting for glued clips if needed
        int clipIndex = page;
        if (glueFirstTwo && page > 0)
        {
            clipIndex = page + 1; // skip over the second glued clip
        }

        // If clip index is within clips, play that clip directly
        if (data.clips != null && clipIndex >= 0 && clipIndex < data.clips.Length)
        {
            // Use SoundManager to play the specific clip
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayClipAt(data, clipIndex);
            else
                VoiceLocalizer.PlayLocalized(localized, clipIndex);
            return;
        }

        // Fallback: let VoiceLocalizer handle it
        VoiceLocalizer.PlayLocalized(localized, page);
    }

    IEnumerator PlayClipsSequential(SoundData data, int firstIndex, int secondIndex)
    {
        if (data == null || SoundManager.Instance == null) yield break;
        if (firstIndex < 0 || firstIndex >= data.clips.Length) yield break;

        SoundManager.Instance.PlayClipAt(data, firstIndex);
        var clip = data.clips[firstIndex];
        float wait = clip != null ? clip.length : 0f;
        yield return new WaitForSeconds(wait);

        if (secondIndex >= 0 && secondIndex < data.clips.Length)
            SoundManager.Instance.PlayClipAt(data, secondIndex);

        currentCoroutine = null;
    }
}
