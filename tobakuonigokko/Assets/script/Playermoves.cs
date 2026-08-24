using UnityEngine;
using UnityEngine.InputSystem;

public class Playermoves : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f; // 移動速度
    private Vector2 moveInput;

    // PlayerInput (Behavior: Send Messages) により、Moveアクション実行時に自動で呼ばれる
    private void OnMove(InputValue value)
    {
        // 入力値（WASDキーやスティック）を Vector2 として取得
        moveInput = value.Get<Vector2>();
    }

    // Jumpアクション実行時に自動で呼ばれる（必要に応じて使用）
    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log($"{gameObject.name} がジャンプ！");
            // ※ここにジャンプ処理（Rigidbody.AddForceなど）を追加できます
        }
    }

    private void Update()
    {
        // 入力値（X, Y）に基づいて、3次元空間（X, Z）の移動ベクトルを作成
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        
        // 世界空間（Space.World）を基準にオブジェクトを移動
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }
}
