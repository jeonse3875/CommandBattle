using UnityEngine;
using UnityEngine.UI;

public class ClassTap : MonoBehaviour
{
    public ClassType cType;
    public Image image_Background;
    public Image image_ClassIcon;
    public Text text_ClassName;
    public Text text_MountedInfo;
    private LobbyUI lobbyUI;

    private void Start()
    {
        lobbyUI = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
        image_ClassIcon.sprite = Command.GetClassIcon(cType);
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

    public void UpdateMountedInfo()
    {
        int count = UserInfo.instance.mountedCommands[cType].Count;
        string color;

        if(count.Equals(0))
            color = "#F13242";
        else if (count.Equals(8))
            color = "#03BD5B";
        else
            color = "#FF8400";

        text_MountedInfo.text = string.Format("<color={1}>{0}</color>/8", count.ToString(), color);
        text_MountedInfo.gameObject.SetActive(true);
    }
}
