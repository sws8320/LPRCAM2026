using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace PlateBatch
{
    /// <summary>
    /// OptionK의 검출 알고리즘만 분리한 standalone 버전.
    /// frmLprMain / clsThread 의존 없이 작동 — 배치 검증용.
    /// </summary>
    public static class Detector
    {
        /// <summary>2배 확대 → BT.601 그레이스케일 → Otsu 이진화 (Format24bppRgb 유지)</summary>
        public static Bitmap PreprocessForOcr(Bitmap input)
        {
            int newW = input.Width * 2;
            int newH = input.Height * 2;

            Bitmap output = new Bitmap(newW, newH, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(output))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(input, new Rectangle(0, 0, newW, newH));
            }

            var rect = new Rectangle(0, 0, newW, newH);
            BitmapData bd = output.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                int total = stride * newH;
                byte[] buf = new byte[total];
                Marshal.Copy(bd.Scan0, buf, 0, total);

                // 그레이스케일
                for (int y = 0; y < newH; y++)
                {
                    int rowStart = y * stride;
                    for (int x = 0; x < newW; x++)
                    {
                        int p = rowStart + x * 3;
                        byte b = buf[p], gr = buf[p + 1], r = buf[p + 2];
                        byte luma = (byte)((r * 299 + gr * 587 + b * 114) / 1000);
                        buf[p] = buf[p + 1] = buf[p + 2] = luma;
                    }
                }

                // Otsu 임계값
                int[] hist = new int[256];
                for (int y = 0; y < newH; y++)
                {
                    int rs = y * stride;
                    for (int x = 0; x < newW; x++) hist[buf[rs + x * 3]]++;
                }
                int totalPixels = newW * newH;
                long sumAll = 0;
                for (int i = 0; i < 256; i++) sumAll += (long)i * hist[i];

                long sumB = 0; int wB = 0; double maxVar = 0; int threshold = 127;
                for (int t = 0; t < 256; t++)
                {
                    wB += hist[t];
                    if (wB == 0) continue;
                    int wF = totalPixels - wB;
                    if (wF == 0) break;
                    sumB += (long)t * hist[t];
                    double mB = (double)sumB / wB;
                    double mF = (double)(sumAll - sumB) / wF;
                    double varBetween = (double)wB * wF * (mB - mF) * (mB - mF);
                    if (varBetween > maxVar) { maxVar = varBetween; threshold = t; }
                }

                for (int y = 0; y < newH; y++)
                {
                    int rs = y * stride;
                    for (int x = 0; x < newW; x++)
                    {
                        int p = rs + x * 3;
                        byte v = (byte)(buf[p] >= threshold ? 255 : 0);
                        buf[p] = buf[p + 1] = buf[p + 2] = v;
                    }
                }

                Marshal.Copy(buf, 0, bd.Scan0, total);
            }
            finally { output.UnlockBits(bd); }

            return output;
        }

        /// <summary>
        /// Edge Density 기반 번호판 후보 사각형 검출.
        /// row/column 전환 카운트 → 종횡비 2.5~7 + 최소 폭 200 + 종횡비 4.5 가까운 순 상위 5개.
        /// </summary>
        public static List<Rectangle> DetectPlateCandidates(Bitmap binarized)
        {
            int w = binarized.Width, h = binarized.Height;
            var rect = new Rectangle(0, 0, w, h);
            var bd = binarized.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = bd.Stride;
            byte[] buf = new byte[stride * h];
            Marshal.Copy(bd.Scan0, buf, 0, buf.Length);
            binarized.UnlockBits(bd);

            int[] rowTrans = new int[h];
            for (int y = 0; y < h; y++)
            {
                int rs = y * stride;
                byte prev = buf[rs];
                int cnt = 0;
                for (int x = 1; x < w; x++)
                {
                    byte cur = buf[rs + x * 3];
                    if (cur != prev) { cnt++; prev = cur; }
                }
                rowTrans[y] = cnt;
            }

            const int rowTh = 30;
            const int maxRowGap = 5;
            var bands = new List<Tuple<int, int>>();
            int bandStart = -1, bandLast = -1, gap = 0;
            for (int y = 0; y < h; y++)
            {
                if (rowTrans[y] >= rowTh)
                {
                    if (bandStart == -1) bandStart = y;
                    bandLast = y; gap = 0;
                }
                else if (bandStart != -1)
                {
                    gap++;
                    if (gap > maxRowGap)
                    {
                        int bh = bandLast - bandStart + 1;
                        if (bh >= 40 && bh <= 400) bands.Add(Tuple.Create(bandStart, bandLast + 1));
                        bandStart = -1; bandLast = -1; gap = 0;
                    }
                }
            }
            if (bandStart != -1)
            {
                int bh = bandLast - bandStart + 1;
                if (bh >= 40 && bh <= 400) bands.Add(Tuple.Create(bandStart, bandLast + 1));
            }

            var candidates = new List<Rectangle>();
            foreach (var b in bands)
            {
                int yS = b.Item1, yE = b.Item2, bandH = yE - yS;
                int[] colTrans = new int[w];
                for (int x = 0; x < w; x++)
                {
                    byte prev = buf[yS * stride + x * 3];
                    int cnt = 0;
                    for (int y = yS + 1; y < yE; y++)
                    {
                        byte cur = buf[y * stride + x * 3];
                        if (cur != prev) { cnt++; prev = cur; }
                    }
                    colTrans[x] = cnt;
                }
                int colTh = Math.Max(3, bandH * 2 / 10);
                int maxColGap = Math.Max(20, bandH);

                int xS = -1, xL = -1, cgap = 0;
                for (int x = 0; x < w; x++)
                {
                    if (colTrans[x] >= colTh)
                    {
                        if (xS == -1) xS = x;
                        xL = x; cgap = 0;
                    }
                    else if (xS != -1)
                    {
                        cgap++;
                        if (cgap > maxColGap)
                        {
                            AddBoxIfValid(candidates, xS, xL, yS, yE, bandH, w, h);
                            xS = -1; xL = -1; cgap = 0;
                        }
                    }
                }
                if (xS != -1) AddBoxIfValid(candidates, xS, xL, yS, yE, bandH, w, h);
            }

            candidates.Sort((a, b) =>
            {
                double dA = Math.Abs((double)a.Width / a.Height - 4.5);
                double dB = Math.Abs((double)b.Width / b.Height - 4.5);
                return dA.CompareTo(dB);
            });
            if (candidates.Count > 5) candidates = candidates.GetRange(0, 5);
            return candidates;
        }

        private static void AddBoxIfValid(List<Rectangle> list, int xS, int xL, int yS, int yE, int bandH, int imgW, int imgH)
        {
            int boxW = xL - xS + 1;
            double aspect = (double)boxW / bandH;
            if (aspect < 2.5 || aspect > 7.0 || boxW < 200) return;
            int padX = (int)(bandH * 0.1), padY = (int)(bandH * 0.1);
            int x0 = Math.Max(0, xS - padX);
            int y0 = Math.Max(0, yS - padY);
            int x1 = Math.Min(imgW, xL + 1 + padX);
            int y1 = Math.Min(imgH, yE + padY);
            list.Add(new Rectangle(x0, y0, x1 - x0, y1 - y0));
        }

        /// <summary>검출 박스를 빨간 사각형 + 번호 표시로 그린 디버그 이미지 저장.</summary>
        public static void SaveOverlay(Bitmap binarized, List<Rectangle> boxes, string outPath)
        {
            using (var clone = new Bitmap(binarized))
            using (var g = Graphics.FromImage(clone))
            using (var pen = new Pen(Color.Red, 5))
            using (var font = new Font("Arial", 30, FontStyle.Bold))
            {
                for (int i = 0; i < boxes.Count; i++)
                {
                    g.DrawRectangle(pen, boxes[i]);
                    g.DrawString((i + 1).ToString(), font, Brushes.Red, boxes[i].X, boxes[i].Y);
                }
                clone.Save(outPath, ImageFormat.Jpeg);
            }
        }
    }
}
