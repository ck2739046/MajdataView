using UnityEngine;

public class NoteDrop : MonoBehaviour
{
    public float time;
    public int noteSortOrder;
}

public class NoteLongDrop : NoteDrop
{
    public float LastFor = 1f;

    // 让音符提前到达终点
    // 假设正常走完全程要 100 秒
    // 现在只要 97 秒就能提前走完全程
    // 剩下 3 秒在终点原地等待，保持全程耗时不变
    private const float TravelPortion = 0.97f;

    protected static float GetHoldTailProgress(float rawProgress)
    {
        if (rawProgress <= 0f)
            return 0f;

        if (rawProgress >= 1f)
            return 1f;

        return rawProgress >= TravelPortion ? 1f : Mathf.Clamp01(rawProgress / TravelPortion);
    }
}