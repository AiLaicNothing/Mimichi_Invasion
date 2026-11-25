using UnityEngine;
using UnityEngine.UI;

public class Scoreboard_View : MonoBehaviour
{
    [SerializeField] private Button scoreboard_Button;
    [SerializeField] private Button historial_Button;

    private Scoreboard_Controller scoreboard_Controller;

    private void Awake()
    {
        scoreboard_Controller = GetComponent<Scoreboard_Controller>();
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }
}
