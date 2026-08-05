using UnityEngine;

public sealed class BattleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private string encounterID;
    [SerializeField] private BattleSimpleUIController battleUIController;
    [SerializeField] private bool useDebugTestInitialization = false;

    private bool hasStartedInitialization;
    private BattleDefinitionBootstrapResult activeBootstrapResult;

    public BattleDefinitionBootstrapResult ActiveBootstrapResult
    {
        get { return activeBootstrapResult; }
    }

    public bool HasStartedInitialization => hasStartedInitialization;
    public bool UseDebugTestInitialization => useDebugTestInitialization;
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
            return battleUIController.InitializeDebugTestBattle();
        }

        BattleDefinitionBootstrapResult bootstrapResult =
            BattleDefinitionBootstrap.CreateRuntimeState(encounterID);

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

        // BootstrapResult由场景入口持有，Controller只消费其中的RuntimeState。
        activeBootstrapResult = bootstrapResult;
        if (!battleUIController.InitializeFromRuntimeState(
                bootstrapResult.runtimeState))
        {
            Debug.LogError(
                "BattleScene正式初始化失败：Controller拒绝RuntimeState，" +
                "encounterID=" + encounterID,
                this
            );
            return false;
        }

        BattleRuntimeState runtimeState = bootstrapResult.runtimeState;
        Debug.Log(
            "BattleScene正式初始化成功：encounterID=" + encounterID +
            "，runtimeUnitIDs=[" +
            runtimeState.allyA.runtimeUnitID + ", " +
            runtimeState.allyB.runtimeUnitID + ", " +
            runtimeState.enemy.runtimeUnitID + ", " +
            runtimeState.enemy2.runtimeUnitID + "]",
            this
        );
        return true;
    }
}
