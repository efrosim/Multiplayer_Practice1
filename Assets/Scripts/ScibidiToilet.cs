using Unity.Netcode;
using UnityEngine;

public class ScibidiToilet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("host started");
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("client started");
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("shutdown");
        }
    }
}
