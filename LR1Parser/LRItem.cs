namespace LR1Parser {
    /// <summary>
    /// LRアイテム
    /// </summary>
    public class LRItem {
        /// <summary>
        /// 文法ルール
        /// </summary>
        internal Rule Rule { get; set; }

        /// <summary>
        /// マーカー位置
        /// </summary>

        public int Position { get; set; }

        /// <summary>
        /// 先読みシンボル集合（終端シンボル）
        /// </summary>
        internal HashSet<Symbol> LookaheadSymbols { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_rule">文法ルール</param>
        /// <param name="_position">マーカー位置</param>
        /// <param name="_lookaheadSymbols">先読みシンボル集合</param>
        public LRItem(Rule _rule, int _position, HashSet<Symbol> _lookaheadSymbols) {
            Rule = _rule;
            Position = _position;
            LookaheadSymbols = _lookaheadSymbols ?? new();
        }

        public override string ToString() {
            string right = (Rule.RightSymbols.Count == 0 || Position < 0) ? "." : "";
            for (int i = 0; i < Rule.RightSymbols.Count; i++) {
                right += Rule.RightSymbols[i];
                right += (i == Position) ? "." : "";
                right += (i != Position && i != Rule.RightSymbols.Count - 1) ? " " : "";
            }

            return $"{Rule.LeftSymbol} -> {right} [{string.Join("/", LookaheadSymbols)}]";
        }

        public override int GetHashCode() {
            return Rule.GetHashCode() ^ Position.GetHashCode() ^ LookaheadSymbols.Count.GetHashCode();
        }

        public override bool Equals(object? obj) {
            if (obj is not LRItem _obj) {
                return false;
            }
            return Rule.Equals(_obj.Rule) && Position == _obj.Position
                && LookaheadSymbols.SetEquals(_obj.LookaheadSymbols);
        }
    }
}
