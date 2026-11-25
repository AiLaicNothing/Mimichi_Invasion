using System;
using System.Collections;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.Networking;

public class InsertAccount_Controller : MonoBehaviour
{
    private const string url = "http://localhost/Progra26-2-Promedio2/InsertScore.php";

    public void SendRequest(string _username, string _password, string _nickname, Action<bool> callback)
    {
        StartCoroutine(Send(_username, _password, _nickname, callback));
    }

    private IEnumerator Send(string _username, string _password, string _nickname, Action<bool> callback)
    {
        WWWForm form = new WWWForm();

        form.AddField("_username", _username);
        form.AddField("_password", _password);
        form.AddField("_nickname", _nickname);

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
