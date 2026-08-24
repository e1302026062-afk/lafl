using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class B11InitialHandSelectionUI : MonoBehaviour
{
    private void Update()
    {
        KeepPanelVisible("P1");
        KeepPanelVisible("P2");
    }

    private void KeepPanelVisible(string playerName)
    {
        GameObject player = GameObject.Find(playerName);
        if (player == null) return;

        PlayerJanken janken = player.GetComponent<PlayerJanken>();
        if (janken == null || janken.isGameActive || !janken.isSelectingHand) return;
        if (janken.respawnUIPanel != null) janken.respawnUIPanel.SetActive(true);
    }
}
