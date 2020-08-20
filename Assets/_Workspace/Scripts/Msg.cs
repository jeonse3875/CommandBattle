using BackEnd.Tcp;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Msg
{
	public enum MsgType
	{
		none, gameStart, commandComplete, battleEnd, dead
	}

	public Who sender;
	public MsgType type;

	public Msg()
	{
		if (BackendManager.instance.isP1 == true)
			sender = Who.p1;
		else
			sender = Who.p2;
	}
}

public class GameStartMsg : Msg
{
	public SessionId sessionId;
	public GameStartMsg() : base()
	{
		type = MsgType.gameStart;
		sessionId = BackendManager.instance.GetMySessionId();
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