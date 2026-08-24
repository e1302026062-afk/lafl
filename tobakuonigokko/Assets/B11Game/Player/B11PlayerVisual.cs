using UnityEngine;

/// <summary>
/// P1/P2の見た目をPolyPeopleのPlayerPackへ置き換え、移動中に走りアニメーションを再生します。
/// </summary>
public sealed class B11PlayerVisual : MonoBehaviour
{
    [SerializeField] private GameObject playerPackPrefab;
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private float animationMoveThreshold = 0.0001f;

    private Animator animator;
    private B11PlayerController playerController;
    private float baseMoveSpeed;
    private Vector3 previousPosition;

    private void Awake()
    {
        MeshRenderer oldRenderer = GetComponent<MeshRenderer>();
        if (oldRenderer != null) oldRenderer.enabled = false;

        MeshFilter oldMeshFilter = GetComponent<MeshFilter>();
        if (oldMeshFilter != null) oldMeshFilter.mesh = null;

        GameObject playerPack = transform.Find($"PlayerPack{playerNumber}")?.gameObject;
        if (playerPack == null && playerPackPrefab != null)
        {
            playerPack = Instantiate(playerPackPrefab, transform);
            playerPack.name = $"PlayerPack{playerNumber}";
            playerPack.transform.localPosition = Vector3.zero;
            playerPack.transform.localRotation = Quaternion.identity;
            playerPack.transform.localScale = Vector3.one;
        }

        animator = playerPack != null ? playerPack.GetComponentInChildren<Animator>() : null;
        if (animator != null) animator.applyRootMotion = false;

        playerController = GetComponent<B11PlayerController>();
        baseMoveSpeed = playerController != null ? playerController.moveSpeed : 0f;

        previousPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null) return;

        Vector3 movement = transform.position - previousPosition;
        movement.y = 0f;
        float moveAmount = movement.sqrMagnitude > animationMoveThreshold ? 1f : 0f;

        // 現在の通常速度を100%とし、アイテム効果による速度変化を
        // アニメーション再生速度にも反映する（低速50%、高速150%）。
        float animationSpeed = 1f;
        if (playerController != null && baseMoveSpeed > 0f)
        {
            animationSpeed = (playerController.moveSpeed * playerController.speedMultiplier) / baseMoveSpeed;
        }

        float playbackSpeed = moveAmount > 0f ? animationSpeed : 0.5f;
        animator.speed = Mathf.Clamp(playbackSpeed, 0.1f, 3f);
        animator.SetFloat("Speed", moveAmount);
        previousPosition = transform.position;
    }
}
