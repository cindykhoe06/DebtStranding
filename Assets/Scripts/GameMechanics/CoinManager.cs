using UnityEngine;
using System.Collections.Generic;

// Owns the pool of CoinItem instances. Spawns an initial batch at game start, then on every
// blackout destroys whatever coins are left and spawns a fresh batch at new random points
// from the full spawn-point list (minimum 25), so the player has to search the map again.
public class CoinManager : MonoBehaviour
{
    public Transform spawnPointsRoot;   // parent whose children are the coin spawn points
    public GameObject coinPrefabOrTemplate; // existing "Coin" GameObject (has CoinItem+collider+mesh)

    public int coinsPerBatch = 10;
    public Vector2Int coinValueRange = new Vector2Int(1, 3);

    List<Transform> spawnPoints = new List<Transform>();
    List<GameObject> activeCoins = new List<GameObject>();

    void Awake()
    {
        if (spawnPointsRoot != null)
        {
            foreach (Transform t in spawnPointsRoot) spawnPoints.Add(t);
        }
    }

    void Start()
    {
        SpawnInitialCoins();
    }

    public void SpawnInitialCoins()
    {
        SpawnBatch();
    }

    public void RespawnCoins()
    {
        foreach (var c in activeCoins)
        {
            if (c != null) Destroy(c);
        }
        activeCoins.Clear();
        SpawnBatch();
        GameManager.Instance?.ShowNotification("Coins have respawned!");
    }

    void SpawnBatch()
    {
        if (coinPrefabOrTemplate == null || spawnPoints.Count == 0) return;

        var pool = new List<Transform>(spawnPoints);
        int count = Mathf.Min(coinsPerBatch, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            var point = pool[idx];
            pool.RemoveAt(idx);

            var coin = Instantiate(coinPrefabOrTemplate, point.position, Quaternion.identity);
            coin.SetActive(true);
            var ci = coin.GetComponent<CoinItem>();
            if (ci != null) ci.coinValue = Random.Range(coinValueRange.x, coinValueRange.y + 1);
            activeCoins.Add(coin);
        }
    }
}
