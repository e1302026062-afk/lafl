using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public sealed class B11GameTimer : MonoBehaviour
{
    public static B11GameTimer Instance { get; private set; }

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float initialSeconds = 120f;
    [SerializeField] private string resultSceneName = "ResultScene";
    [SerializeField] private float resultTransitionDelay = 2f;
    [SerializeField] private AudioSource gameplayMusic;

    private float remainingSeconds;
    private float pauseSeconds;
    private bool timeUpHandled;
    private bool timerStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        remainingSeconds = initialSeconds;
        UpdateText();
    }

    private void Update()
    {
        if (!timerStarted) return;
        if (remainingSeconds <= 0f) return;

        if (pauseSeconds > 0f)
        {
            pauseSeconds = Mathf.Max(0f, pauseSeconds - Time.deltaTime);
        }
        else
        {
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        }

        if (remainingSeconds <= 0f && !timeUpHandled)
        {
            HandleTimeUp();
        }

        UpdateText();
    }

    public void PauseTimer(float duration)
    {
        if (!timerStarted) return;
        pauseSeconds = Mathf.Max(pauseSeconds, duration);
    }

    public void StartTimer()
    {
        if (timerStarted || timeUpHandled) return;
        timerStarted = true;
        remainingSeconds = initialSeconds;
        if (gameplayMusic != null && !gameplayMusic.isPlaying)
        {
            gameplayMusic.Play();
        }
        UpdateText();
    }

    public void ModifyTime(float seconds)
    {
        if (!timerStarted) return;
        remainingSeconds = Mathf.Max(0f, remainingSeconds + seconds);
        if (remainingSeconds <= 0f && !timeUpHandled)
        {
            HandleTimeUp();
        }
        UpdateText();
    }

    private void UpdateText()
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(remainingSeconds).ToString();
        }
    }

    private void HandleTimeUp()
    {
        timeUpHandled = true;
        remainingSeconds = 0f;

        B11PlayerController player1 = GameObject.Find("P1")?.GetComponent<B11PlayerController>();
        B11PlayerController player2 = GameObject.Find("P2")?.GetComponent<B11PlayerController>();
        StopPlayer(player1);
        StopPlayer(player2);

        int p1Score = GetScore(player1);
        int p2Score = GetScore(player2);
        B11ResultSceneController.SetResult(p1Score, p2Score);

        ShowGameEndMessage(Camera.main);
        ShowGameEndMessage(GameObject.Find("Player2Camera")?.GetComponent<Camera>());
        StartCoroutine(LoadResultSceneAfterDelay());
    }

    private static void StopPlayer(B11PlayerController player)
    {
        if (player == null) return;
        player.SetInputActive(false);
        PlayerJanken janken = player.GetComponent<PlayerJanken>();
        if (janken != null) janken.isGameActive = false;
    }

    private static int GetScore(B11PlayerController player)
    {
        PlayerJanken janken = player != null ? player.GetComponent<PlayerJanken>() : null;
        return janken != null ? janken.point : 0;
    }

    private static void ShowGameEndMessage(Camera targetCamera)
    {
        if (targetCamera == null) return;

        GameObject canvasObject = new GameObject("GameEndCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = targetCamera;
        canvas.planeDistance = 1f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject textObject = new GameObject("GameEndText");
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "ゲーム終了";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 72f;
        text.color = Color.white;
        text.enableWordWrapping = false;
        TMP_FontAsset japaneseFont = JapaneseFontUtility.GetJapaneseFontAsset();
        if (japaneseFont != null) text.font = japaneseFont;
    }

    private IEnumerator LoadResultSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resultTransitionDelay);
        SceneManager.LoadScene(resultSceneName);
    }
}
