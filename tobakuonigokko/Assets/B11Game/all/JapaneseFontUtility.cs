using TMPro;
using UnityEngine;

public static class JapaneseFontUtility
{
    private const string SourceFontResourcePath = "Fonts/Meiryo";
    private static TMP_FontAsset cachedFont;

    public static TMP_FontAsset GetJapaneseFontAsset()
    {
        if (cachedFont != null) return cachedFont;

        Font sourceFont = Resources.Load<Font>(SourceFontResourcePath);
        if (sourceFont == null) return null;

        cachedFont = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (cachedFont == null) return null;

        // 実行時生成フォントは初期状態でアウトライン幅が0のため、
        // シーン側のマテリアル設定が反映されない日本語テキストにも
        // 白色の縁取りを確実に適用する。
        Material fontMaterial = cachedFont.material;
        if (fontMaterial != null)
        {
            if (fontMaterial.HasProperty("_OutlineWidth"))
            {
                fontMaterial.SetFloat("_OutlineWidth", 0.1f);
            }

            if (fontMaterial.HasProperty("_OutlineColor"))
            {
                fontMaterial.SetColor("_OutlineColor", Color.white);
            }
        }

        cachedFont.hideFlags = HideFlags.DontSave;
        return cachedFont;
    }
}
