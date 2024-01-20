using LR1Parser;

internal class Program {
    private static void Main(string[] args) {

        // ファイルから文法を読み込んで構文解析器をビルドし、
        // 入力文字列を構文解析する簡単なサンプル

        LR1Parser.LR1Parser LRP = new();

        Console.WriteLine("文法ルールのファイルパスを入力してください。");
        Console.Write("> ");
        string grammarFilePath = Console.ReadLine() ?? "";

        // ファイル読み込み
        List<string> grammarStrings = new();
        using (StreamReader sr = new(grammarFilePath)) {
            while (true) {
                string? line = sr.ReadLine();
                if (line == null) { break; }
                grammarStrings.Add(line);
            }
        }

        // ファイルから読み込んだ文字列を１行ずつパーサの文法ルールに追加
        grammarStrings
            .Where(s => s.Trim(' ') != "")
            .Where(s => s.Trim(' ')[0] != '#')
            .ToList()
            .ForEach(s => LRP.AddRuleByString(s));

        Console.WriteLine("開始シンボルを入力してください。");
        Console.Write("> ");
        string startSymbol = Console.ReadLine() ?? "";

        // 文法の開始シンボルを設定
        LRP.SetStartSymbol(startSymbol);

        // 文法をビルド
        LRP.Build();

        // ビルド後のパーサ情報を表示
        LRP.PrintParserInfo("grammar");
        LRP.PrintParserInfo("symbol");
        LRP.PrintParserInfo("null");
        LRP.PrintParserInfo("first");
        LRP.PrintParserInfo("node");
        LRP.PrintParserInfo("table");

        while (true) {
            Console.WriteLine("構文解析したい文字列（シンボル同士は半角スペース区切り）を入力してください。");
            Console.Write("> ");

            string? inputString = Console.ReadLine();
            if (inputString == null || inputString.Length == 0) { break; }

            try {
                // ビルドしたパーサを使って入力文字列を構文解析
                TreeNode top = LRP.ParseByString(inputString);

                // 構文解析によって作成された構文木の文字列表現を表示
                // １行表示
                Console.WriteLine("[解析結果（１行表示）]");
                Console.WriteLine(TreeNode.GetTreeStringFlat(top));
                // １行表示（シンプル）
                Console.WriteLine("[解析結果（シンプル）]");
                Console.WriteLine(TreeNode.GetTreeStringSimple(top));
                // 階層表示
                Console.WriteLine("[解析結果（階層表示）]");
                Console.WriteLine(TreeNode.GetTreeStringLayer(top));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }
    }
}