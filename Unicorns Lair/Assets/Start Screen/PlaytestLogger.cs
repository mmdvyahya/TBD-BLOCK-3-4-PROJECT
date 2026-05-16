using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaytestLogger : MonoBehaviour
{
    public static PlaytestLogger Instance { get; private set; }

    [Header("Participant")]
    [SerializeField] private string participantId = "P01";
    [SerializeField] private bool autoStartSession = false;

    [Header("File")]
    [SerializeField] private string fileName = "playtest_log.csv";

    private string filePath;

    private float sessionStartTime;
    private float sceneStartTime;

    private string currentSceneName;

    private bool sessionActive;
    private bool sessionEndedProperly;

    // NEW TRACKING
    private int totalSuccessCount;
    private int totalFailCount;
    private int totalRetryCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine(
            Application.persistentDataPath,
            fileName
        );

        EnsureFileExists();

        Debug.Log("PLAYTEST LOG FILE: " + filePath);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (autoStartSession && !sessionActive)
        {
            StartNewSession(participantId);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    // SESSION
 
    public void StartNewSession(string newParticipantId)
    {
        participantId = newParticipantId;

        sessionStartTime = Time.realtimeSinceStartup;
        sceneStartTime = Time.realtimeSinceStartup;

        currentSceneName =
            SceneManager.GetActiveScene().name;

        sessionActive = true;
        sessionEndedProperly = false;

        // RESET COUNTERS
        totalSuccessCount = 0;
        totalFailCount = 0;
        totalRetryCount = 0;

        LogEvent(
            "SessionStart",
            "Session started"
        );

        LogEvent(
            "SceneEnter",
            currentSceneName
        );
    }

    public void EndSessionProperly(
        string reason = "Completed test"
    )
    {
        if (!sessionActive)
            return;

        LogCurrentSceneExit(
            "Session ended properly"
        );

        float totalTime =
            Time.realtimeSinceStartup -
            sessionStartTime;

        // SUMMARY
        LogEvent(
            "SessionSummary",
            "successes=" + totalSuccessCount +
            " | fails=" + totalFailCount +
            " | retries=" + totalRetryCount
        );

        LogEvent(
            "SessionEnd",
            reason +
            " | totalTime=" +
            totalTime.ToString("F1") + "s"
        );

        sessionEndedProperly = true;
        sessionActive = false;
    }

 
    // SCENE TRACKING
 

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        if (!sessionActive)
            return;

        LogCurrentSceneExit(
            "Scene changed"
        );

        currentSceneName = scene.name;

        sceneStartTime =
            Time.realtimeSinceStartup;

        LogEvent(
            "SceneEnter",
            currentSceneName
        );
    }

    private void LogCurrentSceneExit(
        string reason
    )
    {
        if (string.IsNullOrEmpty(currentSceneName))
        {
            currentSceneName =
                SceneManager.GetActiveScene().name;
        }

        float sceneTime =
            Time.realtimeSinceStartup -
            sceneStartTime;

        LogEvent(
            "SceneExit",
            currentSceneName +
            " | duration=" +
            sceneTime.ToString("F1") +
            "s | reason=" + reason
        );
    }

    // GENERAL PROGRESS


    public void LogProgress(
        string progressDetails
    )
    {
        if (!sessionActive)
            return;

        LogEvent(
            "Progress",
            progressDetails
        );
    }

   
    // SUCCESS / FAIL / RETRY
 

    public void LogMinigameSuccess(
        string minigameName
    )
    {
        if (!sessionActive)
            return;

        totalSuccessCount++;

        LogEvent(
            "MinigameSuccess",
            minigameName +
            " | totalSuccess=" +
            totalSuccessCount
        );
    }

    public void LogMinigameFail(
        string minigameName,
        string reason
    )
    {
        if (!sessionActive)
            return;

        totalFailCount++;

        LogEvent(
            "MinigameFail",
            minigameName +
            " | reason=" + reason +
            " | totalFails=" +
            totalFailCount
        );
    }

    public void LogMinigameRetry(
        string minigameName
    )
    {
        if (!sessionActive)
            return;

        totalRetryCount++;

        LogEvent(
            "MinigameRetry",
            minigameName +
            " | totalRetries=" +
            totalRetryCount
        );
    }

    
    // SUS
 

    public void LogSUSAnswer(
        int questionNumber,
        int score
    )
    {
        if (!sessionActive)
            return;

        LogEvent(
            "SUSAnswer",
            "Q" + questionNumber +
            "=" + score
        );
    }

    public void LogSUSScore(
        int rawScore,
        float finalScore
    )
    {
        if (!sessionActive)
            return;

        LogEvent(
            "SUSScore",
            "raw=" + rawScore +
            " | final=" +
            finalScore.ToString("F1")
        );
    }


    // APP CLOSE / INTERRUPT
   

    private void OnApplicationPause(
        bool paused
    )
    {
        if (paused)
        {
            LogTemporaryStop(
                "Application paused"
            );
        }
    }

    private void OnApplicationQuit()
    {
        if (!sessionActive ||
            sessionEndedProperly)
            return;

        LogCurrentSceneExit(
            "Application closed early"
        );

        float totalTime =
            Time.realtimeSinceStartup -
            sessionStartTime;

        LogEvent(
            "SessionAborted",
            "Application closed early | lastScene=" +
            currentSceneName +
            " | totalTime=" +
            totalTime.ToString("F1") + "s"
        );
    }

    private void LogTemporaryStop(
        string reason
    )
    {
        if (!sessionActive ||
            sessionEndedProperly)
            return;

        float sceneTime =
            Time.realtimeSinceStartup -
            sceneStartTime;

        float totalTime =
            Time.realtimeSinceStartup -
            sessionStartTime;

        LogEvent(
            "SessionInterrupted",
            reason +
            " | currentScene=" +
            currentSceneName +
            " | sceneTime=" +
            sceneTime.ToString("F1") +
            "s | totalTime=" +
            totalTime.ToString("F1") + "s"
        );
    }


    // CSV
 

    private void LogEvent(
        string eventName,
        string details
    )
    {
        string timestamp =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );

        string activeScene =
            SceneManager
            .GetActiveScene()
            .name;

        float totalTime =
            sessionActive
            ? Time.realtimeSinceStartup -
              sessionStartTime
            : 0f;

        string line =
            Csv(timestamp) + "," +
            Csv(participantId) + "," +
            Csv(eventName) + "," +
            Csv(activeScene) + "," +
            Csv(totalTime.ToString("F1")) + "," +
            Csv(details) +
            "\n";

        File.AppendAllText(
            filePath,
            line
        );

        Debug.Log(
            "[PlaytestLogger] " +
            eventName +
            " | " + details
        );
    }

    private void EnsureFileExists()
    {
        if (File.Exists(filePath))
            return;

        string header =
            "timestamp," +
            "participant," +
            "event," +
            "activeScene," +
            "totalSessionTime," +
            "details\n";

        File.WriteAllText(
            filePath,
            header
        );
    }

    private string Csv(string value)
    {
        if (value == null)
            value = "";

        value = value.Replace(
            "\"",
            "\"\""
        );

        return "\"" + value + "\"";
    }

    
    // DEBUG
    

    public string GetLogFilePath()
    {
        return filePath;
    }
}