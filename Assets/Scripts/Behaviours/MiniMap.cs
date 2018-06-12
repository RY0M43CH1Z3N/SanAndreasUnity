﻿using SanAndreasUnity.Importing.Conversion;
using SanAndreasUnity.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SanAndreasUnity.Behaviours
{
    /*[RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(ScrollRect))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Mask))]*/

    public class MiniMap : MonoBehaviour
    {
        private const int tileEdge = 12; // width/height of map in tiles
        private const int tileCount = tileEdge * tileEdge; // number of tiles
        private const int mapEdge = 6000; // width/height of map in world coordinates
        private const int texSize = 128; // width/height of single tile in px
        private const int mapSize = tileEdge * texSize; // width/height of whole map in px
        private const int uiSize = 256, uiOffset = 10;
        private const bool outputChunks = false, outputImage = true;

        private TextureDictionary huds;

        private Texture2D northBlip, playerBlip, mapTexture;
        private Sprite mapSprite, circleMask;
        private bool enableMinimap, isReady, isSetup;

        public static void AssingMinimap()
        {
            GameObject UI = GameObject.FindGameObjectWithTag("UI");
            Transform root = UI != null ? UI.transform : null;

            GameObject minimap = GameObject.FindGameObjectWithTag("Minimap");
            if (minimap == null)
            {
                minimap = new GameObject();

                minimap.name = "Minimap";
                minimap.tag = "Minimap";

                minimap.transform.parent = root;
            }

            MiniMap map = minimap.GetComponent<MiniMap>();

            if (map == null)
                map = minimap.AddComponent<MiniMap>();

            map.isReady = true;
            if (!map.isSetup) map.Setup();
        }

        private void loadTextures()
        {
            mapTexture = new Texture2D(mapSize, mapSize);

            //Dictionary<string, byte[]> byteArr = new Dictionary<string, byte[]>();
            string folder = Path.Combine(Application.streamingAssetsPath, "map-chunks");

            if (outputChunks)
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }

            Debug.Log("Merging all map sprites into one sprite.");
            for (int i = 0; i < tileCount; i++)
            {
                // Offset
                int y = ((i / tileEdge) + 1) * texSize,
                    x = (i % tileEdge) * texSize;

                string name = "radar" + ((i < 10) ? "0" : "") + i;
                var texDict = TextureDictionary.Load(name);

                Texture2D tex = texDict.GetDiffuse(name).Texture;

                //if (outputImage)
                //    tex = name.Substring(5).WriteTextToTexture(tex);

                if (outputChunks)
                {
                    string id = name.Substring(5);
                    Texture2D image = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);

                    //Color[] arr = tex.GetPixels();
                    //Array.Reverse(arr);

                    //image.SetPixels(0, 0, texSize, texSize, arr);

                    //for (int k = 0; k < id.Length; ++k)
                    //    image.SetPixels(12 * k, 0, 12, 18, id[k].WriteLetterToTexture());

                    for (int xx = 0; xx < texSize; ++xx)
                        for (int yy = 0; yy < texSize; ++yy)
                            image.SetPixel(xx, texSize - yy - 1, tex.GetPixel(xx, yy));

                    image.Apply();

                    File.WriteAllBytes(Path.Combine(folder, string.Format("{0}.jpg", id)), ImageConversion.EncodeToPNG(image));
                    //byteArr.Add(id, tex.GetRawTextureData());
                }

                for (int ii = 0; ii < texSize; ++ii)
                    for (int jj = 0; jj < texSize; ++jj)
                        mapTexture.SetPixel(x + ii, texSize - (y + jj) - 1, tex.GetPixel(ii, jj));
            }

            mapTexture.Apply();
            mapSprite = Sprite.Create(mapTexture, new Rect(0, 0, mapTexture.width, mapTexture.height), new Vector2(mapTexture.width, mapTexture.height) / 2);

            if (outputImage)
                File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "gta-map.png"), mapTexture.EncodeToPNG());

            circleMask = Resources.Load<Sprite>("Sprites/MapCircle");

            huds = TextureDictionary.Load("hud");
            northBlip = huds.GetDiffuse("radar_north").Texture;
            playerBlip = huds.GetDiffuse("radar_centre").Texture;
        }

        // --------------------------------

        #region Private fields

        private Player player;
        private PlayerController playerController;
        private Canvas canvas;
        private RectTransform mapTransform, maskTransform;

        #endregion Private fields

        private void Setup()
        {
            loadTextures();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            player = playerObj.GetComponent<Player>();
            playerController = playerObj.GetComponent<PlayerController>();

            // Start object setup

            //Check if parent is a canvas
            canvas = transform.parent.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log("Canvas already exists!");
                maskTransform = GetComponent<RectTransform>();
                mapTransform = transform.Find("Image").GetComponent<RectTransform>();

                // Setup mapSprite
                if (GetComponent<Image>().sprite == null)
                    GetComponent<Image>().sprite = circleMask;

                transform.Find("Image").GetComponent<Image>().sprite = mapSprite;
            }
            else
            {
                GameObject canvasObject = new GameObject();
                canvasObject.name = "Canvas";

                canvasObject.AddComponent<RectTransform>();
                canvas = canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();

                transform.parent = canvasObject.transform;

                if (GetComponent<RectTransform>() == null)
                    mapTransform = gameObject.AddComponent<RectTransform>();

                if (GetComponent<CanvasRenderer>() == null)
                    gameObject.AddComponent<CanvasRenderer>();

                if (GetComponent<Image>() == null)
                {
                    Image img = gameObject.AddComponent<Image>();
                    img.sprite = circleMask;
                }

                if (GetComponent<Mask>() == null)
                    gameObject.AddComponent<Mask>();

                if (transform.Find("Image") == null)
                {
                    GameObject image = new GameObject();
                    image.name = "Image";

                    image.transform.parent = transform;

                    mapTransform = image.AddComponent<RectTransform>();
                    image.AddComponent<CanvasRenderer>();

                    Image mapImage = image.AddComponent<Image>();
                    mapImage.sprite = mapSprite;
                }
            }

            canvas.enabled = false;
            maskTransform.position = new Vector3(Screen.width - uiSize - uiOffset, Screen.height - uiSize - uiOffset);

            Debug.Log("Canvas disabled!");

            maskTransform.localScale = new Vector3(uiSize, uiSize, 1);
            mapTransform.localScale = new Vector3(1f / uiSize, 1f / uiSize, 1);

            isSetup = true;
        }

        private void Awake()
        {
            if (!isReady)
                return;

            Setup();
        }

        private void Update()
        {
            if (!isReady) return;
            if (!Loader.HasLoaded) return;
            if (!playerController.CursorLocked) return;

            if (!enableMinimap)
            {
                canvas.enabled = true;
                enableMinimap = true;
            }
        }
    }
}