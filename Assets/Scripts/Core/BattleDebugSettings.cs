using UnityEngine;

// BattleDebugSettings = 战斗调试设置
// 挂在 Hierarchy 里的 BattleDebugManager 空对象上
public class BattleDebugSettings : MonoBehaviour
{
    // Instance = 当前场景中的调试设置实例
    public static BattleDebugSettings Instance { get; private set; }

    [Header("日志开关")]

    // showJsonLog = 是否打印完整 JSON 原文
    public bool showJsonLog = false;

    // showBattleStateLog = 是否打印全体角色状态
    public bool showBattleStateLog = false;

    // showSpeedLog = 是否打印速度投掷 / 当前速度
    public bool showSpeedLog = true;

    // showDetailBattleLog = 是否打印详细战斗过程
    public bool showDetailBattleLog = true;

    // showDamageLog = 是否打印伤害计算
    public bool showDamageLog = true;

    // showBuffLog = 是否打印 Buff 获得 / 消失
    public bool showBuffLog = true;

    void Awake()
    {
        // 如果场景里已经有一个 BattleDebugSettings，就不要重复创建
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 下面这些 static 属性，是给其他脚本方便调用的
    // 例如：BattleDebugSettings.ShowJsonLog

    public static bool ShowJsonLog
    {
        get
        {
            return Instance != null && Instance.showJsonLog;
        }
    }

    public static bool ShowBattleStateLog
    {
        get
        {
            return Instance != null && Instance.showBattleStateLog;
        }
    }

    public static bool ShowSpeedLog
    {
        get
        {
            return Instance != null && Instance.showSpeedLog;
        }
    }

    public static bool ShowDetailBattleLog
    {
        get
        {
            return Instance != null && Instance.showDetailBattleLog;
        }
    }

    public static bool ShowDamageLog
    {
        get
        {
            return Instance != null && Instance.showDamageLog;
        }
    }

    public static bool ShowBuffLog
    {
        get
        {
            return Instance != null && Instance.showBuffLog;
        }
    }
}