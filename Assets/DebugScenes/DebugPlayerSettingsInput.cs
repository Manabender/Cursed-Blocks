using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/* DebugRootInput class
 * This class has only one job; listen for input on the forced curses debug menu and update pVars appropriately.
 */

public class DebugPlayerSettingsInput : MonoBehaviour
{
    public PlayerInput ref_PlayerInput;

    public int das;
    public int arr;
    public float sdg;
    public int rotateDCD;
    public int harddropDCD;
    public int holdDCD;
    public bool DASinterruption;
    public Text keybindsText;
    public Text rebindingText;
    public Text handlingText;
    public Text selectionCursorText;

    public int selection = 0;
    public int rebindIndex;
    public int totalBindings;
    public InputActionMap map;

    public InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    // Start is called before the first frame update
    void Start()
    {
        das = PlayerPrefs.GetInt("DAS");
        arr = PlayerPrefs.GetInt("ARR");
        sdg = PlayerPrefs.GetFloat("SDG");
        rotateDCD = PlayerPrefs.GetInt("rotateDCD");
        harddropDCD = PlayerPrefs.GetInt("harddropDCD");
        holdDCD = PlayerPrefs.GetInt("holdDCD");
        DASinterruption = PlayerPrefs.GetInt("interruptDAS") == 1; //A somewhat odd way of typcasting an int to a bool...
        //Set player keybinds into a mapping. This mapping's actions aren't linked to any method calls; it's just used as a way to store bindings.
        map = ref_PlayerInput.actions.FindActionMap("BindStorage");
        map.FindAction("Shift left").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyLeft"));
        map.FindAction("Shift right").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyRight"));
        map.FindAction("Soft drop").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keySoftDrop"));
        map.FindAction("Hard drop").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyHardDrop"));
        map.FindAction("Rotate CW").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyCW"));
        map.FindAction("Rotate CCW").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyCCW"));
        map.FindAction("Rotate 180").ChangeBinding(0).WithPath(PlayerPrefs.GetString("key180"));
        map.FindAction("Hold").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyHold"));
        map.FindAction("Pause").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyPause"));
        map.FindAction("Reset").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyReset"));
        Debug.Log(map.actions[0].bindings[0].path);
        totalBindings = map.actions.Count;
    }

    // Update is called once per frame
    void Update()
    {
        //Right side
        handlingText.text = (2*das) + "\n" + (2*arr) + "\n" + sdg + "\n" + (2*rotateDCD) + "\n" + (2*harddropDCD) + "\n" + (2*holdDCD) + "\n\n" + (DASinterruption ? "YES" : "NO");
        string selCursorText = "";
        for (int i = 0; i < selection; i++)
        {
            selCursorText += "\n";
        }
        selCursorText += "<--";
        selectionCursorText.text = selCursorText;

        //Left side
        string bindings = "";
        for (int i = 0; i < totalBindings; i++)
        {
            bindings += map.actions[i].bindings[0].ToDisplayString();
            bindings += "\n";
        }
        keybindsText.text = bindings;
    }

    public void RebindOneKey(int index)
    {
        InputAction actionRef = map.actions[index];

        rebindingText.text = "PRESS KEY FOR: " + actionRef.name;

        rebindingOperation = actionRef.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindComplete())
            .Start();
    }

    public void RebindComplete()
    {
        rebindingOperation.Dispose();
        rebindIndex++;
        if (rebindIndex < totalBindings)
        {
            RebindOneKey(rebindIndex);
        }
        else
        {
            rebindingText.text = "";
            ref_PlayerInput.SwitchCurrentActionMap("PlayerSettings");
        }
    }

    public void OnRebind(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            rebindIndex = 0; //The index into the mapping that is currently being bound.
            ref_PlayerInput.SwitchCurrentActionMap("Rebinding"); //Switch to action map with nothing linked to any method calls, so no other inputs are processed while rebinding.
            RebindOneKey(rebindIndex);
        }
    }

    public void OnSelectionUp(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            selection--;
            if (selection <= -1)
            {
                selection += 6;
            }
        }
    }

    public void OnSelectionDown(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            selection++;
            if (selection >= 6)
            {
                selection -= 6;
            }
        }
    }

    public void OnIncrement(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            ChangeSelectedSetting(1);
        }
    }

    public void OnDecrement(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            ChangeSelectedSetting(-1);
        }
    }

    public void OnIncrementLarge(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            ChangeSelectedSetting(10);
        }
    }

    public void OnDecrementLarge(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            ChangeSelectedSetting(-10);
        }
    }

    public void OnToggleDasInterruption(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            DASinterruption = !DASinterruption;
        }
    }

    public void OnSDGLow(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            sdg = 20.0f;
        }
    }

    public void OnSDGHigh(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            sdg = 11000.0f;
        }
    }

    public void OnReturnToDebugRoot(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            //Save handling settings
            PlayerPrefs.SetInt("DAS", das);
            PlayerPrefs.SetInt("ARR", arr);
            PlayerPrefs.SetFloat("SDG", sdg);
            PlayerPrefs.SetInt("rotateDCD", rotateDCD);
            PlayerPrefs.SetInt("harddropDCD", harddropDCD);
            PlayerPrefs.SetInt("holdDCD", holdDCD);
            PlayerPrefs.SetInt("interruptDAS", DASinterruption ? 1 : 0) ; //A somewhat odd way of typcasting an int to a bool...            
            //Save keybinds
            PlayerPrefs.SetString("keyLeft", map.FindAction("Shift left").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyRight", map.FindAction("Shift right").bindings[0].effectivePath);
            PlayerPrefs.SetString("keySoftDrop", map.FindAction("Soft drop").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyHardDrop", map.FindAction("Hard drop").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyCW", map.FindAction("Rotate CW").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyCCW", map.FindAction("Rotate CCW").bindings[0].effectivePath);
            PlayerPrefs.SetString("key180", map.FindAction("Rotate 180").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyHold", map.FindAction("Hold").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyPause", map.FindAction("Pause").bindings[0].effectivePath);
            PlayerPrefs.SetString("keyReset", map.FindAction("Reset").bindings[0].effectivePath);
            PlayerPrefs.Save();
            //Load scene
            SceneManager.LoadScene("DebugScenes/DebugRoot");
        }
    }

    public void ChangeSelectedSetting(int delta)
    {
        if (selection == 0)
        {
            das = Mathf.Max(0, das + delta);
        }
        else if (selection == 1)
        {
            arr = Mathf.Max(0, arr + delta);
        }
        else if (selection == 2)
        {
            sdg = Mathf.Max(20.0f, sdg + 2 * delta);
        }
        else if (selection == 3)
        {
            rotateDCD = Mathf.Max(0, rotateDCD + delta);
        }
        else if (selection == 4)
        {
            harddropDCD = Mathf.Max(0, harddropDCD + delta);
        }
        else //if (selection == 5)
        {
            holdDCD = Mathf.Max(0, holdDCD + delta);
        }
    }
}
