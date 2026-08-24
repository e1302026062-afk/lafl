using UnityEngine;
using UnityEngine.InputSystem;

public sealed class B11GameBootstrap : MonoBehaviour
{
    [SerializeField] private B11PlayerController player1;
    [SerializeField] private B11PlayerController player2;
    private bool initialSelectionRunning;

    private void Awake()
    {
        if (player1 == null) player1 = GameObject.Find("P1")?.GetComponent<B11PlayerController>();
        if (player2 == null) player2 = GameObject.Find("P2")?.GetComponent<B11PlayerController>();
    }

    private void Start()
    {
        InputDevice p1Device = null;
        InputDevice p2Device = null;

        if (B11DeviceSession.Instance != null && B11DeviceSession.Instance.HasAssignment)
        {
            p1Device = B11DeviceSession.Instance.Player1Device;
            p2Device = B11DeviceSession.Instance.Player2Device;
        }
        else
        {
            // 接続画面をまだ通らない場合の開発用フォールバック。
            if (Gamepad.all.Count >= 2)
            {
                p1Device = Gamepad.all[0];
                p2Device = Gamepad.all[1];
            }
            else if (Keyboard.current != null)
            {
                p1Device = Keyboard.current;
                p2Device = Keyboard.current;
            }
        }

        if (player1 != null && p1Device != null)
        {
            AssignPlayerDevices(player1, p1Device);
            player1.SetInputActive(false);
        }

        if (player2 != null && p2Device != null)
        {
            AssignPlayerDevices(player2, p2Device);
            player2.SetInputActive(false);
        }

        StartCoroutine(BeginGameAfterInitialHandSelection());
    }

    private void AssignPlayerDevices(B11PlayerController player, InputDevice primaryDevice)
    {
        if (primaryDevice is Keyboard && Mouse.current != null)
        {
            player.AssignDevices(primaryDevice, Mouse.current);
            return;
        }

        player.AssignDevices(primaryDevice);
    }

    private void SetGameActive(B11PlayerController controller)
    {
        PlayerJanken janken = controller.GetComponent<PlayerJanken>();
        if (janken != null) janken.isGameActive = true;
    }

    private System.Collections.IEnumerator BeginGameAfterInitialHandSelection()
    {
        PlayerJanken p1Janken = player1 != null ? player1.GetComponent<PlayerJanken>() : null;
        PlayerJanken p2Janken = player2 != null ? player2.GetComponent<PlayerJanken>() : null;
        if (p1Janken == null || p2Janken == null || player1.PrimaryDevice == null || player2.PrimaryDevice == null) yield break;

        yield return null;

        p1Janken.isGameActive = false;
        p2Janken.isGameActive = false;
        initialSelectionRunning = true;
        p1Janken.BeginInitialHandSelection();
        p2Janken.BeginInitialHandSelection();

        const string gameStartTimestampKey = "B11_GameStartTimestamp";
        float initialSelectionLimit = 10f;
        if (PlayerPrefs.HasKey(gameStartTimestampKey))
        {
            float startTimestamp = PlayerPrefs.GetFloat(gameStartTimestampKey, Time.realtimeSinceStartup);
            initialSelectionLimit = Mathf.Max(0f, 10f - (Time.realtimeSinceStartup - startTimestamp));
            PlayerPrefs.DeleteKey(gameStartTimestampKey);
            PlayerPrefs.Save();
        }
        while (initialSelectionLimit > 0f)
        {
            if (p1Janken.respawnUIPanel != null) p1Janken.respawnUIPanel.SetActive(true);
            if (p2Janken.respawnUIPanel != null) p2Janken.respawnUIPanel.SetActive(true);
            int seconds = Mathf.CeilToInt(initialSelectionLimit);
            if (p1Janken.respawnTimerText != null) p1Janken.respawnTimerText.text = seconds.ToString();
            if (p2Janken.respawnTimerText != null) p2Janken.respawnTimerText.text = seconds.ToString();
            if (player1 != null) player1.SetInputActive(false);
            if (player2 != null) player2.SetInputActive(false);
            initialSelectionLimit -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 10秒以内に決定しなかったプレイヤーは、最後に選択していた手で確定する。
        if (p1Janken.respawnTimerText != null) p1Janken.respawnTimerText.text = "0";
        if (p2Janken.respawnTimerText != null) p2Janken.respawnTimerText.text = "0";
        p1Janken.ForceFinishInitialHandSelection();
        p2Janken.ForceFinishInitialHandSelection();

        // 初回手選択の10秒終了後に、制限時間のカウントダウンを開始する。
        if (B11GameTimer.Instance != null)
        {
            B11GameTimer.Instance.StartTimer();
        }

        // 初回の手選択と開始カウントダウンが終わってから、
        // アイテムボックスの初回スポーンを開始する。
        foreach (ItemBox itemBox in FindObjectsByType<ItemBox>(FindObjectsSortMode.None))
        {
            itemBox.BeginInitialSpawn();
        }

        if (p1Janken.respawnUIPanel != null) p1Janken.respawnUIPanel.SetActive(false);
        if (p2Janken.respawnUIPanel != null) p2Janken.respawnUIPanel.SetActive(false);
        initialSelectionRunning = false;

        if (player1 != null)
        {
            player1.SetInputActive(true);
            SetGameActive(player1);
        }
        if (player2 != null)
        {
            player2.SetInputActive(true);
            SetGameActive(player2);
        }
    }

    private void LateUpdate()
    {
        if (player1 != null)
        {
            var janken1 = player1.GetComponent<PlayerJanken>();
            if (janken1 != null && janken1.isSelectingHand && !janken1.isGameActive && janken1.respawnUIPanel != null)
            {
                janken1.respawnUIPanel.SetActive(true);
            }
        }
        if (player2 != null)
        {
            var janken2 = player2.GetComponent<PlayerJanken>();
            if (janken2 != null && janken2.isSelectingHand && !janken2.isGameActive && janken2.respawnUIPanel != null)
            {
                janken2.respawnUIPanel.SetActive(true);
            }
        }
    }
}
