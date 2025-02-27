using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using ClickHouse.Client.ADO;
using DoubletsVsClickHouseBenchmarks.Library;

namespace DoubletsVsClickHouseBenchmarks;

[Config(typeof(Config))]
[MemoryDiagnoser]
public class DoubletsVsClickHouseBenchmarks
{
     private class Config : ManualConfig
     {
          public Config()
          {
               Add(Job.ShortRun.WithWarmupCount(1).WithLaunchCount(1).WithIterationCount(1));
          }
     }
    public static string CsvFilePath;
    public static ClickHouseConnection ClickHouseConnection;
    public static List<Candle> Candles;
    public static System.Random Random;

    static DoubletsVsClickHouseBenchmarks()
    {
         CsvFilePath = Environment.GetEnvironmentVariable(nameof(CsvFilePath)) ?? throw new Exception($"{nameof(CsvFilePath)} environment variable must be set");
         Candles = new CsvCandleParser().Parse(CsvFilePath).ToList();
         Random = new System.Random();
         var clickHouseConnectionString = Environment.GetEnvironmentVariable(nameof(ClickHouseConnection)) ?? throw new Exception($"{nameof(ClickHouseConnection)} environment variable must be set"); 
         ClickHouseConnection = new ClickHouseConnection(clickHouseConnectionString);
    }

    public static IEnumerable<IBenchmarkable> Benchmarkables() => new List<IBenchmarkable>()
    {
         new ClickHouseAdapter(ClickHouseConnection),
         new DoubletsAdapter<UInt64>(),
    };
    public (DateTimeOffset, DateTimeOffset) GenerateRandomMinAndMaxStartingTimes()
    {
         var randomStartingTime0 = Random.NextInt64(DateTimeOffset.Now.AddMonths(-1).ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
         var randomStartingTime1 = Random.NextInt64(DateTimeOffset.Now.AddMonths(-1).ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
         if (randomStartingTime0 > randomStartingTime1)
         {
              return (DateTimeOffset.FromUnixTimeSeconds(randomStartingTime0), DateTimeOffset.FromUnixTimeSeconds(randomStartingTime1));
         }
         else
         {
              return (DateTimeOffset.FromUnixTimeSeconds(randomStartingTime1), DateTimeOffset.FromUnixTimeSeconds(randomStartingTime0));
         }
    }

    [ParamsSource(nameof(Benchmarkables))]
    public IBenchmarkable Benchmarkable { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {

    }

    [IterationSetup(Target = nameof(DeleteBenchmark))]
    public void DeleteIterationSetup()
    {
         Benchmarkable.SaveCandles(Candles).Wait();
    }

    [Benchmark]
    public void DeleteBenchmark()
    {
         var minAndMaxStartingTimes = GenerateRandomMinAndMaxStartingTimes();
         Benchmarkable.DeleteCandles(minAndMaxStartingTimes.Item1, minAndMaxStartingTimes.Item2).Wait();
    }

    [Benchmark]
    public void SaveBenchmark()
    {
         Benchmarkable.SaveCandles(Candles).Wait();
    }
    
    [IterationCleanup(Target = nameof(SaveBenchmark))]
    public void SaveIterationCleanup()
    {
         var minAndMaxStartingTimes = GenerateRandomMinAndMaxStartingTimes();
         Benchmarkable.DeleteCandles(minAndMaxStartingTimes.Item1, minAndMaxStartingTimes.Item2).Wait();
    }

    [IterationSetup(Target = nameof(GetBenchmark))]
    public void GetIterationSetup()
    {
         Benchmarkable.SaveCandles(Candles).Wait();
    }
    
    [Benchmark]
    public void GetBenchmark()
    {
         var minAndMaxStartingTimes = GenerateRandomMinAndMaxStartingTimes();
         Benchmarkable.GetCandles(minAndMaxStartingTimes.Item1, minAndMaxStartingTimes.Item2).Wait();
    }

    [IterationCleanup(Target = nameof(GetBenchmark))]
    public void GetIterationCleanup()
    {
         var minAndMaxStartingTimes = GenerateRandomMinAndMaxStartingTimes();
         Benchmarkable.DeleteCandles(minAndMaxStartingTimes.Item1, minAndMaxStartingTimes.Item2).Wait();
    }

}
