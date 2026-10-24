using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一关的数据资产：阶段列表 + 文案。
/// 加新关卡 = 新建一个 asset，不用改 LevelDirector。
/// </summary>
[CreateAssetMenu(menuName = "OUTDOOR/Level Definition", fileName = "Level_01")]
public class LevelDefinition : ScriptableObject
{
    public string levelId = "level_01";
    public string displayName = "LEVEL 1";

    public List<PhaseDefinition> phases = new List<PhaseDefinition>();
}
