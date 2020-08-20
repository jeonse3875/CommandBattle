using UnityEngine;
using UnityEngine.UI;

public enum ClassChoiceType
{ 
    mountCommon
}

public class ClassBlock : MonoBehaviour
{
    public ClassChoiceType choiceType;
    public ClassType classType;
    public Text text_Class;
    public void SetBlock(ClassType cType, ClassChoiceType cType1)
    {
        classType = cType;
        choiceType = cType1;
        switch (cType)
        {
            case ClassType.common:
                text_Class.text = "공용";
                break;
            case ClassType.knight:
                text_Class.text = "기사";
                break;
            default:
                break;
        }
    }

    public void Button_ChooseClass()
    {
        LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
        lobby.group_MountCommon.SetActive(false);

        switch (choiceType)
        {
            case ClassChoiceType.mountCommon:
                UserInfo.instance.MountCommand(classType, lobby.commonCommandIdForWaiting);
                break;
            default:
                break;
        }
    }
}
