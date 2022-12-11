using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class WaveFunctionCell : MonoBehaviour
{
	#region Borders & Neighbors
	private enum NeighborBorderState { DontCare, Allow, DontAllow }

	[SerializeField] private NeighborBorderState ForwardNeighborState;
	[SerializeField] private NeighborBorderState BackNeighborsState;
	[SerializeField] private NeighborBorderState RightNeighborsState;
	[SerializeField] private NeighborBorderState LeftNeighborsState;

	private NeighborBorderState GetNeighborBordersState(Vector3 localDirection)
	{
		localDirection.Normalize();
		if (localDirection == Vector3.forward)
		{
			return ForwardNeighborState;
		}

		if (localDirection == Vector3.back)
		{
			return BackNeighborsState;
		}

		if (localDirection == Vector3.right)
		{
			return RightNeighborsState;
		}

		if (localDirection == Vector3.left)
		{
			return LeftNeighborsState;
		}

		throw new System.ArgumentException(localDirection.ToString());
	}

	public bool DoesNeighborFit(float currentRotation, WaveFunctionCell neighbor, float neighborRotation, Vector3 directionToNeighbor)
	{
		var bordersState = GetNeighborBordersState(Quaternion.Euler(0f, -currentRotation, 0f) * directionToNeighbor);
		if (!neighbor)
		{
			return bordersState != NeighborBorderState.DontAllow;
		}

		if (bordersState == NeighborBorderState.DontCare)
		{
			return true;
		}

		var neighborBordersState = neighbor.GetNeighborBordersState(Quaternion.Euler(0f, -neighborRotation, 0f) * -directionToNeighbor);

		return bordersState == neighborBordersState;
	}
	#endregion

	#region Weights
	[Header("Weights")]
	[Min(0f)] public float Weight = 100f;
	[Min(0f)] public float InteriorWeightMultiplier = 1f;
	[Min(0f)] public float EdgeWeightMultiplier = 1f;

	public float GetWeight(Vector2Int cellPosition, Vector2Int gridSize, float rotation)
	{
		if (IsOnEdge(cellPosition, gridSize, out List<Vector3> edgeDirections))
		{
			foreach (Vector3 direction in edgeDirections)
			{
				if (DoesNeighborFit(rotation, null, 0f, direction))
				{
					return 0f;
				}
			}
		}

		return Weight * Mathf.Lerp(InteriorWeightMultiplier, EdgeWeightMultiplier, GetCellOffsetFromCenter(cellPosition, gridSize));
	}

	private bool IsOnEdge(Vector2Int cellPosition, Vector2Int gridSize, out List<Vector3> edgeDirections)
	{
		edgeDirections = new List<Vector3>();
		if (cellPosition.y == 0)
		{
			edgeDirections.Add(Vector3.back);
		}
		else if (cellPosition.y == gridSize.y - 1)
		{
			edgeDirections.Add(Vector3.forward);
		}

		if (cellPosition.x == 0)
		{
			edgeDirections.Add(Vector3.left);
		}
		else if (cellPosition.x == gridSize.x - 1)
		{
			edgeDirections.Add(Vector3.right);
		}

		return edgeDirections.Count != 0;
	}

	private float GetCellOffsetFromCenter(Vector2 cellPosition, Vector2 gridSize)
	{
		Vector2 centerPosition = (gridSize / 2f) - (0.5f * Vector2.one);
		Vector2 offset = cellPosition - centerPosition;
		Vector2 absoluteOffset = new(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
		Vector2 LerpOffset = absoluteOffset / centerPosition;
		return Mathf.Max(LerpOffset.x, LerpOffset.y);
	}
	#endregion

#if UNITY_EDITOR
	[Header("Gizmos")]
	[SerializeField] private Color AllowNeighborsColor = Color.green;
	[SerializeField] private Color DontCareNeighborsColor = Color.yellow;
	[SerializeField] private Color DisallowNeighborsColor = Color.red;
	[SerializeField][Min(0f)] private float BorderGizmoCenterOffset = 1f;
	[SerializeField][Min(0f)] private Vector3 BorderGizmoSize = new(1, 0.1f, 0.1f);

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		DrawBorderGizmo(Vector3.forward, GetBorderStateColor(ForwardNeighborState));
		DrawBorderGizmo(Vector3.back, GetBorderStateColor(BackNeighborsState));
		DrawBorderGizmo(Vector3.right, GetBorderStateColor(RightNeighborsState));
		DrawBorderGizmo(Vector3.left, GetBorderStateColor(LeftNeighborsState));
	}

	private void DrawBorderGizmo(Vector3 direction, Color color)
	{
		Gizmos.color = color;
		Vector3 size = Quaternion.LookRotation(direction, Vector3.up) * BorderGizmoSize;
		Gizmos.DrawCube(direction.normalized * BorderGizmoCenterOffset, size);
	}

	private Color GetBorderStateColor(NeighborBorderState state)
	{
		return state switch
		{
			NeighborBorderState.Allow => AllowNeighborsColor,
			NeighborBorderState.DontAllow => DisallowNeighborsColor,
			NeighborBorderState.DontCare => DontCareNeighborsColor,
			_ => throw new System.ArgumentOutOfRangeException(),
		};
	}
#endif
}
