using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Cell
{
    public HashSet<GameObject> Objects { get; } = new HashSet<GameObject>();
    public HashSet<GemController> Gems { get; } = new HashSet<GemController>();
}

[RequireComponent(typeof(UnityEngine.Grid))]
public class GridController : BaseController
{
    [SerializeField] UnityEngine.Grid _grid;

    Dictionary<Vector3Int, Cell> _cells = new Dictionary<Vector3Int, Cell>();
    readonly Dictionary<GameObject, Vector3Int> _objectCells = new Dictionary<GameObject, Vector3Int>();
    readonly Dictionary<GameObject, GemController> _gemObjects = new Dictionary<GameObject, GemController>();

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

	public void Add(GameObject go)
	{
		AddInternal(go, null);
	}

	public void Add(GemController gem)
	{
		if (gem == null)
			return;

		AddInternal(gem.gameObject, gem);
	}

	void AddInternal(GameObject go, GemController gem)
	{
		if (go == null)
			return;

		Remove(go);

		Vector3Int cellPos = _grid.WorldToCell(go.transform.position);

		Cell cell = GetCell(cellPos);
		if (cell == null)
			return;

		cell.Objects.Add(go);
		if (gem != null)
		{
			cell.Gems.Add(gem);
			_gemObjects[go] = gem;
		}

		_objectCells[go] = cellPos;
	}

	public void Remove(GameObject go)
	{
		if (go == null)
			return;

		if (_objectCells.TryGetValue(go, out Vector3Int cellPos) == false)
			return;

		if (_cells.TryGetValue(cellPos, out Cell cell))
		{
			cell.Objects.Remove(go);
			if (_gemObjects.TryGetValue(go, out GemController gem))
				cell.Gems.Remove(gem);
		}

		_objectCells.Remove(go);
		_gemObjects.Remove(go);
	}

	Cell GetCell(Vector3Int cellPos)
	{
		Cell cell = null;

		if (_cells.TryGetValue(cellPos, out cell) == false)
		{
			cell = new Cell();
			_cells.Add(cellPos, cell);
		}

		return cell;
	}

	public void GatherObjects(Vector3 pos, float range, List<GameObject> results)
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
				if (_cells.TryGetValue(key, out Cell cell) == false)
					continue;

				results.AddRange(cell.Objects);
			}
		}
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
				if (_cells.TryGetValue(key, out Cell cell) == false)
					continue;

				results.AddRange(cell.Gems);
			}
		}
	}

	public void ClearObjects()
	{
		_cells.Clear();
		_objectCells.Clear();
		_gemObjects.Clear();
	}
}
