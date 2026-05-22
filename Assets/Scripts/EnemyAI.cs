using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
	{
	public static EnemyAI Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	public void MakeMove()
	{
		Piece[] allPieces = FindObjectsOfType<Piece>();
		List<(Piece, List<Tile>)> candidates = new List<(Piece, List<Tile>)>();

		foreach (var p in allPieces)
		{
			if (p.owner != PieceOwner.Enemy)
				continue;

			var moves = p.GetLegalMoves();
			if (moves.Count > 0)
				candidates.Add((p, moves));
		}

		if (candidates.Count == 0)
		{
			GameManager.Instance.EndEnemyMove();
			return;
		}

		var pair = candidates[Random.Range(0, candidates.Count)];
		Piece piece = pair.Item1;
		List<Tile> movesList = pair.Item2;

		Tile target = movesList[Random.Range(0, movesList.Count)];

		// wykonaje ruch 
		//piece.MoveToTileFromAI(target);
	}
}
