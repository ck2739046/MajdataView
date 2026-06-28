using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 帧率调度器：在不需要高帧率的场景下降低帧率以省 CPU，需要时恢复。
///
/// 触发降帧（降到 ThrottledFrameRate）的两个条件，满足其一即可：
///   1. 宿主窗口被最小化
///   2. 处于默认界面
/// 恢复原帧率的条件：宿主在前台且已载入谱面。
/// 录制中（Time.captureFramerate != 0）永不降帧，避免破坏固定帧率捕获。
///
/// 本组件可能被 SetParent 嵌入宿主进程，因此无法用常规焦点 API 判定前后台，
/// 改用 Win32 轮询：取自身 root owner（即宿主顶级窗口），与 GetForegroundWindow 比较。
/// </summary>
public class FocusProbe : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    private const uint GA_ROOTOWNER = 3;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);

    private const int STATE_FOREGROUND = 0;
    private const int STATE_COVERED = 1;
    private const int STATE_MINIMIZED = 2;

    // 降帧目标帧率。
    // 不可过低：帧率过低会拉长主循环间隔，导致从最小化恢复或响应 HTTP 指令时出现明显延迟。
    private const int ThrottledFrameRate = 50;

    private IntPtr _cachedRootOwner;

    // Notes 节点，用于判定是否处于默认界面（无谱面）。
    // 载入谱面时 note 会被实例化到 Notes 下，Stop 会重建场景清空，暂停不动 Notes。
    // 跨场景重载后引用失效，靠 Unity 重载的 ==null 判定自动重查。
    private Transform _notesTransform;

    // 锁帧状态：进入降帧时保存原值，退出时恢复。
    private bool _isThrottling;
    private int _savedTargetFrameRate;
    private int _savedVSyncCount;

    private void Update()
    {
        var fg = GetForegroundWindow();

        // 懒捕获宿主顶级窗口句柄（GetActiveWindow 依赖调用线程，必须在主线程）。
        if (_cachedRootOwner == IntPtr.Zero)
        {
            var act = GetActiveWindow();
            if (act == IntPtr.Zero) return;
            _cachedRootOwner = GetAncestor(act, GA_ROOTOWNER);
            if (_cachedRootOwner == IntPtr.Zero) return;
        }

        var state = Classify(fg);
        ApplyGovernor(state, IsIdle());
    }

    /// <summary>
    /// 根据降帧条件切换 targetFrameRate / vSyncCount（幂等）。
    /// 降帧时必须同时把 vSyncCount 置 0，否则 targetFrameRate 会被垂直同步忽略。
    /// </summary>
    private void ApplyGovernor(int state, bool idle)
    {
        bool recording = Time.captureFramerate != 0;
        bool shouldThrottle = !recording && (state == STATE_MINIMIZED || idle);

        if (shouldThrottle == _isThrottling) return;

        if (shouldThrottle)
        {
            _savedTargetFrameRate = Application.targetFrameRate;
            _savedVSyncCount = QualitySettings.vSyncCount;
            Application.targetFrameRate = ThrottledFrameRate;
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            Application.targetFrameRate = _savedTargetFrameRate;
            QualitySettings.vSyncCount = _savedVSyncCount;
        }
        _isThrottling = shouldThrottle;
    }

    /// <summary>是否处于默认界面（Notes 下无 note 子物体）。Notes 未就绪时返回 false。</summary>
    private bool IsIdle()
    {
        if (_notesTransform == null)
        {
            var go = GameObject.Find("Notes");
            if (go == null) return false;
            _notesTransform = go.transform;
        }
        return _notesTransform.childCount == 0;
    }

    /// <summary>
    /// 判定宿主窗口状态。
    /// 优先用 IsIconic 查最小化：GetForegroundWindow 返回 0 只是切换瞬态，不可靠。
    /// </summary>
    private int Classify(IntPtr fg)
    {
        if (IsIconic(_cachedRootOwner)) return STATE_MINIMIZED;
        if (fg == _cachedRootOwner) return STATE_FOREGROUND;
        return STATE_COVERED;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        var go = new GameObject("__FocusProbe");
        go.AddComponent<FocusProbe>();
        DontDestroyOnLoad(go);
    }
}
