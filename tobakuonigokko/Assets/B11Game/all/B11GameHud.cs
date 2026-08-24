using TMPro;
using UnityEngine;

public sealed class B11GameHud : MonoBehaviour
{
    [SerializeField] private TMP_Text[] japaneseTexts;

    private void Awake()
    {
        TMP_FontAsset font = JapaneseFontUtility.GetJapaneseFontAsset();
        if (font == null || japaneseTexts == null) return;

        foreach (TMP_Text text in japaneseTexts)
        {
            if (text != null) text.font = font;
        }
    }
}
