using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 管理当前剧情会话的布尔、整数和字符串变量，统一提供修改与条件判断。
    public sealed class StoryVariableStore
    {
        private readonly Dictionary<string, StoryValueData> values =
            new Dictionary<string, StoryValueData>(StringComparer.Ordinal);

        // 开始新剧情或重置状态时移除全部运行时变量。
        public void Clear()
        {
            values.Clear();
        }

        // 批量操作采用事务式提交：全部成功才替换正式变量集合。
        public bool TryApplyOperations(
            IReadOnlyList<StoryVariableOperationData> operations,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (operations == null)
            {
                return true;
            }

            // 先在副本上执行，确保同一节点或选项中的批量操作要么全部成功，
            // 要么完全不修改运行时变量，避免后续操作失败时留下半完成状态。
            Dictionary<string, StoryValueData> workingValues = CloneValues(values);

            foreach (StoryVariableOperationData operation in operations)
            {
                if (!TryApplyOperation(workingValues, operation, out errorMessage))
                {
                    return false;
                }
            }

            values.Clear();

            foreach (KeyValuePair<string, StoryValueData> pair in workingValues)
            {
                values.Add(pair.Key, pair.Value);
            }

            return true;
        }

        // 根据 mode 执行“任一满足”或“全部满足”；空条件默认可通过。
        public bool Evaluate(
            IReadOnlyList<StoryConditionData> conditions,
            StoryConditionMode mode
        )
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            if (mode == StoryConditionMode.Any)
            {
                foreach (StoryConditionData condition in conditions)
                {
                    if (EvaluateCondition(condition))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (StoryConditionData condition in conditions)
            {
                if (!EvaluateCondition(condition))
                {
                    return false;
                }
            }

            return true;
        }

        // 在指定工作副本上执行单次 Set/Add，并返回可展示的失败原因。
        private bool TryApplyOperation(
            Dictionary<string, StoryValueData> targetValues,
            StoryVariableOperationData operation,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (operation == null || string.IsNullOrWhiteSpace(operation.variableName))
            {
                errorMessage = "变量操作缺少 variableName";
                return false;
            }

            if (operation.value == null)
            {
                errorMessage = "变量操作缺少 value：" + operation.variableName;
                return false;
            }

            if (operation.operation == StoryVariableOperationType.Set)
            {
                targetValues[operation.variableName] = operation.value.Clone();
                return true;
            }

            if (operation.operation == StoryVariableOperationType.Add)
            {
                StoryValueData currentValue;

                if (!targetValues.TryGetValue(operation.variableName, out currentValue))
                {
                    currentValue = new StoryValueData
                    {
                        valueType = StoryValueType.Int,
                        intValue = 0
                    };
                }

                if (currentValue.valueType != StoryValueType.Int ||
                    operation.value.valueType != StoryValueType.Int)
                {
                    errorMessage = "Add 只支持 Int 变量：" + operation.variableName;
                    return false;
                }

                int addedValue;

                try
                {
                    addedValue = checked(
                        currentValue.intValue + operation.value.intValue
                    );
                }
                catch (OverflowException)
                {
                    errorMessage = "Int 变量加法溢出：" + operation.variableName;
                    return false;
                }

                targetValues[operation.variableName] = new StoryValueData
                {
                    valueType = StoryValueType.Int,
                    intValue = addedValue
                };
                return true;
            }

            errorMessage = "未知变量操作：" + operation.operation;
            return false;
        }

        // 克隆操作工作区，隔离尚未提交的中间结果。
        private static Dictionary<string, StoryValueData> CloneValues(
            Dictionary<string, StoryValueData> source
        )
        {
            Dictionary<string, StoryValueData> clone =
                new Dictionary<string, StoryValueData>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, StoryValueData> pair in source)
            {
                clone.Add(
                    pair.Key,
                    pair.Value != null ? pair.Value.Clone() : null
                );
            }

            return clone;
        }

        // Exists/NotExists 不要求比较值，其余比较要求变量存在且类型有效。
        private bool EvaluateCondition(StoryConditionData condition)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.variableName))
            {
                return false;
            }

            StoryValueData currentValue;
            bool exists = values.TryGetValue(condition.variableName, out currentValue);

            if (condition.comparison == StoryComparisonType.Exists)
            {
                return exists;
            }

            if (condition.comparison == StoryComparisonType.NotExists)
            {
                return !exists;
            }

            if (!exists || currentValue == null || condition.compareValue == null)
            {
                return false;
            }

            if (currentValue.valueType != condition.compareValue.valueType)
            {
                return condition.comparison == StoryComparisonType.NotEquals;
            }

            int comparison = Compare(currentValue, condition.compareValue);

            switch (condition.comparison)
            {
                case StoryComparisonType.Equals:
                    return comparison == 0;
                case StoryComparisonType.NotEquals:
                    return comparison != 0;
                case StoryComparisonType.Greater:
                    return comparison > 0;
                case StoryComparisonType.GreaterOrEqual:
                    return comparison >= 0;
                case StoryComparisonType.Less:
                    return comparison < 0;
                case StoryComparisonType.LessOrEqual:
                    return comparison <= 0;
                default:
                    return false;
            }
        }

        // 同类型值统一归一为负数、零或正数，供所有关系运算复用。
        private int Compare(StoryValueData left, StoryValueData right)
        {
            if (left.valueType != right.valueType)
            {
                return -1;
            }

            switch (left.valueType)
            {
                case StoryValueType.Bool:
                    return left.boolValue.CompareTo(right.boolValue);
                case StoryValueType.Int:
                    return left.intValue.CompareTo(right.intValue);
                case StoryValueType.String:
                    return string.Compare(left.stringValue, right.stringValue, StringComparison.Ordinal);
                default:
                    return -1;
            }
        }
    }
}
