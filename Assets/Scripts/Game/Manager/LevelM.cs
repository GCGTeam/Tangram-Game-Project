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
    public Image test;

    private void Start()
    {
        instance = this;
        LoadLevel(4);

        //Loading Textures for Form
        Empty      = Resources.Load<Texture2D>("FormEditor/Shape_Empty");
        TriangleDL = Resources.Load<Texture2D>("FormEditor/Shape_TriangleDL");
        TriangleDR = Resources.Load<Texture2D>("FormEditor/Shape_TriangleDR");
        TriangleTL = Resources.Load<Texture2D>("FormEditor/Shape_TriangleTL");
        TriangleTR = Resources.Load<Texture2D>("FormEditor/Shape_TriangleTR");
        Square     = Resources.Load<Texture2D>("FormEditor/Shape_Square");
        Debug.Log("1");
        Sprite level = GenerateLevelSprite();
        test.sprite = level;

    }

    static bool LoadLevel(int Number, bool AutoGenerate = true)
    {
        if (Number < 0 || Number >= instance.LevelList.Count) return false;

        CurrentLevel = new Level();
        
        string jsonformatted = instance.LevelList[Number].text;

        if (jsonformatted.Length != 0)
        {
            for (int x = 0; x < jsonformatted.Split('\n').Length - 1; x++)
            {
                Debug.Log(x);
                string jsontile = jsonformatted.Split('\n')[x];
                if (jsontile.Trim().Length != 0)
                {
                    LevelData dataCache = JsonUtility.FromJson<LevelData>(jsontile);
                    CurrentLevel.Data.Add(dataCache);
                }
            }

            if (AutoGenerate == true) return Generate();
            return true;
        }

        return false;
    }


    static bool Generate()
    {
        if(CurrentLevel.Data != null)
        {;
            foreach (LevelData ld in CurrentLevel.Data)
            {
                LEM_FormController Cache = instance.SpawnForm(ld.FormName);
                if (Cache != null && ld != null)
                {
                    instance.StartCoroutine(instance.waitForFormLoad(Cache, ld));
                }
            }

            return true;
        }

        return false;
    }


    public LEM_FormController SpawnForm(string Name = "")
    {
        LEM_FormController c = null;

        List<Form> forms = MISC.FindAssetsByType<Form>();

        for (int x = 0; x < forms.Count; x++)
        {
            if (forms[x].Name == Name)
            {
                GameObject Cache = Instantiate(FormFrame);
                Cache.transform.SetParent(LevelFormContainer.transform);
                Cache.name = "Form";

                LEM_FormController LEMFC = Cache.GetComponent<LEM_FormController>();
                if (LEMFC != null)
                {
                    LEMFC.FormPlan = forms[x];
                    RectTransform LEMFCRT = Cache.GetComponent<RectTransform>();
                    LEMFCRT.anchoredPosition = Vector2.zero;
                    LEMFCRT.anchorMin = new Vector2(0, 1);
                    LEMFCRT.anchorMax = new Vector2(0, 1);
                    LEMFCRT.pivot = new Vector2(0, 1);
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

    public IEnumerator waitForFormLoad(LEM_FormController lem, LevelData LED)
    {
        while (lem.Init == false)
        {
            yield return new WaitForSeconds(0.5f);
        }

        lem.xpos = LED.x;
        lem.ypos = LED.y;

        while (lem.FormBuild.Rotated != LED.RotationState)
        {
            lem.FormBuild.RotateRight();
        }

        lem.GenerateImages();
        lem.ChangeColor(100);
        lem.UpdatePosition();
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

            Cache.SetPixels(x * 64, (data.Height-1)*64 - y * 64 , 64, 64, pixels);

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
                    Cache.SetPixel(pixelX, pixelY,Color.white);
                }
            }
        }

        Cache.Apply();

        Sprite retSprite = Sprite.Create(Cache, new Rect(0, 0, Cache.width, Cache.height), Vector2.zero);
        retSprite.texture.filterMode = FilterMode.Point;
        return retSprite;
    }

    public Sprite GenerateLevelSprite()
    {
        if (CurrentLevel == null) return null;

        Form LevelForm = ScriptableObject.CreateInstance<Form>();
        LevelForm.Width = 9;
        LevelForm.Height = 12;
        LevelForm.Resize(9, 12);

        List<Form> forms = MISC.FindAssetsByType<Form>();

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
                            Debug.Log(CacheTri.Type);

                            if (CacheTri.Type != SimpleTriforce.TriforceType.EMPTY)
                            {
                                if (LevelForm.Get(realX, realY).Type != SimpleTriforce.TriforceType.EMPTY) CacheTri.Type = SimpleTriforce.TriforceType.FILLED;
                                LevelForm.Set(realX, realY, CacheTri);
                            }
                        }
                    }
                }
            }
        }
        Debug.Log(LevelForm.Get(0,0).Type);
        Sprite retSprite = GenerateFormSprite(LevelForm);
        return retSprite;
    }     
}

//=======================================================================