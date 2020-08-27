using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
	private readonly int sizeX;
	private readonly int sizeY;
	private readonly float gridSize;
	private readonly Vector3 zeroPoint;

	public Grid(int sizeX, int sizeY, float gridSize, Vector3 zeroPoint)
	{
		this.sizeX = sizeX;
		this.sizeY = sizeY;
		this.gridSize = gridSize;
		this.zeroPoint = zeroPoint;
	}

	public Vector3 PosToVec3((int x, int y) pos)
	{
		//pos = ClampPos(pos);

		Vector3 vec3 = zeroPoint;
		vec3 += gridSize * (Vector3.right * pos.x + Vector3.forward * pos.y);

		return vec3;
	}

	public (int x, int y) SwitchDir((int x, int y) pos, Direction dir)
	{
		//플레이어 기준 (0,0), 위치 기준 Direction.up
		switch (dir)
		{
			case Direction.up:
				return pos;
			case Direction.rightUp:
				return (pos.y, pos.y);
			case Direction.right:
				return (pos.y, -pos.x);
			case Direction.rightDown:
				return (pos.y, -pos.y);
			case Direction.down:
				return (-pos.x, -pos.y);
			case Direction.leftDown:
				return (-pos.y, -pos.y);
			case Direction.left:
				return (-pos.y, pos.x);
			case Direction.leftUp:
				return (-pos.y, pos.y);
			default:
				return pos;
		}
	}

	public List<(int x, int y)> SwitchDirList(List<(int x, int y)> list, Direction dir)
	{
		List<(int x, int y)> switchedList = new List<(int x, int y)>();

		foreach (var pos in list)
		{
			switchedList.Add(SwitchDir(pos, dir));
		}

		return switchedList;
	}

	public (int x, int y) AddPos((int x, int y) pos1, (int x, int y) pos2, bool isClamp = false)
	{
		if (isClamp)
			return ClampPos((pos1.x + pos2.x, pos1.y + pos2.y));
		else
			return (pos1.x + pos2.x, pos1.y + pos2.y);
	}

	public int DistancePos((int x, int y) pos1, (int x, int y) pos2)
	{
		return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y);
	}

	public Vector3 GetBetweenVec3((int x, int y) pos1, (int x, int y) pos2)
	{
		float x = (pos1.x + pos2.x) / (float)2;
		float y = (pos1.y + pos2.y) / (float)2;
		Vector3 vec3 = zeroPoint;
		vec3 += gridSize * (Vector3.right * x + Vector3.forward * y);

		return vec3;
	}

	public (int x, int y) ClampPos((int x, int y) pos)
	{
		return (Mathf.Clamp(pos.x, 0, sizeX - 1), Mathf.Clamp(pos.y, 0, sizeY - 1));
	}

	public (int x, int y) Vec3ToPos(Vector3 vec)
	{
		Vector3 relativeVec = vec - zeroPoint;
		int x = Mathf.RoundToInt(relativeVec.x / gridSize);
		int y = Mathf.RoundToInt(relativeVec.z / gridSize);
		x = Mathf.Clamp(x, 0, sizeX - 1);
		y = Mathf.Clamp(y, 0, sizeX - 1);

		return (x, y);
	}

	public bool IsInGrid((int x, int y) pos)
	{
		return pos.x < sizeX && pos.x >= 0 && pos.y < sizeY && pos.y >= 0;
	}

	public static string DirToKorean(Direction dir)
	{
		string str = "";
		switch (dir)
		{
			case Direction.up:
				str = "위로";
				break;
			case Direction.rightUp:
				str = "오른쪽 위로";
				break;
			case Direction.right:
				str = "오른쪽으로";
				break;
			case Direction.rightDown:
				str = "오른쪽 아래로";
				break;
			case Direction.down:
				str = "아래로";
				break;
			case Direction.leftDown:
				str = "왼쪽 아래로";
				break;
			case Direction.left:
				str = "왼쪽으로";
				break;
			case Direction.leftUp:
				str = "왼쪽 위로";
				break;
			default:
				break;
		}

		return str;
	}
}

public enum DirectionType
{
	none, cross, diagonal, all
}

public enum Direction
{
	up, rightUp, right, rightDown, down, leftDown, left, leftUp
}