using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
	public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
	{
		T component = go.GetComponent<T>();
		if (component == null)
			component = go.AddComponent<T>();
		return component;
	}

	public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
	{
		Transform transform = FindChild<Transform>(go, name, recursive);
		if (transform == null)
			return null;

		return transform.gameObject;
	}

	public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
	{
		if (go == null)
			return null;

		if (recursive == false)
		{
			for (int i = 0; i < go.transform.childCount; i++)
			{
				Transform transform = go.transform.GetChild(i);
				if (string.IsNullOrEmpty(name) || transform.name == name)
				{
					T component = transform.GetComponent<T>();
					if (component != null)
						return component;
				}
			}
		}
		else
		{
			foreach (T component in go.GetComponentsInChildren<T>())
			{
				if (string.IsNullOrEmpty(name) || component.name == name)
					return component;
			}
		}

		return null;
	}

	public static Vector2 GenerateMonsterSpawnPosition(Vector2 characterPosition, float minDistance = 10.0f, float maxDistance = 20.0f)
	{
		float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
		float distance = Random.Range(minDistance, maxDistance);

		float xDist = Mathf.Cos(angle) * distance;
		float yDist = Mathf.Sin(angle) * distance;

		Vector2 spawnPosition = characterPosition + new Vector2(xDist, yDist);

		return spawnPosition;
	}

	public static Vector2 GenerateMonsterSpawnPositionOutsideCamera(Vector2 characterPosition, float minMargin, float maxMargin)
	{
		float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
		Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		float margin = Random.Range(minMargin, maxMargin);
		return GenerateMonsterSpawnPositionOutsideCamera(characterPosition, direction, margin);
	}

	public static Vector2 GenerateMonsterSpawnPositionOutsideCamera(Vector2 characterPosition, Vector2 direction, float margin)
	{
		if (direction.sqrMagnitude <= 0.0001f)
			direction = Vector2.right;

		direction.Normalize();
		float distance = ResolveCameraEdgeDistance(direction) + Mathf.Max(0.0f, margin);
		return characterPosition + direction * distance;
	}

	static float ResolveCameraEdgeDistance(Vector2 direction)
	{
		Camera camera = Camera.main;
		if (camera == null || camera.orthographic == false)
			return 5.0f;

		float halfHeight = camera.orthographicSize;
		float halfWidth = halfHeight * camera.aspect;
		float xDistance = Mathf.Abs(direction.x) <= 0.0001f ? float.PositiveInfinity : halfWidth / Mathf.Abs(direction.x);
		float yDistance = Mathf.Abs(direction.y) <= 0.0001f ? float.PositiveInfinity : halfHeight / Mathf.Abs(direction.y);
		return Mathf.Min(xDistance, yDistance);
	}
}
