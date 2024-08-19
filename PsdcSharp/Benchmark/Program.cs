using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Scover.Psdc.Messages;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1) {
            Console.Error.WriteLine($"usage: {Path.GetRelativePath(Environment.CurrentDirectory, Environment.ProcessPath ?? "benchmark")} INPUT_FILE");
            return;
        }

        var filename = Path.GetFullPath(args[0]);
        Console.WriteLine($"Benchmarking {filename}");

        Environment.SetEnvironmentVariable(EnvNameFilename, filename);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args[1..],
            ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.JoinSummary));
    }

    const string EnvNameFilename = "psdc_benchmark_filename";

    public static Parameters Parameters {
        get {
            var code = File.ReadAllText(
                Environment.GetEnvironmentVariable(EnvNameFilename)
                ?? throw new InvalidOperationException("Filename env var not set"));

            return new(code, new BenchmarkMessenger(code));
        }
    }
}

readonly record struct Parameters(string Code, Messenger Msger);
