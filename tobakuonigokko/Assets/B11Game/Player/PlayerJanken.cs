using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerJanken : MonoBehaviour
{
    public enum HandType { Rock, Scissors, Paper }

    [Header("プレイヤー識別 (1 または 2)")]
    public int playerNumber = 1;

    [Header("現在のじゃんけんの手")]
    public HandType currentHand;

    [Header("頭上のアイコン設定")]
    public SpriteRenderer handIconRenderer;
    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;

    [Header("手の自動変更設定")]
    public float handChangeInterval = 10f;
    private float handChangeTimer = 0f;

    [Header("ポイント設定")]
    public int point = 0;
    public TMP_Text pointTextUI;

    [Header("リスポーン設定")]
    public Transform respawnPoint;
    private Vector3 initialPosition;

    [Header("リスポーン選択UI設定")]
    public GameObject respawnUIPanel;
    public TMP_Text respawnHandText;
    public TMP_Text respawnTimerText;

    public bool isSelectingHand = false;
    private float respawnSelectionTimer = 0f;
    private bool isInitialHandSelection = false;

    private float battleCooldown = 0f;
    public bool isGameActive = false;

    [Header("じゃんけん判定範囲")]
    [SerializeField] private float battleDetectionRadius = 1.25f;
    [SerializeField] private float battleDetectionHeight = 1.8f;

    private string[] handNames = new string[] { "ROCK", "SCISSORS", "PAPER" };

    private B11PlayerController playerController;
    private AudioSource clashAudioSource;
    private AudioClip clashAudioClip;
    private AudioClip handSelectAudioClip;

    void Start()
    {
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        initialPosition = transform.position;
        playerController = GetComponent<B11PlayerController>();
        clashAudioClip = Resources.Load<AudioClip>("Audio/clash");
        handSelectAudioClip = Resources.Load<AudioClip>("Audio/hand_select");
        if (clashAudioClip != null)
        {
            clashAudioSource = gameObject.AddComponent<AudioSource>();
            clashAudioSource.playOnAwake = false;
            clashAudioSource.loop = false;
            clashAudioSource.spatialBlend = 0f;
            float masterVolumeDb = PlayerPrefs.GetFloat("B11_MasterVolume", 0f);
            clashAudioSource.volume = masterVolumeDb <= -40f
                ? 0f
                : Mathf.Pow(10f, masterVolumeDb / 20f);
        }

        currentHand = (HandType)Random.Range(0, 3);
        UpdateHandIcon();

        handChangeTimer = Random.Range(0f, handChangeInterval);
        UpdatePointUI();

        if (respawnUIPanel != null && !isGameActive)
        {
            BeginInitialHandSelection();
        }
        ConfigureRespawnButtons();
        ApplyJapaneseFont(respawnHandText);
    }

    void Update()
    {
        if (!isGameActive && !isSelectingHand) return;

        if (isSelectingHand && !isGameActive && respawnUIPanel != null && !respawnUIPanel.activeSelf)
        {
            respawnUIPanel.SetActive(true);
        }

        if (isSelectingHand)
        {
            HandleRespawnHandSelection();
            return;
        }

        if (battleCooldown > 0)
        {
            battleCooldown -= Time.deltaTime;
        }

        handChangeTimer += Time.deltaTime;
        if (handChangeTimer >= handChangeInterval)
        {
            ChangeHandRandomly();
            handChangeTimer = 0f;
        }
    }

    private void HandleRespawnHandSelection()
    {
        respawnSelectionTimer -= Time.deltaTime;

        if (respawnTimerText != null && !isInitialHandSelection)
        {
            respawnTimerText.text = Mathf.Max(1, Mathf.CeilToInt(respawnSelectionTimer)).ToString();
        }

        bool selectNext = false;
        bool selectPrev = false;
        bool confirm = false;

        InputDevice assignedDevice = (playerController != null) ? playerController.PrimaryDevice : null;

        if (assignedDevice is Gamepad gamepad)
        {
            if (gamepad.dpad.right.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) selectNext = true;
            if (gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) selectPrev = true;
            if (gamepad.buttonSouth.wasPressedThisFrame) confirm = true;
        }
        else
        {
            if (Keyboard.current != null)
            {
                if (playerNumber == 1)
                {
                    if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) selectNext = true;
                    if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) selectPrev = true;
                    if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) confirm = true;
                }
                else if (playerNumber == 2)
                {
                    if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) selectNext = true;
                    if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) selectPrev = true;
                    if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) confirm = true;
                }
            }
        }

        if (selectNext)
        {
            currentHand = (HandType)(((int)currentHand + 1) % 3);
            UpdateHandIcon();
            UpdateRespawnUI();
        }
        else if (selectPrev)
        {
            currentHand = (HandType)(((int)currentHand + 2) % 3);
            UpdateHandIcon();
            UpdateRespawnUI();
        }

        if (confirm || (!isInitialHandSelection && respawnSelectionTimer <= 0f))
        {
            FinishHandSelection();
        }
    }

    public void BeginInitialHandSelection()
    {
        isInitialHandSelection = true;
        isSelectingHand = true;
        respawnSelectionTimer = 0f;

        if (respawnUIPanel != null) respawnUIPanel.SetActive(true);
        ConfigureRespawnButtons();
        FocusRespawnButton(currentHand);
        UpdateRespawnUI();
        if (respawnTimerText != null) respawnTimerText.text = string.Empty;
    }

    public void ForceFinishInitialHandSelection()
    {
        if (isInitialHandSelection && isSelectingHand)
        {
            FinishHandSelection();
        }
    }

    public void OnSelectHandByUI(int handIndex)
    {
        if (!isSelectingHand) return;
        currentHand = (HandType)handIndex;
        UpdateHandIcon();
        FocusRespawnButton(currentHand);
        UpdateRespawnButtonHighlights();
        // UIボタンは手の選択だけを行う。決定は割り当て済みデバイスの入力のみで確定する。
    }

    public void ClearReadyText()
    {
        if (!isInitialHandSelection && respawnHandText != null)
        {
            respawnHandText.text = string.Empty;
        }
    }

    private void UpdateRespawnUI()
    {
        EnsureRespawnButtonNavigation();
        ApplyJapaneseFont(respawnHandText);

        if (respawnHandText != null)
        {
            // 手選択中はP1/P2や選択中テキストを表示せず、決定時のREADYだけを表示する。
            respawnHandText.text = string.Empty;
        }

        UpdateRespawnButtonHighlights();
    }

    private void FinishHandSelection()
    {
        bool wasInitialSelection = isInitialHandSelection;
        isSelectingHand = false;
        isInitialHandSelection = false;
        handChangeTimer = 0f;

        if (!wasInitialSelection && playerController != null)
        {
            playerController.SetInputActive(true);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (wasInitialSelection)
        {
            if (respawnHandText != null) respawnHandText.text = "READY";
            if (respawnTimerText != null) respawnTimerText.text = string.Empty;
            PlayHandSelectSound();
        }
        else if (respawnUIPanel != null)
        {
            respawnUIPanel.SetActive(false);
        }

        Debug.Log($"P{playerNumber} の手選択が確定しました ({currentHand})。勝負再開！");
    }

    private void ChangeHandRandomly()
    {
        HandType oldHand = currentHand;

        while (currentHand == oldHand)
        {
            currentHand = (HandType)Random.Range(0, 3);
        }

        UpdateHandIcon();
        Debug.Log($"{gameObject.name} の手が {currentHand} に変わりました！");
        FocusRespawnButton(currentHand);
        UpdateRespawnButtonHighlights();
    }

    public void SetHand(HandType newHand)
    {
        currentHand = newHand;
        UpdateHandIcon();
        handChangeTimer = 0f;
    }

    public void UpdateHandIcon()
    {
        if (handIconRenderer == null) return;

        if (currentHand == HandType.Rock) handIconRenderer.sprite = rockSprite;
        else if (currentHand == HandType.Scissors) handIconRenderer.sprite = scissorsSprite;
        else if (currentHand == HandType.Paper) handIconRenderer.sprite = paperSprite;
    }

    private void ConfigureRespawnButtons()
    {
        if (respawnUIPanel == null) return;

        Button[] buttons = respawnUIPanel.GetComponentsInChildren<Button>(true);
        System.Array.Sort(buttons, (a, b) => GetRespawnHandSortIndex(a.name).CompareTo(GetRespawnHandSortIndex(b.name)));

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            // 方向キーはこのスクリプトで処理するため、UIナビゲーションは全方向を無効化する。
            // これによりEventSystem経由で別プレイヤーのボタンへ移動しない。
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            navigation.selectOnUp = null;
            navigation.selectOnDown = null;
            button.navigation = navigation;
        }
    }

    private int GetRespawnHandSortIndex(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName)) return int.MaxValue;
        if (buttonName.EndsWith("gu-")) return 0;
        if (buttonName.EndsWith("tyoki")) return 1;
        if (buttonName.EndsWith("pa-")) return 2;
        return int.MaxValue;
    }

    private void EnsureRespawnButtonNavigation()
    {
        ConfigureRespawnButtons();
    }

    private void UpdateRespawnButtonHighlights()
    {
        if (respawnUIPanel == null) return;

        Button[] buttons = respawnUIPanel.GetComponentsInChildren<Button>(true);
        string selectedButtonName = GetHandButtonName(currentHand);

        foreach (Button button in buttons)
        {
            bool selected = button.gameObject.name == selectedButtonName;
            button.transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    private void FocusRespawnButton(HandType hand)
    {
        if (EventSystem.current == null || respawnUIPanel == null) return;

        Button[] buttons = respawnUIPanel.GetComponentsInChildren<Button>(true);
        string selectedButtonName = GetHandButtonName(hand);

        foreach (Button button in buttons)
        {
            if (button == null) continue;
            if (button.gameObject.name == selectedButtonName)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                return;
            }
        }
    }

    private void ApplyJapaneseFont(TMP_Text text)
    {
        if (text == null) return;

        TMP_FontAsset japaneseFont = JapaneseFontUtility.GetJapaneseFontAsset();
        if (japaneseFont != null)
        {
            text.font = japaneseFont;
        }
    }

    private string GetHandButtonName(HandType hand)
    {
        string prefix = "P" + playerNumber + "_";

        if (hand == HandType.Rock) return prefix + "gu-";
        if (hand == HandType.Scissors) return prefix + "tyoki";
        return prefix + "pa-";
    }

    public void AddPoint(int value)
    {
        // ポイントは加算のみ。減点は行わない。
        if (value <= 0) return;

        point += value;

        UpdatePointUI();
    }

    private void UpdatePointUI()
    {
        if (pointTextUI != null)
        {
            pointTextUI.text = $"<size=130%>{point}</size><voffset=-0.22em><size=70%> Pt</size></voffset>";
        }
    }

    public void Respawn()
    {
        Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : initialPosition;
        Quaternion targetRotation = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

        // 手変更アイテムの選択中にリスポーンした場合、古い選択状態を解除する。
        // 解除しないと、リスポーン後もPlayerItemが選択入力を処理し続けてしまう。
        PlayerItem item = GetComponent<PlayerItem>();
        if (item != null)
        {
            item.CancelHandChangeSelection();
        }

        if (playerController != null)
        {
            playerController.SetInputActive(false);
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;

        isSelectingHand = true;
        isInitialHandSelection = false;
        respawnSelectionTimer = 10f;

        if (respawnUIPanel != null)
        {
            respawnUIPanel.SetActive(true);
        }
        ConfigureRespawnButtons();
        FocusRespawnButton(currentHand);
        UpdateRespawnUI();

        Debug.Log($"{gameObject.name} がリスポーンしました。手選択UIを起動します。");
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryStartBattle(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartBattle(other.gameObject);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryStartBattle(hit.gameObject);
    }

    private void TryStartBattle(GameObject targetObj)
    {
        if (!isGameActive || isSelectingHand) return;

        PlayerJanken opponent = targetObj.GetComponentInParent<PlayerJanken>();

        if (opponent == null || opponent == this || opponent.isSelectingHand) return;

        if (opponent.isGameActive && this.battleCooldown <= 0 && opponent.battleCooldown <= 0)
        {
            if (this.gameObject.GetInstanceID() > opponent.gameObject.GetInstanceID())
            {
                JudgeBattle(this, opponent);
            }
        }
    }

    private void JudgeBattle(PlayerJanken playerA, PlayerJanken playerB)
    {
        playerA.battleCooldown = 1.0f;
        playerB.battleCooldown = 1.0f;
        playerA.PlayClashSound();

        HandType a = playerA.currentHand;
        HandType b = playerB.currentHand;

        PlayerItem itemA = playerA.GetComponent<PlayerItem>();
        PlayerItem itemB = playerB.GetComponent<PlayerItem>();

        if (a == b)
        {
            Debug.Log("結果：あいこ！");

            playerA.Respawn();
            playerB.Respawn();
        }
        else if ((a == HandType.Rock && b == HandType.Scissors) ||
                 (a == HandType.Scissors && b == HandType.Paper) ||
                 (a == HandType.Paper && b == HandType.Rock))
        {
            Debug.Log($"結果：{playerA.gameObject.name} の勝ち！");

            int pointsToAdd = (itemA != null && itemA.isPointUpActive) ? 2 : 1;
            playerA.AddPoint(pointsToAdd);
            playerB.AddPoint(-1);

            if (itemA != null && itemA.isPointUpActive) itemA.ConsumePointUp();

            playerB.Respawn();
        }
        else
        {
            Debug.Log($"結果：{playerB.gameObject.name} の勝ち！");

            int pointsToAdd = (itemB != null && itemB.isPointUpActive) ? 2 : 1;
            playerB.AddPoint(pointsToAdd);
            playerA.AddPoint(-1);

            if (itemB != null && itemB.isPointUpActive) itemB.ConsumePointUp();

            playerA.Respawn();
        }
    }

    void LateUpdate()
    {
        CheckForNearbyOpponent();

        if (handIconRenderer != null && playerController != null && playerController.cameraTransform != null)
        {
            // 各プレイヤーのカメラの左右方向だけを反映し、上下には回転させない。
            float cameraYaw = playerController.cameraTransform.eulerAngles.y;
            handIconRenderer.transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
        }
    }

    private void PlayClashSound()
    {
        if (clashAudioSource != null && clashAudioClip != null)
        {
            clashAudioSource.PlayOneShot(clashAudioClip);
        }
    }

    private void PlayHandSelectSound()
    {
        if (handSelectAudioClip == null) return;

        float masterVolumeDb = PlayerPrefs.GetFloat("B11_MasterVolume", 0f);
        float volume = masterVolumeDb <= -40f
            ? 0f
            : Mathf.Pow(10f, masterVolumeDb / 20f) * 1.4f;
        AudioSource.PlayClipAtPoint(handSelectAudioClip, transform.position, volume);
    }

    private void CheckForNearbyOpponent()
    {
        if (!isGameActive || isSelectingHand || battleCooldown > 0f) return;

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController == null || !characterController.enabled) return;

        Vector3 center = transform.TransformPoint(characterController.center);
        float radius = Mathf.Max(battleDetectionRadius, characterController.radius);
        float height = Mathf.Max(battleDetectionHeight, radius * 2f);
        float halfSegment = (height * 0.5f) - radius;
        Vector3 bottom = center + Vector3.down * halfSegment;
        Vector3 top = center + Vector3.up * halfSegment;
        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            TryStartBattle(hit.gameObject);
            if (battleCooldown > 0f) break;
        }
    }
}
