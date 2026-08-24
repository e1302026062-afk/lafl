using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class B11DeviceAssignmentSceneController : MonoBehaviour, IObserver<InputControl>
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image player1DeviceImage;
    [SerializeField] private Image player2DeviceImage;
    [SerializeField] private Sprite keyboardMouseSprite;
    [SerializeField] private Sprite controllerSprite;
    [SerializeField] private string nextScene = "TitleScene";
    [SerializeField] private float completeDisplaySeconds = 2f;

    private InputDevice player1Device;
    private InputDevice player2Device;
    private bool keyboardAssigned;
    private IDisposable listener;
    private bool isComplete;

    private void OnEnable()
    {
        listener = InputSystem.onAnyButtonPress.Subscribe(this);
    }

    private void OnDisable()
    {
        listener?.Dispose();
    }

    private void Start()
    {
        ApplyFont();
        SetStatus("ボタンを押してコントローラーを接続");
    }

    public void OnNext(InputControl control)
    {
        if (isComplete || !(control is ButtonControl)) return;

        InputDevice device = control.device;
        bool isKeyboardMouse = device is Keyboard || device is Mouse;
        if (isKeyboardMouse)
        {
            if (keyboardAssigned || Keyboard.current == null) return;
            keyboardAssigned = true;
            device = Keyboard.current;
        }

        if (device == player1Device || device == player2Device) return;

        if (player1Device == null)
        {
            player1Device = device;
            SetDeviceImage(player1DeviceImage, device);
            return;
        }

        player2Device = device;
        SetDeviceImage(player2DeviceImage, device);
        CompleteAssignment();
    }

    public void OnError(Exception error) { }
    public void OnCompleted() { }

    private void CompleteAssignment()
    {
        isComplete = true;
        listener?.Dispose();

        B11DeviceSession session = B11DeviceSession.Instance;
        if (session == null)
        {
            GameObject sessionObject = new GameObject("B11DeviceSession");
            session = sessionObject.AddComponent<B11DeviceSession>();
        }

        session.SetAssignment(player1Device, player2Device);
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSecondsRealtime(completeDisplaySeconds);
        SceneManager.LoadScene(nextScene);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void SetDeviceImage(Image target, InputDevice device)
    {
        if (target == null) return;
        target.sprite = device is Keyboard || device is Mouse ? keyboardMouseSprite : controllerSprite;
        target.enabled = target.sprite != null;
        target.preserveAspect = true;
    }

    private void ApplyFont()
    {
        if (statusText == null) return;
        TMP_FontAsset font = JapaneseFontUtility.GetJapaneseFontAsset();
        if (font != null) statusText.font = font;
    }
}
