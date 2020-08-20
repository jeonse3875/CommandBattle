using UnityEngine;
using UnityEngine.UI;

public class ClassTap : MonoBehaviour
{
    public ClassType cType;
    public Image image_Background;
    public Image image_ClassIcon;
    public Text text_ClassName;
    private LobbyUI lobbyUI;

    private void Start()
    {
        lobbyUI = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
        image_ClassIcon.sprite = Resources.Load<Sprite>("ClassIcon/" + cType.ToString());
        text_ClassName.text = Command.GetKoreanClassName(cType);
    }

    public void SetType(ClassType type)
    {
        cType = type;
    }

    public void SetStatus(bool isSelect)
    {
        if (isSelect)
        {
            ColorUtility.TryParseHtmlString("#088DE4", out Color color);
            image_Background.color = color;
        }
        else
        {
            ColorUtility.TryParseHtmlString("#03BD5B", out Color color);
            image_Background.color = color;
        }
    }

    public void Button_SelectTap()
    {
        if (lobbyUI.tapMode == 0)
        {
            foreach (GameObject obj in lobbyUI.commandListObjDic.Values)
            {
                obj.SetActive(false);
            }
            lobbyUI.commandListObjDic[cType].SetActive(true);

            foreach (ClassTap tap in lobbyUI.classTapDic_List.Values)
            {
                tap.SetStatus(false);
            }
            lobbyUI.classTapDic_List[cType].SetStatus(true);
        }
        else if (lobbyUI.tapMode == 1)
        {
            foreach (GameObject obj in lobbyUI.mountedListObjDic.Values)
            {
                obj.SetActive(false);
            }
            lobbyUI.mountedListObjDic[cType].SetActive(true);

            foreach (ClassTap tap in lobbyUI.classTapDic_Mounted.Values)
            {
                tap.SetStatus(false);
            }
            lobbyUI.classTapDic_Mounted[cType].SetStatus(true);
        }
    }
}
