namespace LR1Parser {
    /// <summary>
    /// 文法ルール
    /// </summary>
    public class Rule {
        /// <summary>
        /// 左辺シンボル
        /// </summary>
        internal Symbol LeftSymbol { get; set; }

        /// <summary>
        /// 右辺シンボルリスト
        /// </summary>
        internal List<Symbol> RightSymbols { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_leftSymbol">左辺シンボル</param>
        /// <param name="_rightSymbols">右辺シンボルリスト</param>
        public Rule(Symbol _leftSymbol, List<Symbol> _rightSymbols) {
            LeftSymbol = _leftSymbol;
            RightSymbols = _rightSymbols ?? new();
        }

        public override string ToString() {
            return $"{LeftSymbol} -> {string.Join(" ", RightSymbols.Select(e => e.Name))}";
        }

        public override int GetHashCode() {
            return LeftSymbol.GetHashCode() ^ RightSymbols.Count.GetHashCode();
        }

        public override bool Equals(object? obj) {
            if (obj is not Rule _obj) {
                return false;
            }
            return LeftSymbol.Equals(_obj.LeftSymbol)
                && RightSymbols.SequenceEqual(_obj.RightSymbols);
        }
    }
}
