using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Wave_Manager : MonoBehaviour
{
    public static Wave_Manager Instance;
    private int currentWave;
    private int enemiesToSpawn;

    [SerializeField] private GameObject[] enemiesPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    public Action onWaveComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartWave()
    {
        StartCoroutine(HandleWave());
    }

    private IEnumerator HandleWave()
    {
        yield return null;
    }

    private void EndOfWave()
    {
        onWaveComplete?.Invoke();
    }
}
