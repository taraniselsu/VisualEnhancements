﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.IO;
using CommonUtils;

namespace Clouds
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class Clouds : MonoBehaviour
    {
        static List<CloudLayer> CloudLayers = new List<CloudLayer>();
        static bool Loaded = false;
        static KeyCode GUI_KEYCODE = KeyCode.N;

        static ParticleEmitter cloudParticleEmitter;
        static ParticleAnimator cloudParticleAnimator;
        static ParticleRenderer cloudParticleRenderer;
        static GameObject particleSystemGo;
        static bool spawned = false;

        static bool useEditor = false;
        static bool AdvancedGUI = false;
        static Vector2 ScrollPosLayerList = Vector2.zero;
        static int SelectedLayer = 0;
        static CloudGUI CloudGUI = new CloudGUI();
        static CelestialBody currentBody = null;
        static CelestialBody oldBody = null;


        private void loadCloudLayers(String configString)
        {
            foreach (CloudLayer cl in CloudLayers)
            {
                cl.Remove();
            }
            CloudLayers.Clear();
            if (configString == null)
            {
                configString = KSPUtil.ApplicationRootPath + "GameData/BoulderCo/Clouds/userCloudLayers.cfg";
            }
            else
            {
                configString = KSPUtil.ApplicationRootPath + "GameData/BoulderCo/Clouds/" + configString;
            }

            ConfigNode config = ConfigNode.Load(configString);
            if (config == null)
            {
                config = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/BoulderCo/Clouds/cloudLayers.cfg");
            }
            String keycodeString = config.GetValue("GUI_KEYCODE");
            if (keycodeString != null)
            {
                GUI_KEYCODE = (KeyCode)Enum.Parse(typeof(KeyCode), keycodeString);
            }
            else
            {
                GUI_KEYCODE = KeyCode.N;
            }

            ConfigNode[] cloudLayersConfigs = config.GetNodes("CLOUD_LAYER");

            foreach (ConfigNode node in cloudLayersConfigs)
            {
                String body = node.GetValue("body");
                Transform bodyTransform = null;
                try
                {
                    bodyTransform = ScaledSpace.Instance.scaledSpaceTransforms.Single(t => t.name == body);
                }
                catch
                {

                }
                if (bodyTransform != null)
                {
                    float radius = float.Parse(node.GetValue("radius"));

                    TextureSet mTexture = new TextureSet(node.GetNode("main_texture"), false);
                    TextureSet dTexture = new TextureSet(node.GetNode("detail_texture"), false);
                    TextureSet bTexture = new TextureSet(node.GetNode("bump_texture"), true);
                    ConfigNode floatsConfig = node.GetNode("shader_floats");
                    ShaderFloats shaderFloats = null;
                    if (floatsConfig != null)
                    {
                        shaderFloats = new ShaderFloats(floatsConfig);
                    }
                    ConfigNode undersideFloatsConfig = node.GetNode("underside_shader_floats");
                    ShaderFloats undersideShaderFloats = null;
                    if (undersideFloatsConfig != null)
                    {
                        undersideShaderFloats = new ShaderFloats(undersideFloatsConfig);
                    }
                    ConfigNode colorNode = node.GetNode("color");
                    Color color = new Color(
                        float.Parse(colorNode.GetValue("r")),
                        float.Parse(colorNode.GetValue("g")),
                        float.Parse(colorNode.GetValue("b")));

                    CloudLayers.Add(
                        new CloudLayer(body, color, radius,
                        mTexture, dTexture, bTexture, shaderFloats, undersideShaderFloats));
                }
                else
                {
                    CloudLayer.Log("body " + body + " does not exist!");
                }
            }
        }

        private void saveCloudLayers(String configString)
        {
            ConfigNode saveConfig = new ConfigNode();
            if (configString == null)
            {
                configString = KSPUtil.ApplicationRootPath + "GameData/BoulderCo/Clouds/userCloudLayers.cfg";
            }
            else
            {
                configString = KSPUtil.ApplicationRootPath + "GameData/BoulderCo/Clouds/" + configString;
            }
            saveConfig.SetValue("GUI_KEYCODE", GUI_KEYCODE.ToString());

            foreach (KeyValuePair<String, List<CloudLayer>> cloudList in CloudLayer.BodyDatabase.ToArray())
            {
                String body = cloudList.Key;
                List<CloudLayer> list = cloudList.Value;
                foreach (CloudLayer cloudLayer in list)
                {
                    ConfigNode newNode = saveConfig.AddNode("CLOUD_LAYER");
                    newNode.AddValue("body", body);
                    newNode.AddValue("radius", cloudLayer.Radius);
                    ConfigNode colorNode = newNode.AddNode("color");
                    colorNode.AddValue("r", cloudLayer.Color.r);
                    colorNode.AddValue("g", cloudLayer.Color.g);
                    colorNode.AddValue("b", cloudLayer.Color.b);
                    newNode.AddNode(cloudLayer.MainTexture.GetNode("main_texture"));
                    ConfigNode detailNode = cloudLayer.DetailTexture.GetNode("detail_texture");
                    if (detailNode != null)
                    {
                        newNode.AddNode(detailNode);
                    }
                    ConfigNode bumpNode = cloudLayer.BumpTexture.GetNode("bump_texture");
                    if (bumpNode != null)
                    {
                        newNode.AddNode(bumpNode);
                    }
                    ConfigNode shaderFloatNode = cloudLayer.ShaderFloats.GetNode("shader_floats");
                    if (!CloudLayer.IsDefaultShaderFloat(cloudLayer.ShaderFloats))
                    {
                        newNode.AddNode(shaderFloatNode);
                    }
                    ConfigNode undersideShaderFloatNode = cloudLayer.UndersideShaderFloats.GetNode("underside_shader_floats");
                    if (!CloudLayer.IsDefaultShaderFloat(cloudLayer.UndersideShaderFloats, true))
                    {
                        newNode.AddNode(undersideShaderFloatNode);
                    }
                }
            }
            saveConfig.Save(configString);
        }

        private void placePQS(float longitude, float latitude, float altitude, GameObject go)
        {

            UnityEngine.Object[] celestialBodies = CelestialBody.FindObjectsOfType(typeof(CelestialBody));

            CelestialBody currentBody = null;

            foreach (CelestialBody cb in celestialBodies)
            {
                string name = cb.bodyName;
                if (name == "Kerbin")
                {
                    currentBody = cb;
                }
            }

            GameObject blockHolder = new GameObject();
            blockHolder.name = "blockHolder";

            //GameObject block = GameDatabase.Instance.GetModel("Hubs/Static/FLG/FloatingLaunchGround");
            //GameObject building = BuildingGenerator.Generate(20,20,20);
            GameObject block = go;
            if (block == null)
            {

            }


            block.transform.parent = blockHolder.transform;

            foreach (Transform aTransform in currentBody.transform)
            {
                if (aTransform.name == currentBody.transform.name)
                    blockHolder.transform.parent = aTransform;
            }
            PQSCity blockScript = blockHolder.AddComponent<PQSCity>();
            blockScript.debugOrientated = false;
            blockScript.frameDelta = 1;
            blockScript.lod = new PQSCity.LODRange[1];
            blockScript.lod[0] = new PQSCity.LODRange();
            blockScript.lod[0].visibleRange = 20000;
            blockScript.lod[0].renderers = new GameObject[1];
            blockScript.lod[0].renderers[0] = block;
            blockScript.lod[0].objects = new GameObject[0];
            blockScript.modEnabled = true;
            blockScript.order = 100;
            blockScript.reorientFinalAngle = -105;
            blockScript.reorientInitialUp = Vector3.up;
            blockScript.reorientToSphere = true;
            //railScript.repositionRadial = (GameObject.Find("Runway").transform.position - currentBody.transform.position) + Vector3.up * 50 + Vector3.right * -350;
            blockScript.repositionRadial = QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right;
            blockScript.repositionRadiusOffset = altitude + (currentBody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right) - currentBody.pqsController.radius);
            blockScript.repositionToSphere = true;
            blockScript.requirements = PQS.ModiferRequirements.Default;
            blockScript.sphere = currentBody.pqsController;

            blockScript.RebuildSphere();
        }

        protected void spawnVolumeClouds()
        {
            if (!spawned)
            {

                particleSystemGo = GenerateCloudMesh();

                particleSystemGo.transform.localScale = Vector3.one * 5;
                /*                Material test = new Material(Shader.Find("Transparent/Diffuse"));
                                Utils.Log("2");
                                test.color = new Color(0, 0, 0, 0f);
                                particleSystemGo.renderer.material = test;*/
                particleSystemGo.layer = 15;
                float longitude = -74.559f;
                float latitude = -0.0975f;
                placePQS(longitude, latitude, 4000f, particleSystemGo);

                cloudParticleEmitter = (ParticleEmitter)particleSystemGo.AddComponent("MeshParticleEmitter");
                cloudParticleEmitter.minSize = 1000;
                cloudParticleEmitter.maxSize = 1600;
                cloudParticleEmitter.minEnergy = 100;
                cloudParticleEmitter.maxEnergy = 200;
                cloudParticleEmitter.minEmission = 4;
                cloudParticleEmitter.maxEmission = 8;
                cloudParticleEmitter.localVelocity = new Vector3(.05f, .05f, .05f);
                cloudParticleEmitter.rndVelocity = new Vector3(.5f, .5f, .5f);
                cloudParticleEmitter.rndAngularVelocity = 3f;
                cloudParticleEmitter.rndRotation = true;
                cloudParticleEmitter.useWorldSpace = false;
                

                cloudParticleAnimator = (ParticleAnimator)particleSystemGo.AddComponent<ParticleAnimator>();
                cloudParticleAnimator.sizeGrow = -.0125f;
                cloudParticleAnimator.colorAnimation = new Color[5] { 
                    new Color(1, 1, 1, 0), 
                    new Color(1, 1, 1, 1), 
                    new Color(1, 1, 1, 1), 
                    new Color(1, 1, 1, 1), 
                    new Color(1, 1, 1, 0) };

                cloudParticleRenderer = particleSystemGo.AddComponent<ParticleRenderer>();
                //cloudParticleRenderer.particleRenderMode = ParticleRenderMode.SortedBillboard;
                cloudParticleRenderer.particleRenderMode = ParticleRenderMode.Stretch;
                cloudParticleRenderer.receiveShadows = false;
                cloudParticleRenderer.castShadows = false;
                cloudParticleRenderer.cameraVelocityScale = 0;
                cloudParticleRenderer.lengthScale = 1;
                cloudParticleRenderer.velocityScale = 0;
                cloudParticleRenderer.maxParticleSize = 50;
                
                Assembly assembly = Assembly.GetExecutingAssembly();
                StreamReader shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream("Clouds.CompiledCloudParticleShader.txt"));
                Utils.Log("reading stream...");

                Material cloudMaterial = new Material(shaderStreamReader.ReadToEnd());
                cloudMaterial.mainTexture = GameDatabase.Instance.GetTexture("BoulderCo/Clouds/Textures/particle", false);
                cloudMaterial.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("BoulderCo/Clouds/Textures/particleNormal", true));

                //cloudMaterial.color = new Color(1, 1, 1, .80f);
                cloudParticleRenderer.material = cloudMaterial;
                cloudParticleEmitter.enabled = true;
                cloudParticleEmitter.emit = true;
                cloudParticleEmitter.Simulate(100);
                
                
                spawned = true;
                CloudLayer.Log("making particles");
            }

        }

        List<Vector3> GenerateCloud(Vector3 location)
        {
            List<Vector3> verticiesList = new List<Vector3>();
            for (int v = 0; v < 6; v++)
            {
                Vector3 start = location + new Vector3(UnityEngine.Random.Range(-50, 50), UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-50, 50));
                for (int i = 0; i < 5; i++)
                {
                    Vector3 place = new Vector3(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-2.5f, 2.5f), UnityEngine.Random.Range(-20, 20));
                    for (int n = 0; n < 3; n++)
                    {
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-.2f, .2f), UnityEngine.Random.Range(-1, 1));
                        verticiesList.Add(start + place + (dir * UnityEngine.Random.Range(10, 20)));
                        //   Utils.Log("setting up verticies " + i);
                    }
                    /*
                    for (int l = 0; l < 7; l++)
                    {
                        Vector3 expand = new Vector3(place.x, place.y, place.z);
                        expand.Scale(new Vector3(UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(1f, 1f), UnityEngine.Random.Range(2, 5)));
                        for (int n = 0; n < 5; n++)
                        {
                            Vector3 dir = new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-.2f, .2f), UnityEngine.Random.Range(-1, 1));
                            verticiesList.Add(start + expand + (dir * UnityEngine.Random.Range(5, 10)));
                            Utils.Log("setting up verticies " + i);
                        }
                    }*/
                }
            }
            return verticiesList;
        }

        private GameObject GenerateCloudMesh()
        {
            GameObject cloudObject = new GameObject();
            Mesh mesh = cloudObject.AddComponent<MeshFilter>().mesh;
            //var mr = cloudObject.AddComponent<MeshRenderer>();

            Utils.Log("setting up verticies");

            List<Vector3> verticiesList = new List<Vector3>();
            for (int i = 0; i < 18; i++)
            {
                verticiesList.AddRange(GenerateCloud(new Vector3(UnityEngine.Random.Range(-500, 500), UnityEngine.Random.Range(-2.5f, 2.5f), UnityEngine.Random.Range(-500, 500))));
            }

            Vector3[] vertices = verticiesList.ToArray();
            int nbFaces = vertices.Length;
            int nbTriangles = (int)Math.Ceiling(nbFaces / 3.0); ;
            int nbIndexes = nbTriangles * 3;

            int[] triangles = new int[nbIndexes];
            //Utils.Log("setting up triangles " + nbTriangles);

            for (int i = 0; i < nbIndexes; i += 3)
            {
                triangles[i + 0] = i + 0;
                triangles[i + 1] = i + 1;
                triangles[i + 2] = i + 2;
                //    Utils.Log("setting up triangle "+i);
            }

            int leftover = nbFaces % 3;
            if (leftover == 2)
            {
                //    Utils.Log("leftover2 " + nbIndexes);
                triangles[nbIndexes - 2] = nbFaces - 3;
                triangles[nbIndexes - 1] = nbFaces - 2;
            }
            else if (leftover == 1)
            {
                //    Utils.Log("leftover1 " + nbIndexes);
                triangles[nbIndexes - 1] = nbFaces - 3;
            }
            //Utils.Log("finished triangle gen.");

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            //mr.renderer.sharedMaterial = overlayMaterial;

            //mr.castShadows = false;
            //mr.receiveShadows = false;
            //mr.enabled = true;
            Utils.Log("generated Mesh");
            return cloudObject;
        }

        protected void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !Loaded)
            {
                Utils.Init();
                loadCloudLayers(null);

                Loaded = true;
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
            //        spawnVolumeClouds();
            }
        }

        protected void Update()
        {

            foreach (CloudLayer layer in CloudLayers)
            {
                layer.PerformUpdate();
            }
            bool alt = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            if (alt && Input.GetKeyDown(GUI_KEYCODE))
            {
                useEditor = !useEditor;
            }
        }


        private GUISkin _mySkin;
        private Rect _mainWindowRect = new Rect(20, 20, 260, 600);

        private void OnGUI()
        {

            GUI.skin = _mySkin;

            CelestialBody current = null;
            if (MapView.MapIsEnabled)
            {
                current = Utils.GetMapBody();
            }
            else
            {
                current = FlightGlobals.currentMainBody;
            }
            if (useEditor && current != null)
            {

                if (AdvancedGUI)
                {
                    _mainWindowRect.width = 520;
                }
                else
                {
                    _mainWindowRect.width = 260;
                }
                if (CloudLayer.GetBodyLayerCount(current.name) != 0)
                {
                    _mainWindowRect.height = 715;
                    _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "Clouds");
                }
                else
                {
                    _mainWindowRect.height = 115;
                    _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "Clouds");
                }
            }
            
        }

        private void DrawMainWindow(int windowID)
        {
            oldBody = currentBody;
            currentBody = null;
            if (MapView.MapIsEnabled)
            {
                currentBody = Utils.GetMapBody();
            }
            else
            {
                currentBody = FlightGlobals.currentMainBody;
            }
            if (currentBody != null)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label);
                gs.alignment = TextAnchor.MiddleCenter;

                AdvancedGUI = GUI.Toggle(
                        new Rect(10, 50, 125, 25), AdvancedGUI, "Advanced Settings");
                float itemFullWidth = AdvancedGUI ? (_mainWindowRect.width / 2) - 20 : _mainWindowRect.width - 20;

                GUI.Label(new Rect(35, 20, itemFullWidth - 50, 25), currentBody.name, gs);

                if (MapView.MapIsEnabled)
                {
                    if (GUI.Button(new Rect(10, 20, 25, 25), "<"))
                    {
                        MapView.MapCamera.SetTarget(Utils.GetPreviousBody(currentBody).name);
                    }
                    if (GUI.Button(new Rect(itemFullWidth - 15, 20, 25, 25), ">"))
                    {
                        MapView.MapCamera.SetTarget(Utils.GetNextBody(currentBody).name);
                    }
                }
                float halfWidth;
                if (AdvancedGUI)
                {
                    if (GUI.Button(new Rect(itemFullWidth + 20, 20, itemFullWidth, 25), "Generate Test Launchpad Cloud"))
                    {
                        spawnVolumeClouds();
                    }
                    halfWidth = (itemFullWidth / 2) - 5;
                    if (GUI.Button(new Rect(itemFullWidth + 20, 50, halfWidth, 25), "Reset to Save"))
                    {
                        loadCloudLayers(null);
                        oldBody = null;
                    }
                    if (GUI.Button(new Rect(itemFullWidth + (itemFullWidth / 2) + 20, 50, halfWidth, 25), "Reset to Default"))
                    {
                        loadCloudLayers("cloudLayers.cfg");
                        oldBody = null;
                    }
                }

                int layerCount = CloudLayer.GetBodyLayerCount(currentBody.name);
                bool hasLayers = layerCount != 0;

                halfWidth = hasLayers ? (itemFullWidth / 2) - 5 : itemFullWidth;
                if (GUI.Button(new Rect(10, 80, halfWidth, 25), "Add"))
                {
                    CloudLayers.Add(
                    new CloudLayer(currentBody.name, new Color(1, 1, 1), 1.01f,
                    new TextureSet(), new TextureSet(), new TextureSet(), null, null));
                }
                if (hasLayers)
                {

                    if (GUI.Button(new Rect(halfWidth + 20, 80, halfWidth, 25), "Remove"))
                    {
                        CloudLayer.RemoveLayer(currentBody.name, SelectedLayer);
                    }
                    GUI.Box(new Rect(10, 110, itemFullWidth, 115), "");
                    String[] layerList = CloudLayer.GetBodyLayerStringList(currentBody.name);
                    ScrollPosLayerList = GUI.BeginScrollView(new Rect(15, 115, itemFullWidth - 10, 100), ScrollPosLayerList, new Rect(0, 0, itemFullWidth - 30, 25 * layerList.Length));
                    float layerWidth = layerCount > 4 ? itemFullWidth - 30 : itemFullWidth - 10;
                    SelectedLayer = SelectedLayer >= layerCount ? 0 : SelectedLayer;
                    int OldSelectedLayer = SelectedLayer;
                    SelectedLayer = GUI.SelectionGrid(new Rect(0, 0, layerWidth, 25 * layerList.Length), SelectedLayer, layerList, 1);
                    GUI.EndScrollView();

                    if (SelectedLayer != OldSelectedLayer || currentBody != oldBody)
                    {
                        if (CloudLayer.BodyDatabase.ContainsKey(currentBody.name) && CloudLayer.BodyDatabase[currentBody.name].Count > SelectedLayer)
                        {
                            CloudGUI.MainTexture.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].MainTexture);
                            CloudGUI.DetailTexture.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].DetailTexture);
                            CloudGUI.BumpTexture.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].BumpTexture);
                            CloudGUI.Color.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].Color);
                            CloudGUI.Radius.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].Radius);
                            CloudGUI.ShaderFloats.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].ShaderFloats);
                            CloudGUI.UndersideShaderFloats.Clone(CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].UndersideShaderFloats);
                        }
                    }

                    if (CloudGUI.IsValid())
                    {
                        if (GUI.Button(new Rect(145, 50, 105, 25), "Apply & Save"))
                        {
                            CloudLayer.BodyDatabase[currentBody.name][SelectedLayer].ApplyGUIUpdate(CloudGUI);
                            saveCloudLayers(null);
                        }
                    }



                    int nextLine = 230;
                    gs.alignment = TextAnchor.MiddleRight;
                    if (AdvancedGUI)
                    {
                        int advancedNextLine = HandleAdvancedGUI(CloudGUI.ShaderFloats, 80, _mainWindowRect.width / 2);
                        GUI.Label(new Rect((_mainWindowRect.width / 2) + 10, advancedNextLine, itemFullWidth, 25), "UnderCloud Settings:");
                        HandleAdvancedGUI(CloudGUI.UndersideShaderFloats, advancedNextLine + 30, _mainWindowRect.width / 2);
                    }

                    nextLine = HandleRadiusGUI(CloudGUI.Radius, nextLine);
                    nextLine = HandleColorGUI(CloudGUI.Color, nextLine);


                    GUI.Label(
                        new Rect(10, nextLine, 80, 25), "MainTex: ", gs);
                    nextLine = HandleTextureGUI(CloudGUI.MainTexture, nextLine);

                    GUI.Label(
                        new Rect(10, nextLine, 80, 25), "DetailTex: ", gs);
                    CloudGUI.DetailTexture.InUse = GUI.Toggle(
                        new Rect(10, nextLine, 25, 25), CloudGUI.DetailTexture.InUse, "");
                    if (CloudGUI.DetailTexture.InUse)
                    {
                        nextLine = HandleTextureGUI(CloudGUI.DetailTexture, nextLine);
                    }
                    else
                    {
                        nextLine += 30;
                    }

                    GUI.Label(
                        new Rect(10, nextLine, 80, 25), "BumpTex: ", gs);
                    CloudGUI.BumpTexture.InUse = GUI.Toggle(
                        new Rect(10, nextLine, 25, 25), CloudGUI.BumpTexture.InUse, "");
                    if (CloudGUI.BumpTexture.InUse)
                    {
                        nextLine = HandleTextureGUI(CloudGUI.BumpTexture, nextLine);
                    }
                    else
                    {
                        nextLine += 30;
                    }

                }
            }
            else
            {
                GUI.Label(new Rect(50, 50, 230, 25), "----");
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));

        }

        private int HandleAdvancedGUI(ShaderFloatsGUI floats, int y, float offset)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;


            GUI.Label(
                new Rect(offset + 10, y, 65, 25), "RimPower: ", gs);
            if (float.TryParse(floats.FalloffPowerString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            String SFalloffPower = GUI.TextField(new Rect(offset + 80, y, 50, 25), floats.FalloffPowerString, texFieldGS);
            float FFalloffPower = GUI.HorizontalSlider(new Rect(offset + 135, y + 5, 115, 25), floats.FalloffPower, 0, 3);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 65, 25), "RimScale: ", gs);
            if (float.TryParse(floats.FalloffScaleString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SFalloffScale = GUI.TextField(new Rect(offset + 80, y, 50, 25), floats.FalloffScaleString, texFieldGS);
            float FFalloffScale = GUI.HorizontalSlider(new Rect(offset + 135, y + 5, 115, 25), floats.FalloffScale, 0, 20);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 65, 25), "DetailDist: ", gs);
            if (float.TryParse(floats.DetailDistanceString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SDetailDistance = GUI.TextField(new Rect(offset + 80, y, 50, 25), floats.DetailDistanceString, texFieldGS);
            float FDetailDistance = GUI.HorizontalSlider(new Rect(offset + 135, y + 5, 115, 25), floats.DetailDistance, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 65, 25), "MinLight: ", gs);
            if (float.TryParse(floats.MinimumLightString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SMinimumLight = GUI.TextField(new Rect(offset + 80, y, 50, 25), floats.MinimumLightString, texFieldGS);
            float FMinimumLight = GUI.HorizontalSlider(new Rect(offset + 135, y + 5, 115, 25), floats.MinimumLight, 0, 1);

            floats.Update(SFalloffPower, FFalloffPower, SFalloffScale, FFalloffScale, SDetailDistance, FDetailDistance, SMinimumLight, FMinimumLight);

            return y + 30;
        }

        private int HandleRadiusGUI(RadiusSetGUI radius, int y)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            GUI.Label(
                new Rect(10, y, 65, 25), "Radius: ", gs);
            if (float.TryParse(radius.RadiusS, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            String sRadius = GUI.TextField(new Rect(80, y, 50, 25), radius.RadiusS, texFieldGS);
            float fRadius = GUI.HorizontalSlider(new Rect(135, y + 5, 115, 25), radius.RadiusF, 0, 2);
            radius.Update(fRadius, sRadius);
            return y + 30;
        }

        private int HandleColorGUI(ColorSetGUI color, int y)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            GUI.Label(
                new Rect(10, y, 65, 25), "Color: R: ", gs);
            if (float.TryParse(color.Red, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SRed = GUI.TextField(new Rect(80, y, 50, 25), color.Red, texFieldGS);
            float FRed = GUI.HorizontalSlider(new Rect(135, y + 5, 115, 25), color.Color.r, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(10, y, 65, 25), "G: ", gs);
            if (float.TryParse(color.Green, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SGreen = GUI.TextField(new Rect(80, y, 50, 25), color.Green, texFieldGS);
            float FGreen = GUI.HorizontalSlider(new Rect(135, y + 5, 115, 25), color.Color.g, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(10, y, 65, 25), "B: ", gs);
            if (float.TryParse(color.Blue, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SBlue = GUI.TextField(new Rect(80, y, 50, 25), color.Blue, texFieldGS);
            float FBlue = GUI.HorizontalSlider(new Rect(135, y + 5, 115, 25), color.Color.b, 0, 1);

            color.Update(SRed, FRed, SGreen, FGreen, SBlue, FBlue);
            return y += 30;
        }

        private int HandleTextureGUI(TextureSetGUI textureSet, int y)
        {

            GUIStyle labelGS = new GUIStyle(GUI.skin.label);
            labelGS.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            float vectorWidth = (_mainWindowRect.width - 140) / 2;
            float vectorStart = 105 + (_mainWindowRect.width - 140) / 2;

            textureSet.TextureFile = GUI.TextField(
                new Rect(90, y, _mainWindowRect.width - 100, 25), textureSet.TextureFile);
            y += 30;
            GUI.Label(
                new Rect(10, y, 90, 25), "  Scale: X:", labelGS);
            if (float.TryParse(textureSet.ScaleX, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.ScaleX = GUI.TextField(
                new Rect(100, y, vectorWidth, 25), textureSet.ScaleX, texFieldGS);
            GUI.Label(
                new Rect(vectorStart, y, 25, 25), "  Y:", labelGS);
            if (float.TryParse(textureSet.ScaleY, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.ScaleY = GUI.TextField(
                new Rect(vectorStart + 25, y, vectorWidth, 25), textureSet.ScaleY, texFieldGS);
            y += 30;
            GUI.Label(
                new Rect(10, y, 90, 25), "  Offset: X:", labelGS);
            if (float.TryParse(textureSet.StartOffsetX, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.StartOffsetX = GUI.TextField(
                new Rect(100, y, vectorWidth, 25), textureSet.StartOffsetX, texFieldGS);
            GUI.Label(
                new Rect(vectorStart, y, 25, 25), "  Y:", labelGS);
            if (float.TryParse(textureSet.StartOffsetY, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.StartOffsetY = GUI.TextField(
                new Rect(vectorStart + 25, y, vectorWidth, 25), textureSet.StartOffsetY, texFieldGS);
            y += 30;
            GUI.Label(
                new Rect(10, y, 90, 25), "  Speed: X:", labelGS);
            if (float.TryParse(textureSet.SpeedX, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.SpeedX = GUI.TextField(
                new Rect(100, y, vectorWidth, 25), textureSet.SpeedX, texFieldGS);
            GUI.Label(
                new Rect(vectorStart, y, 25, 25), "  Y:", labelGS);
            if (float.TryParse(textureSet.SpeedY, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.SpeedY = GUI.TextField(
                new Rect(vectorStart + 25, y, vectorWidth, 25), textureSet.SpeedY, texFieldGS);
            return y + 30;

        }

    }

    internal class ShaderFloats
    {
        public float FalloffPower;
        public float FalloffScale;
        public float DetailDistance;
        public float MinimumLight;

        public ShaderFloats()
        {
            this.FalloffPower = 0;
            this.FalloffScale = 0;
            this.DetailDistance = 0;
            this.MinimumLight = 0;
        }

        public ShaderFloats(float FalloffPower, float FalloffScale, float DetailDistance, float MinimumLight)
        {
            this.FalloffPower = FalloffPower;
            this.FalloffScale = FalloffScale;
            this.DetailDistance = DetailDistance;
            this.MinimumLight = MinimumLight;
        }

        public ShaderFloats(ConfigNode configNode)
        {
            this.FalloffPower = float.Parse(configNode.GetValue("falloffPower"));
            this.FalloffScale = float.Parse(configNode.GetValue("falloffScale"));
            this.DetailDistance = float.Parse(configNode.GetValue("detailDistance"));
            this.MinimumLight = float.Parse(configNode.GetValue("minimumLight"));
        }

        public void Clone(ShaderFloatsGUI toClone)
        {
            FalloffPower = toClone.FalloffPower;
            FalloffScale = toClone.FalloffScale;
            DetailDistance = toClone.DetailDistance;
            MinimumLight = toClone.MinimumLight;
        }

        internal ConfigNode GetNode(string name)
        {
            ConfigNode newNode = new ConfigNode(name);
            newNode.AddValue("falloffPower", this.FalloffPower);
            newNode.AddValue("falloffScale", this.FalloffScale);
            newNode.AddValue("detailDistance", this.DetailDistance);
            newNode.AddValue("minimumLight", this.MinimumLight);
            return newNode;
        }
    }

    internal class ShaderFloatsGUI
    {
        public float FalloffPower;
        public float FalloffScale;
        public float DetailDistance;
        public float MinimumLight;
        public String FalloffPowerString;
        public String FalloffScaleString;
        public String DetailDistanceString;
        public String MinimumLightString;

        public ShaderFloatsGUI()
        {
            this.FalloffPower = 0;
            this.FalloffScale = 0;
            this.DetailDistance = 0;
            this.MinimumLight = 0;
            FalloffPowerString = "0";
            FalloffScaleString = "0";
            DetailDistanceString = "0";
            MinimumLightString = "0";
        }

        public ShaderFloatsGUI(float FalloffPower, float FalloffScale, float DetailDistance, float MinimumLight)
        {
            this.FalloffPower = FalloffPower;
            this.FalloffScale = FalloffScale;
            this.DetailDistance = DetailDistance;
            this.MinimumLight = MinimumLight;
            FalloffPowerString = FalloffPower.ToString("R");
            FalloffScaleString = FalloffScale.ToString("R");
            DetailDistanceString = DetailDistance.ToString("R");
            MinimumLightString = MinimumLight.ToString("R");
        }

        public void Clone(ShaderFloats toClone)
        {
            FalloffPower = toClone.FalloffPower;
            FalloffScale = toClone.FalloffScale;
            DetailDistance = toClone.DetailDistance;
            MinimumLight = toClone.MinimumLight;
            FalloffPowerString = FalloffPower.ToString("R");
            FalloffScaleString = FalloffScale.ToString("R");
            DetailDistanceString = DetailDistance.ToString("R");
            MinimumLightString = MinimumLight.ToString("R");
        }

        internal void Update(string SFalloffPower, float FFalloffPower, string SFalloffScale, float FFalloffScale, string SDetailDistance, float FDetailDistance, string SMinimumLight, float FMinimumLight)
        {
            if (this.FalloffPowerString != SFalloffPower)
            {
                this.FalloffPowerString = SFalloffPower;
                float.TryParse(SFalloffPower, out this.FalloffPower);
            }
            else if (this.FalloffPower != FFalloffPower)
            {
                this.FalloffPower = FFalloffPower;
                this.FalloffPowerString = FFalloffPower.ToString("R");
            }
            if (this.FalloffScaleString != SFalloffScale)
            {
                this.FalloffScaleString = SFalloffScale;
                float.TryParse(SFalloffScale, out this.FalloffScale);
            }
            else if (this.FalloffScale != FFalloffScale)
            {
                this.FalloffScale = FFalloffScale;
                this.FalloffScaleString = FFalloffScale.ToString("R");
            }
            if (this.DetailDistanceString != SDetailDistance)
            {
                this.DetailDistanceString = SDetailDistance;
                float.TryParse(SDetailDistance, out this.DetailDistance);
            }
            else if (this.DetailDistance != FDetailDistance)
            {
                this.DetailDistance = FDetailDistance;
                this.DetailDistanceString = FDetailDistance.ToString("R");
            }
            if (this.MinimumLightString != SMinimumLight)
            {
                this.MinimumLightString = SMinimumLight;
                float.TryParse(SMinimumLight, out this.MinimumLight);
            }
            else if (this.MinimumLight != FMinimumLight)
            {
                this.MinimumLight = FMinimumLight;
                this.MinimumLightString = FMinimumLight.ToString("R");
            }
        }

        internal bool IsValid()
        {

            float dummy;
            if (float.TryParse(FalloffPowerString, out dummy) &&
                float.TryParse(FalloffScaleString, out dummy) &&
                float.TryParse(DetailDistanceString, out dummy) &&
                float.TryParse(MinimumLightString, out dummy))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    internal class CloudGUI
    {
        public TextureSetGUI MainTexture = new TextureSetGUI();
        public TextureSetGUI DetailTexture = new TextureSetGUI();
        public TextureSetGUI BumpTexture = new TextureSetGUI();
        public ColorSetGUI Color = new ColorSetGUI();
        public RadiusSetGUI Radius = new RadiusSetGUI();
        public ShaderFloatsGUI ShaderFloats = new ShaderFloatsGUI();
        public ShaderFloatsGUI UndersideShaderFloats = new ShaderFloatsGUI();

        internal bool IsValid()
        {
            if (MainTexture.IsValid() &&
                DetailTexture.IsValid() &&
                BumpTexture.IsValid() &&
                Color.IsValid() &&
                Radius.IsValid() &&
                ShaderFloats.IsValid() &&
                UndersideShaderFloats.IsValid())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    internal class CloudLayer
    {
        public static Dictionary<String, List<CloudLayer>> BodyDatabase = new Dictionary<string, List<CloudLayer>>();
        private static Shader GlobalCloudShader;
        private static Shader GlobalUndersideCloudShader;
        private Material CloudMaterial;
        private Material UndersideCloudMaterial;
        private float timeDelta = 0;
        private String body;
        private Color color;
        private float radius;
        private TextureSet mainTexture;
        private TextureSet detailTexture;
        private TextureSet bumpTexture;
        private ShaderFloats shaderFloats;
        private ShaderFloats undersideShaderFloats;
        private Overlay CloudOverlay;
        private Overlay UndersideCloudOverlay;

        public TextureSet MainTexture { get { return mainTexture; } }
        public TextureSet DetailTexture { get { return detailTexture; } }
        public TextureSet BumpTexture { get { return bumpTexture; } }
        public Color Color { get { return color; } }
        public float Radius { get { return radius; } }
        public ShaderFloats ShaderFloats { get { return shaderFloats; } }
        public ShaderFloats UndersideShaderFloats { get { return undersideShaderFloats; } }

        internal void ApplyGUIUpdate(CloudGUI cloudGUI)
        {
            mainTexture.Clone(cloudGUI.MainTexture);
            detailTexture.Clone(cloudGUI.DetailTexture);
            bumpTexture.Clone(cloudGUI.BumpTexture);
            shaderFloats.Clone(cloudGUI.ShaderFloats);
            undersideShaderFloats.Clone(cloudGUI.UndersideShaderFloats);
            radius = cloudGUI.Radius.RadiusF;
            color = cloudGUI.Color.Color;
            UpdateTextures();
            UpdateFloats();
        }

        public CloudLayer(String body, Color color, float radius,
            TextureSet mainTexture,
            TextureSet detailTexture,
            TextureSet bumpTexture,
            ShaderFloats ShaderFloats,
            ShaderFloats UndersideShaderFloats)
        {
            if (!BodyDatabase.ContainsKey(body))
            {
                BodyDatabase.Add(body, new List<CloudLayer>());
            }
            BodyDatabase[body].Add(this);
            this.body = body;
            this.color = color;
            this.radius = radius;
            this.mainTexture = mainTexture;

            this.detailTexture = detailTexture;
            this.bumpTexture = bumpTexture;

            this.shaderFloats = ShaderFloats;
            this.undersideShaderFloats = UndersideShaderFloats;
            Init();
        }

        private void UpdateTextures()
        {
            CloudMaterial.SetTexture("_MainTex", mainTexture.Texture);
            CloudMaterial.SetTextureScale("_MainTex", mainTexture.Scale);
            CloudMaterial.SetColor("_Color", color);

            UndersideCloudMaterial.SetTexture("_MainTex", mainTexture.Texture);
            UndersideCloudMaterial.SetTextureScale("_MainTex", mainTexture.Scale);
            UndersideCloudMaterial.SetColor("_Color", color);

            if (detailTexture.InUse)
            {
                CloudMaterial.SetTexture("_DetailTex", detailTexture.Texture);
                CloudMaterial.SetTextureScale("_DetailTex", detailTexture.Scale);
                UndersideCloudMaterial.SetTexture("_DetailTex", detailTexture.Texture);
                UndersideCloudMaterial.SetTextureScale("_DetailTex", detailTexture.Scale);
            }

            if (bumpTexture.InUse)
            {
                CloudMaterial.SetTexture("_BumpMap", bumpTexture.Texture);
                CloudMaterial.SetTextureScale("_BumpMap", bumpTexture.Scale);
                UndersideCloudMaterial.SetTexture("_BumpMap", bumpTexture.Texture);
                UndersideCloudMaterial.SetTextureScale("_BumpMap", bumpTexture.Scale);
            }
        }

        public void Init()
        {
            if (GlobalCloudShader == null)
            {
                Utils.Log("Initializing Textures");
                Assembly assembly = Assembly.GetExecutingAssembly();
                StreamReader shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream("Clouds.CompiledCloudShader.txt"));
                Utils.Log("reading stream...");
                String shaderTxt = shaderStreamReader.ReadToEnd();
                GlobalCloudShader = new Material(shaderTxt).shader;

                shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream("Clouds.undersideCompiledCloudShader.txt"));
                Utils.Log("reading stream...");
                shaderTxt = shaderStreamReader.ReadToEnd();
                GlobalUndersideCloudShader = new Material(shaderTxt).shader;
            }
            CloudMaterial = new Material(GlobalCloudShader);
            UndersideCloudMaterial = new Material(GlobalUndersideCloudShader);

            UpdateFloats();

            Log("Cloud Material initialized");
            UpdateTextures();
            Log("Generating Overlay...");
            CloudOverlay = Overlay.GeneratePlanetOverlay(body, radius, CloudMaterial, Utils.OVER_LAYER, false, true);
            UndersideCloudOverlay = Overlay.GeneratePlanetOverlay(body, radius, UndersideCloudMaterial, Utils.UNDER_LAYER, false, true);
            Log("Textures initialized");
        }

        public void UpdateFloats()
        {
            if (this.shaderFloats != null)
            {
                CloudMaterial.SetFloat("_FalloffPow", shaderFloats.FalloffPower);
                CloudMaterial.SetFloat("_FalloffScale", shaderFloats.FalloffScale);
                CloudMaterial.SetFloat("_DetailDist", shaderFloats.DetailDistance);
                CloudMaterial.SetFloat("_MinLight", shaderFloats.MinimumLight);
            }
            else
            {
                this.shaderFloats = new ShaderFloats(CloudMaterial.GetFloat("_FalloffPow"), CloudMaterial.GetFloat("_FalloffScale"), CloudMaterial.GetFloat("_DetailDist"), CloudMaterial.GetFloat("_MinLight"));
            }
            if (this.undersideShaderFloats != null)
            {
                UndersideCloudMaterial.SetFloat("_FalloffPow", undersideShaderFloats.FalloffPower);
                UndersideCloudMaterial.SetFloat("_FalloffScale", undersideShaderFloats.FalloffScale);
                UndersideCloudMaterial.SetFloat("_DetailDist", undersideShaderFloats.DetailDistance);
                UndersideCloudMaterial.SetFloat("_MinLight", undersideShaderFloats.MinimumLight);
            }
            else
            {
                this.undersideShaderFloats = new ShaderFloats(UndersideCloudMaterial.GetFloat("_FalloffPow"), UndersideCloudMaterial.GetFloat("_FalloffScale"), UndersideCloudMaterial.GetFloat("_DetailDist"), UndersideCloudMaterial.GetFloat("_MinLight"));
            }
        }

        private void updateOffset(float time)
        {
            float rateOffset = time;

            mainTexture.UpdateOffset(rateOffset);
            CloudMaterial.SetTextureOffset("_MainTex", mainTexture.Offset);
            UndersideCloudMaterial.SetTextureOffset("_MainTex", mainTexture.Offset);

            if (detailTexture.InUse)
            {
                detailTexture.UpdateOffset(rateOffset);
                CloudMaterial.SetTextureOffset("_DetailTex", detailTexture.Offset);
                UndersideCloudMaterial.SetTextureOffset("_DetailTex", detailTexture.Offset);
            }

            if (bumpTexture.InUse)
            {
                bumpTexture.UpdateOffset(rateOffset);
                CloudMaterial.SetTextureOffset("_BumpMap", bumpTexture.Offset);
                UndersideCloudMaterial.SetTextureOffset("_BumpMap", bumpTexture.Offset);
            }

        }

        public void PerformUpdate()
        {
            timeDelta = Time.time - timeDelta;
            float timeOffset = timeDelta * TimeWarp.CurrentRate;

            updateOffset(timeOffset);

            timeDelta = Time.time;
        }

        public static void Log(String message)
        {
            UnityEngine.Debug.Log("Clouds: " + message);
        }

        public static int GetBodyLayerCount(string p)
        {
            if (BodyDatabase.ContainsKey(p))
            {
                return BodyDatabase[p].Count;
            }
            else
            {
                return 0;
            }
        }

        public static String[] GetBodyLayerStringList(string p)
        {
            if (BodyDatabase.ContainsKey(p))
            {
                int count = BodyDatabase[p].Count;
                String[] layerList = new String[count];
                for (int i = 0; i < count; i++)
                {
                    layerList[i] = "Layer " + i;
                }
                return layerList;
            }
            else
            {
                return new String[0];
            }
        }


        internal static void RemoveLayer(string body, int SelectedLayer)
        {
            if (BodyDatabase.ContainsKey(body))
            {
                CloudLayer layer = BodyDatabase[body][SelectedLayer];
                layer.Remove();
            }
        }

        internal static bool IsDefaultShaderFloat(ShaderFloats shaderFloats, bool isUnderside = false)
        {
            Material compareMaterial;
            if (isUnderside)
            {
                compareMaterial = new Material(GlobalUndersideCloudShader);
            }
            else
            {
                compareMaterial = new Material(GlobalCloudShader);
            }
            if (shaderFloats.FalloffPower == compareMaterial.GetFloat("_FalloffPow") &&
                shaderFloats.FalloffScale == compareMaterial.GetFloat("_FalloffScale") &&
                shaderFloats.DetailDistance == compareMaterial.GetFloat("_DetailDist") &&
                shaderFloats.MinimumLight == compareMaterial.GetFloat("_MinLight"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void Remove()
        {
            this.CloudOverlay.RemoveOverlay();
            this.UndersideCloudOverlay.RemoveOverlay();
            BodyDatabase[body].Remove(this);
        }
    }
}
