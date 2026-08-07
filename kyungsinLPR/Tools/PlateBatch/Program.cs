using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace PlateBatch
{
    /// <summary>
    /// labels.csv (path,cam,plate,date,time) 를 읽어 모든 이미지에 휴리스틱 검출을 돌리고
    /// (1) 검출 성공률 통계, (2) 박스 수 분포, (3) 종횡비 분포를 출력.
    /// 일부 샘플은 _detect.jpg 오버레이로 저장하여 시각적 검증 가능.
    ///
    /// 사용법:
    ///   PlateBatch.exe <labels.csv> <outDir> [sampleCount]
    /// 예:
    ///   PlateBatch.exe D:\image\labels.csv D:\image\batch_out 100
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("사용법: PlateBatch.exe <labels.csv> <outDir> [sampleCount=100]");
                return 1;
            }
            string csvPath = args[0];
            string outDir = args[1];
            int sampleCount = args.Length >= 3 && int.TryParse(args[2], out var sc) ? sc : 100;

            if (!File.Exists(csvPath)) { Console.Error.WriteLine("CSV 없음: " + csvPath); return 2; }
            Directory.CreateDirectory(outDir);
            string sampleDir = Path.Combine(outDir, "samples");
            Directory.CreateDirectory(sampleDir);

            var rows = ReadCsv(csvPath);
            Console.WriteLine("총 라벨 행: " + rows.Count);

            int processed = 0, opened = 0, withCandidates = 0, savedSamples = 0;
            int[] candHist = new int[6]; // 0,1,2,3,4,5+
            var aspectList = new List<double>();
            var widthList = new List<int>();
            var heightList = new List<int>();
            var perfMs = new List<long>();
            var failExamples = new List<string>();

            // 결과 CSV
            using (var resWriter = new StreamWriter(Path.Combine(outDir, "results.csv"), false, new UTF8Encoding(true)))
            {
                resWriter.WriteLine("path,plate,opened,candidateCount,topBoxX,topBoxY,topBoxW,topBoxH,topAspect,elapsedMs");

                int sampleEvery = Math.Max(1, rows.Count / Math.Max(1, sampleCount));
                int rowIdx = -1;
                var sw = new Stopwatch();
                var totalSw = Stopwatch.StartNew();
                foreach (var r in rows)
                {
                    rowIdx++;
                    processed++;

                    Bitmap src = null;
                    Bitmap pre = null;
                    int candCount = 0;
                    Rectangle top = Rectangle.Empty;
                    long elapsed = 0;
                    bool isOpen = false;
                    try
                    {
                        if (!File.Exists(r.Path)) { failExamples.Add("MISSING: " + r.Path); goto WRITE; }
                        sw.Restart();
                        src = new Bitmap(r.Path);
                        isOpen = true; opened++;
                        pre = Detector.PreprocessForOcr(src);
                        var cands = Detector.DetectPlateCandidates(pre);
                        sw.Stop();
                        elapsed = sw.ElapsedMilliseconds;
                        perfMs.Add(elapsed);

                        candCount = cands.Count;
                        if (candCount > 0)
                        {
                            withCandidates++;
                            top = cands[0];
                            // 원본 좌표 환산 (전처리는 2배 확대)
                            widthList.Add(top.Width / 2);
                            heightList.Add(top.Height / 2);
                            aspectList.Add((double)top.Width / top.Height);
                        }
                        candHist[Math.Min(5, candCount)]++;

                        // 샘플 저장 — 일정 간격
                        if (rowIdx % sampleEvery == 0 && savedSamples < sampleCount)
                        {
                            string baseName = Path.GetFileNameWithoutExtension(r.Path);
                            string outPath = Path.Combine(sampleDir, baseName + "_detect.jpg");
                            Detector.SaveOverlay(pre, cands, outPath);
                            savedSamples++;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (failExamples.Count < 20) failExamples.Add("ERR " + r.Path + " — " + ex.Message);
                    }
                    finally
                    {
                        pre?.Dispose();
                        src?.Dispose();
                    }

                    WRITE:
                    resWriter.WriteLine(string.Join(",", new[]
                    {
                        EscapeCsv(r.Path), r.Plate,
                        isOpen ? "1" : "0",
                        candCount.ToString(),
                        top.X.ToString(), top.Y.ToString(), top.Width.ToString(), top.Height.ToString(),
                        candCount > 0 ? ((double)top.Width / top.Height).ToString("F2") : "",
                        elapsed.ToString()
                    }));

                    if (processed % 200 == 0)
                        Console.WriteLine(string.Format("[{0,4}/{1}] opened={2} withCands={3} ({4:F1}%) avg={5:F1}ms",
                            processed, rows.Count, opened, withCandidates,
                            opened > 0 ? withCandidates * 100.0 / opened : 0,
                            perfMs.Count > 0 ? perfMs.Average() : 0));
                }
                totalSw.Stop();
                Console.WriteLine("총 처리 시간: " + totalSw.Elapsed);
            }

            // 통계 출력
            Console.WriteLine();
            Console.WriteLine("================ 검출 통계 ================");
            Console.WriteLine("총 처리:                " + processed);
            Console.WriteLine("이미지 열림:            " + opened);
            Console.WriteLine("후보 1개 이상 검출:     " + withCandidates +
                              (opened > 0 ? string.Format(" ({0:F1}%)", withCandidates * 100.0 / opened) : ""));
            Console.WriteLine();
            Console.WriteLine("후보 개수 분포:");
            for (int i = 0; i < candHist.Length; i++)
                Console.WriteLine(string.Format("  후보 {0}개: {1,5}", i == 5 ? "5+" : i.ToString(), candHist[i]));

            if (perfMs.Count > 0)
            {
                perfMs.Sort();
                Console.WriteLine();
                Console.WriteLine(string.Format("처리 시간 (ms): avg={0:F1} p50={1} p95={2} max={3}",
                    perfMs.Average(), perfMs[perfMs.Count / 2], perfMs[(int)(perfMs.Count * 0.95)], perfMs.Last()));
            }

            if (widthList.Count > 0)
            {
                widthList.Sort(); heightList.Sort(); aspectList.Sort();
                Console.WriteLine();
                Console.WriteLine("Top1 박스 (원본 좌표계 환산):");
                Console.WriteLine(string.Format("  폭   px:   p25={0} p50={1} p75={2}", widthList[widthList.Count / 4], widthList[widthList.Count / 2], widthList[widthList.Count * 3 / 4]));
                Console.WriteLine(string.Format("  높이 px:   p25={0} p50={1} p75={2}", heightList[heightList.Count / 4], heightList[heightList.Count / 2], heightList[heightList.Count * 3 / 4]));
                Console.WriteLine(string.Format("  종횡비:   p25={0:F2} p50={1:F2} p75={2:F2}", aspectList[aspectList.Count / 4], aspectList[aspectList.Count / 2], aspectList[aspectList.Count * 3 / 4]));
            }

            if (failExamples.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("실패 예시 (최대 20개):");
                foreach (var f in failExamples) Console.WriteLine("  " + f);
            }

            Console.WriteLine();
            Console.WriteLine("결과 CSV: " + Path.Combine(outDir, "results.csv"));
            Console.WriteLine("샘플 오버레이: " + sampleDir + " (" + savedSamples + "개)");
            return 0;
        }

        private class LabelRow
        {
            public string Path, Cam, Plate, Date, Time;
            public LabelRow(string p, string c, string pl, string d, string t)
            { Path = p; Cam = c; Plate = pl; Date = d; Time = t; }
        }

        private static List<LabelRow> ReadCsv(string csvPath)
        {
            var list = new List<LabelRow>();
            using (var sr = new StreamReader(csvPath, Encoding.UTF8))
            {
                string header = sr.ReadLine(); // header
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var parts = ParseCsvLine(line);
                    if (parts.Count >= 5) list.Add(new LabelRow(parts[0], parts[1], parts[2], parts[3], parts[4]));
                }
            }
            return list;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuote = false;
            foreach (var ch in line)
            {
                if (ch == '"') inQuote = !inQuote;
                else if (ch == ',' && !inQuote) { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
            result.Add(sb.ToString());
            return result;
        }

        private static string EscapeCsv(string s)
        {
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
