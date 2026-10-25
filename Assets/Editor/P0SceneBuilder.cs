using System;
using System.IO;
using System.Linq;
using GEL;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GEL.EditorTools
{
    public static class P0SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/P0_Prototype.unity";
        private const string MaterialFolder = "Assets/Materials";
        private const string PlayerModelPath = "Assets/Models/FreeModule/Characters/ybot.fbx";
        private const string AIModelPath = "Assets/Models/FreeModule/Characters/xbot.fbx";
        private const string DribblePath = "Assets/Models/FreeModule/Actions/Dribble.fbx";
        private const string JogBackwardPath = "Assets/Models/FreeModule/Actions/JogBackward.fbx";
        private const string SoccerSpinPath = "Assets/Models/FreeModule/Actions/SoccerSpin.fbx";
        private const string SoccerTacklePath = "Assets/Models/FreeModule/Actions/SoccerTackle.fbx";
        private const string StandardRunPath = "Assets/Models/FreeModule/Actions/StandardRun.fbx";
        private const string SlowJogBackwardsPath = "Assets/Models/FreeModule/Actions/SlowJogBackwards.fbx";
        private const string RunToStopPath = "Assets/Models/FreeModule/Actions/RunToStop.fbx";
        private const string StrikeForwardJogPath = "Assets/Models/FreeModule/Actions/StrikeForwardJog.fbx";
        private const string DwarfIdlePath = "Assets/Models/FreeModule/Actions/DwarfIdle.fbx";
        private const string WarriorIdlePath = "Assets/Models/FreeModule/Actions/WarriorIdle.fbx";
        private const string Turn180Path = "Assets/Models/FreeModule/Actions/Turn180.fbx";
        private const string TacklePath = "Assets/Models/FreeModule/Actions/Tackle.fbx";
        private const string AIPlayerAnimatorPath = "Assets/Animations/FreeAI.controller";
        private const string BallModelPath = "Assets/Models/FreeModule/Creature/Itm_Scr_Ball00.fbx";
        private const string BallAlbedoPath = "Assets/Models/FreeModule/Creature/mitm_scr_ball00_alb.png";
        private const string BallNormalPath = "Assets/Models/FreeModule/Creature/mitm_scr_ball00_nrm.png";
        private const string BallModelMaterialPath = "Assets/Materials/BallModel.mat";
        private const string AnimatorFolder = "Assets/Animations";
        private const string PlayerAnimatorPath = "Assets/Animations/FreePlayer.controller";
        private static bool _building;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        private static void AutoBuild()
        {
            if (_building || EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Scene active = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path))
                    EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            Build();
        }

        [MenuItem("Tools/GEL/Build P0 Prototype Scene")]
        public static void Build()
        {
            if (_building || EditorApplication.isPlaying) return;

            _building = true;
            try
            {
                BuildInternal();
            }
            catch (System.Exception exception)
            {
                Debug.LogError("P0 scene build failed: " + exception);
            }
            finally
            {
                _building = false;
            }
        }

        private static void BuildInternal()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets", "Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material pitchMaterial = CreateGrassMaterial();
            Material pitchRunoffMaterial = MakeMaterial("PitchRunoff", new Color(0.035f, 0.16f, 0.075f));
            Material lineMaterial = MakeMaterial("Line", new Color(0.92f, 0.95f, 0.92f));
            Material wallMaterial = MakeMaterial("Wall", new Color(0.055f, 0.07f, 0.09f));
            Material techWallMaterial = CreateTechMaterial(
                "TechWall", new Color(0.10f, 0.42f, 1f, 0.32f),
                new Color(0.08f, 0.42f, 1f), 0.32f);
            Texture2D circuitTexture = CreateCircuitTexture();
            techWallMaterial.mainTexture = circuitTexture;
            techWallMaterial.mainTextureScale = new Vector2(6f, 2.5f);
            Material techGlowMaterial = CreateTechMaterial(
                "TechGlow", new Color(0.35f, 0.85f, 1f, 0.9f),
                new Color(0.15f, 0.75f, 1f), 0.1f);
            Material domeMaterial = CreateDomeMaterial();
            Material domePatternMaterial = CreateDomePatternMaterial();
            Material playerMaterial = MakeMaterial("Player", new Color(0.15f, 0.45f, 1f));
            Material aiMaterial = MakeMaterial("AI", new Color(0.95f, 0.25f, 0.2f));
            Material ballMaterial = MakeMaterial("Ball", new Color(0.95f, 0.95f, 0.9f));
            Material skinMaterial = MakeMaterial("Skin", new Color(0.82f, 0.62f, 0.48f));
            Material shortsMaterial = MakeMaterial("Shorts", new Color(0.07f, 0.09f, 0.14f));
            Material socksMaterial = MakeMaterial("Socks", new Color(0.9f, 0.9f, 0.86f));
            Material bootsMaterial = MakeMaterial("Boots", new Color(0.03f, 0.03f, 0.04f));
            Material hairMaterial = MakeMaterial("Hair", new Color(0.05f, 0.035f, 0.025f));

            PhysicMaterial ballPhysics = MakePhysicMaterial("StadiumBallPhysics", 0.35f, 0.15f);
            PhysicMaterial groundPhysics = MakePhysicMaterial("StadiumGrassPhysics", 0.35f, 0.40f);
            PhysicMaterial boundaryPhysics = MakePhysicMaterial("StadiumBoundaryPhysics", 0.66f, 0.12f);

            // Playing field returns to the previous 42 x 26 proportions. The visible
            // stadium adds runoff and the sealed outer shell around it.
            CreatePlane("StadiumGround", Vector3.zero,
                new Vector3(5.0f, 1f, 3.4f), pitchRunoffMaterial, groundPhysics);
            CreatePlane("Pitch", new Vector3(0f, 0.01f, 0f),
                new Vector3(4.2f, 1f, 2.6f), pitchMaterial, groundPhysics);

            CreateStadiumShell(
                wallMaterial, techWallMaterial, techGlowMaterial,
                domeMaterial, domePatternMaterial, boundaryPhysics);
            CreateCornerPylons(wallMaterial, techWallMaterial, techGlowMaterial);
            CreatePitchMarkings(lineMaterial);

            var glowObject = new GameObject("StadiumGlow");
            var glowController = glowObject.AddComponent<StadiumGlow>();
            glowController.Materials = new[]
            {
                domePatternMaterial,
                techWallMaterial,
                techGlowMaterial
            };

            GameObject managerObject = new GameObject("GameManager");
            var match = managerObject.AddComponent<MatchDirector>();
            var possession = managerObject.AddComponent<PossessionManager>();
            var slowMotion = managerObject.AddComponent<SlowMotionController>();
            var hud = managerObject.AddComponent<PrototypeHUD>();
            var gesture = managerObject.AddComponent<GestureInput>();

            CreateGoal(new Vector3(18f, 0f, 0f), 1f, true, lineMaterial, wallMaterial, match);
            CreateGoal(new Vector3(-18f, 0f, 0f), -1f, false, lineMaterial, wallMaterial, match);

            RuntimeAnimatorController[] animators = PreparePlayerAnimator();
            RuntimeAnimatorController playerAnimator = animators != null && animators.Length > 0 ? animators[0] : null;
            RuntimeAnimatorController aiAnimator = animators != null && animators.Length > 1 ? animators[1] : playerAnimator;

            PlayerAgent player = CreateAgent("Player", new Vector3(-6f, 1f, 0f), PlayerModelPath, playerMaterial, skinMaterial, shortsMaterial, socksMaterial, bootsMaterial, hairMaterial, playerAnimator, true);
            PlayerAgent ai = CreateAgent("AI", new Vector3(8f, 1f, 0f), AIModelPath, aiMaterial, skinMaterial, shortsMaterial, socksMaterial, bootsMaterial, hairMaterial, aiAnimator, false);

            GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.name = "Ball";
            ballObject.transform.position = new Vector3(0f, 0.11f, 0f);
            ballObject.transform.localScale = Vector3.one * 0.22f;
            ballObject.GetComponent<Renderer>().sharedMaterial = ballMaterial;

            GameObject ballModel = AssetDatabase.LoadAssetAtPath<GameObject>(BallModelPath);
            if (ballModel != null)
            {
                GameObject ballVisual = UnityEngine.Object.Instantiate(ballModel);
                if (ballVisual != null)
                {
                    ballVisual.name = "BallVisual";

                    Renderer[] visualRenderers = ballVisual.GetComponentsInChildren<Renderer>(true);
                    Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
                    float modelScale = 1f;
                    bool hasBounds = false;

                    if (visualRenderers.Length > 0)
                    {
                        bounds = visualRenderers[0].bounds;
                        for (int i = 1; i < visualRenderers.Length; i++)
                            bounds.Encapsulate(visualRenderers[i].bounds);

                        float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                        if (maxExtent > 0.0001f)
                        {
                            modelScale = 1f / maxExtent;
                            hasBounds = true;
                        }
                    }

                    ballVisual.transform.SetParent(ballObject.transform, false);
                    ballVisual.transform.localRotation = Quaternion.identity;
                    ballVisual.transform.localScale = Vector3.one * modelScale;
                    ballVisual.transform.localPosition = hasBounds
                        ? -bounds.center * modelScale
                        : Vector3.zero;

                    Material ballModelMaterial = CreateBallModelMaterial();
                    if (ballModelMaterial != null)
                    {
                        Renderer[] modelRenderers = ballVisual.GetComponentsInChildren<Renderer>(true);
                        for (int i = 0; i < modelRenderers.Length; i++)
                            modelRenderers[i].sharedMaterial = ballModelMaterial;
                    }

                    Collider[] modelColliders = ballVisual.GetComponentsInChildren<Collider>(true);
                    for (int i = 0; i < modelColliders.Length; i++)
                        UnityEngine.Object.DestroyImmediate(modelColliders[i]);

                    Rigidbody[] modelBodies = ballVisual.GetComponentsInChildren<Rigidbody>(true);
                    for (int i = 0; i < modelBodies.Length; i++)
                        UnityEngine.Object.DestroyImmediate(modelBodies[i]);

                    ballObject.GetComponent<Renderer>().enabled = false;
                }
            }

            SphereCollider ballCollider = ballObject.GetComponent<SphereCollider>();
            if (ballCollider != null) ballCollider.material = ballPhysics;

            var ballRigidbody = ballObject.AddComponent<Rigidbody>();
            ballRigidbody.mass = 0.45f;
            ballRigidbody.drag = 0.025f;
            ballRigidbody.angularDrag = 0.18f;
            ballRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            ballRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var ball = ballObject.AddComponent<BallSimulator>();
            ball.Rb = ballRigidbody;
            ball.RestHeight = 0.11f;

            match.PlayerAgent = player;
            match.AIAgent = ai;
            match.Ball = ball;
            match.Possession = possession;

            possession.Ball = ball;
            possession.Agents = new[] { player, ai };

            var playerMotor = player.gameObject.AddComponent<PlayerMotor>();
            playerMotor.Agent = player;
            playerMotor.Possession = possession;
            playerMotor.Ball = ball;
            playerMotor.SlowMotion = slowMotion;

            var weightSystem = player.gameObject.AddComponent<WeightSystem>();
            weightSystem.Agent = player;
            weightSystem.Motor = playerMotor;
            playerMotor.Weight = weightSystem;
            player.Weight = weightSystem;

            var playerAttackGoal = new GameObject("Player_AttackGoal").transform;
            playerAttackGoal.position = new Vector3(18f, 0f, 0f);
            playerMotor.AttackGoal = playerAttackGoal;

            var aiController = ai.gameObject.AddComponent<AIController>();
            aiController.Agent = ai;
            aiController.Possession = possession;
            aiController.Ball = ball;
            aiController.Human = player;

            var attackGoal = new GameObject("AI_AttackGoal").transform;
            attackGoal.position = new Vector3(-18f, 0f, 0f);
            var defendGoal = new GameObject("AI_DefendGoal").transform;
            defendGoal.position = new Vector3(18f, 0f, 0f);
            aiController.AttackGoal = attackGoal;
            aiController.DefendGoal = defendGoal;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.7f, 0.9f);
            cameraObject.transform.position = new Vector3(-13f, 4.8f, 0.9f);

            var shoulderCamera = cameraObject.AddComponent<ShoulderCamera>();
            shoulderCamera.Target = player.transform;
            shoulderCamera.Possession = possession;
            shoulderCamera.Ball = ball;
            shoulderCamera.PlayerAgent = player;
            shoulderCamera.Snap();
            match.CameraRig = shoulderCamera;
            playerMotor.CameraRig = shoulderCamera;

            var trailObject = new GameObject("GestureTrail");
            var trail = trailObject.AddComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.startWidth = 0.07f;
            trail.endWidth = 0.07f;
            trail.positionCount = 0;
            trail.sharedMaterial = MakeTrailMaterial();
            trail.startColor = new Color(0.2f, 1f, 1f, 1f);
            trail.endColor = new Color(0.2f, 1f, 1f, 0f);

            gesture.Player = playerMotor;
            gesture.Cam = camera;
            gesture.Trail = trail;

            var mobileControls = managerObject.AddComponent<MobileControls>();
            mobileControls.Player = playerMotor;
            mobileControls.Gesture = gesture;

            hud.Match = match;
            hud.Possession = possession;
            hud.Gesture = gesture;
            hud.AI = aiController;
            hud.SlowMotion = slowMotion;

            slowMotion.Match = match;
            slowMotion.Player = player;
            slowMotion.AI = aiController;
            slowMotion.Gesture = gesture;

            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.4f, 0.45f);

            PlayerSettings.productName = "GEL Prototype";
            PlayerSettings.companyName = "Portfolio";

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("P0 prototype scene created: " + ScenePath);
        }

        private static PlayerAgent CreateAgent(
            string name,
            Vector3 position,
            string modelPath,
            Material jersey,
            Material skin,
            Material shorts,
            Material socks,
            Material boots,
            Material hair,
            RuntimeAnimatorController animatorController,
            bool isHuman)
        {
            var agentObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            agentObject.name = name;
            agentObject.transform.position = position;
            agentObject.transform.localScale = Vector3.one;

            var capsule = agentObject.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height = 2f;
                capsule.radius = 0.4f;
            }

            // Keep the capsule as the physics body, but hide its mesh and use a humanoid visual.
            var capsuleRenderer = agentObject.GetComponent<MeshRenderer>();
            if (capsuleRenderer != null)
                capsuleRenderer.enabled = false;

            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(agentObject.transform, false);

            bool usingModel = TryCreateModelVisual(
                visualRoot, modelPath, jersey, skin, shorts, socks, boots, hair, animatorController);

            if (!usingModel)
            {
                BuildHumanoidVisual(visualRoot, jersey, skin, shorts, socks, boots, hair);
                visualRoot.gameObject.AddComponent<PlayerVisual>();
            }

            var rigidbody = agentObject.AddComponent<Rigidbody>();
            rigidbody.mass = 70f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0.05f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var agent = agentObject.AddComponent<PlayerAgent>();
            agent.IsHuman = isHuman;
            agent.ControlEnabled = true;

            Transform ballFoot = null;
            Transform ballToe = null;
            Transform leftFoot = null;
            if (usingModel)
            {
                Transform[] bones = visualRoot.GetComponentsInChildren<Transform>(true);
                ballFoot = bones.FirstOrDefault(t => t.name == "mixamorig:RightFoot");
                if (ballFoot == null)
                    ballFoot = bones.FirstOrDefault(t => t.name == "mixamorig:RightToeBase");

                ballToe = bones.FirstOrDefault(t => t.name == "mixamorig:RightToe_End" || t.name == "RightToe_End");
                if (ballToe == null)
                    ballToe = bones.FirstOrDefault(t => t.name == "mixamorig:RightToeBase" || t.name == "RightToeBase");

                leftFoot = bones.FirstOrDefault(t => t.name == "mixamorig:LeftFoot");
                if (leftFoot == null)
                    leftFoot = bones.FirstOrDefault(t => t.name == "mixamorig:LeftToeBase");
            }

            if (ballFoot == null)
            {
                var anchor = new GameObject("BallFoot").transform;
                anchor.SetParent(visualRoot, false);
                anchor.localPosition = new Vector3(0.14f, -0.95f, 0.08f);
                ballFoot = anchor;
            }

            if (leftFoot == null)
            {
                var anchor = new GameObject("LeftFoot").transform;
                anchor.SetParent(visualRoot, false);
                anchor.localPosition = new Vector3(-0.14f, -0.95f, 0.08f);
                leftFoot = anchor;
            }

            agent.BallFoot = ballFoot;
            agent.BallToe = ballToe != null ? ballToe : ballFoot;
            AttachFootTouch(ballFoot, FootSide.Right, agent);
            AttachFootTouch(leftFoot, FootSide.Left, agent);

            if (usingModel)
            {
                var animationDriver = visualRoot.gameObject.AddComponent<PlayerAnimationDriver>();
                animationDriver.Agent = agent;
                animationDriver.Animator = visualRoot.GetComponentInChildren<Animator>();
            }

            return agent;
        }

        private static void AttachFootTouch(Transform foot, FootSide side, PlayerAgent agent)
        {
            if (foot == null || agent == null) return;

            SphereCollider trigger = foot.GetComponent<SphereCollider>();
            if (trigger == null)
                trigger = foot.gameObject.AddComponent<SphereCollider>();

            trigger.isTrigger = true;
            trigger.radius = 0.12f;
            trigger.center = Vector3.zero;

            FootTouch touch = foot.GetComponent<FootTouch>();
            if (touch == null)
                touch = foot.gameObject.AddComponent<FootTouch>();

            touch.Agent = agent;
            touch.Side = side;
        }

        private static bool TryCreateModelVisual(
            Transform visualRoot,
            string modelPath,
            Material jersey,
            Material skin,
            Material shorts,
            Material socks,
            Material boots,
            Material hair,
            RuntimeAnimatorController animatorController)
        {
            if (animatorController == null) return false;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
            {
                Debug.LogWarning("Player FBX not found at " + modelPath + ", using procedural fallback.");
                return false;
            }

            GameObject model = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (model == null) return false;

            model.name = "PlayerModel";
            model.transform.SetParent(visualRoot, false);
            model.transform.localPosition = new Vector3(0f, -0.95f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            OverrideModelMaterials(model, jersey, skin, shorts, socks, boots, hair);

            Animator animator = model.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar != null)
                animator.avatar = avatar;
            else
                Debug.LogWarning("No Humanoid avatar found for " + modelPath);

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            return true;
        }

        private static void OverrideModelMaterials(
            GameObject model,
            Material jersey,
            Material skin,
            Material shorts,
            Material socks,
            Material boots,
            Material hair)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i] != null
                        ? materials[i].name.ToLowerInvariant()
                        : string.Empty;

                    if (materialName.Contains("jersey") || materialName.Contains("player")) materials[i] = jersey;
                    else if (materialName.Contains("skin")) materials[i] = skin;
                    else if (materialName.Contains("shorts")) materials[i] = shorts;
                    else if (materialName.Contains("socks")) materials[i] = socks;
                    else if (materialName.Contains("boots")) materials[i] = boots;
                    else if (materialName.Contains("hair")) materials[i] = hair;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static RuntimeAnimatorController[] PreparePlayerAnimator()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Models", "FreeModule", "Characters", "ybot.fbx")) ||
                !File.Exists(Path.Combine(Application.dataPath, "Models", "FreeModule", "Characters", "xbot.fbx")))
            {
                Debug.LogWarning("FreeModule character FBX missing.");
                return null;
            }

            ConfigureHumanoid(PlayerModelPath);
            ConfigureHumanoid(AIModelPath);

            ConfigureAction(StandardRunPath, "StandardRun", true);
            ConfigureAction(SlowJogBackwardsPath, "SlowJogBackwards", true);
            ConfigureAction(RunToStopPath, "RunToStop", false);
            ConfigureAction(StrikeForwardJogPath, "StrikeForwardJog", false);
            ConfigureAction(DwarfIdlePath, "DwarfIdle", true);
            ConfigureAction(WarriorIdlePath, "WarriorIdle", true);
            ConfigureAction(Turn180Path, "Turn180", false);
            ConfigureAction(TacklePath, "Tackle", false);

            AnimationClip standardRun = LoadClip(StandardRunPath, "StandardRun");
            AnimationClip slowBack = LoadClip(SlowJogBackwardsPath, "SlowJogBackwards");
            AnimationClip runToStop = LoadClip(RunToStopPath, "RunToStop");
            AnimationClip strike = LoadClip(StrikeForwardJogPath, "StrikeForwardJog");
            AnimationClip playerIdle = LoadClip(DwarfIdlePath, "DwarfIdle");
            AnimationClip aiIdle = LoadClip(WarriorIdlePath, "WarriorIdle");
            AnimationClip turn180 = LoadClip(Turn180Path, "Turn180");
            AnimationClip tackle = LoadClip(TacklePath, "Tackle");

            if (standardRun == null || slowBack == null || runToStop == null ||
                strike == null || playerIdle == null || aiIdle == null ||
                turn180 == null || tackle == null)
            {
                Debug.LogWarning("FreeModule animation set incomplete.");
                return null;
            }

            RuntimeAnimatorController playerController = CreateFreeController(
                PlayerAnimatorPath, playerIdle, standardRun, slowBack, runToStop, strike,
                turn180, tackle);

            RuntimeAnimatorController aiController = CreateFreeController(
                AIPlayerAnimatorPath, aiIdle, standardRun, slowBack, runToStop, strike,
                turn180, tackle);

            return new[] { playerController, aiController };
        }

        private static void ConfigureHumanoid(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();

            Avatar[] avatars = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().ToArray();
            foreach (Avatar avatar in avatars)
                Debug.Log("FREE_AVATAR " + path + " " + avatar.name + " human=" + avatar.isHuman + " valid=" + avatar.isValid);
        }

        private static Material CreateBallModelMaterial()
        {
            Texture2D albedo = LoadBallTexture(BallAlbedoPath, false, true);
            Texture2D normal = LoadBallTexture(BallNormalPath, true, false);

            Shader shader = Shader.Find("Standard");
            if (shader == null) return null;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(BallModelMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, BallModelMaterialPath);
            }

            material.shader = shader;
            material.color = Color.white;
            material.mainTexture = albedo;
            material.SetFloat("_Glossiness", 0.3f);
            material.SetFloat("_Metallic", 0f);

            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Texture2D LoadBallTexture(string path, bool isNormal, bool sRGB)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                TextureImporterType type = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != type)
                {
                    importer.textureType = type;
                    changed = true;
                }

                if (importer.sRGBTexture != sRGB)
                {
                    importer.sRGBTexture = sRGB;
                    changed = true;
                }

                if (changed)
                    importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ConfigureAction(string path, string clipName, bool loop)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            bool clipChanged = false;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    string desiredName = i == 0 ? clipName : clipName + "_" + i;
                    if (clips[i].name != desiredName)
                    {
                        clips[i].name = desiredName;
                        clipChanged = true;
                    }

                    if (clips[i].loopTime != loop)
                    {
                        clips[i].loopTime = loop;
                        clipChanged = true;
                    }
                }
            }

            if (clipChanged)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }

            AnimationClip[] loaded = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .ToArray();
            foreach (AnimationClip clip in loaded)
                Debug.Log($"FREE_CLIP_READY path={path} name={clip.name} len={clip.length:0.00} " +
                          $"human={clip.isHumanMotion}");
        }

        private static AnimationClip LoadClip(string path, string nameHint)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();

            AnimationClip clip = clips.FirstOrDefault(
                candidate => candidate.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0);
            return clip ?? clips.FirstOrDefault();
        }

        private static RuntimeAnimatorController CreateFreeController(
            string controllerPath,
            AnimationClip idleClip,
            AnimationClip standardRun,
            AnimationClip slowBack,
            AnimationClip runToStop,
            AnimationClip strike,
            AnimationClip turn180,
            AnimationClip tackle)
        {
            if (!AssetDatabase.IsValidFolder(AnimatorFolder))
                AssetDatabase.CreateFolder("Assets", "Animations");

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Backward", AnimatorControllerParameterType.Bool);
            controller.AddParameter("S180", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Braking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Tackle", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AnimatorState idle = stateMachine.AddState("Idle");
            idle.motion = idleClip;

            AnimatorState forward = stateMachine.AddState("JogForward");
            forward.motion = standardRun;

            AnimatorState backward = stateMachine.AddState("JogBackward");
            backward.motion = slowBack;

            AnimatorState turn180State = stateMachine.AddState("Turn180");
            turn180State.motion = turn180;

            AnimatorState brake = stateMachine.AddState("Brake");
            brake.motion = runToStop;

            AnimatorState shoot = stateMachine.AddState("Shoot");
            shoot.motion = strike;

            AnimatorState tackleState = stateMachine.AddState("Tackle");
            tackleState.motion = tackle;
            tackleState.speed = 1.5f;

            stateMachine.defaultState = idle;

            AnimatorStateTransition transition = Link(idle, forward, 0.12f, false);
            transition.AddCondition(AnimatorConditionMode.Greater, 0.20f, "Speed");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Backward");

            transition = Link(idle, backward, 0.12f, false);
            transition.AddCondition(AnimatorConditionMode.Greater, 0.20f, "Speed");
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Backward");

            transition = Link(forward, backward, 0.10f, false);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Backward");

            transition = Link(backward, forward, 0.10f, false);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Backward");

            transition = Link(forward, idle, 0.12f, false);
            transition.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            transition = Link(backward, idle, 0.12f, false);
            transition.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            transition = LinkAny(stateMachine, turn180State, 0.08f, false);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "S180");

            transition = Link(turn180State, idle, 0.15f, true, 0.85f);

            transition = LinkAny(stateMachine, brake, 0.05f, false);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Braking");

            transition = Link(brake, idle, 0.10f, false);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Braking");
            transition.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            transition = Link(brake, forward, 0.10f, false);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Braking");
            transition.AddCondition(AnimatorConditionMode.Greater, 0.20f, "Speed");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Backward");

            transition = Link(brake, backward, 0.10f, false);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Braking");
            transition.AddCondition(AnimatorConditionMode.Greater, 0.20f, "Speed");
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Backward");

            transition = LinkAny(stateMachine, shoot, 0.05f, false);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

            transition = Link(shoot, idle, 0.15f, true, 0.85f);

            transition = LinkAny(stateMachine, tackleState, 0.05f, false);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Tackle");

            transition = Link(tackleState, idle, 0.10f, true, 0.75f);

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorStateTransition Link(
            AnimatorState from,
            AnimatorState to,
            float duration,
            bool hasExitTime,
            float exitTime = 1f)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.hasFixedDuration = true;
            return transition;
        }

        private static AnimatorStateTransition LinkAny(
            AnimatorStateMachine stateMachine,
            AnimatorState to,
            float duration,
            bool hasExitTime,
            float exitTime = 1f)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = false;
            return transition;
        }

        private static void BuildHumanoidVisual(
            Transform root,
            Material jersey,
            Material skin,
            Material shorts,
            Material socks,
            Material boots,
            Material hair)
        {
            CreatePart(PrimitiveType.Cube, "Hips", root, new Vector3(0f, -0.18f, 0f), new Vector3(0.36f, 0.2f, 0.24f), Quaternion.identity, shorts);
            CreatePart(PrimitiveType.Cube, "Torso", root, new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.55f, 0.28f), Quaternion.identity, jersey);
            CreatePart(PrimitiveType.Cube, "Chest", root, new Vector3(0f, 0.4f, 0.02f), new Vector3(0.54f, 0.2f, 0.3f), Quaternion.identity, jersey);

            CreatePart(PrimitiveType.Cylinder, "Neck", root, new Vector3(0f, 0.52f, 0f), new Vector3(0.12f, 0.1f, 0.12f), Quaternion.identity, skin);
            CreatePart(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 0.7f, 0.03f), new Vector3(0.3f, 0.32f, 0.3f), Quaternion.identity, skin);
            CreatePart(PrimitiveType.Sphere, "Hair", root, new Vector3(0f, 0.76f, -0.04f), new Vector3(0.32f, 0.3f, 0.32f), Quaternion.identity, hair);

            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? "L" : "R";

                CreatePart(PrimitiveType.Sphere, "Shoulder_" + suffix, root,
                    new Vector3(0.3f * side, 0.38f, 0f), new Vector3(0.16f, 0.16f, 0.16f), Quaternion.identity, jersey);
                CreatePart(PrimitiveType.Capsule, "UpperArm_" + suffix, root,
                    new Vector3(0.35f * side, 0.18f, 0f), new Vector3(0.12f, 0.28f, 0.12f), Quaternion.Euler(0f, 0f, -12f * side), jersey);
                CreatePart(PrimitiveType.Capsule, "Forearm_" + suffix, root,
                    new Vector3(0.41f * side, -0.08f, 0.02f), new Vector3(0.1f, 0.26f, 0.1f), Quaternion.Euler(0f, 0f, -8f * side), skin);
                CreatePart(PrimitiveType.Sphere, "Hand_" + suffix, root,
                    new Vector3(0.44f * side, -0.24f, 0.04f), new Vector3(0.12f, 0.12f, 0.12f), Quaternion.identity, skin);

                CreatePart(PrimitiveType.Capsule, "Thigh_" + suffix, root,
                    new Vector3(0.14f * side, -0.35f, 0f), new Vector3(0.16f, 0.26f, 0.16f), Quaternion.Euler(0f, 0f, 2f * side), shorts);
                CreatePart(PrimitiveType.Capsule, "Knee_" + suffix, root,
                    new Vector3(0.15f * side, -0.58f, 0.01f), new Vector3(0.12f, 0.2f, 0.12f), Quaternion.identity, skin);
                CreatePart(PrimitiveType.Capsule, "Shin_" + suffix, root,
                    new Vector3(0.15f * side, -0.8f, 0.01f), new Vector3(0.12f, 0.24f, 0.12f), Quaternion.identity, socks);
                CreatePart(PrimitiveType.Cube, "Boot_" + suffix, root,
                    new Vector3(0.15f * side, -0.95f, 0.07f), new Vector3(0.14f, 0.09f, 0.3f), Quaternion.identity, boots);
            }
        }

        private static GameObject CreatePart(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            return part;
        }

        private static void CreateGoal(Vector3 basePosition, float outward, bool playerScores, Material frameMaterial, Material netMaterial, MatchDirector match)
        {
            CreateCube("GoalPost_Top", basePosition + new Vector3(0f, 1.1f, 3f), new Vector3(0.18f, 2.2f, 0.18f), frameMaterial, true);
            CreateCube("GoalPost_Bottom", basePosition + new Vector3(0f, 1.1f, -3f), new Vector3(0.18f, 2.2f, 0.18f), frameMaterial, true);
            CreateCube("Crossbar", basePosition + new Vector3(0f, 2.2f, 0f), new Vector3(0.18f, 0.18f, 6f), frameMaterial, true);

            CreateCube("Net_Back", basePosition + new Vector3(outward * 1.8f, 1f, 0f), new Vector3(0.12f, 2f, 6f), netMaterial, true);
            CreateCube("Net_Side_Top", basePosition + new Vector3(outward * 0.9f, 1f, 3f), new Vector3(1.8f, 2f, 0.12f), netMaterial, true);
            CreateCube("Net_Side_Bottom", basePosition + new Vector3(outward * 0.9f, 1f, -3f), new Vector3(1.8f, 2f, 0.12f), netMaterial, true);

            var triggerObject = new GameObject("GoalTrigger");
            triggerObject.transform.position = basePosition + new Vector3(outward * 0.9f, 1f, 0f);
            var box = triggerObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.2f, 2f, 5.6f);

            var goalTrigger = triggerObject.AddComponent<GoalTrigger>();
            goalTrigger.Match = match;
            goalTrigger.PlayerScores = playerScores;
        }

        private static GameObject CreatePlane(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            PhysicMaterial physics)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.position = position;
            plane.transform.localScale = scale;
            plane.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = plane.GetComponent<Collider>();
            if (collider != null) collider.material = physics;

            return plane;
        }

        private static Material CreateGrassMaterial()
        {
            Material material = MakeMaterial("StadiumGrassLawn", Color.white);
            Texture2D texture = CreateGrassTexture();
            material.mainTexture = texture;
            material.color = Color.white;
            material.mainTextureScale = new Vector2(10f, 7f);
            material.mainTextureOffset = Vector2.zero;
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.18f);
            return material;
        }

        private static Texture2D CreateGrassTexture()
        {
            string path = MaterialFolder + "/StadiumGrassTexture2.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float stripe = ((y / 24) % 2 == 0) ? 1f : 0.84f;
                for (int x = 0; x < size; x++)
                {
                    float broad = Mathf.PerlinNoise(x * 0.035f, y * 0.055f);
                    float blades = Mathf.PerlinNoise(x * 0.55f, y * 0.65f);
                    float shade = stripe * (0.78f + broad * 0.34f + blades * 0.08f);
                    pixels[y * size + x] = new Color(
                        0.10f * shade,
                        0.44f * shade,
                        0.16f * shade,
                        1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;
            texture.name = "StadiumGrassTexture";
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Material CreateTechMaterial(
            string name,
            Color color,
            Color emission,
            float smoothness)
        {
            Material material = MakeMaterial(name, color);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.color = color;
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * 1.4f);
            }
            return material;
        }

        private static Material CreateDomeMaterial()
        {
            Material material = MakeMaterial(
                "StadiumDome", new Color(0.06f, 0.30f, 0.78f, 0.22f));
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_EMISSION");
            material.mainTexture = null;
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.color = new Color(0.06f, 0.30f, 0.78f, 0.22f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.55f);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);
            return material;
        }

        private static Material CreateDomePatternMaterial()
        {
            Material material = CreateTechMaterial(
                "StadiumDomePattern",
                new Color(0.008f, 0.025f, 0.08f, 0.78f),
                new Color(0.01f, 0.055f, 0.2f),
                0.4f);

            Texture2D circuit = CreateCircuitTexture();
            material.mainTexture = circuit;
            material.mainTextureScale = new Vector2(4f, 2f);
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;
            return material;
        }

        private static Texture2D CreateCircuitTexture()
        {
            string path = MaterialFolder + "/StadiumCircuit.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color trace = new Color(0.24f, 0.86f, 1f, 0.78f);
            Color node = new Color(0.65f, 1f, 1f, 0.95f);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            var random = new System.Random(9041);
            for (int p = 0; p < 34; p++)
            {
                int x = random.Next(24, size - 24);
                int y = random.Next(24, size - 24);

                int segments = random.Next(2, 6);
                for (int s = 0; s < segments; s++)
                {
                    int length = random.Next(16, 68);
                    bool horizontal = random.Next(0, 2) == 0;
                    int nx = horizontal ? Mathf.Clamp(x + length, 0, size - 1) : x;
                    int ny = horizontal ? y : Mathf.Clamp(y + length, 0, size - 1);

                    DrawCircuitLine(pixels, size, x, y, nx, ny, trace);
                    DrawCircuitNode(pixels, size, nx, ny, node);
                    x = nx;
                    y = ny;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;
            texture.name = "StadiumCircuit";
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static void DrawCircuitLine(
            Color[] pixels,
            int size,
            int x0,
            int y0,
            int x1,
            int y1,
            Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int steps = Mathf.Max(dx, dy);
            if (steps <= 0) return;

            for (int i = 0; i <= steps; i++)
            {
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, i / (float)steps));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, i / (float)steps));

                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        if (ox != 0 && oy != 0) continue;
                        int px = x + ox;
                        int py = y + oy;
                        if (px < 0 || py < 0 || px >= size || py >= size) continue;
                        pixels[py * size + px] = color;
                    }
                }
            }
        }

        private static void DrawCircuitNode(
            Color[] pixels,
            int size,
            int cx,
            int cy,
            Color color)
        {
            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) > 2) continue;
                    int px = cx + x;
                    int py = cy + y;
                    if (px < 0 || py < 0 || px >= size || py >= size) continue;
                    pixels[py * size + px] = color;
                }
            }
        }

        private static void CreateStadiumShell(
            Material wallMaterial,
            Material techMaterial,
            Material glowMaterial,
            Material domeMaterial,
            Material domePatternMaterial,
            PhysicMaterial boundaryPhysics)
        {
            const float halfX = 22f;
            const float halfZ = 15f;
            const float shellHeight = 12f;

            // Invisible colliders keep the stadium sealed; there is no visible wall.
            CreateInvisibleBoundary("Boundary_North",
                new Vector3(0f, shellHeight * 0.5f, halfZ),
                new Vector3(halfX * 2f + 0.8f, shellHeight, 0.5f), boundaryPhysics);
            CreateInvisibleBoundary("Boundary_South",
                new Vector3(0f, shellHeight * 0.5f, -halfZ),
                new Vector3(halfX * 2f + 0.8f, shellHeight, 0.5f), boundaryPhysics);
            CreateInvisibleBoundary("Boundary_East",
                new Vector3(halfX, shellHeight * 0.5f, 0f),
                new Vector3(0.5f, shellHeight, halfZ * 2f + 0.8f), boundaryPhysics);
            CreateInvisibleBoundary("Boundary_West",
                new Vector3(-halfX, shellHeight * 0.5f, 0f),
                new Vector3(0.5f, shellHeight, halfZ * 2f + 0.8f), boundaryPhysics);
            CreateInvisibleBoundary("Boundary_Ceiling",
                new Vector3(0f, shellHeight + 0.2f, 0f),
                new Vector3(halfX * 2f + 0.8f, 0.4f, halfZ * 2f + 0.8f), boundaryPhysics);

            // The playing lines are the rebound surface; the dome is the only
            // visible enclosure.
            CreateInvisibleBoundary("LineBoundary_TouchNorth",
                new Vector3(0f, shellHeight * 0.5f, 12f),
                new Vector3(36f, shellHeight, 0.32f), boundaryPhysics);
            CreateInvisibleBoundary("LineBoundary_TouchSouth",
                new Vector3(0f, shellHeight * 0.5f, -12f),
                new Vector3(36f, shellHeight, 0.32f), boundaryPhysics);

            CreateInvisibleBoundary("LineBoundary_EastNorth",
                new Vector3(18f, shellHeight * 0.5f, 7.5f),
                new Vector3(0.32f, shellHeight, 9f), boundaryPhysics);
            CreateInvisibleBoundary("LineBoundary_EastSouth",
                new Vector3(18f, shellHeight * 0.5f, -7.5f),
                new Vector3(0.32f, shellHeight, 9f), boundaryPhysics);
            CreateInvisibleBoundary("LineBoundary_WestNorth",
                new Vector3(-18f, shellHeight * 0.5f, 7.5f),
                new Vector3(0.32f, shellHeight, 9f), boundaryPhysics);
            CreateInvisibleBoundary("LineBoundary_WestSouth",
                new Vector3(-18f, shellHeight * 0.5f, -7.5f),
                new Vector3(0.32f, shellHeight, 9f), boundaryPhysics);

            GameObject dome = new GameObject("StadiumDome");
            dome.transform.position = Vector3.zero;
            var meshFilter = dome.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateDomeMesh();
            var domeRenderer = dome.AddComponent<MeshRenderer>();
            domeRenderer.sharedMaterial = domeMaterial;
            domeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            domeRenderer.receiveShadows = false;

            // Dark circuit pattern sits just outside the blue shell. The pattern
            // material is the only dome part that breathes.
            var pattern = new GameObject("StadiumDomePattern");
            pattern.transform.SetParent(dome.transform, false);
            pattern.transform.localScale = Vector3.one * 1.002f;
            var patternFilter = pattern.AddComponent<MeshFilter>();
            patternFilter.sharedMesh = meshFilter.sharedMesh;
            var patternRenderer = pattern.AddComponent<MeshRenderer>();
            patternRenderer.sharedMaterial = domePatternMaterial;
            patternRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            patternRenderer.receiveShadows = false;
        }

        private static void CreateDomeRibs(Material material)
        {
            const float baseY = 8f;
            const float halfX = 22f;
            const float halfZ = 15f;
            const float height = 5.2f;

            // Barrel-vault ribs follow the capsule's cross-section.
            for (int r = 0; r < 7; r++)
            {
                float x = Mathf.Lerp(-18f, 18f, r / 6f);
                var ribObject = new GameObject("DomeRib_" + r);
                var rib = ribObject.AddComponent<LineRenderer>();
                rib.useWorldSpace = true;
                rib.sharedMaterial = material;
                rib.startWidth = 0.045f;
                rib.endWidth = 0.045f;
                rib.startColor = new Color(0.3f, 0.9f, 1f, 0.8f);
                rib.endColor = new Color(0.3f, 0.9f, 1f, 0.8f);
                rib.positionCount = 25;

                for (int i = 0; i < rib.positionCount; i++)
                {
                    float t = i / (float)(rib.positionCount - 1);
                    float theta = t * Mathf.PI;
                    rib.SetPosition(i, new Vector3(
                        x,
                        baseY + Mathf.Sin(theta) * height,
                        Mathf.Cos(theta) * halfZ));
                }
            }

            // A ridge line running the length of the vault.
            var ridgeObject = new GameObject("DomeRidge");
            var ridge = ridgeObject.AddComponent<LineRenderer>();
            ridge.useWorldSpace = true;
            ridge.sharedMaterial = material;
            ridge.startWidth = 0.05f;
            ridge.endWidth = 0.05f;
            ridge.startColor = new Color(0.3f, 0.9f, 1f, 0.9f);
            ridge.endColor = new Color(0.3f, 0.9f, 1f, 0.9f);
            ridge.positionCount = 2;
            ridge.SetPosition(0, new Vector3(-halfX, baseY + height, 0f));
            ridge.SetPosition(1, new Vector3(halfX, baseY + height, 0f));

            // Base rim exactly on the wall plane.
            var rimObject = new GameObject("DomeRim");
            var rim = rimObject.AddComponent<LineRenderer>();
            rim.useWorldSpace = true;
            rim.sharedMaterial = material;
            rim.startWidth = 0.06f;
            rim.endWidth = 0.06f;
            rim.startColor = new Color(0.3f, 0.9f, 1f, 0.9f);
            rim.endColor = new Color(0.3f, 0.9f, 1f, 0.9f);
            rim.loop = true;
            rim.positionCount = 4;
            rim.SetPosition(0, new Vector3(-halfX, baseY + 0.03f, halfZ));
            rim.SetPosition(1, new Vector3(halfX, baseY + 0.03f, halfZ));
            rim.SetPosition(2, new Vector3(halfX, baseY + 0.03f, -halfZ));
            rim.SetPosition(3, new Vector3(-halfX, baseY + 0.03f, -halfZ));
        }

        private static void CreateCornerPylons(
            Material baseMaterial,
            Material prismMaterial,
            Material coreMaterial)
        {
            const float halfX = 22f;
            const float halfZ = 15f;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var root = new GameObject("CornerPylon_" + sx + "_" + sz);
                    root.transform.position = new Vector3(
                        sx * (halfX - 0.55f), 0f, sz * (halfZ - 0.55f));

                    CreatePrismObject(
                        "PylonBase", root.transform,
                        CreatePrismMesh("PylonFrustum", 3, 0.58f, 0.30f, 1.25f),
                        baseMaterial, 0f);

                    var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    core.name = "PylonEnergyCore";
                    core.transform.SetParent(root.transform, false);
                    core.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                    core.transform.localScale = Vector3.one * 0.42f;
                    core.GetComponent<Renderer>().sharedMaterial = coreMaterial;
                    Collider coreCollider = core.GetComponent<Collider>();
                    if (coreCollider != null) UnityEngine.Object.DestroyImmediate(coreCollider);

                    CreatePrismObject(
                        "PylonPrism", root.transform,
                        CreatePrismMesh("PylonPrism", 6, 0.38f, 0.26f, 1.3f),
                        prismMaterial, 3.55f);
                }
            }
        }

        private static void CreatePrismObject(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            float baseY)
        {
            var prism = new GameObject(name);
            prism.transform.SetParent(parent, false);
            prism.transform.localPosition = new Vector3(0f, baseY, 0f);

            var filter = prism.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = prism.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Mesh CreatePrismMesh(
            string name,
            int sides,
            float bottomRadius,
            float topRadius,
            float height)
        {
            string path = MaterialFolder + "/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();

            for (int i = 0; i < sides; i++)
            {
                float angle = i / (float)sides * Mathf.PI * 2f;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * bottomRadius, 0f,
                    Mathf.Sin(angle) * bottomRadius));
                uvs.Add(new Vector2(i / (float)sides, 0f));
            }

            for (int i = 0; i < sides; i++)
            {
                float angle = i / (float)sides * Mathf.PI * 2f;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * topRadius, height,
                    Mathf.Sin(angle) * topRadius));
                uvs.Add(new Vector2(i / (float)sides, 1f));
            }

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int b0 = i;
                int b1 = next;
                int t0 = sides + i;
                int t1 = sides + next;

                triangles.Add(b0); triangles.Add(t0); triangles.Add(b1);
                triangles.Add(b1); triangles.Add(t0); triangles.Add(t1);
            }

            int bottomCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles.Add(bottomCenter); triangles.Add(next); triangles.Add(i);
            }

            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, height, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles.Add(topCenter);
                triangles.Add(sides + i);
                triangles.Add(sides + next);
            }

            var mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void CreateInvisibleBoundary(
            string name,
            Vector3 position,
            Vector3 scale,
            PhysicMaterial physics)
        {
            GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = name;
            boundary.transform.position = position;
            boundary.transform.localScale = scale;

            var renderer = boundary.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            var collider = boundary.GetComponent<Collider>();
            if (collider != null) collider.material = physics;
        }

        private static Mesh CreateDomeMesh()
        {
            string path = MaterialFolder + "/StadiumDomeCapsuleFull.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            // Top half of a capsule lying along X, springing directly from the
            // ground and covering the whole stadium. It has no supporting walls.
            const int lengthSegments = 32;
            const int arcSegments = 32;
            const float halfX = 23f;
            const float halfZ = 16f;
            const float height = 12f;

            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();

            for (int i = 0; i <= lengthSegments; i++)
            {
                float u = i / (float)lengthSegments;
                float x = Mathf.Lerp(-halfX, halfX, u);

                for (int s = 0; s <= arcSegments; s++)
                {
                    float v = s / (float)arcSegments;
                    float theta = v * Mathf.PI;
                    float z = Mathf.Cos(theta) * halfZ;
                    float y = Mathf.Sin(theta) * height;
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, v));
                }
            }

            int stride = arcSegments + 1;
            for (int i = 0; i < lengthSegments; i++)
            {
                for (int s = 0; s < arcSegments; s++)
                {
                    int a = i * stride + s;
                    int b = a + 1;
                    int c = (i + 1) * stride + s;
                    int d = c + 1;

                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }

            // Vertical half-ellipse caps at both ends.
            for (int end = 0; end <= 1; end++)
            {
                float x = end == 0 ? -halfX : halfX;
                int center = vertices.Count;
                vertices.Add(new Vector3(x, 0f, 0f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                int ringStart = end == 0 ? 0 : lengthSegments * stride;

                for (int s = 0; s < arcSegments; s++)
                {
                    int a = ringStart + s;
                    int b = ringStart + s + 1;

                    if (end == 0)
                    {
                        triangles.Add(center); triangles.Add(b); triangles.Add(a);
                    }
                    else
                    {
                        triangles.Add(center); triangles.Add(a); triangles.Add(b);
                    }
                }
            }

            var mesh = new Mesh();
            mesh.name = "StadiumDomeCapsule";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void CreatePitchMarkings(Material lineMaterial)
        {
            const float y = 0.028f;
            const float halfLength = 18f;
            const float halfWidth = 12f;

            CreateCube("TouchLine_North", new Vector3(0f, y, halfWidth),
                new Vector3(36f, 0.02f, 0.09f), lineMaterial, false);
            CreateCube("TouchLine_South", new Vector3(0f, y, -halfWidth),
                new Vector3(36f, 0.02f, 0.09f), lineMaterial, false);
            CreateCube("GoalLine_East", new Vector3(halfLength, y, 0f),
                new Vector3(0.09f, 0.02f, 24f), lineMaterial, false);
            CreateCube("GoalLine_West", new Vector3(-halfLength, y, 0f),
                new Vector3(0.09f, 0.02f, 24f), lineMaterial, false);
            CreateCube("CenterLine", new Vector3(0f, y, 0f),
                new Vector3(0.09f, 0.02f, 24f), lineMaterial, false);

            for (int side = -1; side <= 1; side += 2)
            {
                float goalX = side * halfLength;
                float penaltyX = side * 13f;
                float goalAreaX = side * 15.5f;

                CreateCube("PenaltyBox_Line_" + side,
                    new Vector3(penaltyX, y, 0f),
                    new Vector3(0.09f, 0.02f, 16f), lineMaterial, false);
                CreateCube("PenaltyBox_Top_" + side,
                    new Vector3((goalX + penaltyX) * 0.5f, y, 8f),
                    new Vector3(5f, 0.02f, 0.09f), lineMaterial, false);
                CreateCube("PenaltyBox_Bottom_" + side,
                    new Vector3((goalX + penaltyX) * 0.5f, y, -8f),
                    new Vector3(5f, 0.02f, 0.09f), lineMaterial, false);

                CreateCube("GoalArea_Line_" + side,
                    new Vector3(goalAreaX, y, 0f),
                    new Vector3(0.09f, 0.02f, 8f), lineMaterial, false);
                CreateCube("GoalArea_Top_" + side,
                    new Vector3((goalX + goalAreaX) * 0.5f, y, 4f),
                    new Vector3(2.5f, 0.02f, 0.09f), lineMaterial, false);
                CreateCube("GoalArea_Bottom_" + side,
                    new Vector3((goalX + goalAreaX) * 0.5f, y, -4f),
                    new Vector3(2.5f, 0.02f, 0.09f), lineMaterial, false);

                CreateCube("PenaltySpot_" + side,
                    new Vector3(side * 12f, y + 0.002f, 0f),
                    new Vector3(0.18f, 0.02f, 0.18f), lineMaterial, false);
            }

            CreateCube("CenterSpot", new Vector3(0f, y + 0.002f, 0f),
                new Vector3(0.18f, 0.02f, 0.18f), lineMaterial, false);

            CreateCircleLine("CenterCircle", lineMaterial, Vector3.zero, 3.2f, 0f, 360f, 64);
            CreateCircleLine("CornerArc_EastNorth", lineMaterial,
                new Vector3(halfLength, 0f, halfWidth), 1f, 180f, 270f, 18);
            CreateCircleLine("CornerArc_EastSouth", lineMaterial,
                new Vector3(halfLength, 0f, -halfWidth), 1f, 90f, 180f, 18);
            CreateCircleLine("CornerArc_WestNorth", lineMaterial,
                new Vector3(-halfLength, 0f, halfWidth), 1f, 270f, 360f, 18);
            CreateCircleLine("CornerArc_WestSouth", lineMaterial,
                new Vector3(-halfLength, 0f, -halfWidth), 1f, 0f, 90f, 18);
        }

        private static void CreateCircleLine(
            string name,
            Material material,
            Vector3 center,
            float radius,
            float startDegrees,
            float endDegrees,
            int segments)
        {
            var go = new GameObject(name);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;

            bool fullCircle = Mathf.Abs(endDegrees - startDegrees) >= 359f;
            line.loop = fullCircle;
            int count = fullCircle ? segments : segments + 1;
            line.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                line.SetPosition(i, center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.032f,
                    Mathf.Sin(angle) * radius));
            }
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, bool withCollider)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;

            if (!withCollider)
            {
                Collider collider = cube.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }

            return cube;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("No usable shader found for material " + name);
                return null;
            }

            var material = new Material(shader);
            material.name = name;
            material.color = color;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.05f);

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material MakeTrailMaterial()
        {
            string path = MaterialFolder + "/GestureTrail.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.name = "GestureTrail";
            material.color = new Color(0.2f, 1f, 1f);

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PhysicMaterial MakePhysicMaterial(string name, float bounciness, float friction)
        {
            string path = MaterialFolder + "/" + name + ".physicMaterial";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
            if (existing != null)
            {
                existing.bounciness = bounciness;
                existing.dynamicFriction = friction;
                existing.staticFriction = friction;
                existing.bounceCombine = PhysicMaterialCombine.Maximum;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new PhysicMaterial(name);
            material.bounciness = bounciness;
            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounceCombine = PhysicMaterialCombine.Maximum;

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
