# Benchmarks

## 18/08: sudoku.psc: IgnoreMessenger

### Before benchmark CLI

| Type                    | Method | Mean      | Error     | StdDev    | Gen0     | Gen1   | Allocated |
|------------------------ |------- |----------:|----------:|----------:|---------:|-------:|----------:|
| CodeGenerationBenchmark | RunC   |  45.09 μs |  0.324 μs |  0.287 μs |   5.4321 |      - |  22.28 KB |
| ParsingBenchmark        | Run    | 970.10 μs | 12.957 μs | 12.120 μs | 137.6953 | 0.9766 | 565.16 KB |
| StaticAnalysisBenchmark | Run    |  40.76 μs |  0.505 μs |  0.473 μs |   7.9956 |      - |  32.73 KB |
| TokenizationBenchmark   | Run    | 296.66 μs |  1.394 μs |  1.236 μs |   8.7891 |      - |  37.26 KB |

### After

| Type                    | Method | Mean      | Error     | StdDev    | Gen0     | Gen1   | Allocated |
|------------------------ |------- |----------:|----------:|----------:|---------:|-------:|----------:|
| CodeGenerationBenchmark | RunC   |  45.66 μs |  0.740 μs |  0.692 μs |   5.4321 |      - |  22.28 KB |
| ParsingBenchmark        | Run    | 992.63 μs | 18.163 μs | 16.101 μs | 136.7188 | 1.9531 | 565.16 KB |
| StaticAnalysisBenchmark | Run    |  41.27 μs |  0.620 μs |  0.549 μs |   7.9956 |      - |  32.73 KB |
| TokenizationBenchmark   | Run    | 302.02 μs |  4.515 μs |  4.002 μs |   8.7891 |      - |  37.23 KB |

## 18/08: sudoku.psc: BenchmarkMessenger

| Type                    | Method | Mean      | Error     | StdDev    | Gen0     | Gen1   | Allocated |
|------------------------ |------- |----------:|----------:|----------:|---------:|-------:|----------:|
| CodeGenerationBenchmark | RunC   |  45.98 μs |  0.892 μs |  1.128 μs |   5.4321 |      - |  22.28 KB |
| ParsingBenchmark        | Run    | 993.95 μs | 19.215 μs | 17.974 μs | 136.7188 | 1.9531 | 565.16 KB |
| StaticAnalysisBenchmark | Run    |  85.08 μs |  0.551 μs |  0.515 μs |   9.0332 |      - |  37.29 KB |
| TokenizationBenchmark   | Run    | 303.29 μs |  3.579 μs |  3.348 μs |   8.7891 |      - |  37.23 KB |

## 18/08: cd.psc: IgnoreMessenger

| Type                    | Method | Mean      | Error    | StdDev   | Gen0    | Allocated |
|------------------------ |------- |----------:|---------:|---------:|--------:|----------:|
| CodeGenerationBenchmark | RunC   |  32.55 μs | 0.095 μs | 0.084 μs |  2.2583 |   9.45 KB |
| ParsingBenchmark        | Run    | 292.63 μs | 1.811 μs | 1.605 μs | 38.0859 |  156.2 KB |
| StaticAnalysisBenchmark | Run    |  15.44 μs | 0.045 μs | 0.040 μs |  2.8992 |  11.88 KB |
| TokenizationBenchmark   | Run    | 124.62 μs | 0.921 μs | 0.769 μs |  3.4180 |  14.29 KB |

## 18/08: cd.psc: BenchmarkMessenger

| Type                    | Method | Mean      | Error    | StdDev   | Gen0    | Allocated |
|------------------------ |------- |----------:|---------:|---------:|--------:|----------:|
| CodeGenerationBenchmark | RunC   |  32.68 μs | 0.331 μs | 0.293 μs |  2.2583 |   9.45 KB |
| ParsingBenchmark        | Run    | 293.11 μs | 4.016 μs | 3.354 μs | 38.0859 |  156.2 KB |
| StaticAnalysisBenchmark | Run    |  30.78 μs | 0.223 μs | 0.186 μs |  3.7231 |  15.25 KB |
| TokenizationBenchmark   | Run    | 127.91 μs | 1.298 μs | 1.214 μs |  3.4180 |  14.29 KB |
