using UnityEngine;
using UnityEngine.InputSystem;

public class TwoPlayerSetup : MonoBehaviour
{
    [Header("プレイヤーの参照")]
    [SerializeField] private PlayerInput player1Input;
    [SerializeField] private PlayerInput player2Input;

    private void Start()
    {
        var gamepads = Gamepad.all;

        // 【本番用】コントローラーが2台以上ある場合
        if (gamepads.Count >= 2)
        {
            player1Input.SwitchCurrentControlScheme("Gamepad", gamepads[0]);
            player2Input.SwitchCurrentControlScheme("Gamepad", gamepads[1]);
            Debug.Log("P1: 1台目パッド / P2: 2台目パッド");
        }
        // 【テスト用】コントローラーがない、または1台の場合（キーボード1台で2人プレイ）
        else
        {
            // P1 に WASD (KeyboardP1) を割り当て
            player1Input.SwitchCurrentControlScheme("KeyboardP1", Keyboard.current);

            // P2 に 矢印キー (KeyboardP2) を割り当て
            player2Input.SwitchCurrentControlScheme("KeyboardP2", Keyboard.current);

            Debug.Log("【テストモード】P1: WASDキー / P2: 矢印キー で操作可能です");
        }
    }
}
