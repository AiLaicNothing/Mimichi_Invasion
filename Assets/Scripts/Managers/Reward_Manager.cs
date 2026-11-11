using System;
using UnityEngine;

public class Reward_Manager : MonoBehaviour
{
    public static Reward_Manager Instance;

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject rewardBoxPrefab;
    private GameObject hatBoxPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        Wave_Manager.Instance.onWaveComplete += SpawnReward;
        Game_Manager.Instance.onVictory += SpawnHatReward;
    }

    private void OnDisable()
    {
        Wave_Manager.Instance.onWaveComplete -= SpawnReward;
        Game_Manager.Instance.onVictory -= SpawnHatReward;
    }

    private void SpawnReward()
    {
        if (rewardBoxPrefab != null || spawnPoint != null)
        {
            GameObject prefab = Instantiate(rewardBoxPrefab, spawnPoint.position, Quaternion.identity);
        }
    }

    private void SpawnHatReward()
    {
        if (hatBoxPrefab != null || spawnPoint != null)
        {
            GameObject prefab = Instantiate(hatBoxPrefab, spawnPoint.position, Quaternion.identity);
        }
    }
}
