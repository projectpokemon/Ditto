using System;
using System.Diagnostics;
using System.IO;

namespace Watchog
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: Watchog <filename> [args]");
                    return;
                }

                Console.WriteLine($"[{DateTime.Now.ToString()}] Watchog started.");

                var p = new Process();

                Console.CancelKeyPress += delegate
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Received Control+C. Closing program.");
                    p.Close();
                    if (!p.HasExited)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Program not running. Waiting for up to 10 seconds.");
                        p.WaitForExit(10 * 1000);
                        if (!p.HasExited)
                        {
                            p.Kill();
                            Console.WriteLine($"[{DateTime.Now.ToString()}] Killed program.");
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString()}] Program exited on its own. Program not killed.");
                        }
                    }
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Watchog exiting.");
                };

                if (File.Exists(args[0]))
                {
                    while (true)
                    {
                        p = new Process();
                        p.StartInfo.FileName = args[0];
                        p.StartInfo.Arguments = args[1];
                        p.WaitForExit();
                        p.Dispose();
                        p = null;

                        // Program exited, and Watchog should restart
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Program exited. Restarting...");
                    }
                }
                else
                {
                    throw new FileNotFoundException("Can't find the file on disk", args[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Watchog encountered an exception: {ex.ToString()}");
                throw;
            }            
        }
    }
}
