using System;
using UnityEngine;

public class Game_Manager : MonoBehaviour
{
    public static Game_Manager Instance;

    public Action onVictory;
    public Action onDefeat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OnVictory()
    {
        onVictory?.Invoke();
    }

    public void OnDefeat()
    {
        onDefeat?.Invoke(); 
    }
}
