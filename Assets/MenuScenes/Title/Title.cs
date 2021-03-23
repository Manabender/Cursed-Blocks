﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/* Title class
 * This class has the extremely simple job of waiting for a button click on the title screen, and loading the main menu when that click is found.
 */

public class Title : MonoBehaviour
{
    public void OnClick()
    {
        SceneManager.LoadScene("MenuScenes/Main/MainMenu");
    }
}
