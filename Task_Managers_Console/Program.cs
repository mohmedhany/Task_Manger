using System.Diagnostics;
using System.Text;

// إعدادات الـ Console لتحسين الأداء والشكل
Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

// قاموس لتخزين القراءات السابقة لحساب الـ CPU
var lastCpuCheck = new Dictionary<int, (DateTime Time, TimeSpan CpuTime)>();

while (true)
{
    Console.SetCursorPosition(0, 0);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Advanced System Monitor v3.0 | .NET 10 | {DateTime.Now:T}");
    Console.WriteLine(new string('=', 75));
    Console.WriteLine($"{"PID",-8} | {"Process Name",-25} | {"CPU %",-8} | {"Memory",-15}");
    Console.WriteLine(new string('-', 75));
    Console.ResetColor();

    var processes = Process.GetProcesses()
                          .OrderByDescending(p => p.WorkingSet64)
                          .Take(15);

    foreach (var p in processes)
    {
        try
        {
            // 1. حساب الـ Memory
            double memoryMb = p.WorkingSet64 / 1024.0 / 1024.0;

            // 2. حساب الـ CPU % (Logic)
            double cpuUsage = 0;
            var currentTime = DateTime.Now;
            var currentCpuTime = p.TotalProcessorTime;

            if (lastCpuCheck.TryGetValue(p.Id, out var last))
            {
                // المعادلة: (الفرق في وقت المعالجة / الفرق في الوقت الزمني) / عدد الأنوية
                double cpuUsedMs = (currentCpuTime - last.CpuTime).TotalMilliseconds;
                double totalMsPassed = (currentTime - last.Time).TotalMilliseconds;
                cpuUsage = (cpuUsedMs / (totalMsPassed * Environment.ProcessorCount)) * 100;
            }

            // تحديث القاموس للقراءة القادمة
            lastCpuCheck[p.Id] = (currentTime, currentCpuTime);

            // 3. العرض الملون بناءً على الاستهلاك
            if (cpuUsage > 50 || memoryMb > 1000) Console.ForegroundColor = ConsoleColor.Red;
            else if (cpuUsage > 20 || memoryMb > 500) Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine($"{p.Id,-8} | {p.ProcessName,-25} | {cpuUsage,6:F1}% | {memoryMb,10:F2} MB");
            Console.ResetColor();
        }
        catch { continue; }
    }

    Console.WriteLine("\n[K] Kill PID | [Q] Quit | Refreshing every 2s...");

    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.K) HandleKill();
        if (key == ConsoleKey.Q) break;
    }

    await Task.Delay(2000);
}

static void HandleKill()
{
    Console.CursorVisible = true;
    Console.Write("\nEnter PID: ");
    if (int.TryParse(Console.ReadLine(), out int pid))
    {
        try { Process.GetProcessById(pid).Kill(); }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); Thread.Sleep(1000); }
    }
    Console.CursorVisible = false;
}