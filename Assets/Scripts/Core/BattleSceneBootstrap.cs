using UnityEngine;

public sealed class BattleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private string encounterID;
    [SerializeField] private BattleSimpleUIController battleUIController;
    [SerializeField] private BattleFormalPresentationTestHarness formalPresentationTestHarness;
    [SerializeField] private bool useDebugTestInitialization = false;
    [SerializeField] private bool useSingleUnitDemo = false;

    private bool hasStartedInitialization;
    private BattleDefinitionBootstrapResult activeBootstrapResult;

    public BattleDefinitionBootstrapResult ActiveBootstrapResult
    {
        get { return activeBootstrapResult; }
    }

    public bool HasStartedInitialization => hasStartedInitialization;
    public bool UseDebugTestInitialization => useDebugTestInitialization;
    public bool UseSingleUnitDemo => useSingleUnitDemo;
    public string EncounterID => encounterID;

    private void Start()
    {
        InitializeBattleScene();
    }

    public bool InitializeBattleScene()
    {
        if (hasStartedInitialization)
        {
            Debug.LogWarning(
                "BattleSceneBootstrap重复初始化已被阻止。",
                this
            );
            return false;
        }

        hasStartedInitialization = true;

        if (battleUIController == null)
        {
            Debug.LogError(
                "BattleSceneBootstrap初始化失败：未绑定BattleSimpleUIController。",
                this
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(encounterID))
        {
            const string errorMessage =
                "BattleSceneBootstrap初始化失败：encounterID为空。";
            Debug.LogError(errorMessage, this);
            battleUIController.ShowInitializationFailure(errorMessage);
            return false;
        }

        if (useDebugTestInitialization)
        {
            Debug.LogWarning(
                "BattleSceneBootstrap已启用显式Debug测试初始化。",
                this
            );
            bool initialized = battleUIController.InitializeDebugTestBattle();
            if (initialized)
            {
                BattleEndPanelController.Bind(battleUIController.RuntimeState);
            }
            return initialized;
        }

        BattleDefinitionBootstrapResult bootstrapResult =
            BattleDefinitionBootstrap.CreateRuntimeState(
                encounterID,
                useSingleUnitDemo
            );

        if (bootstrapResult == null ||
            !bootstrapResult.isSuccess ||
            bootstrapResult.runtimeState == null)
        {
            string bootstrapError = bootstrapResult != null
                ? bootstrapResult.errorMessage
                : "Bootstrap未返回结果";
            string errorMessage =
                "BattleScene正式初始化失败：encounterID=" + encounterID +
                "，原因：" + bootstrapError;
            Debug.LogError(errorMessage, this);
            battleUIController.ShowInitializationFailure(errorMessage);
            return false;
        }

        // 正式RuntimeState先由Definition创建，开发场景数据再在UI首次读取前注入。
        if (formalPresentationTestHarness != null &&
            !formalPresentationTestHarness.TryPrepareScenario(
                bootstrapResult.runtimeState,
                out string testScenarioFailure))
        {
            string errorMessage =
                "BattleScene正式初始化失败：Presentation测试场景准备失败，" +
                "原因：" + testScenarioFailure;
            Debug.LogError(errorMessage, this);
            battleUIController.ShowInitializationFailure(errorMessage);
            return false;
        }

        bool allowPreparedActionSlots =
            formalPresentationTestHarness != null &&
            formalPresentationTestHarness.HasPreparedScenarioFor(
                bootstrapResult.runtimeState
            );

        // BootstrapResult由场景入口持有，Controller只消费其中的RuntimeState。
        activeBootstrapResult = bootstrapResult;
        if (!battleUIController.InitializeFromRuntimeState(
                bootstrapResult.runtimeState,
                allowPreparedActionSlots))
        {
            Debug.LogError(
                "BattleScene正式初始化失败：Controller拒绝RuntimeState，" +
                "encounterID=" + encounterID,
                this
            );
            return false;
        }

        BattleEndPanelController.Bind(bootstrapResult.runtimeState);

        BattleRuntimeState runtimeState = bootstrapResult.runtimeState;
        Debug.Log(
            "BattleScene正式初始化成功：encounterID=" + encounterID +
            "，singleUnitDemo=" + useSingleUnitDemo +
            "，runtimeUnitIDs=[" + GetRuntimeUnitIDList(runtimeState) + "]",
            this
        );
        return true;
    }

    private static string GetRuntimeUnitIDList(BattleRuntimeState runtimeState)
    {
        if (runtimeState == null || runtimeState.battleUnits == null)
        {
            return string.Empty;
        }

        string[] runtimeUnitIDs = new string[runtimeState.battleUnits.Count];
        for (int index = 0; index < runtimeState.battleUnits.Count; index++)
        {
            CharacterData unit = runtimeState.battleUnits[index];
            runtimeUnitIDs[index] = unit != null
                ? unit.runtimeUnitID
                : "<null>";
        }
        return string.Join(", ", runtimeUnitIDs);
    }
}
