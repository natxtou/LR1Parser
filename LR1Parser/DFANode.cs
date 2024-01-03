namespace LR1Parser {
    /// <summary>
    /// 決定性有限オートマトンの単体ノード
    /// </summary>
    public class DFANode {
        /// <summary>
        /// ノード番号
        /// </summary>
        public int Seq { get; set; }

        /// <summary>
        /// ノード内のLRアイテム集合
        /// </summary>
        internal HashSet<LRItem> Items { get; set; }

        /// <summary>
        /// 別ノードへのリンク集合<br/>
        /// シンボル⇒リンク先のノード番号
        /// </summary>
        internal Dictionary<Symbol, int> MoveTo { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_seq">ノード番号</param>
        /// <param name="_items">LRアイテム集合</param>
        /// <param name="_moveTo">リンク集合</param>
        public DFANode(int _seq, HashSet<LRItem> _items, Dictionary<Symbol, int> _moveTo) {
            Seq = _seq;
            Items = _items ?? new();
            MoveTo = _moveTo ?? new();
        }

        public override string ToString() {
            string strMoveTo = "";
            foreach (Symbol s in MoveTo.Keys) {
                if (strMoveTo != "") {
                    strMoveTo += ",";
                }
                strMoveTo += $"{s}=>{MoveTo[s]}";
            }
            return Seq + " : (" + string.Join(")(", Items) + "){" + strMoveTo + "}";
        }
    }
}
