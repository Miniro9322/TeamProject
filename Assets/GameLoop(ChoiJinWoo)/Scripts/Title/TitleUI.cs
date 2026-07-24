using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    public void OnStart()
    {
        SceneManager.LoadScene("Map_Test5");
    }

    public void OnUpgrade()
    {

    }

    public void OnSetting()
    {

    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }
}
