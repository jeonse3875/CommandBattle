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
                if(UserInfo.instance.mountedCommands[classType].Count.Equals(0))
                {
                    lobby.GetError("장착한 커맨드가 없습니다. 커맨드를 장착하고 다시 시도해주세요.", ForWhat.none);
                }
                else
                {
                    lobby.RequestRandomMatchMaking();
                }
                break;
            default:
                break;
        }
    }
}
