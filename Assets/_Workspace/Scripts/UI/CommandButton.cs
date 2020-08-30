using UnityEngine;
using UnityEngine.UI;

public class CommandButton : MonoBehaviour
{
    public Command command;
    public Button button;
    public Image image_CommandIcon;
    public Text text_Time;
    public Text text_Left;

    public GameObject group_MoreInfo;
    public Text text_CommandName;
    public Text text_Description;
    public Text text_Damage;

    private bool isTouching = false;
    private float touchingTime = 0f;

    public void InitializeButton(CommandId id)
    {
        command = Command.FromId(id);
        command.commander = InGame.instance.me;
        image_CommandIcon.sprite = command.GetCommandIcon();

        if (command.limit.Equals(10))
            text_Left.text = "제한없음";
        else
            text_Left.text = "X " + command.limit.ToString();

        text_Time.text = command.time.ToString();

        text_CommandName.text = command.name;
        text_Damage.text = command.totalDamage.ToString();
        if (command.totalDamage.Equals(0))
            text_Damage.text = "-";
        text_Description.text = command.description;
    }

    public void SetUnuseButton()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if(isTouching)
        {
            touchingTime += Time.deltaTime;
        }
        else
        {
            touchingTime = 0f;
        }

        group_MoreInfo.SetActive(touchingTime > 0.4f);
    }

    public void SetMoreInfo(bool status)
    {
        isTouching = status;
    }
}
