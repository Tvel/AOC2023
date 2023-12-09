﻿using System.Buffers;
using Xunit;

namespace AOC2023;

public class Day05
{
    [Fact]
    void Part1_Sample()
    {
        long result = SolvePart1(Input.Sample1);
        Assert.Equal(35, result);
    }

    [Fact]
    void Part1_Input()
    {
        long result = SolvePart1(Input.Part1);
        Assert.Equal(457535844, result);
    }

    private long SolvePart1(string input)
    {
        (List<long> seeds, Dictionary<string, Mapping> mappings) = Parser.Parse(input);

        List<long> locations = [];
        foreach (var seed in seeds)
        {
            long soil = mappings["seed-to-soil"][seed];
            long fertilizer = mappings["soil-to-fertilizer"][soil];
            long water = mappings["fertilizer-to-water"][fertilizer];
            long light = mappings["water-to-light"][water];
            long temperature = mappings["light-to-temperature"][light];
            long humidity = mappings["temperature-to-humidity"][temperature];
            long location = mappings["humidity-to-location"][humidity];

            locations.Add(location);
        }

        return locations.Min();
    }

    [Fact]
    void Part2_Sample()
    {
        long result = SolvePart2Slow(Input.Sample1);
        Assert.Equal(46, result);
    }

    [Fact]
    void Part2_Input()
    {
        long result = SolvePart2(Input.Part1);
        Assert.Equal(41222968, result);
    }

    private long SolvePart2(string input)
    {
        (List<long> seeds, Dictionary<string, Mapping> mappings) = Parser.Parse(input);

        var chunks = seeds.Chunk(2).ToArray();

        var soilToSeed = mappings["seed-to-soil"].Reverse();
        var fertilizerToSoil = mappings["soil-to-fertilizer"].Reverse();
        var waterToFertilizer = mappings["fertilizer-to-water"].Reverse();
        var lightToWater = mappings["water-to-light"].Reverse();
        var temperatureToLight = mappings["light-to-temperature"].Reverse();
        var humidityToTemperature = mappings["temperature-to-humidity"].Reverse();
        var locationToHumidity = mappings["humidity-to-location"].Reverse();

        long min = long.MaxValue;

        var locationsBySource = locationToHumidity.Mappings.Select(x => (x.Value.sourceStart, x.Value.sourceEnd)).OrderBy(x => x.sourceStart).ToArray();

        foreach ((long locationStart, long locationEnd) in locationsBySource)
        {
            for (long l = locationStart; l <= locationEnd; l++)
            {
                var humidity = locationToHumidity[l];
                var temperature = humidityToTemperature[humidity];
                var light = temperatureToLight[temperature];
                var water = lightToWater[light];
                var fertilizer = waterToFertilizer[water];
                var soil = fertilizerToSoil[fertilizer];
                var seed = soilToSeed[soil];

                foreach (var seedChunk in chunks)
                {
                    if (seed >= seedChunk[0] && seed <= (seedChunk[0] + seedChunk[1]))
                    {
                        return l;
                    }
                }
            }
        }

        return min;
    }

    private long SolvePart2Slow(string input)
    {
        (List<long> seeds, Dictionary<string, Mapping> mappings) = Parser.Parse(input);

        var chunks = seeds.Chunk(2).ToArray();
        
        var seedToSoil = mappings["seed-to-soil"];
        var soilToFertilizer = mappings["soil-to-fertilizer"];
        var fertilizerToWater = mappings["fertilizer-to-water"];
        var waterToLight = mappings["water-to-light"];
        var lightToTemperature = mappings["light-to-temperature"];
        var temperatureToHumidity = mappings["temperature-to-humidity"];
        var humidityToLocation = mappings["humidity-to-location"];

        long min = long.MaxValue;
        foreach (var range in chunks)
        {
            Parallel.For(range[0], range[0] + range[1], (seed =>
            {
                long soil = seedToSoil[seed];
                long fertilizer = soilToFertilizer[soil];
                long water = fertilizerToWater[fertilizer];
                long light = waterToLight[water];
                long temperature = lightToTemperature[light];
                long humidity = temperatureToHumidity[temperature];
                long location = humidityToLocation[humidity];

                long initialValue, computedValue;
                do
                {
                    initialValue = min;
                    computedValue = initialValue;
                    if (location < initialValue) computedValue = location;

                } while (initialValue != Interlocked.CompareExchange(ref min, computedValue, initialValue));
            }));
        }

        return min;
    }

    [Fact]
    void MappingTest()
    {
        var mapping = new Mapping("seed", "soil");
        mapping.Add(50, 98, 2);
        mapping.Add(52, 50, 48);

        Assert.Equal(50, mapping.Get(98));
        Assert.Equal(51, mapping.Get(99));
        Assert.Equal(55, mapping.Get(53));
        Assert.Equal(10, mapping.Get(10));

        Assert.Equal(50, mapping[98]);
        Assert.Equal(51, mapping[99]);
        Assert.Equal(55, mapping[53]);
        Assert.Equal(10, mapping[10]);
    }
}


file static class Parser
{
    public static (List<long> seeds, Dictionary<string, Mapping> mappings) Parse(string input)
    {
        Dictionary<string, Mapping> mappings = [];
        List<long> seeds = [];
        var inputSpan = input.AsSpan();
        Range[] linesRanges = ArrayPool<Range>.Shared.Rent(400);
        Span<Range> mapRanges = stackalloc Range[3];
        Mapping currentMap = null;
        int lineCount = inputSpan.Split(linesRanges, Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        {
            ReadOnlySpan<char> seedsLine = inputSpan[linesRanges[0]]["seeds: ".Length..];
            Range[] seedsRanges = ArrayPool<Range>.Shared.Rent(100);

            int count = seedsLine.Split(seedsRanges, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var seedRange in seedsRanges[..count])
            {
                seeds.Add(long.Parse(seedsLine[seedRange]));
            }
        }
        for (var lineIndex = 1; lineIndex < lineCount; lineIndex++)
        {
            ReadOnlySpan<char> line = inputSpan[linesRanges[lineIndex]];
            if (line.EndsWith(" map:"))
            {
                ReadOnlySpan<char> mappingLine = line[..^" map:".Length];
                mappingLine.Split(mapRanges, "-to-", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                currentMap = new Mapping(mappingLine[mapRanges[0]].ToString(), mappingLine[mapRanges[1]].ToString());
                mappings.Add(mappingLine.ToString(), currentMap);
            }
            else
            {
                line.Split(mapRanges, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                currentMap.Add(long.Parse(line[mapRanges[0]]), long.Parse(line[mapRanges[1]]), long.Parse(line[mapRanges[2]]));
            }
        }

        return (seeds, mappings);
    }

}

file record Mapping(string From, string To)
{
    private readonly List<(long sourceStart, long sourceEnd, long destinationStart, long range)?> mappingList = new();
    
    public void Add(long destination, long source, long range)
    {
        mappingList.Add((source, source + range - 1, destination, range));
    }

    public long Get(long sourceValue)
    {
        var map = mappingList
                      .FirstOrDefault(x => sourceValue >= x.Value.sourceStart && sourceValue <= x.Value.sourceEnd) ?? (sourceValue, sourceValue, sourceValue, 1);

        return map.destinationStart + (sourceValue - map.sourceStart);
    }
    public long this[long index] => Get(index);

    public IReadOnlyCollection<(long sourceStart, long sourceEnd, long destinationStart, long range)?> Mappings => mappingList.AsReadOnly();

    public Mapping Reverse()
    {
        var mapping = new Mapping(this.To, this.From);

        foreach (var map in mappingList)
        {
            mapping.Add(map.Value.sourceStart, map.Value.destinationStart, map.Value.range);
        }
        return mapping;
    }
}

file static class Input
{
    public const string Sample1 = """
                                  seeds: 79 14 55 13
                                  
                                  seed-to-soil map:
                                  50 98 2
                                  52 50 48
                                  
                                  soil-to-fertilizer map:
                                  0 15 37
                                  37 52 2
                                  39 0 15
                                  
                                  fertilizer-to-water map:
                                  49 53 8
                                  0 11 42
                                  42 0 7
                                  57 7 4
                                  
                                  water-to-light map:
                                  88 18 7
                                  18 25 70
                                  
                                  light-to-temperature map:
                                  45 77 23
                                  81 45 19
                                  68 64 13
                                  
                                  temperature-to-humidity map:
                                  0 69 1
                                  1 0 69
                                  
                                  humidity-to-location map:
                                  60 56 37
                                  56 93 4
                                  """;
    
    public const string Part1 = """
                                seeds: 515785082 87905039 2104518691 503149843 720333403 385234193 1357904101 283386167 93533455 128569683 2844655470 24994629 3934515023 67327818 2655687716 8403417 3120497449 107756881 4055128129 9498708
                                
                                seed-to-soil map:
                                2025334497 3876763368 16729580
                                1877945250 2032519622 95086460
                                0 679167893 381174930
                                717319608 469672599 20842400
                                1677700339 1823837909 22353530
                                634816620 1372848321 73458998
                                2756794066 2812828157 182758452
                                3324095721 3392359690 456362171
                                969898963 32396659 196640650
                                1973031710 2127606082 52302787
                                4095486882 3893492948 33982348
                                381174930 591894131 9141137
                                3247991211 2466896352 76104510
                                1645303680 0 32396659
                                3023330013 4070306098 224661198
                                2329063131 1900645524 131874098
                                2042064077 3115509825 242853312
                                969753308 1446307319 145655
                                4214866116 3035408645 80101180
                                589310441 1846191439 28201780
                                4129469230 2811864212 963945
                                510217282 1276450763 79093159
                                2989333460 3358363137 33996553
                                3780457892 2179908869 286987483
                                738162008 229037309 231591300
                                2460937229 3927475296 26993487
                                1205326488 1547352475 237698559
                                2487930716 2543000862 268863350
                                4170255211 3998614525 21910631
                                1543924548 490514999 101379132
                                1443025047 1446452974 100899501
                                2939552518 4020525156 49780942
                                2284917389 3954468783 44145742
                                617512221 1355543922 17304399
                                4067445375 3848721861 28041507
                                1166539613 1785051034 38786875
                                708275618 460628609 9043990
                                390316067 1156549548 119901215
                                4192165842 1877945250 22700274
                                4130433175 2995586609 39822036
                                1700053869 601035268 78132625
                                1778186494 1060342823 96206725
                                
                                soil-to-fertilizer map:
                                0 2341619969 92369762
                                1167455233 3617741643 38094704
                                1499102298 1681004272 234557927
                                3376581938 2517836559 179214694
                                2914723756 619913619 149410902
                                161434282 0 41510814
                                3649464352 3314750891 40779782
                                2317356783 1915562199 186244783
                                713485962 950249988 47781849
                                421834722 1125088099 56442466
                                2507552861 3655836347 34407787
                                3565617524 2433989731 83846828
                                2678581348 103843947 189846804
                                995696604 565539077 54374542
                                410018942 2894022626 11815780
                                938203483 303511643 57493121
                                586429700 998031837 127056262
                                3064134658 1533041508 147962764
                                2219062401 2305658720 35961249
                                2050866563 393391944 168195838
                                3803001923 4143671264 151296032
                                814953468 2101806982 123250015
                                1050071146 1508839376 17695066
                                2629883889 1526534442 6507066
                                92369762 3199390767 69064520
                                3212097422 3355530673 164484516
                                4049139793 3803001923 245827503
                                2541960648 3549487250 55536061
                                2255023650 41510814 62333133
                                478277188 842097476 108152512
                                213047569 2697051253 196971373
                                3954297955 4048829426 48488938
                                4002786893 4097318364 46352900
                                1733660225 1181530565 317206338
                                2649109287 3520015189 29472061
                                2636390955 3605023311 12718332
                                2503601566 561587782 3951295
                                202945096 1498736903 10102473
                                1140539167 2278742654 26916066
                                3555796632 293690751 9820892
                                1205549937 2905838406 293552361
                                1067766212 769324521 72772955
                                2597496709 361004764 32387180
                                761267811 2225056997 53685657
                                2868428152 3268455287 46295604
                                
                                fertilizer-to-water map:
                                152178464 250673346 55422
                                152233886 237175480 13497866
                                3154188384 1047083609 4715554
                                3582007164 1051799163 5818075
                                3259979115 2494521724 322028049
                                402907232 308602731 62253347
                                4236473989 2233392950 58493307
                                2707774309 1208011821 30397527
                                3587825239 2917624950 58473157
                                896622971 631491687 63460153
                                465160579 250728768 57873963
                                165731752 0 237175480
                                2439165 370856078 149739299
                                3074181106 3899730566 80007278
                                3059559654 1999627040 14621452
                                1312214893 3041353489 50783396
                                960083124 694951840 352131769
                                2680544515 1972397246 27229794
                                631491687 1238409348 57882415
                                3646298396 3092136885 387540126
                                2592786348 2014248492 87758167
                                1513392872 3479677011 420053555
                                2509534881 1681896910 83251467
                                0 520595377 2439165
                                2738171836 1352203735 287691765
                                1362998289 1057617238 150394583
                                3158903938 2816549773 101075177
                                4033838522 2291886257 202635467
                                1933446427 1296291763 55911972
                                689374102 1765148377 207248869
                                2152304019 1639895500 42001410
                                1989358399 2135702712 97690238
                                3025863601 2102006659 33696053
                                2087048637 2976098107 65255382
                                2194305429 3979737844 315229452
                                
                                water-to-light map:
                                2953662638 2442860750 178173989
                                1541545030 2734817557 190358536
                                754320741 167246313 76339023
                                1856345147 4205331132 89636164
                                4252036650 1935658232 42930646
                                155447228 526835738 21442665
                                854896092 243585336 283250402
                                1353462278 548278403 77281269
                                3280902948 2925176093 9989800
                                1430743547 1124319386 47116063
                                3675997643 3263466919 126502268
                                3189943537 3895004042 77007091
                                3439702455 2103030459 136553424
                                1836535231 2083220543 19809916
                                1207650119 1274680470 145812159
                                2726058004 2958658296 227604634
                                3131836627 4078001241 58106910
                                3290892748 4136108151 65420425
                                1138146494 74127343 49701217
                                320969084 0 74127343
                                3356313173 4038105259 39895982
                                2313549351 2239583883 7585248
                                3908092261 2247169131 195691619
                                205670012 625559672 115299072
                                830659764 1100083058 24236328
                                3424990872 3972011133 14711583
                                80982326 1171435449 74464902
                                3576255879 1835916468 99741764
                                176889893 1245900351 28780119
                                395096427 740858744 359224314
                                2533259155 1541545030 192798849
                                3396209155 2706035840 28781717
                                1945981311 4201528576 3802556
                                0 123828560 23615345
                                3802499911 3789411692 105592350
                                1949783867 3986722716 51382543
                                2509766752 2935165893 23492403
                                4167035549 2621034739 85001101
                                23615345 1420492629 57366981
                                3266950628 3249514599 13952320
                                1731903566 1978588878 104631665
                                2211976762 1734343879 101572589
                                2001166410 3578601340 210810352
                                2321134599 3389969187 188632153
                                1187847711 147443905 19802408
                                4103783880 3186262930 63251669
                                
                                light-to-temperature map:
                                70532163 2072528772 6548798
                                4144686847 1559382043 150280449
                                54527625 2056524234 16004538
                                3807793247 2758133633 336893600
                                650952420 859025504 83666107
                                1904509744 3183659444 2786814
                                780330456 3472688125 531542959
                                77080961 54527625 154573827
                                2350216539 1865381176 99600417
                                3084022977 1715376404 150004772
                                2107664798 4058224014 213619824
                                1773073526 793980359 19333216
                                231654788 2452888407 305245226
                                3781460885 2030191872 26332362
                                2939630982 2331937347 79772249
                                560023472 1070561556 50945056
                                2478552333 1313852724 245529319
                                3234027749 526405562 267574797
                                536900014 4271843838 23123458
                                1873309968 1709662492 5713912
                                1879023880 2427402543 25485864
                                2724081652 2116388017 215549330
                                1922425580 2079077570 37310447
                                734618527 813313575 45711929
                                1702870875 971426988 70202651
                                3019403231 3186446258 64619746
                                3767451847 4044214976 14009038
                                3575105735 1121506612 192346112
                                1959736027 3324759354 147928771
                                1792406742 2411709596 15692947
                                1808099689 1964981593 65210279
                                1907296558 3095027233 15129022
                                1311873415 209101452 317304110
                                2449816956 942691611 28735377
                                610968528 4004231084 39983892
                                2321284622 1041629639 28931917
                                3501602546 3110156255 73503189
                                1629177525 3251066004 73693350
                                
                                temperature-to-humidity map:
                                2698939019 1899653215 333343198
                                636293562 2781983613 635230295
                                3050376312 0 239799964
                                92201313 2779048878 2934735
                                3290176276 2660814640 25026650
                                2134083411 2489247727 2136257
                                3851297241 3604655691 314442217
                                1483568567 866597732 152309188
                                333293267 1018906920 303000295
                                0 2685841290 1438147
                                1635877755 2687279437 53836876
                                3032282217 2471153632 18094095
                                26400590 1558914368 65800723
                                1271523857 1346869658 212044710
                                2136219668 239799964 524786786
                                2661006454 2741116313 37932565
                                1859145287 1624715091 274938124
                                1689714631 2491383984 169430656
                                1438147 1321907215 24962443
                                3315202926 764586750 102010982
                                95136048 2232996413 238157219
                                3604655691 3919097908 90929169
                                3695584860 4010027077 155712381
                                
                                humidity-to-location map:
                                2245504116 1166524785 164267337
                                2409771453 1729601904 604997365
                                120247692 2545941209 34587342
                                3116311219 3036964896 76986730
                                1512620570 0 147162638
                                3512343576 4259469787 35497509
                                1169583301 1336067922 123578155
                                3921210971 3312190478 126745070
                                2181345242 2427978815 64158874
                                0 1459646077 120247692
                                4223038352 3474502707 71928944
                                779681654 1579893769 149708135
                                3894412843 3962661335 26798128
                                1659783208 1330792122 5275800
                                500368604 393220323 51352254
                                3686138511 3754387003 208274332
                                632498588 2492137689 53803520
                                1135055763 147162638 34527538
                                686302108 2334599269 93379546
                                551720858 2731943364 80777730
                                306544451 181690176 42409340
                                4115726396 3237074496 75115982
                                3547841085 4014859186 138297426
                                3080744060 3438935548 35567159
                                1293161456 2812721094 202047724
                                929389789 960858811 205665974
                                4190842378 3113951626 32195974
                                1736823657 516337226 444521585
                                154835034 224099516 151709417
                                3036964896 3614528274 43779164
                                1495209180 375808933 17411390
                                1665059008 444572577 71764649
                                348953791 2580528551 151414813
                                3299611124 3546431651 68096623
                                3421416680 3146147600 90926896
                                4047956041 3989459463 25399723
                                3367707747 3700678070 53708933
                                4073355764 3658307438 42370632
                                3193297949 4153156612 106313175
                                """;
}