using System.Data;
using System.Text.RegularExpressions;

namespace LR1Parser {
    public class LR1Parser {
        // システム定義開始シンボル「S'」
        private static readonly Symbol SystemStartSymbol = new("S'", true);
        // システム定義終了シンボル「$」
        private static readonly Symbol SystemEndSymbol = new("$", false);

        // 文法ルールリスト
        private readonly List<Rule> Grammar = new();
        // ユーザー定義開始シンボル
        private readonly Symbol StartSymbol = new("#Dummy", false);
        // シンボル集合：文法に登場する全シンボル＋システム定義シンボル
        private readonly HashSet<Symbol> AllSymbols = new();
        // Nulls集合：Nullになりうる非終端シンボル集合
        private readonly HashSet<Symbol> Nulls = new();
        // First集合：非終端シンボル⇒到達しうる最初の終端シンボルの集合
        private readonly Dictionary<Symbol, HashSet<Symbol>> First = new();
        // DFAノードリスト：決定性有限オートマトンのノードリスト
        private readonly List<DFANode> DFANodes = new();
        // 構文解析テーブル：[DFAノード番号][シンボル]=アクション[]
        private readonly Dictionary<int, Dictionary<Symbol, List<ParseAction>>> ParseTable = new();

        /// <summary>
        /// 文法ルールを追加する
        /// </summary>
        /// <param name="rule"></param>
        public void AddRule(Rule rule) {
            Grammar.Add(rule);
        }

        /// <summary>
        /// 文法ルールを追加する。文字列から文法を追加するため以下の制約あり<br/>
        /// ※大文字英字から始まるシンボルは非終端シンボル、それ以外は終端シンボルと解釈されます<br/>
        /// ※左辺と右辺の区切り文字列「->」と右辺のOR区切り文字「|」は変更可能です<br/>
        /// ※例：L -> R1 R2 | R3 | （非終端シンボル[L]はシンボル列[R1,R2]または[R3]または[空シンボル列]を生成）
        /// </summary>
        /// <param name="ruleStr">ルール文字列</param>
        /// <param name="splitStrLR">左辺と右辺の区切り文字列</param>
        /// <param name="splitStrOr">右辺のORを表す文字列</param>
        public void AddRuleByString(string ruleStr, string splitStrLR = "->", string splitStrOr = "|") {
            // 2連続以上の空白記号は除去、かつ全て半角スペースに変換
            string ruleStrConv = new Regex(@"[ 　\t\r\n]+").Replace(ruleStr, " ");

            string symStrL = ruleStrConv.Split(splitStrLR)[0].Trim(' ');
            // シンボル名の先頭が大文字英字なら非終端シンボル、それ以外なら終端シンボルとする
            Symbol symL = new(symStrL, "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(symStrL[0]));

            string[] orStrsR = ruleStrConv.Split(splitStrLR)[1].Trim(' ').Split(splitStrOr);
            foreach (string orStrR in orStrsR) {
                string[] symStrsR = orStrR.Trim(' ').Split(' ');
                // 右シンボルは空の場合あり、その場合でもnullではなく空リストとする
                List<Symbol> symsR = new();
                foreach (string symSyrR in symStrsR) {
                    if (symSyrR != null && symSyrR.Length > 0) {
                        // シンボル名の先頭が大文字英字なら非終端シンボル、それ以外なら終端シンボルとする
                        symsR.Add(new Symbol(symSyrR, "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(symSyrR[0])));
                    }
                }
                Rule rule = new(symL, symsR);
                Grammar.Add(rule);
            }
        }

        /// <summary>
        /// 文法の開始記号を設定する
        /// </summary>
        /// <param name="symbolString"></param>
        public void SetStartSymbol(string symbolString) {
            StartSymbol.Name = symbolString;
            StartSymbol.IsNonTerminal = true;
        }

        /// <summary>
        /// 文法のチェック（明らかに変なもの）を行う
        /// </summary>
        private void CheckGrammar() {
            // ユーザー指定開始シンボルが左辺シンボルであるルールが存在しない場合はエラーとする
            List<Rule> matched = Grammar.Where(r => r.LeftSymbol.Equals(StartSymbol)).ToList();
            if (matched.Count == 0) {
                throw new Exception($"Error : 開始シンボル[{StartSymbol}]を左辺シンボルに持つような文法ルールが存在しません。");
            }

            matched = Grammar.Where(r => !r.LeftSymbol.IsNonTerminal).ToList();
            if (matched.Count > 0) {
                throw new Exception($"Error : 文法ルール[{matched[0]}]の左辺シンボルが終端シンボルです。");
            }

            // 全く同じルールが存在する場合はエラーとする
            for (int i = 0; i < Grammar.Count; i++) {
                for (int j = 0; j < Grammar.Count; j++) {
                    if (i == j) { continue; }
                    if (Grammar[i].Equals(Grammar[j])) {
                        throw new Exception($"Error : 文法ルール[{Grammar[i]}]が重複しています。");
                    }
                }
            }

            // どの非終端シンボルからも生成されない非終端シンボルを探す
            foreach (Symbol s in AllSymbols) {
                if (!s.IsNonTerminal) { continue; }
                // システム開始シンボルはエラー対象外
                if (s.Equals(SystemStartSymbol)) { continue; }
                // あるルールの左辺シンボルが右辺に含まれているルールを探す（複数の場合あり）
                matched = Grammar.Where(r => r.RightSymbols.Contains(s)).ToList();
                // どのルールからも生成されない左辺シンボルはエラーとする
                if (matched.Count == 0) {
                    throw new Exception($"Error : 非終端シンボル[{s}]はどの文法ルールからも生成されません。");
                }
                // 自身ルールのみから生成される左辺シンボルはエラーとする
                if (matched.All(r => r.LeftSymbol.Equals(s))) {
                    throw new Exception($"Error : 非終端シンボル[{s}]は自身を左辺に持つ文法ルール以外からは生成されません。");
                }
                // 2つ以上のルールで閉じたループになっている場合は検知できない
            }
        }

        /// <summary>
        /// 文法をビルドし、構文解析できるようにする
        /// </summary>
        public void Build() {
            // システム定義の開始シンボルからユーザー指定の開始シンボルへのルールを追加
            AddSystemStartRule();
            // 文法内のシンボル一覧を準備
            InitAllSymbols();
            // 文法のチェックを行う（明らかに変なもののみ検知できる）
            CheckGrammar();
            // Nullになる可能性のあるシンボル一覧を準備
            BuildNullsSet();
            // First集合を準備
            BuildFirstSet();
            // DFA（決定性有限オートマトン）を準備
            BuildDFANode();
            // 構文解析テーブルを準備
            BuildParseTable();

        }

        /// <summary>
        /// システム内部の開始シンボルからユーザー指定の開始シンボルへのルールを追加
        /// </summary>
        private void AddSystemStartRule() {
            // S' -> S　※Sはユーザー指定開始シンボル
            Grammar.Insert(0, new(SystemStartSymbol, new() { StartSymbol }));
        }

        /// <summary>
        /// 文法に現れる全シンボルの集合を求める
        /// </summary>
        private void InitAllSymbols() {
            foreach (Rule r in Grammar) {
                AllSymbols.Add(r.LeftSymbol);
                foreach (Symbol s in r.RightSymbols) {
                    AllSymbols.Add(s);
                }
            }

            // システム定義シンボルもここで追加
            AllSymbols.Add(SystemStartSymbol);
            AllSymbols.Add(SystemEndSymbol);
        }

        /// <summary>
        /// Nulls集合を求める
        /// </summary>
        private void BuildNullsSet() {
            while (true) {
                bool isChanged = false;

                foreach (Rule r in Grammar) {
                    // 左シンボルが既にNulls集合に含まれていたらスキップ
                    if (Nulls.Contains(r.LeftSymbol)) { continue; }

                    if (r.RightSymbols.Count == 0) {
                        // 右シンボルが空の場合、左シンボルをNulls集合に追加
                        isChanged |= Nulls.Add(r.LeftSymbol);
                    } else {
                        if (r.RightSymbols.All(s => Nulls.Contains(s))) {
                            // 右シンボルが全てNulls集合に含まれている場合、左シンボルをNulls集合に追加
                            isChanged |= Nulls.Add(r.LeftSymbol);
                        }
                    }
                }

                // 変化がなくなるまで繰り返す
                if (!isChanged) { break; }
            }
        }

        /// <summary>
        /// 与えられたシンボル列に対するFirst集合を求める<br/>
        /// </summary>
        /// <param name="symbols"></param>
        /// <returns></returns>
        private HashSet<Symbol> GetFirst(List<Symbol> symbols) {
            HashSet<Symbol> result = new();
            if (symbols == null || symbols.Count == 0) {
                // シンボル列が空の場合は空を返す
                return result;
            }

            foreach (Symbol s in symbols) {
                if (s.IsNonTerminal) {
                    // 非終端シンボルの場合はFirst(s)を結果に追加
                    First[s].ToList().ForEach(e => result.Add(e));
                } else {
                    // 終端シンボルの場合は自身を結果に追加
                    result.Add(s);
                }

                // Nulls集合に含まれないシンボルの場合、ここで処理終了
                if (!Nulls.Contains(s)) { break; }
            }
            return result;
        }

        /// <summary>
        /// First集合を求める
        /// </summary>
        private void BuildFirstSet() {
            // 非終端シンボルについて、空で初期化
            foreach (Symbol s in AllSymbols) {
                if (s.IsNonTerminal) {
                    First.Add(s, new());
                }
            }

            while (true) {
                bool isChanged = false;

                // あるルールの右シンボル列に対するFirst集合を左シンボル単体のFirst集合に追加
                foreach (Rule r in Grammar) {
                    foreach (Symbol s in GetFirst(r.RightSymbols)) {
                        isChanged |= First[r.LeftSymbol].Add(s);
                    }
                }

                // 変化がなくなるまで繰り返す
                if (!isChanged) { break; }
            }
        }

        /// <summary>
        /// LRアイテム集合に対するClosure集合を求める
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private HashSet<LRItem> GetClosure(HashSet<LRItem> items) {
            // 少なくとも自身は含む
            HashSet<LRItem> closure = new();
            foreach (LRItem itm in items) {
                closure.Add(itm);
            }

            while (true) {
                bool isChanged = false;

                HashSet<LRItem> newItems = new();
                foreach (LRItem itm in closure) {
                    // X -> L. [N]  や  X -> . [N]  のような場合はスキップ
                    if (itm.Rule.RightSymbols.Count == 0
                        || itm.Position + 1 >= itm.Rule.RightSymbols.Count) { continue; }

                    //   X -> L.Y R [N]        というLRアイテムについて
                    //   Y -> Z                という規則があるなら
                    //   Y -> .Z [First(R N)]  を追加
                    //   ※R=[r1 r2]、N=[n1 n2]の場合、First(R N)=[First(r1 r2 n1) + First(r1 r2 n2)]
                    //   ※X,Yは非終端シンボル、Nは終端シンボル、L,R,Zは任意の長さのシンボル列

                    Symbol Y = itm.Rule.RightSymbols[itm.Position + 1];
                    // Yが終端シンボルの場合はスキップ
                    if (!Y.IsNonTerminal) { continue; }

                    List<Symbol> R = new();
                    for (int i = itm.Position + 2; i < itm.Rule.RightSymbols.Count; i++) {
                        R.Add(itm.Rule.RightSymbols[i]);
                    }
                    HashSet<Symbol> N = itm.LookaheadSymbols;

                    // Yが左辺にあるルール
                    List<Rule> RuleOfY = Grammar.Where(r => r.LeftSymbol.Equals(Y)).ToList();
                    foreach (Rule rY in RuleOfY) {
                        HashSet<Symbol> firstRN = new();
                        foreach (Symbol sN in N) {
                            foreach (Symbol sF in GetFirst(R.Append(sN).ToList())) {
                                firstRN.Add(sF);
                            }
                        }
                        // マーカーの位置は先頭固定なことに注意
                        newItems.Add(new(rY, -1, firstRN));
                    }
                }

                foreach (LRItem itmNew in newItems) {
                    // 全く同じLRアイテムの場合は追加しない
                    if (!closure.Contains(itmNew)) {
                        // ルール部分とマーカー位置が同じLRアイテム
                        List<LRItem> itmSameRule = closure.Where(i => i.Rule.Equals(itmNew.Rule) && i.Position == itmNew.Position).ToList();
                        if (itmSameRule.Count == 0) {
                            // そのルールについてのLRアイテムがまだないなら追加
                            isChanged |= closure.Add(itmNew);
                        } else if (itmSameRule.Count == 1) {
                            // そのルールについてのLRアイテムがあるなら、先読みシンボル集合をマージ
                            foreach (Symbol s in itmNew.LookaheadSymbols) {
                                isChanged |= itmSameRule[0].LookaheadSymbols.Add(s);
                            }
                        }
                    }
                }

                // 変化がなくなるまで繰り返す
                if (!isChanged) { break; }
            }

            return closure;
        }

        /// <summary>
        /// DFAノードリストを求める
        /// </summary>
        private void BuildDFANode() {
            // 処理が終わったDFAノードの番号を管理するDictionary
            Dictionary<int, bool> isDoneNthNode = new();

            if (DFANodes.Count == 0) {
                // システム定義開始ルールをもとに最初のDFAノードを作成
                // {S' -> .S [$]}
                Rule sysStartRule = Grammar.Where(e => e.LeftSymbol.Name == "S'").First();
                LRItem sysStartNode = new(sysStartRule, -1, new() { SystemEndSymbol });
                DFANodes.Add(new(0, new() { sysStartNode }, new()));
            }

            // 全てのDFAノードについて未完了とする
            foreach (DFANode node in DFANodes) {
                isDoneNthNode.Add(node.Seq, false);
            }

            while (true) {
                List<DFANode> newNodes = new();

                foreach (DFANode node in DFANodes) {
                    // 既に処理済みのDFAノードの場合はスキップ
                    if (isDoneNthNode[node.Seq]) { continue; }

                    // Closure集合を求める（現在のDFAノードのアイテム集合も含まれる）
                    HashSet<LRItem> closure = GetClosure(node.Items);

                    // 自身も含まれるので、Closure集合で上書きする
                    node.Items = closure;

                    // Closure集合を元に新しいLRアイテムを作成する
                    HashSet<LRItem> newItems = new();
                    foreach (LRItem itm in closure) {
                        // 既にマーカー位置がルール右辺シンボル列の末尾の場合はスキップ
                        if (itm.Position + 1 >= itm.Rule.RightSymbols.Count) { continue; }

                        // マーカーを１つ進めたLRアイテムを作る
                        // {X -> A.B C [N]} なら {X -> A B.C [N]} を作る
                        newItems.Add(new(itm.Rule, itm.Position + 1, itm.LookaheadSymbols));
                    }

                    // 新しいLRアイテムについて、マーカー位置のシンボル毎にグループを作り、
                    // そのグループをアイテム集合とする新しいDFAノードを作成
                    foreach (Symbol s in AllSymbols) {
                        HashSet<LRItem> itmGroup
                            = newItems.Where(i => s.Equals(i.Rule.RightSymbols[i.Position])).ToHashSet();
                        if (itmGroup.Count == 0) { continue; }

                        // 追加予定のDFAノードのアイテム集合を全て含む既存のDFAノードを探す
                        List<DFANode> sameNodes = DFANodes.Where(n => itmGroup.All(i => n.Items.Contains(i))).ToList();
                        if (sameNodes.Count == 0) {
                            // 新しいDFAノードを作成
                            int nextSeq = DFANodes.Count + newNodes.Count;
                            // ループ中に増やすと例外が出るので、一旦新しいリストに追加
                            newNodes.Add(new(nextSeq, itmGroup, new()));
                            // 今回のノードから新しいノードへリンクを貼る
                            node.MoveTo.Add(s, nextSeq);
                        } else {
                            // 新しいDFAノードは作成せずに既存ノードへのリンクを貼る
                            node.MoveTo.TryAdd(s, sameNodes[0].Seq);
                        }
                    }

                    // このノードを完了にする
                    isDoneNthNode[node.Seq] = true;
                }

                // 前回のノード状態から新たに生み出されたDFAノードを追加
                foreach (DFANode node in newNodes) {
                    DFANodes.Add(node);
                    isDoneNthNode.Add(node.Seq, false);
                }

                // 全てのDFAノードに対する処理が終わるまで繰り返し
                if (isDoneNthNode.Values.All(b => b)) { break; }
            }
        }

        /// <summary>
        /// 構文解析テーブルを準備する
        /// </summary>
        private void BuildParseTable() {
            // 全DFAノードについて空で初期化
            foreach (DFANode n in DFANodes) {
                ParseTable.Add(n.Seq, new());
                foreach (Symbol s in AllSymbols) {
                    ParseTable[n.Seq].Add(s, new());
                } 
            }

            foreach (DFANode n in DFANodes) {
                // Shift命令、Goto命令の設定
                foreach (Symbol s in n.MoveTo.Keys) {
                    if (s.IsNonTerminal) {
                        ParseTable[n.Seq][s].Add(new(ParseAction.ActionType.Goto, n.MoveTo[s]));
                    } else {
                        ParseTable[n.Seq][s].Add(new(ParseAction.ActionType.Shift, n.MoveTo[s]));
                    }
                }

                // Reduce命令、Accept命令の設定
                foreach (LRItem itm in n.Items) {
                    if (itm.Position + 1 == itm.Rule.RightSymbols.Count) {
                        foreach (Symbol s in itm.LookaheadSymbols) {
                            if (itm.Rule.Equals(Grammar[0])) {
                                ParseTable[n.Seq][s].Add(new(ParseAction.ActionType.Accept, -1));
                            } else {
                                ParseTable[n.Seq][s].Add(new(ParseAction.ActionType.Reduce, Grammar.IndexOf(itm.Rule)));
                            }
                            
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 文法の情報を表示する
        /// </summary>
        /// <param name="key">
        /// "grammar" : 文法ルール一覧<br/>
        /// "symbol" : シンボル一覧<br/>
        /// "null" : Nulls集合<br/>
        /// "first" : First集合<br/>
        /// "node" : DFAノード一覧<br/>
        /// "table" : 構文解析表<br/>
        /// </param>
        public void PrintParserInfo(string key) {
            switch (key) {
                case "grammar":
                    Console.WriteLine($"[文法ルール一覧]");
                    foreach (Rule r in Grammar) {
                        Console.WriteLine($"    {r}");
                    }
                    break;

                case "symbol":
                    Console.WriteLine($"[シンボル一覧]");
                    Console.WriteLine($"    非終端シンボル : {string.Join(", ", AllSymbols.Where(s => s.IsNonTerminal))}");
                    Console.WriteLine($"      終端シンボル : {string.Join(", ", AllSymbols.Where(s => !s.IsNonTerminal))}");
                    break;

                case "null":
                    Console.WriteLine($"[Nulls集合]");
                    Console.WriteLine($"    {string.Join(", ", Nulls)}");
                    break;

                case "first":
                    Console.WriteLine($"[First集合]");
                    foreach (Symbol s in AllSymbols) {
                        if (s.IsNonTerminal) {
                            Console.WriteLine($"    {s} : {string.Join(", ", First[s])}");
                        }
                    }
                    break;

                case "node":
                    Console.WriteLine($"[DFAノード一覧]");
                    foreach (DFANode n in DFANodes) {
                        Console.WriteLine($"    {n}");
                    }
                    break;

                case "table":
                    Console.WriteLine($"[構文解析表]");
                    List<string> conflicts = new();
                    List<Symbol> syms = new();
                    AllSymbols.Where(s => !s.IsNonTerminal).ToList().ForEach(s => syms.Add(s));
                    AllSymbols.Where(s => s.IsNonTerminal).ToList().ForEach(s => syms.Add(s));
                    Console.WriteLine($"    state,{string.Join(",", syms)}");
                    foreach (DFANode n in DFANodes) {
                        List<string> acts = new();
                        foreach (Symbol s in syms) {
                            if (ParseTable[n.Seq][s].Count > 1) {
                                conflicts.Add($"[{n.Seq}][{s}]");
                            }
                            acts.Add(string.Join("/", ParseTable[n.Seq][s]));
                        }
                        Console.WriteLine($"    {n.Seq},{string.Join(",", acts)}");
                    }
                    if (conflicts.Count > 0) {
                        Console.WriteLine($"    [注意] 構文解析表に競合が含まれています。{string.Join(",", conflicts)}");
                    }
                    break;

                default:
                    Console.WriteLine($"Error : 入力されたキー[{key}]は存在しません。");
                    break;
            }
        }

        /// <summary>
        /// ビルドしたLR(1)文法を用いて構文解析を行う<br/>
        /// 入力文字列を半角スペースで分割し、対応する非終端シンボルに変換して構文解析
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public TreeNode ParseByString(string input) {
            List<TreeNode> inputNodes = new();
            string[] splited = input.Split(" ");
            foreach (string str in splited) {
                inputNodes.Add(new(new(str, false)));
            }

            return Parse(inputNodes);
        }

        /// <summary>
        /// ビルドしたLR(1)文法を用いて構文解析を行う
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TreeNode Parse(List<TreeNode> input) {
            // 末尾に終了記号を追加する（元配列は変更しない）
            List<TreeNode> inputList = new();
            input.ForEach(n => inputList.Add(n));
            inputList.Add(new(SystemEndSymbol));

            // 処理済みの非終端シンボル数（エラー発生位置の検出用）
            int doneSymbolCount = 0;

            // 状態スタック
            Stack<DFANode> stateStack = new();
            // 結果スタック
            Stack<TreeNode> resultStack = new();

            // 最初の状態はDFAノードの先頭
            stateStack.Push(DFANodes[0]);

            while (true) {
                // 現在の状態
                DFANode stateNow = stateStack.Peek();
                // 現在の入力の先頭
                Symbol symbolNow = inputList[0].Symbol;

                if (!AllSymbols.Contains(symbolNow)) {
                    throw new Exception($"Error : {doneSymbolCount + 1}番目のシンボル[{symbolNow}]は未定義です。");
                }

                // 構文解析テーブルから行うべきアクションを取得
                List<ParseAction> acts = ParseTable[stateNow.Seq][symbolNow];
                if (acts.Count == 0) {
                    List<Symbol> symbolMaybe = AllSymbols.Where(s => !s.IsNonTerminal)
                        .Where(s => ParseTable[stateNow.Seq][s].Count > 0).ToList();
                    throw new Exception(
                        $"Error : {doneSymbolCount + 1}番目のシンボルになりうるのは[{string.Join("/", symbolMaybe)}]のみです。");
                } else if (acts.Count > 1) {
                    // 競合が発生している場合はエラーとする
                    throw new Exception($"Error : {doneSymbolCount + 1}番目のシンボルにて競合が発生しました。");
                }

                ParseAction act = acts[0];
                switch (act.Type) {
                    case ParseAction.ActionType.Shift:
                        // 入力を１つ消費（入力から取り出し、結果スタックに積む）
                        resultStack.Push(inputList[0]);
                        inputList.RemoveAt(0);
                        doneSymbolCount++;
                        // Shift命令に付与された番号の状態をスタックに積む
                        stateStack.Push(DFANodes[act.Param]);
                        break;

                    case ParseAction.ActionType.Reduce:
                        // Reduce命令に付与された番号の文法
                        Rule rule = Grammar[act.Param];
                        // 文法の右シンボルの個数だけ状態スタックから除去
                        for (int i = 0; i < rule.RightSymbols.Count; i++) {
                            stateStack.Pop();
                        }
                        // 文法の右シンボルの個数だけ結果スタックから取り出し、
                        // それらを子どもに持つような新たなノードを作成し、結果スタックに積む
                        TreeNode parentNode = new(rule.LeftSymbol);
                        for (int i = 0; i < rule.RightSymbols.Count; i++) {
                            TreeNode childNode = resultStack.Pop();
                            childNode.Parent = parentNode;
                            parentNode.Childs.Add(childNode);
                        }
                        // スタックから取り出した順だと逆順になっているので、反転する
                        parentNode.Childs.Reverse();
                        // 作成したノードを結果スタックに積む
                        resultStack.Push(parentNode);
                        // 次にGoto命令を実行させるために、便宜的に入力の先頭に文法の左シンボルを追加
                        inputList.Insert(0, new(rule.LeftSymbol));
                        break;

                    case ParseAction.ActionType.Goto:
                        // Goto命令に付与された番号の状態をスタックに追加
                        stateStack.Push(DFANodes[act.Param]);
                        // Reduce命令で便宜的に追加した入力なので、消費しておく
                        inputList.RemoveAt(0);
                        break;

                    case ParseAction.ActionType.Accept:
                        if (inputList.Count != 1
                            || !inputList[0].Symbol.Equals(SystemEndSymbol)
                            || resultStack.Count != 1) {
                            throw new Exception($"Error : 入力は受理されましたが、予期せぬエラーが発生しました。");
                        }
                        return resultStack.Pop();
                }
            }
        }
    }
}
