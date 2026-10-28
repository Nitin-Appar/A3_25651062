using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public sealed class GameHUD : MonoBehaviour
{
    public TMP_Text scoreText, timerText, scaredText, overlayText;
    public Image[] lifeIcons;
    public GameObject blockingImage;
    public Button exitButton;
    public TMP_Text energyText;
    public static readonly Color Lime=new Color(.73f,.96f,.30f);
    public static string TimeText(float seconds)
    {
        int n=Mathf.Max(0,Mathf.FloorToInt(seconds*100));
        return $"{n/6000:00}:{n/100%60:00}:{n%100:00}";
    }
    public void ExitLevel() {SceneManager.LoadScene("StartScene");}
    public void ShowOverlay(string text)
    {
        blockingImage.SetActive(true);overlayText.text=text;exitButton.interactable=false;
    }
    public void HideOverlay()
    {
        blockingImage.SetActive(false);exitButton.interactable=true;
    }
    public void Refresh(GameManager game)
    {
        scoreText.text=game.Score.ToString("D6");timerText.text=TimeText(game.Elapsed);
        for(int i=0;i<lifeIcons.Length;i++)lifeIcons[i].enabled=i<game.Lives;
        scaredText.text=game.ScaredRemaining>0?Mathf.CeilToInt(game.ScaredRemaining).ToString():"";
        if(energyText) energyText.text=$"Energy {Mathf.CeilToInt(game.Energy)} / 100 | SHIFT sprint | SPACE parry";
    }
}
