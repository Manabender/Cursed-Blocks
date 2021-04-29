using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* InfoViewerInput class
 * This class has the relatively simple job of handling mouse input on the info viewer screens. To be honest, it's a glorified sound player.
 */

public class InfoViewerInput : MonoBehaviour
{
    public void OnClick()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
    }

    public void OnHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }
}
