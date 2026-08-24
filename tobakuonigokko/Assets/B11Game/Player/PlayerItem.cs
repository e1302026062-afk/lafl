using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // ★画像を扱うために必須
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerItem : MonoBehaviour
{
    public enum ItemType
    {
        None,
        DoublePoint,
        AddTime,
        SubTime,
        Slow,
        Haste,
        ChangeHand
    }

    [Header("Current Item")]
    public ItemType currentItem = ItemType.None;
    public bool hasItem = false;

    [Header("Point Up State")]
    public bool isPointUpActive = false;

    [Header("UI設定 (画像・ゲージ)")]
    public Image itemIconUI;               // 取得したアイテムの画像を表示するUI
    public Image effectDurationRadialUI;   // 効果時間を360度ゲージで表示するUI

    [Header("アイテム画像設定")]
    public Sprite iconDoublePoint;
    public Sprite iconAddTime;
    public Sprite iconSubTime;
    public Sprite iconSlow;
    public Sprite iconHaste;
    public Sprite iconChangeHand;
    public Sprite iconEmpty;               // アイテムを持っていない時の画像（空枠など）

    [Header("手変更アイテム選択UI")]
    public GameObject handChangePanel;
    public Button[] handChangeButtons = new Button[3];
    public Sprite handRockSprite;
    public Sprite handScissorsSprite;
    public Sprite handPaperSprite;

    [Header("References")]
    private B11PlayerController playerController;
    private PlayerJanken playerJanken;
    private TitleMenuManager titleMenuManager;

    [Header("Item Settings")]
    public float slowDuration = 5f;
    public float hasteDuration = 5f;
    public float doublePointDuration = 10f;
    public float timerStopDuration = 10f;

    private float currentEffectTimer = 0f;
    private float maxEffectDuration = 0f;  // ★ゲージの計算用に最大時間を記録
    private bool isSelectingChangedHand;
    private int selectedChangedHand;
    private Coroutine speedEffectCoroutine;
    private Coroutine doublePointCoroutine;
    private float baseMoveSpeed;
    private AudioClip handSelectAudioClip;
    private AudioClip activeItemAudioClip;

    void Start()
    {
        playerController = GetComponent<B11PlayerController>();
        playerJanken = GetComponent<PlayerJanken>();
        titleMenuManager = FindFirstObjectByType<TitleMenuManager>();
        baseMoveSpeed = playerController != null ? playerController.moveSpeed : 0f;
        handSelectAudioClip = Resources.Load<AudioClip>("Audio/hand_select");
        activeItemAudioClip = Resources.Load<AudioClip>("Audio/active_item");

        UpdateItemUI();
        if (effectDurationRadialUI != null) effectDurationRadialUI.gameObject.SetActive(false);
        ConfigureHandChangeUI();
    }

    void Update()
    {
        if (isSelectingChangedHand)
        {
            HandleHandChangeInput();
            HandleEffectTimer();
            return;
        }

        HandleItemInput();
        HandleEffectTimer();
    }

    // ==========================================
    // 1. 入力処理 (Q/LT で破棄、E/RT で使用)
    // ==========================================
    private void HandleItemInput()
    {
        if (!hasItem || currentItem == ItemType.None) return;

        bool discardPressed = false;
        bool usePressed = false;

        InputDevice assignedDevice = (playerController != null) ? playerController.PrimaryDevice : null;

        if (assignedDevice is Gamepad gamepad)
        {
            if (gamepad.leftTrigger.wasPressedThisFrame) discardPressed = true;
            if (gamepad.rightTrigger.wasPressedThisFrame) usePressed = true;
        }
        else if (Keyboard.current != null)
        {
            int pNum = (playerJanken != null) ? playerJanken.playerNumber : 1;

            if (pNum == 1)
            {
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) discardPressed = true;
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) usePressed = true;
            }
            else if (pNum == 2)
            {
                if (Keyboard.current.uKey.wasPressedThisFrame) discardPressed = true;
                if (Keyboard.current.oKey.wasPressedThisFrame) usePressed = true;
            }
        }

        if (discardPressed)
        {
            DiscardItem();
        }
        else if (usePressed)
        {
            UseItem();
        }
    }

    // ==========================================
    // 2. アイテム取得 (ItemBox.csから呼ばれる)
    // ==========================================
    public bool GetItem(ItemType type)
    {
        if (playerJanken != null && !playerJanken.isGameActive) return false;
        if (hasItem) return false;

        currentItem = type;
        hasItem = true;
        Debug.Log($"{gameObject.name} がアイテム 【{type}】 を獲得！");

        UpdateItemUI();
        return true;
    }

    // ==========================================
    // 3. アイテム使用・破棄ロジック
    // ==========================================
    public void UseItem()
    {
        if (!hasItem || currentItem == ItemType.None) return;

        Debug.Log($"{gameObject.name} がアイテム 【{currentItem}】 を使用！");
        PlayActiveItemSound();

        switch (currentItem)
        {
            case ItemType.DoublePoint:
                if (doublePointCoroutine != null) StopCoroutine(doublePointCoroutine);
                doublePointCoroutine = StartCoroutine(DoublePointRoutine());
                StartEffectTimerUI(doublePointDuration);
                break;
            case ItemType.AddTime:
                if (B11GameTimer.Instance != null) B11GameTimer.Instance.PauseTimer(timerStopDuration);
                else if (titleMenuManager != null) titleMenuManager.PauseTimer(timerStopDuration);
                StartEffectTimerUI(timerStopDuration);
                break;
            case ItemType.SubTime:
                if (B11GameTimer.Instance != null) B11GameTimer.Instance.ModifyTime(-10f);
                else if (titleMenuManager != null) titleMenuManager.ModifyTime(-10f);
                break;
            case ItemType.Slow:
                ApplySlowToOpponent();
                break;
            case ItemType.Haste:
                StartSpeedEffect(false, hasteDuration);
                StartEffectTimerUI(hasteDuration);
                break;
            case ItemType.ChangeHand:
                BeginHandChangeSelection();
                break;
        }

        currentItem = ItemType.None;
        hasItem = false;
        UpdateItemUI();
    }

    private void BeginHandChangeSelection()
    {
        isSelectingChangedHand = true;
        selectedChangedHand = playerJanken != null ? (int)playerJanken.currentHand : 0;

        // 初回手選択後に残っているREADYを、手変更アイテムのUIでは表示しない。
        if (playerJanken != null) playerJanken.ClearReadyText();

        currentItem = ItemType.None;
        hasItem = false;
        UpdateItemUI();
        SetHandChangeUIVisible(true);
        UpdateHandChangeHighlights();

        FocusHandChangeButton();
    }

    private void HandleHandChangeInput()
    {
        bool next = false;
        bool previous = false;
        bool confirm = false;

        InputDevice assignedDevice = playerController != null ? playerController.PrimaryDevice : null;
        if (assignedDevice is Gamepad gamepad)
        {
            next = gamepad.dpad.right.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame;
            previous = gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.up.wasPressedThisFrame;
            confirm = gamepad.buttonSouth.wasPressedThisFrame;
        }
        else if (Keyboard.current != null)
        {
            next = Keyboard.current.eKey.wasPressedThisFrame;
            previous = Keyboard.current.qKey.wasPressedThisFrame;
            confirm = Keyboard.current.rKey.wasPressedThisFrame;
        }

        if (next)
        {
            selectedChangedHand = (selectedChangedHand + 1) % 3;
            UpdateHandChangeHighlights();
            FocusHandChangeButton();
        }
        else if (previous)
        {
            selectedChangedHand = (selectedChangedHand + 2) % 3;
            UpdateHandChangeHighlights();
            FocusHandChangeButton();
        }

        if (confirm) FinishHandChangeSelection();
    }

    public void OnSelectChangedHandByUI(int handIndex)
    {
        if (!isSelectingChangedHand || handIndex < 0 || handIndex > 2) return;
        selectedChangedHand = handIndex;
        UpdateHandChangeHighlights();
        FocusHandChangeButton();
    }

    private void FinishHandChangeSelection()
    {
        if (playerJanken != null)
        {
            playerJanken.SetHand((PlayerJanken.HandType)selectedChangedHand);
        }

        PlayHandSelectSound();

        isSelectingChangedHand = false;
        SetHandChangeUIVisible(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        Debug.Log($"{gameObject.name} の手変更が確定しました ({(PlayerJanken.HandType)selectedChangedHand})。");
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

    private void PlayActiveItemSound()
    {
        if (activeItemAudioClip == null) return;

        float masterVolumeDb = PlayerPrefs.GetFloat("B11_MasterVolume", 0f);
        float volume = masterVolumeDb <= -40f
            ? 0f
            : Mathf.Pow(10f, masterVolumeDb / 20f);
        AudioSource.PlayClipAtPoint(activeItemAudioClip, transform.position, volume);
    }

    /// <summary>
    /// リスポーンなどで手変更選択を中断し、通常のアイテム入力へ戻します。
    /// 手変更アイテムは使用開始時点で消費済みのため、アイテムを復元しません。
    /// </summary>
    public void CancelHandChangeSelection()
    {
        if (!isSelectingChangedHand) return;

        isSelectingChangedHand = false;
        selectedChangedHand = 0;
        SetHandChangeUIVisible(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void ConfigureHandChangeUI()
    {
        if (handChangePanel == null) return;

        Button[] buttons = handChangePanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < handChangeButtons.Length && i < buttons.Length; i++)
        {
            if (handChangeButtons[i] == null) handChangeButtons[i] = buttons[i];
            int index = i;
            handChangeButtons[i].onClick.RemoveAllListeners();
            handChangeButtons[i].onClick.AddListener(() => OnSelectChangedHandByUI(index));
        }
    }

    private void SetHandChangeUIVisible(bool visible)
    {
        if (handChangePanel != null) handChangePanel.SetActive(visible);
    }

    private void UpdateHandChangeHighlights()
    {
        for (int i = 0; i < handChangeButtons.Length; i++)
        {
            if (handChangeButtons[i] != null)
            {
                handChangeButtons[i].transform.localScale = i == selectedChangedHand ? Vector3.one * 1.15f : Vector3.one;
            }
        }
    }

    private void FocusHandChangeButton()
    {
        if (EventSystem.current != null && selectedChangedHand >= 0 && selectedChangedHand < handChangeButtons.Length && handChangeButtons[selectedChangedHand] != null)
        {
            EventSystem.current.SetSelectedGameObject(handChangeButtons[selectedChangedHand].gameObject);
        }
    }

    public void DiscardItem()
    {
        if (!hasItem) return;
        Debug.Log($"{gameObject.name} がアイテム 【{currentItem}】 を破棄！");
        currentItem = ItemType.None;
        hasItem = false;
        UpdateItemUI();
    }

    public bool ConsumePointUp()
    {
        if (isPointUpActive)
        {
            isPointUpActive = false;
            currentEffectTimer = 0f;
            if (effectDurationRadialUI != null) effectDurationRadialUI.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    // ==========================================
    // 4. 各種アイテム効果のコルーチン
    // ==========================================
    private void ApplySlowToOpponent()
    {
        B11PlayerController[] players = FindObjectsByType<B11PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p != playerController)
            {
                PlayerItem opponentItem = p.GetComponent<PlayerItem>();
                if (opponentItem != null)
                {
                    opponentItem.StartSpeedEffect(true, slowDuration);
                    opponentItem.StartEffectTimerUI(slowDuration);
                }
            }
        }
    }

    private void StartSpeedEffect(bool slowed, float duration)
    {
        if (playerController == null) return;

        if (speedEffectCoroutine != null) StopCoroutine(speedEffectCoroutine);

        playerController.moveSpeed = baseMoveSpeed * (slowed ? 0.5f : 1.5f);
        speedEffectCoroutine = StartCoroutine(SpeedEffectRoutine(duration));
    }

    private IEnumerator SpeedEffectRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (playerController != null) playerController.moveSpeed = baseMoveSpeed;
        speedEffectCoroutine = null;
    }

    private IEnumerator DoublePointRoutine()
    {
        isPointUpActive = true;
        yield return new WaitForSeconds(doublePointDuration);
        isPointUpActive = false;
        doublePointCoroutine = null;
    }

    // ==========================================
    // 5. 画像UI・360度ゲージの更新処理
    // ==========================================
    private void StartEffectTimerUI(float duration)
    {
        currentEffectTimer = duration;
        maxEffectDuration = duration; // ゲージ計算用に最大時間を保存

        if (effectDurationRadialUI != null)
        {
            effectDurationRadialUI.gameObject.SetActive(true);
            effectDurationRadialUI.fillAmount = 1f; // 最初は満タン(1.0)
        }
    }

    private void HandleEffectTimer()
    {
        if (currentEffectTimer > 0)
        {
            currentEffectTimer -= Time.deltaTime;

            if (effectDurationRadialUI != null && maxEffectDuration > 0)
            {
                // ★残り時間に応じてゲージを減らす (1.0 -> 0.0)
                effectDurationRadialUI.fillAmount = currentEffectTimer / maxEffectDuration;
            }

            if (currentEffectTimer <= 0)
            {
                currentEffectTimer = 0;
                if (effectDurationRadialUI != null) effectDurationRadialUI.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateItemUI()
    {
        if (itemIconUI == null) return;

        if (!hasItem || currentItem == ItemType.None)
        {
            // アイテムがない場合は空枠の画像にするか、非表示にする
            itemIconUI.sprite = iconEmpty;
            if (iconEmpty == null) itemIconUI.enabled = false;
        }
        else
        {
            itemIconUI.enabled = true;

            // 種類に応じて画像を差し替え
            switch (currentItem)
            {
                case ItemType.DoublePoint: itemIconUI.sprite = iconDoublePoint; break;
                case ItemType.AddTime: itemIconUI.sprite = iconAddTime; break;
                case ItemType.SubTime: itemIconUI.sprite = iconSubTime; break;
                case ItemType.Slow: itemIconUI.sprite = iconSlow; break;
                case ItemType.Haste: itemIconUI.sprite = iconHaste; break;
                case ItemType.ChangeHand: itemIconUI.sprite = iconChangeHand; break;
            }
        }
    }
}
