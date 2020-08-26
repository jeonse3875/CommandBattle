using BackEnd;
using LitJson;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Table
{
	command
}

public class UserInfo : MonoBehaviour
{
	public static UserInfo instance;
	public Dictionary<Table, string> tableInDate = new Dictionary<Table, string>();
	const string ownCommandCol = "ownCommand";
	const string mountedCommandCol = "mountedCommand";

	public Dictionary<ClassType, List<CommandId>> ownCommands = new Dictionary<ClassType, List<CommandId>>();
	public Dictionary<ClassType, List<CommandId>> mountedCommands = new Dictionary<ClassType, List<CommandId>>();

	public delegate void updateCommandInfoEventHandler(ClassType cType, CommandId id, bool isMounted);
	public event updateCommandInfoEventHandler UpdateMountInfoEvent;
	public delegate void userInfoErrorEventHandler(string message, ForWhat what);
	public event userInfoErrorEventHandler UserInfoErrorEvent;

	public ClassType playingClass = ClassType.knight;

	public bool isUpdatedCommandData = false;

	private void Awake()
	{
		if (instance != null)
		{
			Destroy(this);
		}
		else
		{
			instance = this;
			DontDestroyOnLoad(this);
		}
	}

	private void Start()
	{
		AddHandler();
	}

	private void OnDestroy()
	{
		RemoveHandler();
	}

	private void OnApplicationQuit()
	{
		if(isUpdatedCommandData)
			UploadCommandInfo();
	}

	private void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			if (isUpdatedCommandData)
				UploadCommandInfo();
		}
	}

	public void AddHandler()
	{
		BackendManager.instance.NewUserEvent += GiveDefaultCommand;
		BackendManager.instance.DetectNewTableEvent += InitializeNewTable;
		BackendManager.instance.DetectExistingTableEvent += AddIndate;
	}

	public void RemoveHandler()
	{
		BackendManager.instance.NewUserEvent -= GiveDefaultCommand;
		BackendManager.instance.DetectNewTableEvent -= InitializeNewTable;
		BackendManager.instance.DetectExistingTableEvent -= AddIndate;
	}

	private void InitializeNewTable(string tableName)
	{
		Table table = (Table)Enum.Parse(typeof(Table), tableName);

		Param param = new Param();
		switch (table)
		{
			case Table.command:
				param.Add("ownCommand");
				param.Add("mountedCommand");
				break;
		}
		tableInDate[table] = BackendManager.instance.InsertData(tableName, param);
	}

	private void AddIndate(string tableName, string inDate)
	{
		Table table = (Table)Enum.Parse(typeof(Table), tableName);
		tableInDate[table] = inDate;
		Debug.Log(table.ToString() + " inDate: " + tableInDate[table]);
	}

	public void UpdateCommandInfo()
	{
		JsonData commandData = BackendManager.instance.GetPrivateData(Table.command.ToString());
		JsonData ownCommandData = commandData["ownCommand"];
		JsonData mountedCommandData = commandData["mountedCommand"];

		foreach(ClassType cType in Enum.GetValues(typeof(ClassType)))
		{
			try
			{
				ownCommands[cType] = GetCommandIdListFromTable(ownCommandData["M"][cType.ToString()]);
			}
			catch(KeyNotFoundException)
			{
				ownCommands[cType] = new List<CommandId>();
			}

			try
			{
				if (cType != ClassType.common)
					mountedCommands[cType] = GetCommandIdListFromTable(mountedCommandData["M"][cType.ToString()]);
			}
			catch (KeyNotFoundException)
			{
				mountedCommands[cType] = new List<CommandId>();
			}

		}

		Debug.Log("커맨드 정보 업데이트 완료");
		isUpdatedCommandData = true;
	}

	public void UploadCommandInfo()
	{
		Param param = new Param();
		Param ownParam = new Param();
		Param mountedParam = new Param();

		foreach (var key in ownCommands.Keys)
		{
			string[] arr = new string[ownCommands[key].Count];
			for(int i = 0; i < arr.Length; i++)
			{
				arr[i] = ownCommands[key][i].ToString();
			}
			ownParam.Add(key.ToString(), arr);
		}

		foreach (var key in mountedCommands.Keys)
		{
			string[] arr = new string[mountedCommands[key].Count];
			for (int i = 0; i < arr.Length; i++)
			{
				arr[i] = mountedCommands[key][i].ToString();
			}
			mountedParam.Add(key.ToString(), arr);
		}

		param.Add(ownCommandCol, ownParam);
		param.Add(mountedCommandCol, mountedParam);
		BackendManager.instance.UpdateData(Table.command.ToString(), tableInDate[Table.command], param);
	}

	public void GiveDefaultCommand()
	{
		Param param = new Param();
		string[] commonCommands = { CommandId.Move.ToString() };
		string[] knightCommands = { CommandId.EarthStrike.ToString(), CommandId.WhirlStrike.ToString() };

		Param ownCommandParam = new Param();
		ownCommandParam.Add(ClassType.common.ToString(), commonCommands);
		ownCommandParam.Add(ClassType.knight.ToString(), knightCommands);

		param.Add(ownCommandCol, ownCommandParam);
		BackendManager.instance.UpdateData(Table.command.ToString(), tableInDate[Table.command], param);

		param.Clear();
		string[] knightMountedCommands = { CommandId.Move.ToString(), CommandId.EarthStrike.ToString() };
		Param mountedCommnadParam = new Param();
		mountedCommnadParam.Add(ClassType.knight.ToString(), knightMountedCommands);
		param.Add(mountedCommandCol, mountedCommnadParam);
		BackendManager.instance.UpdateData(Table.command.ToString(), tableInDate[Table.command], param);
	}

	public static List<CommandId> GetCommandIdListFromTable(JsonData jsonData)
	{
		jsonData = jsonData["L"];
		List<CommandId> list = new List<CommandId>();
		for(int i = 0; i < jsonData.Count; i++)
		{
			string str = jsonData[i]["S"].ToString();
			Enum.TryParse(str, out CommandId id);
			list.Add(id);
		}

		return list;
	}

	public bool MountCommand(ClassType type, CommandId id)
	{
		LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
		if (type == ClassType.common)
			return false;
		
		if (mountedCommands[type].Count + 1 > 8)
		{
			if (UserInfoErrorEvent != null)
				UserInfoErrorEvent("더이상 커맨드를 장착할 수 없습니다. (최대 8개)", ForWhat.none);
			return false;
		}
		else if (mountedCommands[type].Contains(id))
		{
			if (UserInfoErrorEvent != null)
				UserInfoErrorEvent(string.Format("이미 장착한 커맨드입니다."), ForWhat.none);
			return false;
		}
		else
		{
			mountedCommands[type].Add(id);
			if (UpdateMountInfoEvent != null)
				UpdateMountInfoEvent(type, id, true);
			lobby.InstantiateMountedCommand(type, id);
			return true;
		}
	}

	public bool UnMountCommand(ClassType type, CommandId id)
	{
		if (type == ClassType.common)
			return false;

		if (mountedCommands[type].Contains(id))
		{
			mountedCommands[type].Remove(id);
			if (UpdateMountInfoEvent != null)
				UpdateMountInfoEvent(type, id, false);
			return true;
		}
		else
		{
			if (UserInfoErrorEvent != null)
				UserInfoErrorEvent("장착하지 않은 커맨드입니다.", ForWhat.none);
			return false;
		}
	}

	public void RemoveAllHandler()
	{
		if (UpdateMountInfoEvent == null)
			return;

		foreach(var del in UpdateMountInfoEvent.GetInvocationList())
		{
			UpdateMountInfoEvent -= (updateCommandInfoEventHandler)del;
		}
	}
}
