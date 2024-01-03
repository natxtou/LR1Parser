namespace LR1Parser {
    /// <summary>
    /// シンボル
    /// </summary>
    public class Symbol {
        /// <summary>
        /// シンボル文字列
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 非終端シンボルフラグ
        /// </summary>
        public bool IsNonTerminal { get; set; } = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_name">シンボル文字列</param>
        /// <param name="_isNonTerminal">非終端シンボルフラグ</param>
        public Symbol(string _name, bool _isNonTerminal) {
            Name = _name;
            IsNonTerminal = _isNonTerminal;
        }

        public override string ToString() {
            return Name;
        }

        public override int GetHashCode() {
            return Name.GetHashCode() ^ IsNonTerminal.GetHashCode();
        }

        public override bool Equals(object? obj) {
            if (obj is not Symbol _obj) {
                return false;
            }
            return Name == _obj.Name && IsNonTerminal == _obj.IsNonTerminal;
        }
    }
}
