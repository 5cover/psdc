# Benchmarks

## cd.psc

### 18/08

| Type                    | Method |      Mean |    Error |   StdDev |    Gen0 | Allocated |
| ----------------------- | ------ | --------: | -------: | -------: | ------: | --------: |
| CodeGenerationBenchmark | RunC   |  32.55 μs | 0.095 μs | 0.084 μs |  2.2583 |   9.45 KB |
| LexingBenchmark         | Run    | 124.62 μs | 0.921 μs | 0.769 μs |  3.4180 |  14.29 KB |
| ParsingBenchmark        | Run    | 292.63 μs | 1.811 μs | 1.605 μs | 38.0859 |  156.2 KB |
| StaticAnalysisBenchmark | Run    |  15.44 μs | 0.045 μs | 0.040 μs |  2.8992 |  11.88 KB |

### 21/12

| Type                    | Method | Mean      | Error    | StdDev   | Gen0    | Allocated |
|------------------------ |------- |----------:|---------:|---------:|--------:|----------:|
| CodeGenerationBenchmark | RunC   |  36.48 us | 0.121 us | 0.101 us |  2.8076 |   11.7 KB |
| LexingBenchmark         | Run    | 166.82 us | 0.938 us | 0.877 us |  6.1035 |  25.66 KB |
| ParsingBenchmark        | Run    | 334.76 us | 0.737 us | 0.616 us | 53.2227 | 218.53 KB |
| StaticAnalysisBenchmark | Run    |  21.22 us | 0.045 us | 0.038 us |  3.8147 |  15.61 KB |

## if.psc

### 21/12

| Type                    | Method | Mean        | Error    | StdDev   | Gen0     | Gen1   | Allocated |
|------------------------ |------- |------------:|---------:|---------:|---------:|-------:|----------:|
| CodeGenerationBenchmark | RunC   |    64.91 us | 1.246 us | 1.746 us |  10.1318 |      - |  41.44 KB |
| LexingBenchmark         | Run    |   486.63 us | 4.571 us | 4.275 us |  12.6953 |      - |  53.78 KB |
| ParsingBenchmark        | Run    | 1,353.43 us | 6.282 us | 5.569 us | 228.5156 | 1.9531 | 939.95 KB |
| StaticAnalysisBenchmark | Run    |    46.55 us | 0.281 us | 0.249 us |   9.7656 |      - |  40.01 KB |
