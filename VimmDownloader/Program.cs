﻿using System;
using System.IO;
using System.Net;
using Nito.AsyncEx;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace VimmDownloader 
{
    class Program 
    {
        static KeyValuePair<string, int> GetSizeString(long val) {
            string ret = "B";
            int pow = 0;
            if (val > Math.Pow(1024, 3)) {
                ret = "GB";
                pow = 3;
            } else if (val > Math.Pow(1024, 2)) {
                ret = "MB";
                pow = 2;
            } else if (val > 1024) {
                ret = "KB";
                pow = 1;
            }

            return new KeyValuePair<string, int>($"{((double)val / Math.Pow(1024, pow)).ToString("0.00")} {ret}", pow);
        }

        static string SetSizeString(long val, int pow) {
            string ret = "B";
            switch (pow) {
                case 1: ret = "KB"; break;
                case 2: ret = "MB"; break;
                case 3: ret = "GB"; break;
            }

            return $"{((double)val / Math.Pow(1024, pow)).ToString("0.00")} {ret}";
        }

        static void DoDownload(uint id) {
            using (var wc = new WebClient()) {
                var measr = new double[1000];
                var point = 0L;
                var watch = new Stopwatch();
                var lastb = 0L;
                var totab = "";
                var cpowr = 0;
                var mrow = 0;
                var mcol = 0;

                try {
                    wc.DownloadProgressChanged += (sender, e) => {
                        watch.Stop();
                        var el = Math.Max(1, watch.Elapsed.TotalMilliseconds);
                        var bd = e.BytesReceived - lastb;
                        var dp = (double)bd / (el / 1000);
                        measr[point++ % 1000] = dp;
                        lastb = e.BytesReceived;

                        if (mrow == 0 && mcol == 0) {
                            Console.Write("    ");
                            mrow = Console.CursorTop;
                            mcol = Console.CursorLeft;
                        }

                        Console.Write("[i] [");
                        for (int i = 0; i < 50;) {
                            if (e.ProgressPercentage >= (2 * ++i)) {
                                Console.Write("=");
                            } else {
                                Console.Write(" ");
                            }
                        }

                        if (totab == string.Empty) {
                            var temp = GetSizeString(e.TotalBytesToReceive);
                            totab = temp.Key;
                            cpowr = temp.Value;
                        }

                        Console.Write($"] {e.ProgressPercentage.ToString()}%\n    [i] ");
                        Console.Write($"({SetSizeString(e.BytesReceived, cpowr)} / {totab}) ");
                        Console.Write($"({GetSizeString((long)measr.Average()).Key}/s)   \r\r");

                        Console.CursorTop = mrow;
                        Console.CursorLeft = mcol;

                        watch.Restart();
                    };

                    wc.Headers.Add("User-Agent", "Mozilla/5.0");
                    wc.Headers.Add("Referer", "http://vimm.net/vault/?p=details&id={id}");
                    watch.Start();

                    AsyncContext.Run(() => wc.DownloadFileTaskAsync(new Uri($"http://download.vimm.net/download.php?id={id}"), $"{id}.tmp"));

                    var newname = wc.ResponseHeaders["Content-Disposition"].Split('"')[1];
                    File.Delete(newname);
                    File.Move($"{id}.tmp", newname);
                    Console.CursorTop += 2;
                    Console.WriteLine("\n    [i] Download completed!");
                    Console.WriteLine($"    [i] File name: {newname}");
                } catch (Exception ex) {
                    Console.WriteLine($"\n    [!] {ex.Message}");
                    Console.WriteLine("    [!] Error: Unable to download file. Exiting...");
                    return;
                }

                if (wc.IsBusy) {
                    AsyncContext.Run(() => wc.CancelAsync());
                }
            }
        }

        static void Main(string[] args) {
            var id = (uint)0;
            Console.WriteLine("                         _    __ _                                       ");
            Console.WriteLine("                        | |  / /(_)____ ___   ____ ___                   ");
            Console.WriteLine("                        | | / // // __ `__ \\ / __ `__ \\                  ");
            Console.WriteLine("                        | |/ // // / / / / // / / / / /                  ");
            Console.WriteLine("         ____           |___//_//_/ /_/_/_//_/ /_/ /_/      __           ");
            Console.WriteLine("        / __ \\ ____  _      __ ____   / /____   ____ _ ____/ /___   _____");
            Console.WriteLine("       / / / // __ \\| | /| / // __ \\ / // __ \\ / __ `// __  // _ \\ / ___/");
            Console.WriteLine("      / /_/ // /_/ /| |/ |/ // / / // // /_/ // /_/ // /_/ //  __// /    ");
            Console.WriteLine("     /_____/ \\____/ |__/|__//_/ /_//_/ \\____/ \\__,_/ \\__,_/ \\___//_/     ");
            Console.WriteLine();
            Console.WriteLine("   ========================================================================");
            Console.WriteLine();
            Console.Write("    [?] Enter the download ID: ");

            try {
                id = uint.Parse(Console.ReadLine());
            } catch {
                Console.WriteLine("    [!] Error: Unable to parse an ID. Exiting...");
                return;
            }

            Console.WriteLine($"    [i] Attempting to download ID {id}...");
            DoDownload(id);
            Console.Read();
        }
    }
}
