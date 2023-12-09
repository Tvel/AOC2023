using Xunit;

namespace AOC2023;

public class Day06
{
    [Fact]
    void Part1_Sample()
    {
        string input = """
                       Time:      7  15   30
                       Distance:  9  40  200
                       """;

        long result = SolvePart1(input);
        Assert.Equal(288, result);
    }

    [Fact]
    void Part1_Input()
    {
        string input = """
                       Time:        48     93     85     95
                       Distance:   296   1928   1236   1391
                       """;
        long result = SolvePart1(input);
        Assert.Equal(2756160, result);
    }

    private long SolvePart1(string input)
    {
        var raceValues = ParsePart1(input);
        var winWays = new int[raceValues.Length];

        for (var index = 0; index < raceValues.Length; index++)
        {
            (long time, long distance) = raceValues[index];

            for (long holdTime = 1; holdTime < time; holdTime++)
            {
                long remainingTime = time - holdTime;
                long travelLength = holdTime * remainingTime;
                if (travelLength > distance)
                {
                    winWays[index] = winWays[index] + 1;
                }
            }
        }

        var result = winWays.Aggregate(1, (x, y) => x * y);

        return result;
    }

    private static (long Time, long Distance)[] ParsePart1(string input)
    {
        List<long> times = [];
        List<long> distances = [];

        var inputSpan = input.AsSpan();
        Span<Range> linesRanges = stackalloc Range[2]; 
        Span<Range> numbersRanges = stackalloc Range[6];

        int _ = inputSpan.Split(linesRanges, Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        {
            ReadOnlySpan<char> timeLine = inputSpan[linesRanges[0]]["Time:     ".Length..];
            int count = timeLine.Split(numbersRanges, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var numbersRange in numbersRanges[..count])
            {
                times.Add(long.Parse(timeLine[numbersRange]));
            }
        }

        {
            ReadOnlySpan<char> distanceLine = inputSpan[linesRanges[1]]["Distance: ".Length..];
            int count = distanceLine.Split(numbersRanges, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var numbersRange in numbersRanges[..count])
            {
                distances.Add(long.Parse(distanceLine[numbersRange]));
            }
        }

        var raceValues = times.Zip(distances).ToArray();
        return raceValues;
    }

    [Fact]
    void Part2_Sample()
    {
        string input = """
                       Time:      7  15   30
                       Distance:  9  40  200
                       """;

        long result = SolvePart2(input);
        Assert.Equal(71503, result);
    }

    [Fact]
    void Part2_Input()
    {
        string input = """
                       Time:        48     93     85     95
                       Distance:   296   1928   1236   1391
                       """;
        long result = SolvePart2(input);
        Assert.Equal(34788142, result);
    }

    private long SolvePart2(string input)
    {
        (long time, long distance) = ParsePart2(input);
        long winWays = 0;

        for (long holdTime = 1; holdTime < time; holdTime++)
        {
            long remainingTime = time - holdTime;
            long travelLength = holdTime * remainingTime;
            if (travelLength > distance)
            {
                winWays++;
            }
        }
        
        return winWays;
    }

    private static (long Time, long Distance) ParsePart2(string input)
    {
        List<char> times = [];
        List<char> distances = [];

        var inputSpan = input.AsSpan();
        Span<Range> linesRanges = stackalloc Range[2];
        Span<Range> numbersRanges = stackalloc Range[6];

        int _ = inputSpan.Split(linesRanges, Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        {
            ReadOnlySpan<char> timeLine = inputSpan[linesRanges[0]]["Time:     ".Length..];
            int count = timeLine.Split(numbersRanges, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var numbersRange in numbersRanges[..count])
            {
                times.AddRange(timeLine[numbersRange]);
            }
        }

        {
            ReadOnlySpan<char> distanceLine = inputSpan[linesRanges[1]]["Distance: ".Length..];
            int count = distanceLine.Split(numbersRanges, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var numbersRange in numbersRanges[..count])
            {
                distances.AddRange(distanceLine[numbersRange]);
            }
        }

        return (long.Parse(new string(times.ToArray())), long.Parse(new string(distances.ToArray())) );
    }
}