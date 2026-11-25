using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InsertScore_Controller : MonoBehaviour
{
    private const string url = "http://localhost/Progra26-2-Promedio2/InsertScore.php";

    public void SendRequest(int _weapon_ID, int _wavesSurvived, int _killCount, int _Player_ID, Action<bool> callback)
    {
        StartCoroutine(Send(_weapon_ID, _wavesSurvived, _killCount, _Player_ID, callback));
    }

    private IEnumerator Send(int _weapon_ID, int _wavesSurvived, int _killCount, int _Player_ID, Action<bool> callback)
    {
        WWWForm form = new WWWForm();

        // get from info when defeat or won


        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            bool success = www.result == UnityWebRequest.Result.Success;

            if (success)
            {
                Debug.Log("Se insert� con �xito");
            }
            else
            {
                Debug.LogError("Error: " + www.error + " " + www.downloadHandler.text);
            }

            callback?.Invoke(success);
        }
    }
}
}
