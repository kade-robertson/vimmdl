using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

namespace VimmDownloader 
{
    class Program 
    {
        static void Main(string[] args) {
            var id = (uint)0;
            Console.Write("[?] Enter the download ID: ");

            try {
                id = uint.Parse(Console.ReadLine());
            } catch {
                Console.WriteLine("[!] Error: Unable to parse an ID. Exiting...");
                return;
            }

            Console.WriteLine($"[i] Attempting to download ID {id}...");
            DoDownload(id);
            Console.Read();
        }

        static void DoDownload(uint id) {
            using (var wc = new WebClient()) {
                try {
                    wc.DownloadProgressChanged += (sender, e) => {
                        Console.Write("[");
                        for (int i = 0; i < 25; i++) {
                            if (e.ProgressPercentage > 4 * i) {
                                Console.Write("=");
                            } else {
                                Console.Write(" ");
                            }
                        }
                        var bstrings = GetSizeStrings(e.BytesReceived, e.TotalBytesToReceive);
                        Console.Write($"] {e.ProgressPercentage.ToString()}% ({bstrings[0]} / {bstrings[1]})    \r\r");
                    };

                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.98");
                    wc.Headers.Add("Referer", "http://vimm.net/vault/?p=details&id={id}");
                    Nito.AsyncEx.AsyncContext.Run(() => wc.DownloadFileTaskAsync(new Uri($"http://download.vimm.net/download.php?id={id}"), $"{id}.tmp"));
                    File.Move($"{id}.tmp", wc.ResponseHeaders["Content-Disposition"].Split('"')[1]);
                    Console.WriteLine("\nDownload completed!");
                } catch (Exception ex) {
                    Console.WriteLine($"[!] {ex.Message}");
                    Console.WriteLine("[!] Error: Unable to download file. Exiting...");
                    return;
                }
            }
        }

        static string[] GetSizeStrings(long cur, long tot) {
            string ret = "B";
            double divd = 1;
            if (tot > Math.Pow(1024, 3)) {
                ret = "GB";
                divd = Math.Pow(1024, 3);
            } else if (tot > Math.Pow(1024, 2)) {
                ret = "MB";
                divd = Math.Pow(1024, 2);
            } else if (tot > 1024) {
                ret = "KB";
                divd = 1024;
            }

            return new string[] { $"{((double) cur / divd).ToString("#.00")} {ret}", $"{((double) tot / divd).ToString("#.00")} {ret}" };
        }
    }
}
