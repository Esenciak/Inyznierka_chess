using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	public GamePhase currentPhase = GamePhase.Placement; // Zaczynamy od rozstawiania!
	public Turn currentTurn = Turn.Player;

	private void Awake()
	{
		Instance = this;
	}

	public bool CanPieceMove(Piece piece)
	{
		// W fazie Placement sprawdzamy tylko czy pionek należy do gracza
		if (currentPhase == GamePhase.Placement)
			return piece.owner == PieceOwner.Player;

		// W walce standardowe zasady
		if (currentPhase == GamePhase.Battle)
		{
			if (currentTurn == Turn.Player && piece.owner != PieceOwner.Player) return false;
			if (currentTurn == Turn.Enemy && piece.owner != PieceOwner.Enemy) return false;
			return true;
		}
		return false;
	}

	// Przycisk "START BATTLE" powinien wywołać tę metodę
	public void StartBattle()
	{
		currentPhase = GamePhase.Battle;
		currentTurn = Turn.Player;
		Debug.Log("Faza bitwy rozpoczęta!");
	}

	public void EndPlayerMove()
	{
		Debug.Log("Koniec tury gracza. Tura Enemy...");
		currentTurn = Turn.Enemy;
		// Uruchamiamy AI z małym opóźnieniem, żeby było widać, że myśli
		StartCoroutine(EnemyMoveRoutine());
	}

	private IEnumerator EnemyMoveRoutine()
	{
		yield return new WaitForSeconds(1.0f); // Czekaj 1 sekundę

		if (EnemyAI.Instance != null)
		{
			EnemyAI.Instance.MakeMove();
		}
		else
		{
			Debug.LogError("Brak skryptu EnemyAI!");
			EndEnemyMove(); // Awaryjne oddanie tury
		}
	}

	public void EndEnemyMove()
	{
		Debug.Log("Koniec tury Enemy. Tura Gracza.");
		currentTurn = Turn.Player;
	}

	public void GameOver(bool playerWon)
	{
		Debug.Log(playerWon ? "WYGRANA!" : "PRZEGRANA!");
		// Tutaj logika powrotu do sklepu
	}
}