using UnityEngine;
using BackEnd;
using BackEnd.Tcp;
using System;
using LitJson;
using System.Text;

public enum ForWhat
{
	none, signUp, autoLogin, login, logout, signOut, createNickname, updateNickname, checkNicknameAvailable, checkNicknameExist,
	joinMatchingServer, leaveMatchingServer, matchMaking, joinGameServer, joinGameRoom
}

public class BackendManager : MonoBehaviour
{
	#region 공통 필드

	public static BackendManager instance;

	public delegate void loadingEventHandler(bool state, ForWhat what);
	public event loadingEventHandler LoadingEvent;
	public delegate void errorEventHandler(string message, ForWhat what);
	public event errorEventHandler ErrorEvent;

	#endregion


	#region [Login] 회원관리 필드

	public delegate void userManagementEventHandler(bool isSuccess);

	public event userManagementEventHandler CreateNicknameEvent;
	public event userManagementEventHandler UpdateNicknameEvent;
	public event userManagementEventHandler CheckNicknameAvailableEvent;

	public delegate void checkNicknameEventHandler();
	public event checkNicknameEventHandler NeedNicknameEvent;
	public event checkNicknameEventHandler NicknameExistEvent;

	public delegate void newUserEventHandler();
	public event newUserEventHandler NewUserEvent;

	public delegate void detectNewTableEventHandler(string tableName);
	public event detectNewTableEventHandler DetectNewTableEvent;
	public delegate void detectExistingTableEventHandler(string tableName, string inDate);
	public event detectExistingTableEventHandler DetectExistingTableEvent;

	#endregion

	#region [Lobby] 매칭서버 필드

	private bool isJoinMatchingServer = false;
	public delegate void matchMakingEventHandler(bool isSandbox);
	public event matchMakingEventHandler FindMatchEvent;

	public bool isInMatchRoom = false;
	public bool isFriendlyMatch = false;
	(MatchType mType, MatchModeType mmType, string indate) curRoomSetting;

	#endregion

	#region [Lobby] 인게임서버 필드

	private bool isJoinGameServer = false;
	private string roomToken;
	public delegate void getUserInfoEventHandler(MatchUserGameRecord record);
	public event getUserInfoEventHandler UpdateUserInfoEvent;
	public delegate void gameStartEventHandler();
	public event gameStartEventHandler ReadyToStartGameEvent;
	public event gameStartEventHandler GameStartEvent;
	public bool isP1;
	public SessionId opponentId;
	#endregion

	#region [InGame] 인게임 필드

	public delegate void getMsgEventHandler(string json);
	public event getMsgEventHandler GetMsgEvent;
	public delegate void gameEndEventHandler();
	public event gameEndEventHandler GameEndEvent;
	public delegate void leaveGameEventHandler(SessionId id);
	public event leaveGameEventHandler LeaveGameEvent;

	#endregion

	#region LifeCycle
	private void Awake()
	{
		SetSingleton();
		InitializeServer();
	}

	private void Start()
	{
		AddMatchingServerHandler();
		AddGameServerHandler();
		AddInGameHandler();
	}

	private void Update()
	{
		Backend.Match.Poll();
	}

	#endregion

	private void SetSingleton()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;
		DontDestroyOnLoad(this);
	}

	private void InitializeServer()
	{
		Backend.Initialize(() =>
		{
			if (Backend.IsInitialized)
			{

			}
			else
			{

			}
		});
	}

	#region 회원관리: 회원가입, 회원탈퇴, 로그인, 로그아웃, 닉네임 생성/수정, 회원정보

	public void CustomSignUp(string id, string pW)
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.signUp);

		Backend.BMember.CustomSignUp(id, pW, callback =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.signUp);

			if (callback.IsSuccess())
			{
				Debug.Log("회원가입 성공");
				CheckNicknameExist();
				CheckUserInfoTable();
			}
			else
			{
				Debug.Log("회원가입 실패: " + callback.ToString());

				string message;
				switch (callback.GetStatusCode())
				{
					case "409":
						message = "회원가입 실패.\n중복된 아이디가 존재합니다.";
						break;
					case "403":
						message = "Active User exceed 10.";
						break;
					default:
						message = "회원가입 실패. 알 수 없는 오류.";
						break;
				};

				if (ErrorEvent != null)
					ErrorEvent(message, ForWhat.signUp);
			}
		});

	}

	public void CustomLogin(string id, string pW)
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.login);

		Backend.BMember.CustomLogin(id, pW, callback =>
		 {
			 if (LoadingEvent != null)
				 LoadingEvent(false, ForWhat.login);


			 if (callback.IsSuccess())
			 {
				 Debug.Log("커스텀 로그인 성공");
				 CheckNicknameExist();
				 CheckUserInfoTable();
			 }
			 else
			 {
				 Debug.Log("커스텀 로그인 실패: " + callback.ToString());

				 string message;
				 switch (callback.GetStatusCode())
				 {
					 case "401":
						 message = "로그인 실패.\n아이디 또는 비밀번호가 틀렸습니다.";
						 break;
					 case "403":
						 message = string.Format("차단당한 사용자입니다.\n사유 : {0}", callback.GetErrorCode());
						 break;
					 default:
						 message = "로그인 실패. 알 수 없는 오류.";
						 break;
				 }

				 if (ErrorEvent != null)
					 ErrorEvent(message, ForWhat.login);
			 }
		 });
	}

	public void AutoLogin()
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.autoLogin);

		Backend.BMember.LoginWithTheBackendToken(callback =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.autoLogin);

			if (callback.IsSuccess())
			{
				Debug.Log("자동 로그인 성공");
				CheckNicknameExist();
				CheckUserInfoTable();
			}
			else
			{
				Debug.Log("자동 로그인 실패: " + callback.ToString());

				string message;
				switch (callback.GetStatusCode()) 
				{
					case "410":
						message = "자동 로그인 기간이 만료되었습니다. 다시 로그인해주세요.";
						break;
					case "401":
						message = "다른 기기에서 로그인하여 자동 로그인이 해제되었습니다. 다시 로그인해주세요.";
						break;
					case "403":
						message = string.Format("차단당한 사용자입니다.\n사유 : {0}", callback.GetErrorCode());
						break;
					default:
						message = "자동 로그인 실패. 알 수 없는 오류.";
						break;
				};

				if (ErrorEvent != null)
					ErrorEvent(message, ForWhat.autoLogin);
			}
		});
	}

	public void Logout()
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.logout);

		Backend.BMember.Logout(callback =>
		{
			Debug.Log("로그아웃");

			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.logout);
		});
	}

	public void SignOut()
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.signOut);

		Backend.BMember.Logout(callback =>
		{
			Debug.Log("회원탈퇴");

			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.signOut);
		});
	}

	public void CreateNickname(string nickname)
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.createNickname);

		Backend.BMember.CreateNickname(nickname, callback =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.createNickname);

			if (callback.IsSuccess())
			{
				Debug.Log("닉네임 생성 성공");
				if (NewUserEvent != null)
					NewUserEvent();
			}
			else
			{
				Debug.Log("닉네임 생성 실패: " + callback.ToString());
				string message;
				switch (callback.GetStatusCode()) 
				{
					case "400":
						message = "닉네임 생성 실패.\n잘못된 닉네임입니다. 닉네임 앞뒤에 공백이 포함되어있는지 확인해보세요.";
						break;
					case "409":
						message = "이미 사용 중인 닉네임입니다. 다른 닉네임으로 다시 시도해주세요.";
						break;
					default:
						message = "닉네임 생성 실패. 알 수 없는 오류.";
						break;
				};

				if (ErrorEvent != null)
					ErrorEvent(message, ForWhat.createNickname);
			}

			if (CreateNicknameEvent != null)
				CreateNicknameEvent(callback.IsSuccess());
		});

	}

	public void UpdateNickname(string nickname)
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.updateNickname);

		Backend.BMember.CreateNickname(nickname, callback =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.updateNickname);

			if (callback.IsSuccess())
			{
				Debug.Log("닉네임 수정 성공");
			}
			else
			{
				Debug.Log("닉네임 수정 실패: " + callback.ToString());
				string message;
				switch (callback.GetStatusCode()) 
				{
					case "400":
						message = "닉네임 수정 실패.\n잘못된 닉네임입니다. 닉네임 앞뒤에 공백이 포함되어있는지 확인해보세요.";
						break;
					case "409":
						message = "이미 사용 중인 닉네임입니다. 다른 닉네임으로 다시 시도해주세요.";
						break;
					default:
						message = "닉네임 수정 실패. 알 수 없는 오류.";
						break;
				};

				if (ErrorEvent != null)
					ErrorEvent(message, ForWhat.updateNickname);
			}

			if (UpdateNicknameEvent != null)
				UpdateNicknameEvent(callback.IsSuccess());
		});
	}

	public void CheckNicknameAvailable(string nickname)
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.checkNicknameAvailable);

		Backend.BMember.CreateNickname(nickname, callback =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.checkNicknameAvailable);

			if (callback.IsSuccess())
			{
				Debug.Log("사용 가능한 닉네임");
			}
			else
			{
				Debug.Log("사용할 수 없는 닉네임: " + callback.ToString());
				string message;
				switch (callback.GetStatusCode()) 
				{
					case "400":
						message = "잘못된 닉네임입니다. 닉네임 앞뒤에 공백이 포함되어있는지 확인해보세요.";
						break;
					case "409":
						message = "이미 사용 중인 닉네임입니다.";
						break;
					default:
						message = "사용할 수 없는 닉네임. 알 수 없는 오류.";
						break;
				};

				if (ErrorEvent != null)
					ErrorEvent(message, ForWhat.checkNicknameAvailable);
			}

			if (CheckNicknameAvailableEvent != null)
				CheckNicknameAvailableEvent(callback.IsSuccess());
		});
	}

	public void CheckNicknameExist()
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.checkNicknameExist);

		Backend.BMember.GetUserInfo(callback =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.checkNicknameExist);

			JsonData userData = callback.GetReturnValuetoJSON();
			JsonData userNickname = userData["row"]["nickname"];

			if (userNickname == null)
			{
				if (NeedNicknameEvent != null)
					NeedNicknameEvent();
			}
			else
			{
				if (NicknameExistEvent != null)
					NicknameExistEvent();
			}
		});
	}

	public string InsertData(string table, Param param)
	{
		var bro = Backend.GameInfo.Insert(table, param);
		if (bro.GetStatusCode() == "200")
			return bro.GetInDate();
		else
			return null;
	}

	public JsonData GetPrivateData(string table)
	{
		var bro = Backend.GameInfo.GetPrivateContents(table, 1);
		JsonData data = bro.GetReturnValuetoJSON()["rows"];

		return data[0];
	}

	public void UpdateData(string table, string inDate, Param param)
	{
		var bro = Backend.GameInfo.Update(table, inDate, param);
		Debug.Log(string.Format("{0} data updated (Error : {1})", table, bro.GetErrorCode()));
	}

	private void CheckUserInfoTable()
	{
		// 게임 정보 불러오기
		var tableListData = Backend.GameInfo.GetTableList();
		JsonData tableListjson = tableListData.GetReturnValuetoJSON();
		string[] tableList = JsonMapper.ToObject<string[]>(tableListjson["privateTables"].ToJson());
		foreach (string tableName in tableList)
		{
			var tableData = Backend.GameInfo.GetPrivateContents(tableName);
			JsonData tableJson = tableData.GetReturnValuetoJSON()["rows"];

			if (tableJson.Count.Equals(0))
			{
				Debug.Log("초기화되지 않은 테이블이 존재합니다. 테이블을 초기화합니다.");
				if (DetectNewTableEvent != null)
				{
					DetectNewTableEvent(tableName);
				}
			}
			else
			{
				if (DetectExistingTableEvent != null)
				{
					DetectExistingTableEvent(tableName, tableJson[0]["inDate"]["S"].ToString());
				}
			}
		}
	}

	#endregion

	#region 매칭서버: 매칭서버 접속/종료, 대기방 생성, 매칭 신청/취소

	public void JoinMatchingServer()
	{
		if (isJoinMatchingServer)
			return;

		if (Backend.Match.JoinMatchMakingServer(out ErrorInfo errorInfo))
		{
			if (errorInfo.Category == ErrorCode.Success)
			{
				Debug.Log("매칭서버 소켓연결 성공");
				isJoinMatchingServer = true;

				if (LoadingEvent != null)
					LoadingEvent(true, ForWhat.joinMatchingServer);
			}
			else
			{
				if (ErrorEvent != null)
					ErrorEvent("매칭서버 접속 실패.\nReason : " + errorInfo.Reason, ForWhat.joinMatchingServer);
			}
		}
	}

	public void LeaveMatchingServer()
	{
		Debug.Log("매칭서버 접속 종료");
		Backend.Match.LeaveMatchMakingServer();
		isJoinMatchingServer = false;
	}

	public void CreateMatchRoom(MatchType mType, MatchModeType mmType, string indate)
	{
		Backend.Match.CreateMatchRoom();
		curRoomSetting.mType = mType;
		curRoomSetting.mmType = mmType;
		curRoomSetting.indate = indate;
		isInMatchRoom = true;
	}

	public void LeaveMatchRoom()
	{
		if (isInMatchRoom)
		{
			Backend.Match.LeaveMatchRoom();
			isInMatchRoom = false;
		}
	}

	public void RequestMatchMaking(MatchType type, MatchModeType mode, string indate)
	{
		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.matchMaking);

		Debug.Log("매칭 신청");
		Backend.Match.RequestMatchMaking(type, mode, indate);
	}

	public void CancelMatchMaking()
	{
		Debug.Log("매칭 취소");
		Backend.Match.CancelMatchMaking();

		if (LoadingEvent != null)
			LoadingEvent(false, ForWhat.matchMaking);
	}

	private void AddMatchingServerHandler()
	{
		Backend.Match.OnJoinMatchMakingServer += (JoinChannelEventArgs args) =>
		{
			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.joinMatchingServer);

			Debug.Log("매칭서버 접속: " + args.ErrInfo.ToString());
			bool isSuccess = args.ErrInfo.Equals(ErrorInfo.Success);
			string message = args.ErrInfo.Reason;

			if (!isSuccess)
			{
				if (ErrorEvent != null)
					ErrorEvent("매칭서버 접속 실패.\nReason : " + args.ErrInfo.Reason, ForWhat.joinMatchingServer);
			}
		};

		Backend.Match.OnLeaveMatchMakingServer += (LeaveChannelEventArgs args) =>
		{
			Debug.Log("매칭서버종료: " + args.ErrInfo.ToString());
			bool isSuccess = args.ErrInfo.Category.Equals(ErrorCode.Success);

			string message = "";
			switch (args.ErrInfo.Category)
			{
				case ErrorCode.Exception:
					message = "비정상적인 접속 종료";
					break;
				case ErrorCode.DisconnectFromRemote:
					message = "유효하지 않은 매칭 타입";
					break;
				default:
					break;
			}

			if (!isSuccess)
			{
				if (ErrorEvent != null)
					ErrorEvent(message, ForWhat.leaveMatchingServer);
			}
		};

		Backend.Match.OnMatchMakingRoomCreate += (MatchMakingInteractionEventArgs args) =>
		{
			if (args.ErrInfo.Equals(ErrorCode.Success))
			{
				if (!isFriendlyMatch)
					RequestMatchMaking(curRoomSetting.mType, curRoomSetting.mmType, curRoomSetting.indate);
			}
			else
			{
				if (isInMatchRoom && !isFriendlyMatch)
				{
					RequestMatchMaking(curRoomSetting.mType, curRoomSetting.mmType, curRoomSetting.indate);
				}
				else
				{ 
					Debug.Log("매칭룸 생성 실패. Reason:" + args.Reason); 
				}
			}
		};

		Backend.Match.OnMatchMakingResponse += (MatchMakingResponseEventArgs args) =>
		{
			if (args.ErrInfo.Equals(ErrorCode.Match_InProgress))
				return;

			if (LoadingEvent != null)
				LoadingEvent(false, ForWhat.matchMaking);

			switch (args.ErrInfo)
			{
				case ErrorCode.Success:
					TcpEndPoint room = args.RoomInfo.m_inGameServerEndPoint;
					if (FindMatchEvent != null)
						FindMatchEvent(args.RoomInfo.m_enableSandbox);
					LeaveMatchRoom();
					JoinGameServer(room.m_address, room.m_port);
					roomToken = args.RoomInfo.m_inGameRoomToken;
					break;
				case ErrorCode.Match_MatchMakingCanceled:
					if (!isFriendlyMatch)
						LeaveMatchRoom();
					break;
				case ErrorCode.Match_InvalidMatchType:
				case ErrorCode.Match_InvalidModeType:
					if (ErrorEvent != null)
						ErrorEvent("잘못된 매치 타입입니다.", ForWhat.matchMaking);
					break;
				case ErrorCode.InvalidOperation:
					if (ErrorEvent != null)
						ErrorEvent(args.Reason, ForWhat.matchMaking);
					break;
				default:
					break;
			}
		};

		Backend.Match.OnException += (Exception e) =>
		{
			if (ErrorEvent != null)
				ErrorEvent("Exception : " + e.Message, ForWhat.joinMatchingServer);
		};
	}

	#endregion

	#region 인게임서버: 인게임서버 접속/종료, 게임룸 접속
	public void JoinGameServer(string address, ushort port)
	{
		if (isJoinGameServer)
			return;

		if (Backend.Match.JoinGameServer(address, port, false, out ErrorInfo errorInfo))
		{
			if (errorInfo.Category.Equals(ErrorCode.Success))
			{
				Debug.Log("인게임서버 소켓연결 성공");
				isJoinGameServer = true;

				if (LoadingEvent != null)
					LoadingEvent(true, ForWhat.joinGameServer);
			}
			else
			{
				if (ErrorEvent != null)
					ErrorEvent("게임서버 접속 실패.\nReason : " + errorInfo.Reason, ForWhat.joinGameServer);
			}
		}
	}

	public void JoinGameRoom(string roomToken)
	{
		Backend.Match.JoinGameRoom(roomToken);

		if (LoadingEvent != null)
			LoadingEvent(true, ForWhat.joinGameRoom);
	}

	public void LeaveGameServer()
	{
		Debug.Log("인게임서버 접속 종료");
		Backend.Match.LeaveGameServer();
		isJoinGameServer = false;
	}

	private void AddGameServerHandler()
	{
		Backend.Match.OnSessionJoinInServer += (JoinChannelEventArgs args) =>
		{
			if (!args.Session.IsRemote)
			{
				Debug.Log("인게임서버 접속: " + args.ErrInfo.ToString());
				bool isSuccess = args.ErrInfo.Equals(ErrorInfo.Success);
				string message = args.ErrInfo.Reason;

				if (!isSuccess)
				{
					if (ErrorEvent != null)
						ErrorEvent(message, ForWhat.joinGameServer);
				}
				else
				{
					JoinGameRoom(roomToken);
				}

				if (ReadyToStartGameEvent != null)
					ReadyToStartGameEvent();
			}
		};

		Backend.Match.OnSessionListInServer += (MatchInGameSessionListEventArgs args) =>
		{
			if (args.ErrInfo.Equals(ErrorCode.Success))
			{
				foreach (var record in args.GameRecords)
				{
					if (UpdateUserInfoEvent != null)
						UpdateUserInfoEvent(record);

					if (record.m_sessionId == GetMySessionId())
					{
						isP1 = record.m_isSuperGamer;
						Debug.Log("P1 : " + isP1.ToString());
					}
					else
					{
						opponentId = record.m_sessionId;
					}
				}
			}
		};

		Backend.Match.OnMatchInGameAccess += (MatchInGameSessionEventArgs args) =>
		{
			if (args.ErrInfo.Equals(ErrorCode.Success))
			{
				if (UpdateUserInfoEvent != null)
					UpdateUserInfoEvent(args.GameRecord);
				if (args.GameRecord.m_sessionId != GetMySessionId())
					opponentId = args.GameRecord.m_sessionId;
			}
		};

		Backend.Match.OnMatchInGameStart += () =>
		{
			if (GameStartEvent != null)
				GameStartEvent();
		};

		Backend.Match.OnLeaveInGameServer += (MatchInGameSessionEventArgs args) =>
		{
			isJoinGameServer = false;
		};

		Backend.Match.OnException += (Exception e) =>
		{
			if (ErrorEvent != null)
				ErrorEvent("Exception : " + e.Message, ForWhat.joinGameServer);
		};
	}

	#endregion

	#region 인게임: 게임 데이터 송수신, 게임 결과 전송, 세션아이디 조회, 닉네임 조회

	public void SendData<T>(T data)
	{
		string jsonString = JsonUtility.ToJson(data);
		byte[] stringByte = Encoding.UTF8.GetBytes(jsonString);

		Backend.Match.SendDataToInGameRoom(stringByte);
	}

	public void GameEnd(MatchGameResult result)
	{
		Backend.Match.MatchEnd(result);
	}

	public SessionId GetMySessionId()
	{
		return Backend.Match.GetMySessionId();
	}

	public string GetMyNickname()
	{
		SessionId session = GetMySessionId();
		return Backend.Match.GetNickNameBySessionId(session);
	}

	public string GetOpponentNickname()
	{
		return Backend.Match.GetNickNameBySessionId(opponentId);
	}

	private void AddInGameHandler()
	{
		Backend.Match.OnMatchRelay += (MatchRelayEventArgs args) =>
		{
			string dataString = Encoding.Default.GetString(args.BinaryUserData);
			if (GetMsgEvent != null)
				GetMsgEvent(dataString);
		};

		Backend.Match.OnMatchResult += (MatchResultEventArgs args) =>
		{
			if (GameEndEvent != null)
				GameEndEvent();
		};

		Backend.Match.OnSessionOffline += (MatchInGameSessionEventArgs args) =>
		{
			if (LeaveGameEvent != null)
				LeaveGameEvent(args.GameRecord.m_sessionId);
		};
	}

	#endregion
}
