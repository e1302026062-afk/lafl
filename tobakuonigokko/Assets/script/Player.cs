using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("プレイヤーの名前")]
    public string playerName = "Player";

    [Header("選択中の手")]
    public Hand currentHand;

    [Header("手が変わる間隔（秒）")]
    public float changeInterval = 2.0f;

    private float timer = 0.0f;

    void Start()
    {
        // 最初の手をランダムに決定
        ChangeHandRandomly();
    }

    void Update()
    {
        // 時間の経過をカウント
        timer += Time.deltaTime;

        // 設定した間隔を超えたら手を変更
        if (timer >= changeInterval)
        {
            ChangeHandRandomly();
            timer = 0.0f; // タイマーをリセット
        }
    }

    // 手をランダムに変える内部処理
    private void ChangeHandRandomly()
    {
        // Enum（Hand）の総数を取得し、その中からランダムにインデックスを選ぶ
        int numHands = System.Enum.GetValues(typeof(Hand)).Length;
        Hand nextHand = (Hand)Random.Range(0, numHands);

        SetHand(nextHand);
    }

    // 外部やUIからも設定できるように残しておく
    public void SetHand(Hand newHand)
    {
        currentHand = newHand;
        Debug.Log($"{playerName} の手が {currentHand} に変わりました。");
    }
}
