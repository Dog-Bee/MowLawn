using System;
using System.Threading.Tasks;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameState : MonoBehaviour
{
    public static Action GameEndedEvent;

    [SerializeField] private GameObject startCamera;
    [SerializeField] private CinemachineVirtualCamera finishCamera;
    [SerializeField] private GameObject playerGO;
    [SerializeField] private GameObject joystickGO;

    private void OnEnable()
    {
        GameEndedEvent += OnGameEnded;
    }

    private void OnDisable()
    {
        GameEndedEvent -= OnGameEnded;
    }

    private void Start()
    {
        finishCamera.gameObject.SetActive(false);
    }


    private void OnGameEnded()
    {
        startCamera.SetActive(false);
        finishCamera.gameObject.SetActive(true);
        finishCamera.Priority = 100;
        playerGO.SetActive(false);
        joystickGO.SetActive(false);
        
        ReloadAfterDelay();
    }


    private async void ReloadAfterDelay()
    {
        await Task.Delay(6000);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
}
