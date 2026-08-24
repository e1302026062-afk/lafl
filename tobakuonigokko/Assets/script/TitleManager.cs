using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [Header("--- Panels ---")]
    [SerializeField] private GameObject setteingPanel;
    [SerializeField] private GameObject ruleImage; 

    [Header("--- Audio / UI ---")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;

    // 💡【追加】ボタン選択音（SE）用の変数
    [Header("--- Sound Effects ---")]
    [SerializeField] private AudioSource seAudioSource; // 効果音を鳴らすスピーカー
    [SerializeField] private AudioClip selectSound;     // 選択したときのピピッという音音源

    [Header("--- Rule Pages ---")]
    [SerializeField] private Image rulePanelImage;      
    [SerializeField] private Sprite rulePage1Sprite;    
    [SerializeField] private Sprite rulePage2Sprite;    

    [Header("--- Rule Buttons ---")]
    [SerializeField] private GameObject ruleNextButton;
    [SerializeField] private GameObject ruleBackButton;
    [SerializeField] private Button ruleCloseButton; 

    [Header("--- Controller Focus ---")]
    [SerializeField] private GameObject firstSelectedButton;
    [SerializeField] private GameObject settingFirstSelected;
    [SerializeField] private GameObject ruleFirstSelected;

    // 💡最後に選ばれていたオブジェクトを記憶する変数（同じボタンで音が鳴り続けるのを防ぐ）
    private GameObject lastSelectedObject;

    void Start()
    {
        if (setteingPanel != null) setteingPanel.SetActive(false);
        if (ruleImage != null) ruleImage.SetActive(false); 

        StartCoroutine(SelectFirstButtonLater(firstSelectedButton));

        if (volumeSlider != null)
        {
            volumeSlider.minValue = -40f;
            volumeSlider.maxValue = 0f;
            volumeSlider.value = 0f;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    // 💡【追加】毎フレーム、現在選ばれているボタンを監視して音が鳴るようにする
    void Update()
    {
        // 現在フォーカスが当たっているゲームオブジェクトを取得
        GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        // 選択対象が変わり、かつ中身が空ではない（何かボタンが選ばれた）瞬間
        if (currentSelected != lastSelectedObject)
        {
            if (currentSelected != null && lastSelectedObject != null)
            {
                PlaySelectSound(); // 💡選択音を鳴らす
            }
            lastSelectedObject = currentSelected; // 記憶を更新
        }
    }

    // 💡【追加】音を鳴らす関数
    private void PlaySelectSound()
    {
        if (seAudioSource != null && selectSound != null)
        {
            // 音が重なっても綺麗に鳴る方法で再生
            seAudioSource.PlayOneShot(selectSound);
        }
    }

    private IEnumerator SelectFirstButtonLater(GameObject targetButton)
    {
        yield return null; 
        if (targetButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null); 
            EventSystem.current.SetSelectedGameObject(targetButton); 
        }
    }

    public void SetVolume(float volume)
    {
        if (volume <= -40f) audioMixer.SetFloat("MasterVolume", -80f);
        else audioMixer.SetFloat("MasterVolume", volume);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene"); 
    }

    public void OpenSetting()
    {
        if (setteingPanel != null) 
        {
            setteingPanel.SetActive(true); 
            if (settingFirstSelected != null) EventSystem.current.SetSelectedGameObject(settingFirstSelected);
        }
    }

    public void CloseSetting()
    {
        if (setteingPanel != null) 
        {
            setteingPanel.SetActive(false); 
            StartCoroutine(SelectFirstButtonLater(firstSelectedButton));
        }
    }

    public void OpenRule()
    {
        if (ruleImage != null)
        {
            ruleImage.SetActive(true); 
            if (rulePanelImage != null && rulePage1Sprite != null) rulePanelImage.sprite = rulePage1Sprite;
            if (ruleNextButton != null) ruleNextButton.SetActive(true);
            if (ruleBackButton != null) ruleBackButton.SetActive(false);

            UpdateCloseButtonNavigation(ruleNextButton);

            if (ruleFirstSelected != null) EventSystem.current.SetSelectedGameObject(ruleFirstSelected);
        }
    }

    public void CloseRule()
    {
        if (ruleImage != null)
        {
            ruleImage.SetActive(false); 
            StartCoroutine(SelectFirstButtonLater(firstSelectedButton));
        }
    }

    public void ChangeToNextRulePage()
    {
        if (rulePanelImage != null && rulePage2Sprite != null)
        {
            rulePanelImage.sprite = rulePage2Sprite;

            if (ruleNextButton != null) ruleNextButton.SetActive(false);
            if (ruleBackButton != null) ruleBackButton.SetActive(true);

            UpdateCloseButtonNavigation(ruleBackButton);

            StartCoroutine(SelectFirstButtonLater(ruleBackButton));
        }
    }

    public void ChangeToBackRulePage()
    {
        if (rulePanelImage != null && rulePage1Sprite != null)
        {
            rulePanelImage.sprite = rulePage1Sprite;

            if (ruleNextButton != null) ruleNextButton.SetActive(true);
            if (ruleBackButton != null) ruleBackButton.SetActive(false);

            UpdateCloseButtonNavigation(ruleNextButton);

            StartCoroutine(SelectFirstButtonLater(ruleNextButton));
        }
    }

    private void UpdateCloseButtonNavigation(GameObject targetGameObject)
    {
        if (ruleCloseButton == null || targetGameObject == null) return;

        Button targetButton = targetGameObject.GetComponent<Button>();
        if (targetButton == null) return;

        Navigation nav = ruleCloseButton.navigation;
        
        nav.selectOnUp = targetButton;
        nav.selectOnDown = targetButton;
        nav.selectOnLeft = targetButton;
        nav.selectOnRight = targetButton;

        ruleCloseButton.navigation = nav;
    }
}
