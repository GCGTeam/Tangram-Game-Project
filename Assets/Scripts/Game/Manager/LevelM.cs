﻿/* =====================================================================
 * Tristan Herpich - 2020 - Tangram Project
======================================================================== */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelM : MonoBehaviour
{
    public List<TextAsset> LevelList = new List<TextAsset>();
    public static Level CurrentLevel = null;

    public GameObject LevelFormContainer;
    public GameObject FormFrame;

    static public LevelM instance;
    private static Texture2D Empty, TriangleDL, TriangleDR, TriangleTL, TriangleTR, Square;
    public Image LevelShadow1;
    public Image LevelShadow2;
    public static Sprite LevelSprite;

    //For Calculating Level Size
    private static int minx, miny, maxx, maxy;
    public static float LevelX, LevelY;
    public static int LevelWUneven, LevelHUneven;

    public static List<FormController> FormsInLevel = new List<FormController>();
    public static Form LevelTilesShouldBe; //How it should be
    public static Form LevelTilesCurrent;  //What it is now

    public float LevelScale;
    public static bool Init = false;
   // bool wasInBottom = true;

    public static int countForms;
    public static int correctForms;
    public static bool LevelLoad = false;


    private void Start()
    {
        instance = this;

        //Loading Textures for Form
        Empty = Resources.Load<Texture2D>("FormEditor/Shape_Empty");
        TriangleDL = Resources.Load<Texture2D>("FormEditor/Shape_TriangleDL");
        TriangleDR = Resources.Load<Texture2D>("FormEditor/Shape_TriangleDR");
        TriangleTL = Resources.Load<Texture2D>("FormEditor/Shape_TriangleTL");
        TriangleTR = Resources.Load<Texture2D>("FormEditor/Shape_TriangleTR");
        Square = Resources.Load<Texture2D>("FormEditor/Shape_Square");

        LevelTilesShouldBe = ScriptableObject.CreateInstance<Form>();
        LevelTilesShouldBe.Resize(9, 12);
      //  LoadLevel(1);
        LevelTilesCurrent = ScriptableObject.CreateInstance<Form>();
        LevelTilesCurrent.Resize(9, 12);

       Init = true;
    }

    static void SetLevelSize()
    {
        int LevelWidth = maxx - minx;
        int LevelHeight = maxy - miny;

        RectTransform shadow = instance.LevelShadow1.GetComponent<RectTransform>();
        LevelX = (9 - LevelWidth)  * 32 - minx*64;
        LevelY = 235 - (12 - LevelHeight) * 32 +     miny*64;

        LevelWUneven = 0;
        LevelHUneven = 0;
        if (LevelWidth % 2 == 1) LevelWUneven = 1;
        if (LevelHeight % 2 == 1) LevelHUneven = 1;
        if (LevelWUneven == 1) LevelX += 32;
        if (LevelHUneven == 1) LevelY += 32;

        shadow.anchoredPosition = new Vector2(LevelX, LevelY);
        shadow = instance.LevelShadow2.GetComponent<RectTransform>();
        shadow.anchoredPosition = new Vector2(LevelX,LevelY);

        

    }

    static public void NewL()
    {
        instance.Start();
    }

    public static bool LoadLevel(int Number, bool AutoGenerate = true)
    {
        if (Number < 0 || Number >= instance.LevelList.Count) return false;

        CurrentLevel = new Level();




        string jsonformatted = instance.LevelList[Number].text;

        if (jsonformatted.Length != 0)
        {
            for (int x = 0; x < jsonformatted.Split('\n').Length - 1; x++)
            {
               // Debug.Log(x);
                string jsontile = jsonformatted.Split('\n')[x];
                if (jsontile.Trim().Length != 0)
                {
                    LevelData dataCache = JsonUtility.FromJson<LevelData>(jsontile);
                    CurrentLevel.Data.Add(dataCache);
                }
            }

            LevelSprite = GenerateLevelSprite();
            instance.LevelShadow1.sprite = LevelSprite;
            instance.LevelShadow2.sprite = LevelSprite;
            SetLevelSize(); //To Center the Level, does not work yet
            LevelLoad = true;

            if (AutoGenerate == true) return Generate();
            return true;
        }
        

        return false;
    }


    static bool Generate()
    {
        if (CurrentLevel.Data != null)
        {
            int CountX =  40;
            int CountY = -40;

            foreach (LevelData ld in CurrentLevel.Data)
            {
                FormController Cache = instance.SpawnForm(ld.FormName);
                if (Cache != null && ld != null)
                {
                    Cache.xpos = ld.xinbottom *2;
                    Cache.ypos = ld.yinbottom *2;
                    instance.StartCoroutine(instance.waitForFormLoad(Cache, ld));
                }

                CountX += 100;
                if (CountX > 300)
                {
                    CountY -= 100;
                    CountX = 40;
                }
            }

            return true;
        }

        return false;
    }


    public FormController SpawnForm(string Name = "")
    {
        FormController c = null;

        List<Form> forms = MISC.LoadForms();

        for (int x = 0; x < forms.Count; x++)
        {
            if (forms[x].Name == Name)
            {
                GameObject Cache = Instantiate(FormFrame);
                Cache.transform.SetParent(LevelFormContainer.transform);
                //Cache.name = "Form";
                Cache.name = forms[x].Name;

                FormController LEMFC = Cache.GetComponent<FormController>();
                if (LEMFC != null)
                {
                    LEMFC.FormPlan = forms[x];
                    RectTransform LEMFCRT = Cache.GetComponent<RectTransform>();
                    LEMFCRT.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    LEMFCRT.anchoredPosition = Vector2.zero;
                    LEMFCRT.anchorMin = new Vector2(0, 1);
                    LEMFCRT.anchorMax = new Vector2(0, 1);
                    LEMFCRT.pivot = new Vector2(0, 1);
                    
 
                    FormsInLevel.Add(LEMFC);
                    
                    return LEMFC;

                }
                else
                {
                    Debug.LogError("[LVM] No Form Controller found!");
                }

                return c;
            }
        }
        Debug.LogWarning("[LVM] Could not find form!");
        return c;

    }

    public IEnumerator waitForFormLoad(FormController lem, LevelData LED)
    {

        while (lem.Init == false)
        {
            yield return new WaitForSeconds(0.1f);
        }

        //lem.xpos = LED.x;
        //lem.ypos = LED.y;

        while (lem.FormBuild.Rotated != LED.RotationState)
        {
            lem.FormBuild.RotateRight();
        }

        lem.FormBuild.Color = LED.Color;       
        lem.LevelSprite = GenerateFormSprite(lem.FormBuild);
        lem.UpdateImage();
      

        //lem.ChangeColor(100);
        //lem.UpdatePosition();
    }

    public static Sprite GenerateFormSprite(Form data)
    {
        //data.RotateRight();
        Texture2D Cache = new Texture2D(data.Width * 64, data.Height * 64);
        int x = 0;
        int y = 0;


        //data.RotateRight();

        Color fillColor = Color.clear;
        Color[] fillPixels = new Color[Cache.width * Cache.height];

        for (int i = 0; i < fillPixels.Length; i++)
        {
            fillPixels[i] = fillColor;
        }

        Cache.SetPixels(fillPixels);
        Cache.Apply();

        foreach (SimpleTriforce f in data.Triforces)
        {
            Color[] pixels = null;

            switch (f.Type)
            {
                case SimpleTriforce.TriforceType.FILLED:
                    pixels = Square.GetPixels();
                    break;
                case SimpleTriforce.TriforceType.TOPLEFT:
                    pixels = TriangleTL.GetPixels();
                    break;
                case SimpleTriforce.TriforceType.TOPRIGHT:
                    pixels = TriangleTR.GetPixels();
                    break;
                case SimpleTriforce.TriforceType.BOTLEFT:
                    pixels = TriangleDL.GetPixels();
                    break;
                case SimpleTriforce.TriforceType.BOTRIGHT:
                    pixels = TriangleDR.GetPixels();
                    break;
                case SimpleTriforce.TriforceType.EMPTY:
                    pixels = Empty.GetPixels();
                    break;
            }

            Cache.SetPixels(x * 64, (data.Height - 1) * 64 - y * 64, 64, 64, pixels);

            x++;
            if (x == data.Width)
            {
                x = 0;
                y++;
            }
        }

        for (int pixelY = 0; pixelY <= Cache.height - 1; pixelY++)
        {
            for (int pixelX = 0; pixelX <= Cache.width - 1; pixelX++)
            {
                if (Cache.GetPixel(pixelX, pixelY).a > 0.05f)
                {
                    Cache.SetPixel(pixelX, pixelY, Color.white);
                }
            }
        }

        Cache.Apply();

        Sprite retSprite = Sprite.Create(Cache, new Rect(0, 0, Cache.width, Cache.height), Vector2.zero);
        retSprite.texture.filterMode = FilterMode.Point;

        return retSprite;
    }

    public static Sprite GenerateLevelSprite()
    {
        if (CurrentLevel == null) return null;

        Form LevelForm = ScriptableObject.CreateInstance<Form>();
        LevelForm.Width = 9;
        LevelForm.Height = 12;
        LevelForm.Resize(9, 9);

        minx = 5;
        miny = 5;
        maxx = 0;
        maxy = 0;

        LevelTilesShouldBe.Resize(9, 12);

        List<Form> forms = MISC.LoadForms();

        foreach (LevelData ld in CurrentLevel.Data)
        {
            for (int x = 0; x < forms.Count; x++)
            {
                if (forms[x].Name == ld.FormName)
                {
                    Form Cache = ScriptableObject.CreateInstance<Form>();
                    forms[x].CloneTo(Cache);

                    while (Cache.Rotated != ld.RotationState)
                    {
                        Cache.RotateRight();
                    }

                    for (int fy = 0; fy < Cache.Height; fy++)
                    {
                        for (int fx = 0; fx < Cache.Width; fx++)
                        {
                            SimpleTriforce CacheTri = new SimpleTriforce();
                            CacheTri = Cache.Get(fx, fy);
                            int realX = ld.x / 64 + fx;
                            int realY = ld.y / 64 * -1 + fy;

                            if (CacheTri.Type != SimpleTriforce.TriforceType.EMPTY)
                            {
                                if (LevelForm.Get(realX, realY).Type != SimpleTriforce.TriforceType.EMPTY)
                                {
                                    LevelForm.Set(realX, realY, new SimpleTriforce(SimpleTriforce.TriforceType.FILLED));
                                    LevelTilesShouldBe.Set(realX, realY, new SimpleTriforce(SimpleTriforce.TriforceType.FILLED));


                                }
                                else
                                {
                                    LevelForm.Set(realX, realY, CacheTri);
                                    LevelTilesShouldBe.Set(realX, realY, CacheTri);
                                }
                                

                                if (realX < minx) minx = realX;
                                if (realY < miny) miny = realY;
                                if (realX + 1 > maxx) maxx = realX + 1;
                                if (realY + 1 > maxy) maxy = realY + 1;
                            }
                        }
                    }
                }
            }
        }
        Sprite retSprite = GenerateFormSprite(LevelForm);
        return retSprite;
    }


    public static bool CheckNoCollision(FormController ToCheck)
    {
        bool ret = true;
        //return true;

        for (int y = 0; y < ToCheck.FormBuild.Height; y++)
        {
            for (int x = 0; x < ToCheck.FormBuild.Width; x++)
            {

                int realX = (ToCheck.xpos - (int)LevelM.LevelX) / 64  - 1 +x;
                int realY = (ToCheck.ypos + (int)LevelM.LevelY + 235) / 64 * -1 - 2 +y;


                SimpleTriforce Cache = new SimpleTriforce();
                Cache = ToCheck.FormBuild.Get(x, y);

                if (Cache.Type != SimpleTriforce.TriforceType.EMPTY)
                {
                    if (LevelTilesShouldBe.Get(realX, realY).Type == SimpleTriforce.TriforceType.EMPTY) return false;

                    switch (LevelTilesCurrent.Get(realX, realY).Type)
                    {
                        case SimpleTriforce.TriforceType.FILLED:
                            return false;
                            break;

                        case SimpleTriforce.TriforceType.BOTLEFT:
                            if (Cache.Type != SimpleTriforce.TriforceType.TOPRIGHT)
                            {
                                return false;
                            }
                            break;
                        case SimpleTriforce.TriforceType.BOTRIGHT:
                            if (Cache.Type != SimpleTriforce.TriforceType.TOPLEFT)
                            {
                                return false;
                            }
                            break;
                        case SimpleTriforce.TriforceType.TOPLEFT:
                            if (Cache.Type != SimpleTriforce.TriforceType.BOTRIGHT)
                            {
                                return false;
                            }
                            break;
                        case SimpleTriforce.TriforceType.TOPRIGHT:
                            if (Cache.Type != SimpleTriforce.TriforceType.BOTLEFT)
                            {
                                return false;
                            }
                            break;
                    }
                }

            }
        }


        return ret;
    }

    public static void RegenerateCurrentLevel()
    {
        int realX, realY;
        LevelTilesCurrent.Resize(9, 12); //Empty

        foreach (FormController fc in FormsInLevel)
        {
            if (fc.isInFormContainer == true) continue;
            if (fc.isDrag == true) continue;

            for (int y = 0; y < fc.FormBuild.Height; y++)
            {
                for (int x = 0; x < fc.FormBuild.Width; x++)
                {
                    SimpleTriforce CacheTri = new SimpleTriforce();
                    CacheTri = fc.FormBuild.Get(x, y);
                    realX = (fc.xpos - (int)LevelX) / 64 + x - 1;
                    realY = (fc.ypos + (int)LevelY + 235) / 64 * -1 + y -2;


                    if (CacheTri.Type != SimpleTriforce.TriforceType.EMPTY)
                    {
                        if (LevelTilesCurrent.Get(realX, realY).Type != SimpleTriforce.TriforceType.EMPTY)
                        {
                            LevelTilesCurrent.Set(realX, realY, new SimpleTriforce(SimpleTriforce.TriforceType.FILLED));
                        }
                        else
                        {
                            LevelTilesCurrent.Set(realX, realY, CacheTri);
                        }
                        
                    }
                }
            }
        }
    }


    private void Update()
    {

        if (Init == false || LevelLoad == false) return;

        countForms = 0;
        correctForms = 0;
        

        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 9; x++)
            {

                countForms++;
                if (LevelTilesCurrent.Get(x, y).Type == LevelTilesShouldBe.Get(x, y).Type)
                {
                    correctForms++;
                }

            }
        }

        //Is LevelComplete? You can even percentage correctForms/(12*9) = % Completed
        //But this counts in empty tiles aswell, so you have to remove them

        if (correctForms == countForms) Debug.Log("Complete");
    }
}

//=======================================================================