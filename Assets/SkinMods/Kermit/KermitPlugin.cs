using BepInEx;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Security.Permissions;
using MonoMod.RuntimeDetour.HookGen;
using RoR2.ContentManagement;
using UnityEngine.AddressableAssets;


#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace Kermit
{
    
    [BepInPlugin("com.Xironix.Kermit","Kermit","1.0.0")]
    public partial class KermitPlugin : BaseUnityPlugin
    {
        internal static KermitPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger => Instance?.Logger;
        
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        private void Start()
        {
            Instance = this;

            BeforeStart();

            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Kermit.xironixkermit"))
            {
                assetBundle = AssetBundle.LoadFromStream(assetStream);
            }

            BodyCatalog.availability.CallWhenAvailable(BodyCatalogInit);
            HookEndpointManager.Add(typeof(Language).GetMethod(nameof(Language.LoadStrings)), (Action<Action<Language>, Language>)LanguageLoadStrings);

            ReplaceShaders();

            AfterStart();
        }

        partial void BeforeStart();
        partial void AfterStart();
        static partial void BeforeBodyCatalogInit();
        static partial void AfterBodyCatalogInit();

        private static void ReplaceShaders()
        {
            LoadMaterialsWithReplacedShader(@"RoR2/Base/Shaders/HGStandard.shader"
                ,@"Assets/Resources/Combined.mat");
        }

        private static void LoadMaterialsWithReplacedShader(string shaderPath, params string[] materialPaths)
        {
            var shader = Addressables.LoadAssetAsync<Shader>(shaderPath).WaitForCompletion();
            foreach (var materialPath in materialPaths)
            {
                var material = assetBundle.LoadAsset<Material>(materialPath);
                material.shader = shader;
                materialsWithRoRShader.Add(material);
            }
        }

        private static void LanguageLoadStrings(Action<Language> orig, Language self)
        {
            orig(self);

            self.SetStringByToken("XIRONIX_SKIN_COMMANDOSKIN_NAME", "Kermit");
            self.SetStringByToken("XIRONIX_SKIN_HUNTRESSSKIN_NAME", "Kermit");
            self.SetStringByToken("XIRONIX_SKIN_RAILGUNNERSKIN_NAME", "Kermit");
            self.SetStringByToken("XIRONIX_SKIN_CAPTAINSKIN_NAME", "Kermit");
            self.SetStringByToken("XIRONIX_SKIN_LOADERSKIN_NAME", "Kermit");
            self.SetStringByToken("XIRONIX_SKIN_BANDITSKIN_NAME", "Kermit");
        }

        private static void Nothing(Action<SkinDef> orig, SkinDef self)
        {

        }

        private static void BodyCatalogInit()
        {
            BeforeBodyCatalogInit();

            var awake = typeof(SkinDef).GetMethod(nameof(SkinDef.Awake), BindingFlags.NonPublic | BindingFlags.Instance);
            HookEndpointManager.Add(awake, (Action<Action<SkinDef>, SkinDef>)Nothing);

            AddCommandoBodyCommandoSkinSkin();
            AddHuntressBodyHuntressSkinSkin();
            AddRailgunnerBodyRailgunnerSkinSkin();
            AddCaptainBodyCaptainSkinSkin();
            AddMercBodyMercenarySkinSkin();
            AddLoaderBodyLoaderSkinSkin();
            AddBandit2BodyBanditSkinSkin();
            
            HookEndpointManager.Remove(awake, (Action<Action<SkinDef>, SkinDef>)Nothing);

            AfterBodyCatalogInit();
        }

        static partial void CommandoBodyCommandoSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddCommandoBodyCommandoSkinSkin()
        {
            var bodyName = "CommandoBody";
            var skinName = "CommandoSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_COMMANDOSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[0],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = ContentManager.unlockableDefs.FirstOrDefault(def => def.cachedName == "Kermit");
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[6]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitCommando.mesh"),
                            renderer = renderers[6]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                CommandoBodyCommandoSkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void HuntressBodyHuntressSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddHuntressBodyHuntressSkinSkin()
        {
            var bodyName = "HuntressBody";
            var skinName = "HuntressSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_HUNTRESSSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[0],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = ContentManager.unlockableDefs.FirstOrDefault(def => def.cachedName == "Kermit");
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                            ignoreOverlays = false,
                            renderer = renderers[10]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitHuntress.mesh"),
                            renderer = renderers[10]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                HuntressBodyHuntressSkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void RailgunnerBodyRailgunnerSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddRailgunnerBodyRailgunnerSkinSkin()
        {
            var bodyName = "RailgunnerBody";
            var skinName = "RailgunnerSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_RAILGUNNERSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[0],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = ContentManager.unlockableDefs.FirstOrDefault(def => def.cachedName == "Kermit");
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[3]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[2]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitRailgunner.mesh"),
                            renderer = renderers[2]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitRailgunner.mesh"),
                            renderer = renderers[3]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                RailgunnerBodyRailgunnerSkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void CaptainBodyCaptainSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddCaptainBodyCaptainSkinSkin()
        {
            var bodyName = "CaptainBody";
            var skinName = "CaptainSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_CAPTAINSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[1],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = ContentManager.unlockableDefs.FirstOrDefault(def => def.cachedName == "Kermit");
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[0]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[2]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[5]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[6]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[3]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitCaptain.mesh"),
                            renderer = renderers[0]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitCaptain.mesh"),
                            renderer = renderers[2]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitCaptain.mesh"),
                            renderer = renderers[5]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitCaptain.mesh"),
                            renderer = renderers[6]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitCaptain.mesh"),
                            renderer = renderers[3]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\Empty.mesh"),
                            renderer = renderers[1]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                CaptainBodyCaptainSkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void MercBodyMercenarySkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddMercBodyMercenarySkinSkin()
        {
            var bodyName = "MercBody";
            var skinName = "MercenarySkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_MERCENARYSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[0],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = ContentManager.unlockableDefs.FirstOrDefault(def => def.cachedName == "Kermit");
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[3]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitMercenary.mesh"),
                            renderer = renderers[3]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                MercBodyMercenarySkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void LoaderBodyLoaderSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddLoaderBodyLoaderSkinSkin()
        {
            var bodyName = "LoaderBody";
            var skinName = "LoaderSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_LOADERSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[0],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = null;
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[1]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[2]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitLoader.mesh"),
                            renderer = renderers[1]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitLoader.mesh"),
                            renderer = renderers[2]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                LoaderBodyLoaderSkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void Bandit2BodyBanditSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddBandit2BodyBanditSkinSkin()
        {
            var bodyName = "Bandit2Body";
            var skinName = "BanditSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Icon.png");
                });
                skin.name = skinName;
                skin.nameToken = "XIRONIX_SKIN_BANDITSKIN_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = Array.Empty<SkinDef>();
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = null;
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skin.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Combined.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                            ignoreOverlays = false,
                            renderer = renderers[2]
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skin.meshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\Empty.mesh"),
                            renderer = renderers[0]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\Empty.mesh"),
                            renderer = renderers[1]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\KermitBandit.mesh"),
                            renderer = renderers[2]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\Empty.mesh"),
                            renderer = renderers[3]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\Empty.mesh"),
                            renderer = renderers[6]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Kermit\Meshes\Empty.mesh"),
                            renderer = renderers[7]
                        },
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                Bandit2BodyBanditSkinSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        private static void TryCatchThrow(string message, Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                throw new FieldException(message, e);
            }
        }

        private class FieldException : Exception
        {
            public FieldException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}