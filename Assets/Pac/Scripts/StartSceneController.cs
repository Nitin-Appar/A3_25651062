using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public sealed class StartSceneController : MonoBehaviour
{
    public string level1Scene="Level01";
    public TMP_Text level1Record, level2Record;
    public Image[] borderEdges;
    void Start()
    {
        ShowRecord(level1Record,1);ShowRecord(level2Record,2);
    }
    void ShowRecord(TMP_Text text,int level)
    {
        string key="PacStudent.A4.Level"+level;
        text.text="Best "+PlayerPrefs.GetInt(key+".Score",0).ToString("D6")+"  /  "+GameHUD.TimeText(PlayerPrefs.GetFloat(key+".Time",0));
    }
    void Update()
    {
        for(int i=0;i<borderEdges.Length;i++)
        {
            float pulse=(Mathf.Sin(Time.unscaledTime*4-i*1.4f)+1)*.5f;
            borderEdges[i].color=Color.Lerp(new Color(.12f,.2f,.18f),GameHUD.Lime,pulse);
        }
    }
    public void LoadLevel1() {SceneManager.LoadScene(level1Scene);}
    public void LoadLevel2()
    {
        if(Application.CanStreamedLevelBeLoaded("InnovationScene"))SceneManager.LoadScene("InnovationScene");
    }
}
