using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/* MainMenuInput class
 * This class has the simple job of handling input for the ButtonBack prefab. It simply loads an editor-defined scene, which defaults to the main menu.
 */

public class BackButton : MonoBehaviour
{
    public string sceneToLoad;

    public void OnClick()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
