using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class MysteryBox : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colourblind;

    [SerializeField] KMSelectable Box;
    [SerializeField] GameObject Hinge;
    [SerializeField] Light BoxLight;
    [SerializeField] TextMesh ColourblindText;
    [SerializeField] TextMesh WeaponText;
    [SerializeField] GameObject Weapon;
    [SerializeField] MeshRenderer WeaponRenderer;
    [SerializeField] List<Material> WeaponMaterials;
    [SerializeField] List<Material> WonderWeaponMaterials;
    [SerializeField] GameObject SolveSprite;
    [SerializeField] AudioClip StartupSound;
    [SerializeField] AudioClip MusicBox;
    [SerializeField] AudioClip SolveSound;
    [SerializeField] AudioClip OpenSound;
    [SerializeField] AudioClip CloseSound;
    [SerializeField] AudioClip EquipSound;
    [SerializeField] List<AudioClip> StrikeSounds;
    

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    Color[] lightColours = new Color[10] { Color.red, Color.blue, new Color(1, 1, 0), new Color(0.5f, 0, 1), new Color(1, 0.5f, 0), Color.green, Color.cyan, Color.magenta, Color.black, Color.white };
    string[] colourNames = new string[10] { "Red", "Blue", "Yellow", "Purple", "Orange", "Green", "Cyan", "Magenta", "Black", "White" };
    int[,] xTable = new int[10, 10]
    {
        { -1, 1, 83, 63, 52, 78, 27, 17, 58, 44 },
        { 64, -1, 47, 71, 21, 12, 28, 85, 7, 25 },
        { 91, 57, -1, 76, 34, 62, 3, 38, 15, 66 },
        { 26, 92, 72, -1, 33, 84, 65, 79, 32, 39 },
        { 95, 22, 43, 93, -1, 41, 53, 23, 5, 9 },
        { 6, 96, 14, 73, 31, -1, 42, 86, 35, 59 },
        { 97, 4, 68, 36, 67, 29, -1, 51, 18, 69 },
        { 48, 49, 45, 19, 54, 89, 74, -1, 81, 13 },
        { 37, 99, 11, 94, 55, 16, 87, 56, -1, 46 },
        { 75, 24, 98, 61, 2, 88, 83, 8, 82, -1 },
    };
    int[,] weaponsTable = new int[10, 10]
    {
        { 33, 67, 69, 12, 74, 59, 43, 9, 15, 95 },
        { 78, 98, 72, 76, 21, 41, 40, 17, 11, 3 },
        { 45, 92, 52, 39, 8, 16, 2, 19, 31, 37 },
        { 49, 93, 91, 56, 63, 29, 18, 47, 44, 36 },
        { 70, 96, 83, 75, 42, 38, 6, 51, 81, 14 },
        { 1, 28, 87, 10, 88, 48, 20, 66, 85, 27 },
        { 0, 24, 71, 55, 25, 77, 79, 80, 90, 35 },
        { 97, 50, 53, 5, 64, 7, 82, 58, 61, 32 },
        { 86, 46, 94, 26, 22, 23, 84, 62, 57, 34 },
        { 54, 60, 89, 73, 30, 68, 65, 99, 13, 4 }
    };

    int x;
    int boxColour;
    int goalWeapon;

    bool openedBox;
    int curWeapon = -1;
    string rolledWeaponName = "";
    int rolls = 0;
    int guaranteedRoll = 0;
    bool isAnimating;
    bool weaponAvailable;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += delegate () { Audio.PlaySoundAtTransform(StartupSound.name, transform); };
        Box.OnInteract += delegate () { OnBoxInteract(); return false; };
        Weapon.GetComponent<KMSelectable>().OnHighlight += delegate () { OnWeaponHL(); };
        Weapon.GetComponent<KMSelectable>().OnHighlightEnded += delegate () { WeaponText.gameObject.SetActive(false); };
        Weapon.GetComponent<KMSelectable>().OnInteract += delegate () { OnWeaponInteract(); return false; };
    }
    
    void OnBoxInteract()
    {
        if (ModuleSolved || isAnimating) return;
        Box.AddInteractionPunch();
        if (!openedBox)
        {
            Audio.PlaySoundAtTransform(OpenSound.name, transform);
            StartCoroutine("OpenBox");
            openedBox = true;
        }
        else StartCoroutine("CloseBox", false);
    }

    void OnWeaponHL()
    {
        if (ModuleSolved || isAnimating || !weaponAvailable) return;
        WeaponText.gameObject.SetActive(true);
        WeaponText.text = rolledWeaponName;
    }

    void OnWeaponInteract()
    {
        if (ModuleSolved || isAnimating || !weaponAvailable) return;
        Weapon.GetComponent<KMSelectable>().AddInteractionPunch();
        rolls = 0;
        if (curWeapon >= 100)
        {
            Audio.PlaySoundAtTransform(EquipSound.name, transform);
            GetComponent<KMBombModule>().HandlePass();
            Log(String.Format("Picked up {0}, which is a wonder weapon! Module solved!", rolledWeaponName));
            ModuleSolved = true;
            StartCoroutine("CloseBox");
        }
        else if (curWeapon == goalWeapon)
        {
            Audio.PlaySoundAtTransform(EquipSound.name, transform);
            GetComponent<KMBombModule>().HandlePass();
            Log(String.Format("Picked up {0}, which is the goal weapon. Module solved!", rolledWeaponName));
            ModuleSolved = true;
            StartCoroutine("CloseBox");
        }
        else StartCoroutine("Strike");
    }

    void Start()
    {
        BoxLight.range *= transform.lossyScale.x;
        BoxLight.gameObject.SetActive(false);
        boxColour = Rnd.Range(0, 10);
        BoxLight.color = lightColours[boxColour];
        ColourblindText.text = colourNames[boxColour];
        Log(String.Format("Generated Mystery Box colour is {0}.", colourNames[boxColour]));

        CalculateX();
        goalWeapon = weaponsTable[(int)Math.Floor((x - 1.0) / 10), boxColour];
        Log(String.Format("The goal weapon is the {0}.", WeaponMaterials[goalWeapon].name));
        guaranteedRoll = Rnd.Range(6, 9);
    }

    void CalculateX()
    {
        int batteries = Bomb.GetBatteryCount();
        int ports = Bomb.GetPortCount();
        int bHolders = Bomb.GetBatteryHolderCount();
        int pPlates = Bomb.GetPortPlateCount();
        int litInds = Bomb.GetOnIndicators().Count();
        int unlitInds = Bomb.GetOffIndicators().Count();
        bool lastEven = Bomb.GetSerialNumberNumbers().Last() % 2 == 0;

        int r = 0;
        int c = 0;
        string rowUsing = "";
        string colUsing = "";
        bool flag = false;

        if (batteries != ports)
        {
            r = !lastEven ? batteries : ports;
            c = !lastEven ? ports : batteries;
            r %= 10;
            c %= 10;
            x = xTable[r, c];
            rowUsing = !lastEven ? "Batteries" : "Ports";
            colUsing = !lastEven ? "Ports" : "Batteries";
        }
        else if (bHolders != pPlates)
        {
            r = !lastEven ? bHolders : pPlates;
            c = !lastEven ? pPlates : bHolders;
            r %= 10;
            c %= 10;
            x = xTable[r, c];
            rowUsing = !lastEven ? "Battery Holders" : "Port Plates";
            colUsing = !lastEven ? "Port Plates" : "Battery Holders";
        }
        else if (litInds != unlitInds)
        {
            r = !lastEven ? litInds : unlitInds;
            c = !lastEven ? unlitInds : litInds;
            r %= 10;
            c %= 10;
            x = xTable[r, c];
            rowUsing = !lastEven ? "Lit Indicators" : "Unlit Indicators";
            colUsing = !lastEven ? "Unlit Indicators" : "Lit Indicators";
        }
        else
        {
            flag = true;
            x = xTable[6, 9];
        }

        if (!flag) Log(String.Format("Using {0} as the row and {1} as the column: row {2}, column {3} results in X = {4}.", rowUsing, colUsing, r, c, x));
        else Log(String.Format("All edgework combinations are equal, row 6, column 9, results in X = {0}.", x));
    }

    void RollWeapon()
    {
        rolls++;
        weaponAvailable = true;
        isAnimating = false;
        if (Rnd.Range(0, 100) == 0)
        {
            int wonderIdx = Rnd.Range(0, 5);
            curWeapon = 100 + wonderIdx;
            WeaponRenderer.material = WonderWeaponMaterials[wonderIdx];
            rolledWeaponName = WonderWeaponMaterials[wonderIdx].name;
            Log(String.Format("Rolled the {0}, which is a wonder weapon. You can pick it up to immediately solve the module!", rolledWeaponName));
        }
        else
        {
            if (rolls % guaranteedRoll != 0)
            {
                int randomWeapon = Rnd.Range(0, WeaponMaterials.Count);
                while (randomWeapon == curWeapon) randomWeapon = Rnd.Range(0, WeaponMaterials.Count);
                curWeapon = randomWeapon;
            }
            else curWeapon = goalWeapon;
            WeaponRenderer.material = WeaponMaterials[curWeapon];
            rolledWeaponName = WeaponMaterials[curWeapon].name;
            if (rolledWeaponName == "357 Magnum" || rolledWeaponName == "410 Ironhide") rolledWeaponName = "." + rolledWeaponName;
            Log(String.Format("Rolled the {0}. {1}", rolledWeaponName, curWeapon == goalWeapon ? "This is the goal weapon, you should pick it up to solve the module." : "This is not the goal weapon, you should reroll it."));
        }
    }

    void Log(string arg)
    {
        Debug.Log($"[Mystery Box #{ModuleId}] {arg}");
    }

    IEnumerator OpenBox()
    {
        isAnimating = true;
        BoxLight.gameObject.SetActive(true);
        ColourblindText.gameObject.SetActive(Colourblind.ColorblindModeActive);
        Audio.PlaySoundAtTransform(MusicBox.name, transform);

        for (int i = 0; i < 35; i++)
        {
            Hinge.transform.Rotate(4, 0, 0);
            yield return null;
        }
        StartCoroutine("CycleWeapon");
        for (int i = 0; i < 100; i++)
        {
            Weapon.transform.localPosition += new Vector3(0, 0, -0.0000001f * i * i + 0.001f);
            yield return null;
        }
    }

    IEnumerator CloseBox()
    {
        isAnimating = true;
        BoxLight.gameObject.SetActive(false);
        ColourblindText.gameObject.SetActive(false);
        Weapon.gameObject.SetActive(!ModuleSolved);
        Audio.PlaySoundAtTransform(CloseSound.name, transform);

        for (int i = 0; i < 30; i++)
        {
            float magic = 0.001054f;
            Weapon.transform.localPosition -= new Vector3(0, 0, -magic * Mathf.Sqrt(i) + 5.5f * magic);
            yield return null;
        }
        Weapon.transform.localPosition = new Vector3(0, 0.0418f, -0.046f);
        for (int i = 0; i < 35; i++)
        {
            Hinge.transform.Rotate(-4, 0, 0);
            yield return null;
        }

        isAnimating = ModuleSolved;
        openedBox = false;
        if (ModuleSolved) StartCoroutine("SolveAnim");
    }

    IEnumerator SolveAnim()
    {
        Audio.PlaySoundAtTransform(SolveSound.name, transform);
        for (int i = 0; i < 275; i++)
        {
            Box.transform.localPosition += new Vector3(0, 0, 0.0002f);
            Box.transform.Rotate(Mathf.Min(10 + i/10, 30), 0, 0);
            yield return null;
        }
        for (int i = 0; i < 25; i++)
        {
            Box.transform.localPosition += new Vector3(0, 0, 0.05f);
            Box.transform.Rotate(10, 0, 0);
            yield return null;
        }
        Box.gameObject.SetActive(false);
        SolveSprite.SetActive(true);
        for (int i = 0; i < 30; i++)
        {
            SolveSprite.transform.localPosition += new Vector3(0, 0.0023f / 30, 0);
            yield return null;
        }
    }

    IEnumerator Strike()
    {
        weaponAvailable = false;
        AudioClip audio = new AudioClip();
        if (Rnd.Range(0, 10) == 0) audio = StrikeSounds[0];
        else
        {
            if (Rnd.Range(0, 5) == 0) audio = StrikeSounds[1];
            else audio = StrikeSounds[Rnd.Range(2, 6)];
        }
        Audio.PlaySoundAtTransform(audio.name, transform);
        yield return new WaitForSeconds(audio.length);

        GetComponent<KMBombModule>().HandleStrike();
        boxColour = Rnd.Range(0, 10);
        BoxLight.color = lightColours[boxColour];
        ColourblindText.text = colourNames[boxColour];
        goalWeapon = weaponsTable[(int)Math.Floor((x - 1.0) / 10), boxColour];
        guaranteedRoll = Rnd.Range(6, 9);
        Log(String.Format("Tried to pick up the {0}, but it wasn't the goal weapon. Strike! Regenerating the module...", rolledWeaponName));
        Log(String.Format("Mystery Box colour is now {0}, and the goal weapon is the {1}.", colourNames[boxColour], WeaponMaterials[goalWeapon].name));

        StartCoroutine("CloseBox");
    }

    IEnumerator CycleWeapon()
    {
        for (int i = 0; i < 19; i++)
        {
            int randomWeapon = Rnd.Range(0, WeaponMaterials.Count);
            while (randomWeapon == curWeapon) randomWeapon = Rnd.Range(0, WeaponMaterials.Count);
            curWeapon = randomWeapon;
            WeaponRenderer.material = WeaponMaterials[curWeapon];
            yield return new WaitForSeconds(Math.Min(0.5f, 0.02f * i));
        }
        RollWeapon();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} roll> to roll/reroll the weapon. Use <!{0} rollfocus> to focus on the module while rolling. Use <!{0} weapon> to pick the weapon up. Use <!{0} name> to shortly display the weapon's name. Use <!{0} cb> to toggle colourblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        switch (Command)
        {
            case "roll":
            case "rollfocus":
                yield return null;
                if (weaponAvailable)
                {
                    Box.OnInteract();
                    while (openedBox) yield return null;
                }
                Box.OnInteract();
                if (Command == "rollfocus")
                {
                    while (!weaponAvailable) yield return null;
                    yield return new WaitForSeconds(2);
                }
                break;
            case "weapon":
                if (!weaponAvailable)
                {
                    yield return "sendtochatmessage You have to roll the weapon first!";
                    break;
                }
                yield return null;
                Weapon.GetComponent<KMSelectable>().OnInteract();
                break;
            case "name":
                if (!weaponAvailable)
                {
                    yield return "sendtochatmessage You have to roll the weapon first!";
                    break;
                }
                yield return null;
                Weapon.GetComponent<KMSelectable>().OnHighlight();
                yield return new WaitForSeconds(3);
                Weapon.GetComponent<KMSelectable>().OnHighlightEnded();
                yield return null;
                break;
            case "cb":
                yield return null;
                ColourblindText.gameObject.SetActive(!ColourblindText.gameObject.activeInHierarchy);
                break;
            default:
                yield return "sendtochatmessage Invalid command!";
                break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        rolls = guaranteedRoll - 1;
        while (!ModuleSolved)
        {
            if (weaponAvailable)
            {
                if (curWeapon >= 100 || curWeapon == goalWeapon)
                {
                    yield return null;
                    Weapon.GetComponent<KMSelectable>().OnInteract();
                    break;
                }
                else
                {
                    Box.OnInteract();
                    while (openedBox) yield return null;
                }
            }
            else
            {
                Box.OnInteract();
                while (!weaponAvailable) yield return null;
            }
        }
    }
}
