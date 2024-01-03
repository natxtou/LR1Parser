namespace LR1Parser {
    /// <summary>
    /// 構文解析アクション
    /// </summary>
    public class ParseAction {
        /// <summary>
        /// アクションタイプ
        /// </summary>
        internal ActionType Type { get; set; }

        /// <summary>
        /// アクションに付与するパラメータ
        /// </summary>
        public int Param { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_type">アクションタイプ</param>
        /// <param name="_param">アクションに付与するパラメータ</param>
        public ParseAction(ActionType _type, int _param) {
            Type = _type;
            Param = _param;
        }

        /// <summary>
        /// アクションタイプ
        /// </summary>
        public enum ActionType { 
            /// <summary>シフト命令</summary>
            Shift = 0,
            /// <summary>還元命令</summary>
            Reduce = 1,
            /// <summary>移動命令</summary>
            Goto = 2,
            /// <summary>受理命令</summary>
            Accept = 3,
        }

        public override string ToString() {
            switch (Type) {
                case ActionType.Shift:
                    return $"s{Param}";
                case ActionType.Reduce:
                    return $"r{Param}";
                case ActionType.Goto:
                    return $"g{Param}";
                case ActionType.Accept:
                    return "acc";
                default:
                    return "err";
            }
        }
    }
}
