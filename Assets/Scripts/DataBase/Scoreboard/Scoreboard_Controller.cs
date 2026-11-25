using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Scoreboard_Controller : MonoBehaviour
{
    private const string top100Url = "http://localhost/Progra26-2-Promedio2/ShowTop10.php";

    private const string showHistory = "http://localhost/Progra26-2-Promedio2/ShowWinners.php";

    public void GetTop10(Action<ScoreArrayModel> callback)
    {
        StartCoroutine(Send(top100Url, callback));
    }

    public void GetWinners(Action<ScoreArrayModel> callback)
    {
        StartCoroutine(Send(showHistory, callback));
    }

    private IEnumerator Send(string url, Action<ScoreArrayModel> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ScoreArrayModel result = JsonUtility.FromJson<ScoreArrayModel>(www.downloadHandler.text);

                callback?.Invoke(result);
            }
            else
            {
                Debug.LogError("Error: " + www.error);
                callback?.Invoke(null);
            }
        }
    }
}
