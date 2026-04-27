using UnityEditor;
using UnityEngine;

public class AddCoinSpin
{
    [MenuItem("Tools/Add CoinSpin to All Coins")]
    public static void Execute()
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        if (coins.Length == 0)
        {
            Debug.LogWarning("No GameObjects with tag 'Coin' found!");
            return;
        }

        int added = 0;
        foreach (GameObject coin in coins)
        {
            if (coin.GetComponent<CoinSpin>() == null)
            {
                coin.AddComponent<CoinSpin>();
                EditorUtility.SetDirty(coin);
                added++;
                Debug.Log($"Added CoinSpin to: {coin.name} ({coin.transform.parent?.name})");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Done! Added CoinSpin to {added}/{coins.Length} coins.");
    }
}
