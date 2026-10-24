using System.Collections.Generic;

/// <summary>一次关卡运行期间的共享状态（钥匙、门、已访问点位）。</summary>
public class LevelState
{
    public readonly HashSet<string> collectedTargets = new HashSet<string>();
    public bool doorOpen;

    public int Count => collectedTargets.Count;

    public bool Has(string id)
    {
        return !string.IsNullOrEmpty(id) && collectedTargets.Contains(id);
    }

    public bool Add(string id)
    {
        return !string.IsNullOrEmpty(id) && collectedTargets.Add(id);
    }

    public void Reset()
    {
        collectedTargets.Clear();
        doorOpen = false;
    }
}
