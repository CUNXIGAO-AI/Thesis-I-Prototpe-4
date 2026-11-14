using System.Collections.Generic;
using MalbersAnimations;
using MalbersAnimations.Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DevCheatConsole : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private bool enableConsole = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private KeyCode toggleKeyAlt = KeyCode.F8;
    [SerializeField] private Vector3 teleportOffset = new Vector3(2f, 0.2f, 0f);

    [System.Serializable]
    public class WarpPoint
    {
        public string id;
        public string[] searchNames;
    }

    [SerializeField]
    private List<WarpPoint> warpPoints = new List<WarpPoint>
    {
        new WarpPoint { id = "girl", searchNames = new[] { "Girl", "Idle_V2", "2nd_Time" } },
        new WarpPoint { id = "water", searchNames = new[] { "Big_waterSource (Pickup Trigger)", "Big_waterSource" } },
        new WarpPoint { id = "save", searchNames = new[] { "Save Point", "Interaction" } }
    };

    private string command = "";
    private string lastLog = "输入 help 查看指令";
    private Vector2 scroll;
    private bool cursorWasLocked;
    private Transform storedCameraTarget;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        storedCameraTarget = null;
    }

    private void Update()
    {
        if (!enableConsole) return;

        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(toggleKeyAlt))
            Toggle();
    }

    private void Toggle()
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
            cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (cursorWasLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnGUI()
    {
        if (!enableConsole || !IsOpen) return;

        const float uiScale = 2f;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(uiScale, uiScale, 1f));

        float width = Mathf.Min(520f, (Screen.width - 24f) / uiScale);
        float height = Mathf.Min(640f, (Screen.height - 24f) / uiScale);
        Rect box = new Rect(12f / uiScale, 12f / uiScale, width, height);
        GUI.Box(box, "GM / DEV 窗口  (~ 或 F8 关闭)");

        GUILayout.BeginArea(new Rect(box.x + 10f, box.y + 28f, box.width - 20f, box.height - 38f));
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("重启：键盘 R    手柄：长按 B 约 3 秒");
        GUILayout.Label("旧快捷键：1 好结局 / 2 坏结局 / 3-4 最坏结局 / T 重置叙事 / P 打印状态");
        GUILayout.Space(6);
        GUILayout.Label(GetStatus());
        GUILayout.Space(8);

        GUILayout.Label("传送");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("女孩")) Warp("girl");
        if (GUILayout.Button("水源")) Warp("water");
        if (GUILayout.Button("存档点")) Warp("save");
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("给水变量");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("第一次：给")) SetFirst(true);
        if (GUILayout.Button("第一次：不给")) SetFirst(false);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("第二次：给（不触发）")) SetSecond(true, false);
        if (GUILayout.Button("第二次：不给（不触发）")) SetSecond(false, false);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("直接测结局动画");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("好结局")) ForceEnding(NarrativeManager.EndingType.GoodEnding);
        if (GUILayout.Button("坏结局")) ForceEnding(NarrativeManager.EndingType.BadEnding);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("最坏结局")) ForceEnding(NarrativeManager.EndingType.WorstEnding);
        if (GUILayout.Button("回到玩家视角 / 重置相机")) RestoreCameraToPlayer();

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重置叙事")) ResetNarrative();
        if (GUILayout.Button("重启游戏 (R)"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartGame();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("指令  (Enter 执行)   例: tp girl / gift1 1 / ending good");
        GUI.SetNextControlName("gm_cmd");
        command = GUILayout.TextField(command ?? "");
        if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
            GUI.GetNameOfFocusedControl() == "gm_cmd")
        {
            RunCommand(command);
            command = "";
            Event.current.Use();
        }

        GUILayout.Label(lastLog);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.matrix = previousMatrix;
    }

    private string GetStatus()
    {
        if (NarrativeManager.Instance == null)
            return "NarrativeManager 不存在";

        return NarrativeManager.Instance.GetSystemStatus();
    }

    private void RunCommand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        string[] parts = raw.Trim().ToLowerInvariant().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0];
        string arg = parts.Length > 1 ? parts[1] : "";

        switch (cmd)
        {
            case "help":
                lastLog = "tp girl|water|save\ngift1 0/1\ngift2 0/1\nending good|bad|worst\ncam\nreset\nrestart";
                break;
            case "tp":
                Warp(arg);
                break;
            case "gift1":
                SetFirst(ParseBool(arg));
                break;
            case "gift2":
                SetSecond(ParseBool(arg), false);
                break;
            case "ending":
                if (arg == "good") ForceEnding(NarrativeManager.EndingType.GoodEnding);
                else if (arg == "bad") ForceEnding(NarrativeManager.EndingType.BadEnding);
                else ForceEnding(NarrativeManager.EndingType.WorstEnding);
                break;
            case "cam":
            case "camera":
                RestoreCameraToPlayer();
                break;
            case "reset":
                ResetNarrative();
                break;
            case "restart":
                GameManager.Instance?.RestartGame();
                lastLog = "已请求重启游戏";
                break;
            default:
                lastLog = "未知指令: " + cmd + "  （help）";
                break;
        }
    }

    private static bool ParseBool(string arg)
    {
        return arg == "1" || arg == "yes" || arg == "true" || arg == "y";
    }

    private void Warp(string id)
    {
        Transform target = FindWarpTransform(id);
        if (target == null)
        {
            lastLog = "找不到传送点: " + id;
            return;
        }

        Vector3 dest = target.position + teleportOffset;
        MAnimal animal = MAnimal.MainAnimal;
        if (animal != null)
            animal.Teleport(dest);
        else if (GameObject.FindGameObjectWithTag("Player") is GameObject player)
            player.transform.position = dest;
        else
        {
            lastLog = "找不到玩家，无法传送";
            return;
        }

        lastLog = $"已传送到 {id}  ({target.name})";
    }

    private Transform FindWarpTransform(string id)
    {
        if (id == "girl")
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("girlAnimator");
            if (tagged != null) return tagged.transform;
        }

        WarpPoint point = warpPoints.Find(p => p.id == id);
        if (point == null) return GameObject.Find(id)?.transform;

        foreach (string name in point.searchNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null) return found.transform;
        }

        return null;
    }

    private void SetFirst(bool gave)
    {
        if (NarrativeManager.Instance == null) { lastLog = "没有 NarrativeManager"; return; }
        NarrativeManager.Instance.DebugSetFirstGift(gave);
        lastLog = "第一次给水 = " + gave;
    }

    private void SetSecond(bool gave, bool trigger)
    {
        if (NarrativeManager.Instance == null) { lastLog = "没有 NarrativeManager"; return; }
        NarrativeManager.Instance.DebugSetSecondGift(gave, trigger);
        lastLog = "第二次给水 = " + gave;
    }

    private void ForceEnding(NarrativeManager.EndingType type)
    {
        if (NarrativeManager.Instance == null) { lastLog = "没有 NarrativeManager"; return; }
        FocusCamera("girl");
        NarrativeManager.Instance.DebugForceEnding(type);
        lastLog = "已强制触发结局: " + type + "  （相机看向女孩，玩家位置不变）";
    }

    private void FocusCamera(string id)
    {
        Transform target = FindWarpTransform(id);
        if (target == null)
        {
            lastLog = "找不到镜头目标: " + id;
            return;
        }

        ThirdPersonFollowTarget follow = GetFollowCamera();
        if (follow == null)
        {
            lastLog = "找不到跟随相机";
            return;
        }

        Transform current = follow.Target != null ? follow.Target.Value : null;
        if (storedCameraTarget == null && current != null)
            storedCameraTarget = current;

        follow.lockInput = false;
        follow.SetTarget(target);
        follow.TargetTeleport(false);
        lastLog = $"相机已看向 {id}  ({target.name})，玩家未移动";
    }

    public void RestoreCameraToPlayer()
    {
        ThirdPersonFollowTarget follow = GetFollowCamera();
        if (follow == null)
        {
            lastLog = "找不到跟随相机，无法重置视角";
            return;
        }

        Transform player = null;
        if (MAnimal.MainAnimal != null)
            player = MAnimal.MainAnimal.transform;
        else if (storedCameraTarget != null)
            player = storedCameraTarget;
        else if (MRespawner.instance != null && MRespawner.instance.player != null)
            player = MRespawner.instance.player.transform;

        if (player == null)
        {
            lastLog = "找不到玩家，无法重置相机";
            return;
        }

        follow.lockInput = false;
        follow.SetPriority(true);
        follow.SetTarget(player);
        follow.TargetTeleport(true);
        storedCameraTarget = null;
        lastLog = "相机已回到玩家视角，可以转动镜头";
    }

    private ThirdPersonFollowTarget GetFollowCamera()
    {
        ThirdPersonFollowTarget follow = null;
        if (GameManager.Instance != null)
            follow = GameManager.Instance.cameraFollowTarget;

        if (follow == null)
            follow = FindObjectOfType<ThirdPersonFollowTarget>();

        if (GameManager.Instance != null && follow != null)
            GameManager.Instance.cameraFollowTarget = follow;

        return follow;
    }

    private void ResetNarrative()
    {
        if (NarrativeManager.Instance == null) { lastLog = "没有 NarrativeManager"; return; }
        NarrativeManager.Instance.ResetNarrativeSystem();
        RestoreCameraToPlayer();
        lastLog = "叙事已重置，相机已回到玩家";
    }
}
