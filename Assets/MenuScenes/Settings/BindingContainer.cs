using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/* BindingContainer class
 * This class is responsible for handling an individual instance of a binding listing.
 */

public class BindingContainer : MonoBehaviour
{
    //Defined in Unity editor.
    public Text bindingName;
    public Button rebindButton;
    public Text bindingValue;
    //Defined at runtime by SettingsInput when that script creates an instance of BindingContainer.
    public InputActionMap map;
    public SettingsInput inputScript;
    public int myIndex; //The index into the Bindings list that this instance of the prefab refers to.

    public InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    //When the button is clicked, rebind it.
    public void OnClick()
    {
        InputAction actionRef = map.actions[myIndex];

        rebindingOperation = actionRef.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindComplete())
            .Start();
    }

    //Called back by the rebinding operation once it finishes.
    public void RebindComplete()
    {
        rebindingOperation.Dispose();

        bindingValue.text = map.actions[myIndex].bindings[0].ToDisplayString();
    }    
}
