using UnityEngine;
using UnityEngine.UI;

public class CommandInfoBlock : MonoBehaviour
{
    public CommandId id;
    public ClassType cType;
    public ClassType mountedCType;
    public int tapNum;
    public bool isOwn;
    public Image image_CommandIcon;
    public Image image_Background;
    public Image image_MountButton;
    public Text text_MountButton;
    public Text text_name;
    public Text text_Description;
    public Text text_Time;
    public Text text_Limit;
    public Text text_Damage;
    public GameObject mountButton;
    public GameObject obj_Blind;

    private void AddHandler()
    {
        UserInfo.instance.UpdateMountInfoEvent += ChangeMountInfo;
    }

    private void RemoveHandler()
    {
        UserInfo.instance.UpdateMountInfoEvent -= ChangeMountInfo;
    }

    public void SetBlock(Command command, int tapNum, bool isOwn)
    {
        AddHandler();

        text_name.text = command.name;
        text_Description.text = command.description;
        text_Time.text = command.time.ToString();

        if (command.limit.Equals(10))
            text_Limit.text = " -";
        else
            text_Limit.text = command.limit.ToString();

        if (command.totalDamage.Equals(0))
            text_Damage.text = " -";
        else
            text_Damage.text = command.totalDamage.ToString();

        this.tapNum = tapNum;
        this.isOwn = isOwn;
        this.id = command.id;
        cType = command.classType;
        if (cType != ClassType.common)
            mountedCType = cType;
        
        if (command.classType == ClassType.common)
        {
            SetMountButtonUI(tapNum == 1);
        }
        else
        {
            SetMountButtonUI(IsMounted());
        }

        image_CommandIcon.sprite = command.GetCommandIcon();
    }

    public void SetOwn(bool isOwn)
    {
        mountButton.GetComponent<Button>().interactable = isOwn;
        obj_Blind.SetActive(!isOwn);
    }

    public void Button_MountCommand()
    {
        LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();

        if (cType.Equals(ClassType.common))
        {
            if(tapNum.Equals(0))
            {
                lobby.MountCommon(id);
            }
            else if (tapNum.Equals(1))
            {
                bool isSuccess = UserInfo.instance.UnMountCommand(mountedCType, id);
            }
            return;
        }

        if (IsMounted())
        {
            bool isSuccess = UserInfo.instance.UnMountCommand(mountedCType, id);
            if (isSuccess)
            {
                SetMountButtonUI(false);
            }
            else
            {
                // 에러 출력
            }
        }
        else
        {
            bool isSuccess = UserInfo.instance.MountCommand(mountedCType, id);
            if(isSuccess)
            {
                SetMountButtonUI(true);
            }
            else
            {
                //에러 출력
            }
        }
    }

    public void Button_ShowDetail()
    {
        LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
        Command command = Command.FromId(id);
        command.isPreview = true;

        lobby.text_DetailCommandName.text = command.name;
        lobby.text_DetailTime.text = command.time.ToString();

        if (command.totalDamage.Equals(0))
            lobby.text_DetailDamage.text = " -";
        else
            lobby.text_DetailDamage.text = command.totalDamage.ToString();

        if (command.limit.Equals(10))
            lobby.text_DetailLimit.text = " -";
        else
            lobby.text_DetailLimit.text = command.limit.ToString();

        lobby.text_DetailDescription.text = command.description;

        lobby.previewCamera.SetActive(true);
        lobby.group_Detail.SetActive(true);
      
        lobby.StartPreview(command);
    }

    public void SetMountButtonUI(bool status)
    {
        if (status)
        {
            if (cType.Equals(ClassType.common) && tapNum.Equals(0))
                return;
            ColorUtility.TryParseHtmlString("#FF8400", out Color color);
            image_MountButton.color = color;
            text_MountButton.text = "해제";
        }
        else
        {
            if (tapNum.Equals(1))
            {
                RemoveHandler();
                Destroy(gameObject);
            }
            ColorUtility.TryParseHtmlString("#03BD5B", out Color color);
            image_MountButton.color = color;
            text_MountButton.text = "장착";
        }
    }

    public void ChangeMountInfo(ClassType cType, CommandId id, bool status)
    {
        if (id != this.id)
            return;

        SetMountButtonUI(status);
    }

    public bool IsMounted()
    {
        return UserInfo.instance.mountedCommands[mountedCType].Contains(id);
    }
}
