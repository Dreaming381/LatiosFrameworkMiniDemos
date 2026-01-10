using UnityEditor;
using UnityEditor.SceneManagement;


// Fix for Unity loading Untitled Scene the first time
// a project is opened

[InitializeOnLoad]
public class LoadMainScene
{
    static LoadMainScene()
    {
        EditorApplication.update += LoadDefaultScene;
    }

    static void LoadDefaultScene()
    {
        EditorApplication.update -= LoadDefaultScene;

        if (EditorSceneManager.GetActiveScene().path == "")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/AccretionDiskSample.unity");
        }
    }
}