using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class WaveFunctionCell : MonoBehaviour
{
	#region Borders & Neighbors
	private enum NeighborBorderState { DontCare, Allow, DontAllow }
	[System.Serializable]
	private struct NeighborBordersState
	{
		public NeighborBorderState NeighborState;
		public NeighborBorderState RightNeighborState;
		public NeighborBorderState LeftNeighborState;
	}

	[SerializeField] private NeighborBordersState ForwardNeighborState;
	[SerializeField] private NeighborBordersState BackNeighborsState;
	[SerializeField] private NeighborBordersState RightNeighborsState;
	[SerializeField] private NeighborBordersState LeftNeighborsState;

	private NeighborBordersState GetNeighborBordersState(Vector3 localDirection)
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

	private (NeighborBorderState forward, NeighborBorderState Right, NeighborBorderState Left) GetBordersState(Vector3 localDirection)
	{
		localDirection.Normalize();
		if (localDirection == Vector3.forward)
		{
			return (ForwardNeighborState.NeighborState, LeftNeighborsState.NeighborState, RightNeighborsState.NeighborState);
		}

		if (localDirection == Vector3.back)
		{
			return (BackNeighborsState.NeighborState, RightNeighborsState.NeighborState, LeftNeighborsState.NeighborState);
		}

		if (localDirection == Vector3.right)
		{
			return (RightNeighborsState.NeighborState, ForwardNeighborState.NeighborState, BackNeighborsState.NeighborState); ;
		}

		if (localDirection == Vector3.left)
		{
			return (LeftNeighborsState.NeighborState, BackNeighborsState.NeighborState, ForwardNeighborState.NeighborState);
		}

		throw new System.ArgumentException(localDirection.ToString());
	}

	public bool DoesNeighborFit(float currentRotation, WaveFunctionCell neighbor, float neighborRotation, Vector3 directionToNeighbor)
	{
		NeighborBordersState bordersState = GetNeighborBordersState(Quaternion.Euler(0f, -currentRotation, 0f) * directionToNeighbor);
		if (!neighbor)
		{
			return bordersState.NeighborState != NeighborBorderState.DontAllow;
		}

		(NeighborBorderState forward, NeighborBorderState Right, NeighborBorderState Left) neighborBordersState = neighbor.GetBordersState(Quaternion.Euler(0f, -neighborRotation, 0f) * -directionToNeighbor);
		//Debug.Log($"{neighbor.name} ({directionToNeighbor}) : {neighborBordersState.forward}, {neighborBordersState.Right}, {neighborBordersState.Left} ({neighborRotation})");
		if (!AreBorderStatesAValidMatch(bordersState.NeighborState, neighborBordersState.forward))
		{
			return false;
		}

		if (!AreBorderStatesAValidMatch(bordersState.RightNeighborState, neighborBordersState.Right))
		{
			return false;
		}

		if (!AreBorderStatesAValidMatch(bordersState.LeftNeighborState, neighborBordersState.Left))
		{
			return false;
		}

		return true;
	}

	private bool AreBorderStatesAValidMatch(NeighborBorderState source, NeighborBorderState neighbor)
	{
		if (source == NeighborBorderState.DontCare)
		{
			return true;
		}

		return source == neighbor;
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
		DrawNeighborGizmo(Vector3.forward, ForwardNeighborState);
		DrawNeighborGizmo(Vector3.back, BackNeighborsState);
		DrawNeighborGizmo(Vector3.right, RightNeighborsState);
		DrawNeighborGizmo(Vector3.left, LeftNeighborsState);
	}

	private void DrawNeighborGizmo(Vector3 direction, NeighborBordersState neighborState)
	{
		Vector3 neighborCenter = 2f * BorderGizmoCenterOffset * direction.normalized;
		DrawBorderGizmo(neighborCenter, -direction, GetBorderStateColor(neighborState.NeighborState));
		DrawBorderGizmo(neighborCenter, Vector3.Cross(direction, Vector3.down), GetBorderStateColor(neighborState.RightNeighborState));
		DrawBorderGizmo(neighborCenter, Vector3.Cross(direction, Vector3.up), GetBorderStateColor(neighborState.LeftNeighborState));
	}

	private void DrawBorderGizmo(Vector3 center, Vector3 direction, Color color)
	{
		Gizmos.color = color;
		Vector3 size = Quaternion.LookRotation(direction, Vector3.up) * BorderGizmoSize;
		Gizmos.DrawCube(center + (direction.normalized * BorderGizmoCenterOffset), size);
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
