using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public sealed class B11ResultSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text resultTitleText; 
    [SerializeField] private TMP_Text p1ScoreText;     
    [SerializeField] private TMP_Text p2ScoreText;     

    [SerializeField] private Image resultImage;

    [SerializeField] private Sprite p1WinSprite;   
    [SerializeField] private Sprite p2WinSprite;   
    [SerializeField] private Sprite drawSprite;    

    [SerializeField] private Button firstSelectedButton;

    private static int p1Score;
    private static int p2Score;

    public static void SetResult(int player1Score, int player2Score)
    {
        p1Score = player1Score;
        p2Score = player2Score;
    }

    private void Start()
    {
        // ★プログラムでのフォント書き換え処理をすべて削除しました
        if (resultTitleText != null)
        {
            resultTitleText.text = ""; 
        }

        if (p1ScoreText != null)
        {
            p1ScoreText.text = $"{p1Score}";
        }

        if (p2ScoreText != null)
        {
            p2ScoreText.text = $"{p2Score}";
        }

        if (resultImage == null)
        {
            Debug.LogError("ResultImageがインスペクターで設定されていません。");
            return;
        }

        if (p1Score > p2Score)
        {
            if (p1WinSprite != null) resultImage.sprite = p1WinSprite;
        }
        else if (p2Score > p1Score)
        {
            if (p2WinSprite != null) resultImage.sprite = p2WinSprite;
        }
        else
        {
            if (drawSprite != null) resultImage.sprite = drawSprite;
        }

        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            firstSelectedButton.Select();
        }
    }

    public void OnTitleButtonClick()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
