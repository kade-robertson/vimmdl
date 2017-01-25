using System;
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
        // Determines the appropriate size unit to use for reporting the
        // file size.
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

        // This was made so that the units of the current download amount
        // match the units of the final download amount.
        static string SetSizeString(long val, int pow) {
            string ret = "B";
            switch (pow) {
                case 1: ret = "KB"; break;
                case 2: ret = "MB"; break;
                case 3: ret = "GB"; break;
            }

            return $"{((double)val / Math.Pow(1024, pow)).ToString("0.00")} {ret}";
        }

        static void DoDownload(uint id, string opath = "") {
            using (var wc = new WebClient()) {
                // Most of these are state variables for tracking the download speed.
                // Simply uses an array of 1,000 doubles to track the most recent speeds,
                // which will later be averaged when displayed.
                var measr = new double[1000];
                var point = 0L;
                var watch = new Stopwatch();
                var lastb = 0L;
                var totab = string.Empty;
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

                        // This is simply to remember where the status information is printed,
                        // because we need to return there to make sure everything prints nicely.
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

                        // Just some throttling so the title of the window isn't updated too often.
                        if (point % 100 == 0) {
                            Console.Title = $"Vimm Downloader | {SetSizeString(e.BytesReceived, cpowr)} of {totab}, {GetSizeString((long)measr.Average()).Key}";
                        }

                        Console.CursorTop = mrow;
                        Console.CursorLeft = mcol;

                        watch.Restart();
                    };

                    // Vimm won't allow you to download without a "valid" user agent, however this small
                    // snippet of one allows it to work just fine.
                    wc.Headers.Add("User-Agent", "Mozilla/5.0");

                    // You will be directed to the ROM's main page if you don't include it as the
                    // referring page.
                    wc.Headers.Add("Referer", $"http://vimm.net/vault/?p=details&id={id}");
                    watch.Start();

                    // AsyncContext helps an issue I was noticing previously, because some of the progress
                    // reports were coming far after ones that should have been the other way around (i.e.
                    // it would print 44% before 23%).
                    AsyncContext.Run(() => wc.DownloadFileTaskAsync(new Uri($"http://download.vimm.net/download.php?id={id}"), $"{id}.tmp"));

                    // Unfortunately this is the only way I can find out the intended name of the file.
                    var newname = wc.ResponseHeaders["Content-Disposition"].Split('"')[1];
                    if (opath != string.Empty) {
                        newname = opath;
                    }

                    File.Delete(newname);
                    File.Move($"{id}.tmp", newname);
                    Console.CursorTop += 2;
                    Console.WriteLine("\n    [i] Download completed!");
                    Console.WriteLine($"    [i] File name: {newname}");
                    Console.Title = "Vimm Downloader | Complete";
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
            var spath = string.Empty;

            if (args.Length > 0) {
                try {
                    id = uint.Parse(args[0]);
                } catch { }
            }

            if (args.Length > 1) {
                spath = args[1];
            }

            Console.Title = "Vimm Downloader | Idle";

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
            if (args.Where(x => x == "/?").Count() > 0) {
                Console.WriteLine("    [i] Usage:");
                Console.WriteLine("    [i]   - vimmdl (displays the prompt window)");
                Console.WriteLine("    [i]   - vimmdl <id> (downloads the specific ID)");
                Console.WriteLine("    [i]   - vimmdl <id> <path> (downloads the file to a specified path)");
                Console.Read();
            } else {
                Console.Write("    [?] Enter the download ID: ");

                if (id > 0) {
                    Console.WriteLine(id);
                } else {
                    try {
                        id = uint.Parse(Console.ReadLine());
                    } catch {
                        Console.WriteLine("    [!] Error: Unable to parse an ID. Exiting...");
                        return;
                    }
                }

                Console.WriteLine($"    [i] Attempting to download ID {id}...");
                DoDownload(id, spath);
                Console.Read();
            }
        }
    }
}
