using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Play 妯″紡璧拌矾鎺㈤拡锛氳嚜鍔ㄨ繘 Play銆佹寜浣?W 璧?4 绉掞紝閲囨牱鐜╁/鐩告満楂樺害锛岄獙璇?/// 銆岄珮搴︾粷瀵瑰浐瀹氥€嶄笌銆孒eadBob 鎽囨檭銆嶃€傜粨鏋滃啓鍒?tmp-read/walk-probe.txt銆?/// 鐢?SessionState 璺ㄥ煙閲嶈浇淇濇寔鐘舵€侊紙杩?Play 浼氶噸杞藉煙锛夈€?/// </summary>
[InitializeOnLoad]
public static class DshWalkProbe
{
    const string OutPath = "E:/DSH_Unity/tmp-read/walk-probe.txt";
    const string KeyRunning = "DshWalkProbe.running";
    const string KeyPlayStarted = "DshWalkProbe.playStarted";
    const string KeyT0 = "DshWalkProbe.t0";

    static float t0;
    static int frames, samples;
    static float minCamY = 99f, maxCamY = -99f, minCamLocalY = 99f, maxCamLocalY = -99f;
    static float maxPlayerY = -99f, minPlayerY = 99f, maxRoll;
    static float lastSpeed, maxSpeed;
    static StringBuilder sb;

    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        if (SessionState.GetBool(KeyRunning, false))
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
    }

    [MenuItem("Tools/DSH/Test/Walk Probe")]
    public static void StartProbe()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[WalkProbe] already playing");
            return;
        }
        sb = new StringBuilder();
        ResetStats();
        SessionState.SetBool(KeyRunning, true);
        SessionState.SetBool(KeyPlayStarted, false);
        SessionState.SetFloat(KeyT0, (float)EditorApplication.timeSinceStartup);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
        Debug.Log("[WalkProbe] start");
    }

    static void ResetStats()
    {
        frames = samples = 0;
        minCamY = 99f; maxCamY = -99f; minCamLocalY = 99f; maxCamLocalY = -99f;
        maxPlayerY = -99f; minPlayerY = 99f; maxRoll = 0f; maxSpeed = 0f;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(KeyRunning, false))
        {
            EditorApplication.update -= Tick;
            return;
        }
        if (!EditorApplication.isPlaying) return;

        // 杩?Play 浼氶噸杞藉煙锛氶潤鎬佸瓧娈典細琚噸缃紝杩欓噷鍏滃簳
        if (sb == null) sb = new StringBuilder();
        // 鍏跺畠鑴氭湰鐨?error 鍙兘瑙﹀彂 Error Pause锛屾帰閽堟湡闂村己鍒跺彇娑堟殏鍋?        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        // 宸ョ▼ runInBackground=0锛屽け鐒︿細璁╂父鎴忓惊鐜仠浣忥紙frameCount 涓嶆定锛?        if (!Application.runInBackground) Application.runInBackground = true;

        if (!SessionState.GetBool(KeyPlayStarted, false))
        {
            SessionState.SetBool(KeyPlayStarted, true);
            SessionState.SetFloat(KeyT0, (float)EditorApplication.timeSinceStartup);
            ResetStats();
        }

        var player = GameObject.Find("Player");
        if (player == null) return;
        var cam = player.transform.Find("Main Camera");
        if (cam == null) return;
        var cc = player.GetComponent<CharacterController>();
        var pm = player.GetComponent<PlayerMovement>();
        var hb = cam.GetComponent<HeadBob>();
        if (pm == null || hb == null) return;

        float t = (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(KeyT0, 0f);

        // 闃舵绯荤粺浼氶攣绉诲姩/鏆傚仠锛涙帰閽堟湡闂村己鍒舵斁寮€
        var ld = GameObject.Find("LevelDirector");
        if (ld != null && ld.activeSelf) ld.SetActive(false);
        if (!pm.enabled) pm.enabled = true;
        if (!pm.canMove) pm.canMove = true;
        if (!hb.enabled) hb.enabled = true;
        if (Time.timeScale < 1f) Time.timeScale = 1f;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (t > 1.2f && t < 4.5f) InputSystem.QueueStateEvent(kb, new KeyboardState(Key.W));
            else InputSystem.QueueStateEvent(kb, new KeyboardState());
        }

        if (t > 2f && t < 6f)
        {
            samples++;
            float cy = cam.position.y, cly = cam.localPosition.y, py = player.transform.position.y;
            minCamY = Mathf.Min(minCamY, cy); maxCamY = Mathf.Max(maxCamY, cy);
            minCamLocalY = Mathf.Min(minCamLocalY, cly); maxCamLocalY = Mathf.Max(maxCamLocalY, cly);
            minPlayerY = Mathf.Min(minPlayerY, py); maxPlayerY = Mathf.Max(maxPlayerY, py);
            maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0f, cam.localEulerAngles.z)));
            lastSpeed = pm.CurrentSpeed;
            maxSpeed = Mathf.Max(maxSpeed, pm.CurrentSpeed);
        }

        if (frames % 12 == 0)
            sb.AppendLine($"{t,6:F2}s pos=({player.transform.position.x:F2},{player.transform.position.y:F2},{player.transform.position.z:F2}) spd={pm.CurrentSpeed:F2} roll={cam.localEulerAngles.z:F2} dt={Time.deltaTime:F4} gt={Time.time:F2} frm={Time.frameCount} gnd={(cc ? cc.isGrounded : false)} ccen={(cc ? cc.enabled : false)} ts={Time.timeScale:F3} mdt={Time.maximumDeltaTime:F3} can={pm.canMove} en={pm.enabled} kb={(kb != null ? kb.wKey.isPressed.ToString() : "nokb")} paused={EditorApplication.isPaused} ld={(ld == null ? "null" : ld.activeSelf.ToString())}");

        frames++;
        if (t > 5.5f) Stop("done");
        else if (t > 20f) Stop("timeout");
    }

    static void Stop(string why)
    {
        SessionState.SetBool(KeyRunning, false);
        EditorApplication.update -= Tick;
        var kb = Keyboard.current;
        if (kb != null) InputSystem.QueueStateEvent(kb, new KeyboardState());
        if (sb == null) sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"samples={samples} frames={frames} why={why}");
        sb.AppendLine($"camY range [{minCamY:F4},{maxCamY:F4}] span={(maxCamY - minCamY):F4}");
        sb.AppendLine($"camLocalY range [{minCamLocalY:F4},{maxCamLocalY:F4}] span={(maxCamLocalY - minCamLocalY):F4}");
        sb.AppendLine($"playerY range [{minPlayerY:F4},{maxPlayerY:F4}] span={(maxPlayerY - minPlayerY):F4}");
        sb.AppendLine($"maxRollDeg={maxRoll:F3} maxSpeed={maxSpeed:F2}");
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[WalkProbe] stop: " + why + "\n" + sb);
        EditorApplication.isPlaying = false;
    }
}

