using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GameManagerTurnTests
{
    private GameObject gameManagerObject;
    private GameObject battleSyncObject;

    [TearDown]
    public void TearDown()
    {
        if (battleSyncObject != null)
        {
            Object.DestroyImmediate(battleSyncObject);
        }

        if (gameManagerObject != null)
        {
            Object.DestroyImmediate(gameManagerObject);
        }

        ResetGameManagerSingleton();
    }

    [Test]
    public void IsMyTurn_UsesBattleMoveSyncWhenNetworkActive()
    {
        gameManagerObject = new GameObject("GameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        gameManager.isMultiplayer = true;
        gameManager.currentTurn = PieceOwner.Enemy;

        battleSyncObject = new GameObject("BattleMoveSync");
        battleSyncObject.AddComponent<BattleMoveSync>();

        Assert.IsTrue(gameManager.IsMyTurn(), "Expected BattleMoveSync to drive the turn state when networked.");
    }

    private static void ResetGameManagerSingleton()
    {
        FieldInfo instanceField = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        if (instanceField != null)
        {
            instanceField.SetValue(null, null);
        }
    }
}
