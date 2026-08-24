using UnityEngine;
using UnityEngine.InputSystem;

public sealed class B11DeviceSession : MonoBehaviour
{
    public static B11DeviceSession Instance { get; private set; }

    public InputDevice Player1Device { get; private set; }
    public InputDevice Player2Device { get; private set; }

    public bool HasAssignment => Player1Device != null && Player2Device != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetAssignment(InputDevice player1Device, InputDevice player2Device)
    {
        Player1Device = player1Device;
        Player2Device = player2Device;
    }
}
