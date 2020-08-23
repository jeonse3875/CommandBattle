using UnityEngine;
using UnityEngine.UI;

public enum ClassChoiceType
{ 
    mountCommon, selectPlayingClass
}

public class ClassBlock : MonoBehaviour
{
    public ClassChoiceType choiceType;
    public ClassType classType;
    public Image image_ClassIcon;
    public Text text_Class;

    public void SetBlock(ClassType cType, ClassChoiceType choiceType)
    {
        classType = cType;
        this.choiceType = choiceType;
        this.image_ClassIcon.sprite = Command.GetClassIcon(cType);
        text_Class.text = Command.GetKoreanClassName(cType);
    }

    public void Button_ChooseClass()
    {
        LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();

        switch (choiceType)
        {
            case ClassChoiceType.mountCommon:
                UserInfo.instance.MountCommand(classType, lobby.commonCommandIdForWaiting);
                lobby.group_MountCommon.SetActive(false);
                break;
            case ClassChoiceType.selectPlayingClass:
                UserInfo.instance.playingClass = classType;
                lobby.group_PlayingClass.SetActive(false);
                lobby.RequestMatchMaking();
                break;
            default:
                break;
        }
    }
}
