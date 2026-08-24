using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject titleCanvas;
    public GameObject assignmentCanvas;
    public GameObject gameUICanvas;

    [Header("Title Menu Buttons")]
    public Button[] titleButtons;
    private GameObject lastSelectedTitleButton;

    [Header("Players")]
    public B11PlayerController player1;
    public B11PlayerController player2;

    [Header("UI (Start Countdown)")]
    public TMP_Text gameStartCountdownText;

    [Header("UI (Game Timer)")]
    public TMP_Text gameTimerText;
    public float gameDuration = 60f;

    [Header("Hand Selection UI")]
    public GameObject handSelectionPanel;
    public TMP_Text p1HandStatusText;
    public TMP_Text p2HandStatusText;

    private int p1SelectedHandIndex = 0;
    private int p2SelectedHandIndex = 0;
    private bool p1IsReady = false;
    private bool p2IsReady = false;

    private bool isTitleActive = false;
    private bool isStarting = false;
    private bool isGameRunning = false;
    private float currentTime;
    private float timerPauseRemaining = 0f;

    private string[] handNames = new string[] { "ROCK", "SCISSORS", "PAPER" };

    void Start()
    {
        isTitleActive = false;
        isStarting = false;
        isGameRunning = false;

        if (handSelectionPanel != null) handSelectionPanel.SetActive(false);
        if (gameStartCountdownText != null) gameStartCountdownText.gameObject.SetActive(false);
        ApplyJapaneseFont(gameStartCountdownText);
        ApplyJapaneseFont(p1HandStatusText);
        ApplyJapaneseFont(p2HandStatusText);

        if (titleCanvas != null)
        {
            titleCanvas.SetActive(false);
            Canvas c = titleCanvas.GetComponent<Canvas>();
            if (c != null) c.enabled = true;
        }

        if (assignmentCanvas != null) assignmentCanvas.SetActive(true);
        SetAssignmentIntroVisible(true);
        if (gameUICanvas != null) gameUICanvas.SetActive(false);

        if (player1 != null) player1.SetInputActive(false);
        if (player2 != null) player2.SetInputActive(false);

        if (gameStartCountdownText != null) gameStartCountdownText.gameObject.SetActive(false);
        if (gameTimerText != null) gameTimerText.gameObject.SetActive(false);
        if (handSelectionPanel != null) handSelectionPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowTitleScreen()
    {
        SetAssignmentIntroVisible(false);
        if (assignmentCanvas != null) assignmentCanvas.SetActive(false);
        if (titleCanvas != null) titleCanvas.SetActive(true);
        if (gameUICanvas != null) gameUICanvas.SetActive(false);
        isTitleActive = true;

        StartCoroutine(SetInitialSelectionNextFrame());
    }

    private IEnumerator SetInitialSelectionNextFrame()
    {
        yield return null;
        if (titleButtons != null && titleButtons.Length > 0 && titleButtons[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(titleButtons[0].gameObject);
            lastSelectedTitleButton = titleButtons[0].gameObject;
        }
    }

    void Update()
    {
        if (isTitleActive && !isStarting)
        {
            EnsureTitleFocus();
            UpdateTitleButtonScale();
        }

        if (isGameRunning)
        {
            if (timerPauseRemaining > 0f)
            {
                timerPauseRemaining = Mathf.Max(0f, timerPauseRemaining - Time.deltaTime);
                UpdateTimerUI();
                return;
            }

            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                isGameRunning = false;
                OnTimeUp();
            }
            UpdateTimerUI();
        }
    }

    private void EnsureTitleFocus()
    {
        if (EventSystem.current == null) return;
        GameObject currentObj = EventSystem.current.currentSelectedGameObject;
        if (currentObj != null)
        {
            lastSelectedTitleButton = currentObj;
        }
        else if (lastSelectedTitleButton != null && lastSelectedTitleButton.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedTitleButton);
        }
    }

    private void UpdateTitleButtonScale()
    {
        if (titleButtons == null) return;
        GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        foreach (var btn in titleButtons)
        {
            if (btn == null) continue;
            btn.transform.localScale = btn.gameObject == currentSelected ? Vector3.one * 1.15f : Vector3.one * 1.0f;
        }
    }

    public void OnClickGameStart()
    {
        if (isStarting) return;
        isStarting = true;
        isTitleActive = false;
        StartCoroutine(GameStartSequence());
    }

    IEnumerator GameStartSequence()
    {
        yield return new WaitForSeconds(0.2f);

        ResetTitleButtonsScale();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        ResetTitleButtonsScale();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (titleCanvas != null)
        {
            Canvas canvas = titleCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (handSelectionPanel != null) handSelectionPanel.SetActive(true);
        ConfigureHandSelectionButtons();
        if (gameStartCountdownText != null) gameStartCountdownText.gameObject.SetActive(true);

        p1SelectedHandIndex = 0;
        p2SelectedHandIndex = 0;
        p1IsReady = false;
        p2IsReady = false;
        UpdateHandSelectionUI();

        while (!p1IsReady || !p2IsReady)
        {
            if (gameStartCountdownText != null) gameStartCountdownText.text = "手を選択して決定してください";
            HandleGamepadInputForHandSelection();
            yield return null;
        }

        float startDelay = 3f;
        while (startDelay > 0f)
        {
            if (gameStartCountdownText != null)
            {
                gameStartCountdownText.text = Mathf.CeilToInt(startDelay).ToString();
            }
            startDelay -= Time.unscaledDeltaTime;
            yield return null;
        }

        ApplySelectedHandsToPlayers();
        if (handSelectionPanel != null) handSelectionPanel.SetActive(false);

        if (gameStartCountdownText != null)
        {
            gameStartCountdownText.text = "START!";
            yield return new WaitForSecondsRealtime(0.8f);
            gameStartCountdownText.gameObject.SetActive(false);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (gameUICanvas != null) gameUICanvas.SetActive(true);

        if (player1 != null)
        {
            player1.SetInputActive(true);
            PlayerJanken pj1 = player1.GetComponent<PlayerJanken>();
            if (pj1 != null) pj1.isGameActive = true;
        }
        if (player2 != null)
        {
            player2.SetInputActive(true);
            PlayerJanken pj2 = player2.GetComponent<PlayerJanken>();
            if (pj2 != null) pj2.isGameActive = true;
        }

        currentTime = gameDuration;
        timerPauseRemaining = 0f;
        isGameRunning = true;
        if (gameTimerText != null) gameTimerText.gameObject.SetActive(true);
    }

    private void ResetTitleButtonsScale()
    {
        if (titleButtons == null) return;
        foreach (var btn in titleButtons) if (btn != null) btn.transform.localScale = Vector3.one * 1.0f;
    }

    private void ConfigureHandSelectionButtons()
    {
        if (handSelectionPanel == null) return;

        Button[] buttons = handSelectionPanel.GetComponentsInChildren<Button>(true);
        ConfigureHandSelectionNavigation(buttons);
    }

    private void UpdateHandSelectionHighlights()
    {
        if (handSelectionPanel == null) return;

        Button[] buttons = handSelectionPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            bool selected = button.gameObject.name == GetHandButtonName(1, p1SelectedHandIndex) ||
                             button.gameObject.name == GetHandButtonName(2, p2SelectedHandIndex);
            button.transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    private void SetAssignmentIntroVisible(bool visible)
    {
        SetChildActive(assignmentCanvas, "AssignmentBackground", visible);
        SetChildActive(assignmentCanvas, "Assignment_setumei", visible);

        if (!visible) return;

        Transform intro = assignmentCanvas != null ? assignmentCanvas.transform.Find("Assignment_setumei") : null;
        if (intro == null) return;

        TMP_Text introText = intro.GetComponent<TMP_Text>();
        RectTransform introRect = intro.GetComponent<RectTransform>();

        if (introText != null)
        {
            ApplyJapaneseFont(introText);
            introText.enableWordWrapping = false;
            introText.overflowMode = TextOverflowModes.Overflow;
            introText.alignment = TextAlignmentOptions.Center;
        }

        if (introRect != null)
        {
            Vector2 size = introRect.sizeDelta;
            introRect.sizeDelta = new Vector2(1200f, size.y);
        }
    }

    public void HideAssignmentIntro()
    {
        SetAssignmentIntroVisible(false);
    }

    private void SetChildActive(GameObject parent, string childName, bool active)
    {
        if (parent == null) return;

        Transform child = parent.transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private string GetHandButtonName(int playerNumber, int handIndex)
    {
        string prefix = "P" + playerNumber + "_";

        if (handIndex == 0) return prefix + "gu-";
        if (handIndex == 1) return prefix + "tyoki";
        return prefix + "pa-";
    }

    private void HandleGamepadInputForHandSelection()
    {
        HandlePlayerHandInput(player1, ref p1SelectedHandIndex, ref p1IsReady);
        HandlePlayerHandInput(player2, ref p2SelectedHandIndex, ref p2IsReady);
    }

    private void HandlePlayerHandInput(B11PlayerController player, ref int selectedIndex, ref bool isReady)
    {
        if (player == null || isReady) return;

        InputDevice device = player.PrimaryDevice;
        bool moveNext = false;
        bool movePrev = false;
        bool confirm = false;

        if (device is Gamepad gamepad)
        {
            // 十字キーまたは左スティックで操作できるように強化
            if (gamepad.dpad.right.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) moveNext = true;
            if (gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) movePrev = true;

            // XboxコントローラーのAボタン（South）のみで決定
            if (gamepad.buttonSouth.wasPressedThisFrame) confirm = true;
        }
        else
        {
            if (Keyboard.current != null)
            {
                // ★P1とP2でキー割り当てを明確に分離
                if (player == player1)
                {
                    if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) moveNext = true;
                    if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) movePrev = true;
                    if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) confirm = true;
                }
                else if (player == player2)
                {
                    if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) moveNext = true;
                    if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) movePrev = true;
                    if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) confirm = true;
                }
            }
        }

        if (moveNext)
        {
            selectedIndex = (selectedIndex + 1) % 3;
            UpdateHandSelectionUI();
            FocusHandSelectionButton(player, selectedIndex);
        }
        else if (movePrev)
        {
            selectedIndex = (selectedIndex + 2) % 3;
            UpdateHandSelectionUI();
            FocusHandSelectionButton(player, selectedIndex);
        }

        if (confirm)
        {
            isReady = true;
            UpdateHandSelectionUI();
            FocusHandSelectionButton(player, selectedIndex);
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

    public void OnSelectP1HandByUI(int handIndex)
    {
        p1SelectedHandIndex = handIndex;
        p1IsReady = true;
        UpdateHandSelectionUI();
    }

    public void OnSelectP2HandByUI(int handIndex)
    {
        p2SelectedHandIndex = handIndex;
        p2IsReady = true;
        UpdateHandSelectionUI();
    }

    private void UpdateHandSelectionUI()
    {
        EnsureHandSelectionButtonNavigation();

        // --- P1のUI更新 ---
        if (p1HandStatusText != null)
        {
            if (p1IsReady)
            {
                // 決定後は緑色にして READY を強調
                p1HandStatusText.text = $"P1: {handNames[p1SelectedHandIndex]} <color=#00FF00>[READY!]</color>";
                p1HandStatusText.color = Color.white;
            }
            else
            {
                // 選択中は黄色にして強調
                p1HandStatusText.text = $"P1: <color=yellow>{handNames[p1SelectedHandIndex]} [選択中]</color>";
                p1HandStatusText.color = Color.white;
            }
        }

        // --- P2のUI更新 ---
        if (p2HandStatusText != null)
        {
            if (p2IsReady)
            {
                // 決定後は緑色にして READY を強調
                p2HandStatusText.text = $"P2: {handNames[p2SelectedHandIndex]} <color=#00FF00>[READY!]</color>";
                p2HandStatusText.color = Color.white;
            }
            else
            {
                // 選択中は黄色にして強調
                p2HandStatusText.text = $"P2: <color=yellow>{handNames[p2SelectedHandIndex]} [選択中]</color>";
                p2HandStatusText.color = Color.white;
            }
        }

        UpdateHandSelectionHighlights();
    }

    private void EnsureHandSelectionButtonNavigation()
    {
        if (handSelectionPanel == null) return;

        Button[] buttons = handSelectionPanel.GetComponentsInChildren<Button>(true);
        ConfigureHandSelectionNavigation(buttons);
    }

    private void ConfigureHandSelectionNavigation(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0) return;

        Button[] p1Buttons = System.Array.FindAll(buttons, button => button != null && button.gameObject.name.StartsWith("P1_"));
        Button[] p2Buttons = System.Array.FindAll(buttons, button => button != null && button.gameObject.name.StartsWith("P2_"));

        ConfigurePlayerHandNavigation(p1Buttons);
        ConfigurePlayerHandNavigation(p2Buttons);
    }

    private void ConfigurePlayerHandNavigation(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0) return;

        System.Array.Sort(buttons, (a, b) => GetHandSortIndex(a.name).CompareTo(GetHandSortIndex(b.name)));

        for (int i = 0; i < buttons.Length; i++)
        {
            Button current = buttons[i];
            if (current == null) continue;

            Navigation navigation = current.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = current;
            navigation.selectOnDown = current;
            navigation.selectOnLeft = i > 0 ? buttons[i - 1] : current;
            navigation.selectOnRight = i < buttons.Length - 1 ? buttons[i + 1] : current;
            current.navigation = navigation;
        }
    }

    private int GetHandSortIndex(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName)) return int.MaxValue;
        if (buttonName.EndsWith("gu-")) return 0;
        if (buttonName.EndsWith("tyoki")) return 1;
        if (buttonName.EndsWith("pa-")) return 2;
        return int.MaxValue;
    }

    private void FocusHandSelectionButton(B11PlayerController player, int handIndex)
    {
        if (EventSystem.current == null || handSelectionPanel == null || player == null) return;

        int playerNumber = 1;
        PlayerJanken janken = player.GetComponent<PlayerJanken>();
        if (janken != null) playerNumber = janken.playerNumber;

        string targetName = GetHandButtonName(playerNumber, handIndex);
        Button[] buttons = handSelectionPanel.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null) continue;
            if (button.gameObject.name == targetName)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                return;
            }
        }
    }

    private void ApplySelectedHandsToPlayers()
    {
        if (player1 != null) { PlayerJanken pj1 = player1.GetComponent<PlayerJanken>(); if (pj1 != null) pj1.SetHand((PlayerJanken.HandType)p1SelectedHandIndex); }
        if (player2 != null) { PlayerJanken pj2 = player2.GetComponent<PlayerJanken>(); if (pj2 != null) pj2.SetHand((PlayerJanken.HandType)p2SelectedHandIndex); }
    }

    private void UpdateTimerUI()
    {
        if (gameTimerText != null) gameTimerText.text = Mathf.CeilToInt(currentTime).ToString();
    }

    private void OnTimeUp()
    {
        if (player1 != null) { player1.SetInputActive(false); PlayerJanken pj1 = player1.GetComponent<PlayerJanken>(); if (pj1 != null) pj1.isGameActive = false; }
        if (player2 != null) { player2.SetInputActive(false); PlayerJanken pj2 = player2.GetComponent<PlayerJanken>(); if (pj2 != null) pj2.isGameActive = false; }
        if (gameTimerText != null) gameTimerText.text = "TIME UP!";
    }

    public void OnClickSettings() { }
    public void OnClickHowToPlay() { }

    public void ModifyTime(float amount)
    {
        if (!isGameRunning) return;
        currentTime += amount;
        if (currentTime <= 0 || (amount < 0 && currentTime <= 10f))
        {
            currentTime = 0;
            isGameRunning = false;
            OnTimeUp();
        }
        UpdateTimerUI();
    }

    public void PauseTimer(float duration)
    {
        if (!isGameRunning || duration <= 0f) return;
        timerPauseRemaining = Mathf.Max(timerPauseRemaining, duration);
        UpdateTimerUI();
    }
}
