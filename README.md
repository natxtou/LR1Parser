# What's this?
LR(1)構文解析器の生成および、構文解析を実行するC#ライブラリです

# How to use
Testフォルダ内のサンプルプログラムを参照

# Excuse
興味本位、勉強目的で書いてみたものなので、厳密なテストはしていません  
誤った構文解析結果が出力される可能性があります  
また、パフォーマンスも度外視しています  

# Sample
入力文法例
```
Equation -> Add | Mult
Add -> AddLeft AddSymbol AddRight
AddLeft -> 1 | Add | Mult
AddSymbol -> + | -
AddRight -> 1 | Mult
Mult -> MultLeft MultSymbol MultRight
MultLeft -> 1 | Mult
MultSymbol -> * | /
MultRight -> 1
```

構文解析結果例
```
[入力文字列]
1 * 1 - 1 + 1 / 1

[解析結果（１行表示）]
Equation(Add(AddLeft(Add(AddLeft(Mult(MultLeft(1) MultSymbol(*) MultRight(1))) AddSymbol(-) AddRight(1))) AddSymbol(+) AddRight(Mult(MultLeft(1) MultSymbol(/) MultRight(1)))))

[解析結果（シンプル）]
(((1 * 1) - 1) + (1 / 1))

[解析結果（階層表示）]
Equation
`-- Add
    |-- AddLeft
    |   `-- Add
    |       |-- AddLeft
    |       |   `-- Mult
    |       |       |-- MultLeft
    |       |       |   `-- 1
    |       |       |-- MultSymbol
    |       |       |   `-- *
    |       |       `-- MultRight
    |       |           `-- 1
    |       |-- AddSymbol
    |       |   `-- -
    |       `-- AddRight
    |           `-- 1
    |-- AddSymbol
    |   `-- +
    `-- AddRight
        `-- Mult
            |-- MultLeft
            |   `-- 1
            |-- MultSymbol
            |   `-- /
            `-- MultRight
                `-- 1
```
