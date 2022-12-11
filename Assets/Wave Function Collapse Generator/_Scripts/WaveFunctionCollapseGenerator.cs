using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using waveFunctionPossibilitiesType = System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, System.Collections.Generic.List<(WaveFunctionCell cellOption, float rotation, float weight)>>;

public class WaveFunctionCollapseGenerator : MonoBehaviour
{
	[Min(1f)] public Vector2Int GridSize = Vector2Int.one * 5;
	[Min(0f)] public Vector3 CellSize = new(1f, 0.1f, 1f);
	public WaveFunctionCell[] WaveFunctionPossibilities;

	private Vector3 GetCellPosition(int i, int j) => Vector3.Scale(CellSize, new Vector3(i + 0.5f - (GridSize.x / 2f), 0, j + 0.5f - (GridSize.y / 2f)));

	[ContextMenu(nameof(Generate))]
	public void Generate()
	{
		ClearChildren();
		waveFunctionPossibilitiesType waveFunctionPossibilities = new();
		PopulatePossibilities(waveFunctionPossibilities);
		Clean0WeightPossibilities(waveFunctionPossibilities);
		CollapseWaveFunction(waveFunctionPossibilities);
		SpawnAllPossibilities(waveFunctionPossibilities);
	}

	private void ClearChildren()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			if (Application.isPlaying)
			{
				Destroy(transform.GetChild(0).gameObject);
			}
			else
			{
				DestroyImmediate(transform.GetChild(0).gameObject);
			}
		}
	}

	private void PopulatePossibilities(waveFunctionPossibilitiesType waveFunctionPossibilities)
	{
		for (int i = 0; i < GridSize.x; i++)
		{
			for (int j = 0; j < GridSize.y; j++)
			{
				waveFunctionPossibilities.Add(new(i, j), WaveFunctionPossibilities.SelectMany(
					option => new List<(WaveFunctionCell cellOption, float rotation, float weight)>
					{
						(option, 0f, option.GetWeight(new(i, j),GridSize,0f)),
						(option, 90f, option.GetWeight(new(i, j),GridSize,90f)),
						(option, 180f, option.GetWeight(new(i, j),GridSize,180f)),
						(option, -90f, option.GetWeight(new(i, j),GridSize,-90f) )
					}).ToList());
			}
		}
	}

	private void Clean0WeightPossibilities(waveFunctionPossibilitiesType waveFunctionPossibilities)
	{
		for (int i = 0; i < GridSize.x; i++)
		{
			for (int j = 0; j < GridSize.y; j++)
			{
				waveFunctionPossibilities[new(i, j)].RemoveAll(option => option.weight <= 0f);
			}
		}
	}

	private void CollapseWaveFunction(waveFunctionPossibilitiesType waveFunctionPossibilities)
	{
		while (true)
		{
			List<(WaveFunctionCell cellOption, float rotation, float weight)>[] uncollapsedCells = waveFunctionPossibilities.Values.
				Where(possibilities =>
				possibilities.Count > 1).ToArray();
			if (!uncollapsedCells.Any())
			{
				return;
			}

			int minPossibilitiesCount = uncollapsedCells.Min(possibilities => possibilities.Count);
			KeyValuePair<Vector2Int, List<(WaveFunctionCell cellOption, float rotation, float weight)>> minPossibilitiesOption = waveFunctionPossibilities.First(cell => cell.Value.Count == minPossibilitiesCount);
			(WaveFunctionCell cellOption, float rotation, float weight) randomPossibility = RandomWightedCollapse(waveFunctionPossibilities[minPossibilitiesOption.Key]);
			waveFunctionPossibilities[minPossibilitiesOption.Key] = new() { randomPossibility };

			CascadePossibilitiesCollapse(waveFunctionPossibilities, minPossibilitiesOption.Key, Vector2Int.up);
			CascadePossibilitiesCollapse(waveFunctionPossibilities, minPossibilitiesOption.Key, Vector2Int.down);
			CascadePossibilitiesCollapse(waveFunctionPossibilities, minPossibilitiesOption.Key, Vector2Int.right);
			CascadePossibilitiesCollapse(waveFunctionPossibilities, minPossibilitiesOption.Key, Vector2Int.left);
		}
	}

	private (WaveFunctionCell cellOption, float rotation, float weight) RandomWightedCollapse(List<(WaveFunctionCell cellOption, float rotation, float weight)> possibilities)
	{
		float totalWeight = possibilities.Sum(option => option.weight);
		float randomWeight = Random.Range(0f, totalWeight);
		foreach ((WaveFunctionCell cellOption, float rotation, float weight) item in possibilities)
		{
			randomWeight -= item.weight;
			if (randomWeight <= 0f)
			{
				return item;
			}
		}

		return default;
	}

	private void CascadePossibilitiesCollapse(waveFunctionPossibilitiesType waveFunctionPossibilities, Vector2Int cascadeSourceIndex, Vector2Int cascadeDirection)
	{
		if (!waveFunctionPossibilities.ContainsKey(cascadeSourceIndex + cascadeDirection))
		{
			return;
		}

		List<(WaveFunctionCell cellOption, float rotation, float weight)> UnCollapsedPossibilities = waveFunctionPossibilities[cascadeSourceIndex + cascadeDirection];
		if (UnCollapsedPossibilities.Count < 2)
		{
			return;
		}

		(WaveFunctionCell cellOption, float rotation, float weight) CascadeSourceCell = waveFunctionPossibilities[cascadeSourceIndex][0];
		Vector3 cascadeDirection3d = new(cascadeDirection.x, 0, cascadeDirection.y);
		for (int i = UnCollapsedPossibilities.Count - 1; i >= 0; i--)
		{
			(WaveFunctionCell cellOption, float rotation, float weight) option = UnCollapsedPossibilities[i];
			if (!CascadeSourceCell.cellOption.DoesNeighborFit(CascadeSourceCell.rotation, option.cellOption, option.rotation, cascadeDirection3d))
			{
				UnCollapsedPossibilities.RemoveAt(i);
			}
		}

		if (UnCollapsedPossibilities.Count != 1)
		{
			return;
		}

		if (cascadeDirection != Vector2Int.up)
		{
			CascadePossibilitiesCollapse(waveFunctionPossibilities, cascadeSourceIndex + cascadeDirection, Vector2Int.up);
		}

		if (cascadeDirection != Vector2Int.down)
		{
			CascadePossibilitiesCollapse(waveFunctionPossibilities, cascadeSourceIndex + cascadeDirection, Vector2Int.down);
		}

		if (cascadeDirection != Vector2Int.right)
		{
			CascadePossibilitiesCollapse(waveFunctionPossibilities, cascadeSourceIndex + cascadeDirection, Vector2Int.right);
		}

		if (cascadeDirection != Vector2Int.left)
		{
			CascadePossibilitiesCollapse(waveFunctionPossibilities, cascadeSourceIndex + cascadeDirection, Vector2Int.left);
		}
	}

	private void SpawnAllPossibilities(waveFunctionPossibilitiesType waveFunctionPossibilities)
	{
		for (int i = 0; i < GridSize.x; i++)
		{
			for (int j = 0; j < GridSize.y; j++)
			{
				SpawnAllPossibilities(waveFunctionPossibilities, i, j);
			}
		}
	}

	private void SpawnAllPossibilities(waveFunctionPossibilitiesType waveFunctionPossibilities, int i, int j)
	{
		List<(WaveFunctionCell cellOption, float rotation, float weight)> cellPossibilities = waveFunctionPossibilities[new(i, j)];
		Transform cellRoot = new GameObject($"Cell ({i}, {j})").transform;
		cellRoot.SetParent(transform);
		cellRoot.localPosition = GetCellPosition(i, j);
		cellRoot.localRotation = Quaternion.identity;

		for (int k = 0; k < cellPossibilities.Count; k++)
		{
			SpawnCellPossibilities(cellPossibilities[k].cellOption, cellPossibilities[k].rotation, Vector3.zero, cellRoot);
		}
	}

	private void SpawnCellPossibilities(WaveFunctionCell cellPrefab, float rotation, Vector3 cellPosition, Transform parent)
	{
		if (!cellPrefab)
		{
			return;
		}

		Transform cell = Instantiate(cellPrefab, parent).transform;
		cell.localPosition = cellPosition;
		cell.localRotation = Quaternion.Euler(0f, rotation, 0f);
	}

#if UNITY_EDITOR
	[Header("Gizmos")]
	[SerializeField] private Color CellColor = Color.cyan;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = CellColor;
		Gizmos.matrix = transform.localToWorldMatrix;
		for (int i = 0; i < GridSize.x; i++)
		{
			for (int j = 0; j < GridSize.y; j++)
			{
				GrawCellGizmo(i, j);
			}
		}
	}

	private void GrawCellGizmo(int i, int j) => Gizmos.DrawWireCube(GetCellPosition(i, j), CellSize);
#endif
}
