using BackEnd.Tcp;
using System.Collections.Generic;

public class Msg
{
	public enum MsgType
	{
		none, gameStart, commandComplete, battleEnd, mapEvent
	}

	public Who sender;
	public MsgType type;

	public Msg()
	{
		if (BackendManager.instance.isP1 == true)
			sender = Who.p1;
		else
			sender = Who.p2;

		if (UserInfo.instance.playingGameMode.Equals(GameMode.bossRush))
			sender = Who.p1;
	}
}

public class GameStartMsg : Msg
{
	public SessionId sessionId;
	public ClassType cType;
	public int play;
	public int win;

	public GameStartMsg() : base()
	{
		type = MsgType.gameStart;
		sessionId = BackendManager.instance.GetMySessionId();
		cType = UserInfo.instance.playingClass;
		play = UserInfo.instance.matchRecord[cType].play;
		win = UserInfo.instance.matchRecord[cType].win;
	}
}

public class CommandCompleteMsg : Msg
{
	public List<CommandId> commandIdList;
	public List<Direction> dirList;

	public CommandCompleteMsg() : base()
	{
		type = MsgType.commandComplete;
	}

	public List<Command> ToCommandList()
	{
		List<Command> commandList = new List<Command>();
		int index = 0;
		foreach (var id in commandIdList)
		{
			Command command;
			command = Command.FromId(id, dirList[index]);
			command.commander = sender;

			commandList.Add(command);
			index++;
		}

		return commandList;
	}
}

public class BattleEndMsg : Msg
{
	public BattleEndMsg() : base()
	{
		type = MsgType.battleEnd;
	}
}

public class MapEventMsg : Msg
{
	public MapEvent mapEvent;
	public int x;
	public int y;

	public MapEventMsg(MapEvent mapEvent, (int x, int y) pos) : base()
	{
		type = MsgType.mapEvent;
		this.mapEvent = mapEvent;
		this.x = pos.x;
		this.y = pos.y;
	}
}