using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System;
using System.Collections;
using TMPro;

public class SwitchStyleAssigner : MonoBehaviour, IObserver<InputControl>
{
    [Header("Players")]
    public B11PlayerController player1;
    public B11PlayerController player2;

    [Header("P1 UI")]
    public GameObject p1JoinPrompt;
    public GameObject p1ReadyUI;

    [Header("P2 UI")]
    public GameObject p2JoinPrompt;
    public GameObject p2ReadyUI;

    [Header("Countdown UI")]
    public GameObject assignCompleteText;
    public TMP_Text countdownText;            // カウントダウンの数字テキスト(※TextMeshProを使う場合は TMP_Text に変更してください)

    [Header("Next Screen")]
    public GameObject assignmentCanvas;
    public GameObject titleCanvas;

    private InputDevice p1Device;
    private InputDevice p2Device;
    private IDisposable eventListener;
    private bool isKBMAssigned = false;

    // ※ Start() は削除しました（Titleから切り替わった時にOnEnableが呼ばれるため）

    void OnEnable()
    {
        // 割り当て画面が表示された瞬間に、入力を監視し始める
        eventListener = InputSystem.onAnyButtonPress.Subscribe(this);
    }

    void OnDisable()
    {
        eventListener?.Dispose();
    }

    public void OnNext(InputControl control) => OnButtonPressed(control);
    public void OnError(Exception error) { }
    public void OnCompleted() { }

    void OnButtonPressed(InputControl control)
    {
        InputDevice device = control.device;
        if (!(control is ButtonControl)) return;
        if (device == p1Device || device == p2Device) return;

        bool isKBM = device is Keyboard || device is Mouse;
        if (isKBM && isKBMAssigned) return;

        // --- P1の割り当て ---
        if (p1Device == null)
        {
            p1Device = device;
            if (isKBM)
            {
                isKBMAssigned = true;
                player1.AssignDevices(Keyboard.current, Mouse.current);
            }
            else
            {
                player1.AssignDevices(device);
            }

            // ▼▼▼ この1行を追加（P1の画面表示を切り替える直前）▼▼▼
            player1.SetInputActive(false);
            // ▲▲▲ 追加はここまで ▲▲▲

            if (p1JoinPrompt != null) p1JoinPrompt.SetActive(false);
            if (p1ReadyUI != null) p1ReadyUI.SetActive(true);
        }
        // --- P2の割り当て ---
        else if (p2Device == null)
        {
            p2Device = device;
            if (isKBM)
            {
                isKBMAssigned = true;
                player2.AssignDevices(Keyboard.current, Mouse.current);
            }
            else
            {
                player2.AssignDevices(device);
            }

            // ▼▼▼ この1行を追加（P2の画面表示を切り替える直前）▼▼▼
            player2.SetInputActive(false);
            // ▲▲▲ 追加はここまで ▲▲▲

            if (p2JoinPrompt != null) p2JoinPrompt.SetActive(false);
            if (p2ReadyUI != null) p2ReadyUI.SetActive(true);

            StartCoroutine(ShowRecognitionCompleteThenTitle());
        }
    }

    private IEnumerator ShowRecognitionCompleteThenTitle()
    {
        eventListener?.Dispose();

        if (titleCanvas != null) titleCanvas.SetActive(false);

        ApplyJapaneseFont(assignCompleteText != null ? assignCompleteText.GetComponentInChildren<TMP_Text>(true) : null);
        ApplyJapaneseFont(countdownText);

        if (assignCompleteText != null) assignCompleteText.SetActive(true);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "認識完了";
        }

        yield return new WaitForSeconds(2f);

        if (assignCompleteText != null) assignCompleteText.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        TitleMenuManager titleManager = FindFirstObjectByType<TitleMenuManager>();
        if (titleManager != null)
        {
            titleManager.HideAssignmentIntro();
            titleManager.ShowTitleScreen();
        }
        else if (titleCanvas != null)
        {
            if (assignmentCanvas == null) assignmentCanvas = GameObject.Find("Canvas_Assignment");
            SetAssignmentIntroVisible(false);
            if (assignmentCanvas != null) assignmentCanvas.SetActive(false);
            titleCanvas.SetActive(true); // 見つからなかった場合の保険
        }

        gameObject.SetActive(false);
    }

    private void SetAssignmentIntroVisible(bool visible)
    {
        if (assignmentCanvas == null) return;

        Transform background = assignmentCanvas.transform.Find("AssignmentBackground");
        if (background != null) background.gameObject.SetActive(visible);

        Transform intro = assignmentCanvas.transform.Find("Assignment_setumei");
        if (intro != null) intro.gameObject.SetActive(visible);
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
}
