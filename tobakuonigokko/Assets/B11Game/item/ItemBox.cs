using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public enum ContentsMode
    {
        RandomStandardItem,
        SpecificItem
    }

    [Header("出現アイテム設定")]
    public ContentsMode contentsMode = ContentsMode.RandomStandardItem;
    public PlayerItem.ItemType specificItem = PlayerItem.ItemType.ChangeHand;

    [Header("再出現時間（秒）")]
    [Min(0f)] public float respawnTimeMin = 5f;
    [Min(0f)] public float respawnTimeMax = 5f;

    [Header("初回スポーン")]
    public bool spawnImmediately = true;

    private bool canCollect;
    private AudioClip itemGetAudioClip;

    private void Start()
    {
        itemGetAudioClip = Resources.Load<AudioClip>("Audio/item_get");
        canCollect = false;
        SetBoxVisible(false);
    }

    public void BeginInitialSpawn()
    {
        CancelInvoke();

        if (spawnImmediately)
        {
            canCollect = true;
            SetBoxVisible(true);
        }
        else
        {
            Invoke(nameof(EnableInitialSpawn), GetRandomRespawnDelay());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canCollect) return;

        // プレイヤーかどうか、かつアイテム枠が空いているかを確認
        PlayerItem playerItem = other.GetComponentInParent<PlayerItem>();

        if (playerItem == null) return;

        if (playerItem.hasItem || playerItem.currentItem != PlayerItem.ItemType.None)
        {
            // アイテム所持中はアイテムを変更せず、ボックスだけ消費する。
            ConsumeBox();
            return;
        }

        PlayerItem.ItemType itemToGive;
        if (contentsMode == ContentsMode.SpecificItem)
        {
            itemToGive = specificItem;
        }
        else
        {
            // ChangeHandは専用ボックスからのみ出現させる。
            // 1〜5: DoublePoint / AddTime / SubTime / Slow / Haste
            itemToGive = (PlayerItem.ItemType)Random.Range(1, 6);
        }

        if (itemToGive == PlayerItem.ItemType.None || !playerItem.GetItem(itemToGive)) return;

        if (itemGetAudioClip != null)
        {
            float masterVolumeDb = PlayerPrefs.GetFloat("B11_MasterVolume", 0f);
            float volume = masterVolumeDb <= -40f
                ? 0f
                : Mathf.Pow(10f, masterVolumeDb / 20f);
            AudioSource.PlayClipAtPoint(itemGetAudioClip, transform.position, volume);
        }

        ConsumeBox();
    }

    private void ConsumeBox()
    {
        canCollect = false;
        gameObject.SetActive(false);
        CancelInvoke(nameof(Respawn));

        Invoke(nameof(Respawn), GetRandomRespawnDelay());
    }

    private void Respawn()
    {
        canCollect = true;
        gameObject.SetActive(true);
    }

    private void EnableInitialSpawn()
    {
        canCollect = true;
        SetBoxVisible(true);
    }

    private float GetRandomRespawnDelay()
    {
        float minTime = Mathf.Max(0f, Mathf.Min(respawnTimeMin, respawnTimeMax));
        float maxTime = Mathf.Max(0f, Mathf.Max(respawnTimeMin, respawnTimeMax));
        return Random.Range(minTime, maxTime);
    }

    private void SetBoxVisible(bool visible)
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = visible;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }
    }
}
