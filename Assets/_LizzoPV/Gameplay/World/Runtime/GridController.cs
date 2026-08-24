using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class GemCell
{
    public HashSet<GemController> Gems { get; } = new HashSet<GemController>();
}

[RequireComponent(typeof(UnityEngine.Grid))]
public class GridController : BaseController
{
    [SerializeField] UnityEngine.Grid _grid;

    readonly Dictionary<Vector3Int, GemCell> _gemCells = new Dictionary<Vector3Int, GemCell>();
    readonly Dictionary<GameObject, Vector3Int> _gemCellPositions = new Dictionary<GameObject, Vector3Int>();
    readonly Dictionary<GameObject, GemController> _gemsByObject = new Dictionary<GameObject, GemController>();

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		_grid ??= GetComponent<UnityEngine.Grid>();
		if (_grid == null)
		{
			Debug.LogError("[GridController] Authored Grid component is required.", this);
			return false;
		}

		return true;
	}

	public void AddGem(GemController gem)
	{
		if (gem == null)
			return;

		GameObject go = gem.gameObject;
		RemoveGem(go);

		Vector3Int cellPos = _grid.WorldToCell(go.transform.position);

		GemCell cell = GetGemCell(cellPos);
		if (cell == null)
			return;

		cell.Gems.Add(gem);
		_gemsByObject[go] = gem;
		_gemCellPositions[go] = cellPos;
	}

	public void RemoveGem(GameObject go)
	{
		if (go == null)
			return;

		if (_gemCellPositions.TryGetValue(go, out Vector3Int cellPos) == false)
			return;

		if (_gemCells.TryGetValue(cellPos, out GemCell cell))
		{
			if (_gemsByObject.TryGetValue(go, out GemController gem))
				cell.Gems.Remove(gem);
		}

		_gemCellPositions.Remove(go);
		_gemsByObject.Remove(go);
	}

	GemCell GetGemCell(Vector3Int cellPos)
	{
		if (_gemCells.TryGetValue(cellPos, out GemCell cell) == false)
		{
			cell = new GemCell();
			_gemCells.Add(cellPos, cell);
		}

		return cell;
	}

	public void GatherGems(Vector3 pos, float range, List<GemController> results)
	{
		if (results == null)
			return;

		results.Clear();
		Vector3Int left = _grid.WorldToCell(pos + new Vector3(-range, 0));
		Vector3Int right = _grid.WorldToCell(pos + new Vector3(+range, 0));
		Vector3Int bottom = _grid.WorldToCell(pos + new Vector3(0, -range));
		Vector3Int top = _grid.WorldToCell(pos + new Vector3(0, +range));

		int minX = left.x;
		int maxX = right.x;
		int minY = bottom.y;
		int maxY = top.y;

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				Vector3Int key = new Vector3Int(x, y, 0);
				if (_gemCells.TryGetValue(key, out GemCell cell) == false)
					continue;

				results.AddRange(cell.Gems);
			}
		}
	}

	public void ClearGems()
	{
		_gemCells.Clear();
		_gemCellPositions.Clear();
		_gemsByObject.Clear();
	}
}
