using System.Text;

namespace LR1Parser {
    /// <summary>
    /// 構造木の単体ノード
    /// </summary>
    public class TreeNode {
        /// <summary>
        /// シンボル
        /// </summary>
        internal Symbol Symbol { get; set; }

        /// <summary>
        /// 親ノード
        /// </summary>
        internal TreeNode? Parent { get; set; }

        /// <summary>
        /// 子ノードリスト
        /// </summary>
        internal List<TreeNode> Childs { get; set; }

        /// <summary>
        /// 任意データ
        /// </summary>
        public object? AnyData { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_symbol">シンボル</param>
        /// <param name="_obj">任意データ</param>
        public TreeNode(Symbol _symbol, object? _obj = null) {
            Symbol = _symbol;
            Parent = null;
            Childs = new();
            AnyData = _obj;
        }

        /// <summary>
        /// 構造木の文字列表現を取得する<br/>
        /// <code>
        /// ----- 例 -----
        /// 【文法】
        /// S -> A B
        /// A -> A a |
        /// B -> b B |
        /// 【入力】
        /// a a b
        /// 【出力】
        /// S
        /// |-- A
        /// |   |-- A
        /// |   |   |-- A => null
        /// |   |   `-- a
        /// |   `-- a
        /// `-- B
        ///     |-- b
        ///     `-- B => null
        /// </code>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetTreeStringLayer(TreeNode node) {
            List<int> sib = new();
            return GetTreeStringLayerInner(node, sib);
        }

        private static string GetTreeStringLayerInner(TreeNode node, List<int> sib) {
            StringBuilder sb = new();
            string toNull = (node.Symbol.IsNonTerminal && node.Childs.Count == 0) ? " => null" : "";
            if (sib.Count == 0) {
                sb.Append(node.Symbol.Name);
            } else {
                List<string> parts = new();
                TreeNode now = node;
                for (int i = 0; i < sib.Count; i++) {
                    if (i == 0) {
                        if (sib[i] < now.Parent.Childs.Count - 1) {
                            parts.Add($" |- {now.Symbol.Name}{toNull}");
                        } else {
                            parts.Add($" `- {now.Symbol.Name}{toNull}");
                        }
                    } else {
                        if (sib[i] < now.Parent.Childs.Count - 1) {
                            parts.Add($" |  ");
                        } else {
                            parts.Add($"    ");
                        }
                    }
                    now = now.Parent;
                }
                parts.Reverse();
                sb.Append(string.Join("", parts));
            }

            for (int i = 0; i < node.Childs.Count; i++) {
                List<int> sibNext = new(sib);
                sibNext.Insert(0, i);
                sb.AppendLine("");
                sb.Append(GetTreeStringLayerInner(node.Childs[i], sibNext));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 構造木の文字列表現を取得する<br/>
        /// <code>
        /// ----- 例 -----
        /// 【文法】
        /// S -> A B
        /// A -> A a |
        /// B -> b B |
        /// 【入力】
        /// a a b
        /// 【出力】
        /// S(A(A(A() a) a) B(b B()))
        /// </code>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetTreeStringFlat(TreeNode node) {
            StringBuilder sb = new();
            sb.Append(node.Symbol.Name);
            if (node.Symbol.IsNonTerminal) {
                sb.Append('(');
                List<string> childStrs = node.Childs.Select(n => GetTreeStringFlat(n)).ToList();
                sb.Append(string.Join(' ', childStrs));
                sb.Append(')');
            }

            return sb.ToString();
        }

        /// <summary>
        /// 構造木の文字列表現を取得する<br/>
        /// 終端シンボルのみ、無駄な括弧を除去
        /// <code>
        /// ----- 例 -----
        /// 【文法】
        /// S -> A B
        /// A -> A a |
        /// B -> b B |
        /// 【入力】
        /// a a b
        /// 【出力】
        /// ((a a) b)
        /// </code>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetTreeStringSimple(TreeNode node) {
            if (!node.Symbol.IsNonTerminal) {
                return node.Symbol.Name;
            }

            StringBuilder sb = new();
            if (node.Symbol.IsNonTerminal) {
                List<string> childStrs = node.Childs
                    .Select(n => GetTreeStringSimple(n))
                    .Where(s => s != "" && s != "()").ToList();
                if (childStrs.Count > 1) { sb.Append('('); }
                sb.Append(string.Join(' ', childStrs));
                if (childStrs.Count > 1) { sb.Append(')'); }
            }

            return sb.ToString();
        }

        public override string ToString() {
            return Symbol.Name;
        }

    }
}
