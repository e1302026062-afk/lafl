using UnityEngine;

public class zyankensystem : MonoBehaviour
{
    
    private bool hasSettled = false; // 連続衝突による多重判定を防ぐフラグ

    [Header("再判定までのディレイ時間（秒）")]
    public float resetDelay = 3.0f;

    private void OnTriggerEnter(Collider other)
    {
        // 既に判定済みなら処理しない
        if (hasSettled) return;

        // 衝突した相手と自分が両方Playerコンポーネントを持っているか確認
        Player p1 = GetComponent<Player>();
        Player p2 = other.GetComponent<Player>();

        if (p1 != null && p2 != null)
        {
            hasSettled = true;
            JudgeJanken(p1, p2);

            // 【追加】指定した秒数（resetDelay）を待ってからリセットする処理を起動
            StartCoroutine(ResetRefereeAfterDelay());
        }
    }

    private void JudgeJanken(Player p1, Player p2)
    {
        Debug.Log($"じゃんけん開始！ {p1.playerName}({p1.currentHand}) vs {p2.playerName}({p2.currentHand})");

        if (p1.currentHand == p2.currentHand)
        {
            Debug.Log("結果：あいこです！");
        }
        else if ((p1.currentHand == Hand.Rock && p2.currentHand == Hand.Scissors) ||
                 (p1.currentHand == Hand.Paper && p2.currentHand == Hand.Rock) ||
                 (p1.currentHand == Hand.Scissors && p2.currentHand == Hand.Paper))
        {
            Debug.Log($"結果：{p1.playerName} の勝ち！");
            ApplyResult(p1, p2);
        }
        else
        {
            Debug.Log($"結果：{p2.playerName} の勝ち！");
            ApplyResult(p2, p1);
        }
    }

    private void ApplyResult(Player winner, Player loser)
    {
        Debug.Log($"勝者: {winner.playerName} / 敗者: {loser.playerName}");
        // ここに敗者を吹き飛ばすなどの処理を書くことができます
    }

    // 【追加】一定時間待ってからフラグを戻すコルーチン
    private System.Collections.IEnumerator ResetRefereeAfterDelay()
    {
        // 設定された秒数（例: 3秒）だけ処理を一時停止する
        yield return new WaitForSeconds(resetDelay);

        // フラグを戻して、再度じゃんけんができる状態にする
        hasSettled = false;
        Debug.Log("審判：次のじゃんけんが可能です！");
    }
 }
