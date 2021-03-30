using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/* ExtraModesInput class
 * This class handles input on the settings menu. It also sets up a list of keybinds.
 */

public class SettingsInput : MonoBehaviour
{
    public PlayerInput ref_playerInput;
    public GameObject ref_bindingPrefab;
    public InputActionMap map; //Just an abbreviation of playerInput.actions.actionMaps[0]
    public Text ref_dasText;
    public Text ref_arrText;
    public Text ref_sdgText;
    public Text ref_rotatedcdText;
    public Text ref_harddropdcdText;
    public Text ref_holddcdText;
    public Toggle ref_interruptdasToggle;
    public Toggle ref_mirrormonoToggle;

    public int das;
    public int arr;
    public float sdg;
    public int rotateDcd;
    public int hardDropDcd;
    public int holdDcd;
    public bool interruptDas;
    public bool mirrorMonoRotation;

    public const int DAS_MAX = 250;
    public const int ARR_MAX = 50;
    public const float SDG_MIN = 10f;
    public const float SDG_MAX = 20000f;
    public const int DCD_MAX = 50;

    // Start is called before the first frame update
    void Start()
    {
        map = ref_playerInput.actions.actionMaps[0];
        //Read player's bindings from prefs, or, if they don't exist, create defaults.
        ReadPlayerPrefs();
        //Initialize binding list
        int numBinds = ref_playerInput.actions.actionMaps[0].actions.Count;
        for (int i = 0; i < numBinds; i++)
        {
            GameObject bindingListing = Instantiate(ref_bindingPrefab, transform);
            BindingContainer bindingContainer = bindingListing.GetComponent<BindingContainer>();
            bindingContainer.myIndex = i;
            bindingContainer.bindingName.text = map.actions[i].name;
            bindingContainer.bindingValue.text = map.actions[i].bindings[0].ToDisplayString();
            bindingContainer.map = map;
            bindingContainer.inputScript = this;
        }
        //Initialize setting list
        UpdateDasText();
        UpdateArrText();
        UpdateSdgText();
        UpdateRotateDcdText();
        UpdateHarddropDcdText();
        UpdateHoldDcdText();
        if (interruptDas)
        {
            ref_interruptdasToggle.isOn = true;
        }
        else
        {
            ref_interruptdasToggle.isOn = false;
        }
        if (mirrorMonoRotation)
        {
            ref_mirrormonoToggle.isOn = true;
        }
        else
        {
            ref_mirrormonoToggle.isOn = false;
        }
    }
    
    //This method reads PlayerPrefs. If a Pref doesn't exist, it is created.
    public void ReadPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey("DAS")) { PlayerPrefs.SetInt("DAS", 90); }
        das = PlayerPrefs.GetInt("DAS");

        if (!PlayerPrefs.HasKey("ARR")) { PlayerPrefs.SetInt("ARR", 15); }
        arr = PlayerPrefs.GetInt("ARR");

        if (!PlayerPrefs.HasKey("SDG")) { PlayerPrefs.SetFloat("SDG", 20.0f); }
        sdg = PlayerPrefs.GetFloat("SDG");

        if (!PlayerPrefs.HasKey("rotateDCD")) { PlayerPrefs.SetInt("rotateDCD", 0); }
        rotateDcd = PlayerPrefs.GetInt("rotateDCD");

        if (!PlayerPrefs.HasKey("harddropDCD")) { PlayerPrefs.SetInt("harddropDCD", 0); }
        hardDropDcd = PlayerPrefs.GetInt("harddropDCD");

        if (!PlayerPrefs.HasKey("holdDCD")) { PlayerPrefs.SetInt("holdDCD", 0); }
        holdDcd = PlayerPrefs.GetInt("holdDCD");

        if (!PlayerPrefs.HasKey("interruptDAS")) { PlayerPrefs.SetInt("interruptDAS", 0); }
        interruptDas = PlayerPrefs.GetInt("interruptDAS") == 1;

        if (!PlayerPrefs.HasKey("mirrorMono")) { PlayerPrefs.SetInt("mirrorMono", 0); }
        mirrorMonoRotation = PlayerPrefs.GetInt("mirrorMono") == 1;

        if (!PlayerPrefs.HasKey("keyLeft")) { PlayerPrefs.SetString("keyLeft", map.FindAction("Shift left").bindings[0].effectivePath); }
        map.FindAction("Shift left").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyLeft"));

        if (!PlayerPrefs.HasKey("keyRight")) { PlayerPrefs.SetString("keyRight", map.FindAction("Shift right").bindings[0].effectivePath); }
        map.FindAction("Shift right").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyRight"));

        if (!PlayerPrefs.HasKey("keySoftDrop")) { PlayerPrefs.SetString("keySoftDrop", map.FindAction("Soft drop").bindings[0].effectivePath); }
        map.FindAction("Soft drop").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keySoftDrop"));

        if (!PlayerPrefs.HasKey("keyHardDrop")) { PlayerPrefs.SetString("keyHardDrop", map.FindAction("Hard drop").bindings[0].effectivePath); }
        map.FindAction("Hard drop").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyHardDrop"));

        if (!PlayerPrefs.HasKey("keyCW")) { PlayerPrefs.SetString("keyCW", map.FindAction("Rotate CW").bindings[0].effectivePath); }
        map.FindAction("Rotate CW").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyCW"));

        if (!PlayerPrefs.HasKey("keyCCW")) { PlayerPrefs.SetString("keyCCW", map.FindAction("Rotate CCW").bindings[0].effectivePath); }
        map.FindAction("Rotate CCW").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyCCW"));

        if (!PlayerPrefs.HasKey("key180")) { PlayerPrefs.SetString("key180", map.FindAction("Rotate 180").bindings[0].effectivePath); }
        map.FindAction("Rotate 180").ChangeBinding(0).WithPath(PlayerPrefs.GetString("key180"));

        if (!PlayerPrefs.HasKey("keyHold")) { PlayerPrefs.SetString("keyHold", map.FindAction("Hold").bindings[0].effectivePath); }
        map.FindAction("Hold").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyHold"));

        if (!PlayerPrefs.HasKey("keyPause")) { PlayerPrefs.SetString("keyPause", map.FindAction("Pause").bindings[0].effectivePath); }
        map.FindAction("Pause").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyPause"));

        if (!PlayerPrefs.HasKey("keyReset")) { PlayerPrefs.SetString("keyReset", map.FindAction("Reset").bindings[0].effectivePath); }        
        map.FindAction("Reset").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyReset"));

        PlayerPrefs.Save();
    }

    public void UpdateDasText()
    {
        string text = (das * 2).ToString() + " ms";
        ref_dasText.text = text;
    }

    public void UpdateArrText()
    {
        string text = (arr * 2).ToString() + " ms";
        ref_arrText.text = text;
    }

    public void UpdateSdgText()
    {
        string text = sdg.ToString() + " minoes/s";
        ref_sdgText.text = text;
    }

    public void UpdateRotateDcdText()
    {
        string text = (rotateDcd * 2).ToString() + " ms";
        ref_rotatedcdText.text = text;
    }

    public void UpdateHarddropDcdText()
    {
        string text = (hardDropDcd * 2).ToString() + " ms";
        ref_harddropdcdText.text = text;
    }

    public void UpdateHoldDcdText()
    {
        string text = (holdDcd * 2).ToString() + " ms";
        ref_holddcdText.text = text;
    }

    //I just KNOW there has to be a better way to do this...
    //Note that the DISPLAYED value for das, arr, and all dcd's is in milliseconds, but the INTERNAL value is in fixedupdate's. Fixedupdates are at 500 Hz, so +2 ms is +1 fixedupdate.
    /*public void DasDownBig() { das = Mathf.Max(das - 10, 0); UpdateDasText(); }
    public void DasDown() { das = Mathf.Max(das - 1, 0); UpdateDasText(); }
    public void DasUp() { das = Mathf.Min(das + 1, DAS_MAX); UpdateDasText(); }
    public void DasUpBig() { das = Mathf.Min(das + 10, DAS_MAX); UpdateDasText(); }
    public void ArrDownBig() { arr = Mathf.Max(arr - 10, 0); UpdateArrText(); }
    public void ArrDown() { arr = Mathf.Max(arr - 1, 0); UpdateArrText(); }
    public void ArrUp() { arr = Mathf.Min(arr + 1, ARR_MAX); UpdateArrText(); }
    public void ArrUpBig() { arr = Mathf.Min(arr + 10, ARR_MAX); UpdateArrText(); }
    public void SdgDownBig() { sdg = Mathf.Max(sdg - 20f, SDG_MIN); UpdateSdgText(); }
    public void SdgDown() { sdg = Mathf.Max(sdg - 2f, SDG_MIN); UpdateSdgText(); }
    public void SdgUp() { sdg = Mathf.Min(sdg + 2f, SDG_MAX); UpdateSdgText(); }
    public void SdgUpBig() { sdg = Mathf.Min(sdg + 20f, SDG_MAX); UpdateSdgText(); }
    public void SdgMin() { sdg = SDG_MIN; UpdateSdgText(); }
    public void SdgMax() { sdg = SDG_MAX; UpdateSdgText(); }
    public void RotateDcdDownBig() { rotateDcd = Mathf.Max(rotateDcd - 10, 0); UpdateRotateDcdText(); }
    public void RotateDcdDown() { rotateDcd = Mathf.Max(rotateDcd - 1, 0); UpdateRotateDcdText(); }
    public void RotateDcdUp() { rotateDcd = Mathf.Min(rotateDcd + 1, DCD_MAX); UpdateRotateDcdText(); }
    public void RotateDcdUpBig() { rotateDcd = Mathf.Min(rotateDcd + 10, DCD_MAX); UpdateRotateDcdText(); }
    public void HarddropDcdDownBig() { hardDropDcd = Mathf.Max(hardDropDcd - 10, 0); UpdateHarddropDcdText(); }
    public void HarddropDcdDown() { hardDropDcd = Mathf.Max(hardDropDcd - 1, 0); UpdateHarddropDcdText(); }
    public void HarddropDcdUp() { hardDropDcd = Mathf.Min(hardDropDcd + 1, DCD_MAX); UpdateHarddropDcdText(); }
    public void HarddropDcdUpBig() { hardDropDcd = Mathf.Min(hardDropDcd + 10, DCD_MAX); UpdateHarddropDcdText(); }
    public void HoldDcdDownBig() { holdDcd = Mathf.Max(holdDcd - 10, 0); UpdateHoldDcdText(); }
    public void HoldDcdDown() { holdDcd = Mathf.Max(holdDcd - 1, 0); UpdateHoldDcdText(); }
    public void HoldDcdUp() { holdDcd = Mathf.Min(holdDcd + 1, DCD_MAX); UpdateHoldDcdText(); }
    public void HoldDcdUpBig() { holdDcd = Mathf.Min(holdDcd + 10, DCD_MAX); UpdateHoldDcdText(); }*/

    //I was right, there is a better way.
    public void ChangeDas(int change)
    {
        das = Mathf.Clamp(das + change, 0, DAS_MAX);
        UpdateDasText();
    }    
    public void ChangeArr(int change)
    {
        arr = Mathf.Clamp(arr + change, 0, ARR_MAX);
        UpdateArrText();
    }    
    public void ChangeSdg(float change)
    {
        sdg = Mathf.Clamp(sdg + change, SDG_MIN, SDG_MAX);
        UpdateSdgText();
    }    
    public void ChangeRotateDcd(int change)
    {
        rotateDcd = Mathf.Clamp(rotateDcd + change, 0, DCD_MAX);
        UpdateRotateDcdText();
    }    
    public void ChangeHarddropDcd(int change)
    {
        hardDropDcd = Mathf.Clamp(hardDropDcd + change, 0, DCD_MAX);
        UpdateHarddropDcdText();
    }    
    public void ChangeHoldDcd(int change)
    {
        holdDcd = Mathf.Clamp(holdDcd + change, 0, DCD_MAX);
        UpdateHoldDcdText();
    }
    public void SdgMin() 
    {
        sdg = SDG_MIN;
        UpdateSdgText();
    }
    public void SdgMax() 
    {
        sdg = SDG_MAX;
        UpdateSdgText();
    }

    public void InterruptDasClicked(bool newValue)
    {
        interruptDas = newValue;
    }

    public void MirrorMonoClicked(bool newValue)
    {
        mirrorMonoRotation = newValue;
    }

    public void ApplyChanges()
    {
        PlayerPrefs.SetInt("DAS", das);
        PlayerPrefs.SetInt("ARR", arr);
        PlayerPrefs.SetFloat("SDG", sdg);
        PlayerPrefs.SetInt("rotateDCD", rotateDcd);
        PlayerPrefs.SetInt("harddropDCD", hardDropDcd);
        PlayerPrefs.SetInt("holdDCD", holdDcd);
        PlayerPrefs.SetInt("interruptDAS", interruptDas ? 1 : 0);
        PlayerPrefs.SetInt("mirrorMono", mirrorMonoRotation ? 1 : 0);
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
    }
}
