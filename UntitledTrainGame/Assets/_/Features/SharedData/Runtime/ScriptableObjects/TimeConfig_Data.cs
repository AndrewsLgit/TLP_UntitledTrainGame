using SharedData.Runtime;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines the loop boundaries.
/// Designers configure loopStart and loopEnd once, globally.
/// </summary>
[CreateAssetMenu(fileName = "SO_TimeConfig", menuName = "GameTime/TimeConfig")]
public class TimeConfig : ScriptableObject
{
    [Header("Loop Settings")]
    public GameTime m_LoopStart = new GameTime(10, 0);
    public GameTime m_LoopEnd = new GameTime(14, 0);
}