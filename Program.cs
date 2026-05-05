using System;
using System.Collections.Generic;
using System.Globalization;
using Serilog;

namespace TriangleAnalyzer
{
    
    class TriangleResult
    {
        public string Type;                 
        public List<(int X, int Y)> Points; 
    }

    static class TriangleCalculator
    {
        const float Eps = 1e-6f;
        const int CanvasSize = 100;

        
        public static TriangleResult Calculate(string aStr, string bStr, string cStr)
        {
            var res = new TriangleResult();

            
            if (!TryParse(aStr, out float a) || !TryParse(bStr, out float b) || !TryParse(cStr, out float c))
            {
                res.Type = "";  
                return res;
            }

           
            if (!CanFormTriangle(a, b, c))
            {
                res.Type = "не треугольник";
                return res;
            }

            
            res.Type = GetTriangleType(a, b, c);

           
            res.Points = GetVertices(a, b, c);

            return res;
        }

       
        static bool TryParse(string input, out float value)
        {
            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return value > 0;
            return false;
        }

        
        static bool CanFormTriangle(float a, float b, float c)
        {
            return a + b > c && a + c > b && b + c > a;
        }

       
        static string GetTriangleType(float a, float b, float c)
        {
            bool ab = Math.Abs(a - b) < Eps;
            bool bc = Math.Abs(b - c) < Eps;
            bool ca = Math.Abs(c - a) < Eps;

            if (ab && bc) return "равносторонний";
            if (ab || bc || ca) return "равнобедренный";
            return "разносторонний";
        }

        
        static List<(int X, int Y)> GetVertices(float a, float b, float c)
        {
            
            float x = (b * b + c * c - a * a) / (2 * c);
            float y = (float)Math.Sqrt(b * b - x * x);

            var local = new[] { (0f, 0f), (c, 0f), (x, y) };

           
            float minX = local[0].Item1, maxX = local[0].Item1;
            float minY = local[0].Item2, maxY = local[0].Item2;
            foreach (var p in local)
            {
                if (p.Item1 < minX) minX = p.Item1;
                if (p.Item1 > maxX) maxX = p.Item1;
                if (p.Item2 < minY) minY = p.Item2;
                if (p.Item2 > maxY) maxY = p.Item2;
            }

            float width = maxX - minX;
            float height = maxY - minY;
            if (width < Eps) width = 1;
            if (height < Eps) height = 1;

            
            var scaled = new List<(int X, int Y)>();
            foreach (var p in local)
            {
                int sx = (int)Math.Round((p.Item1 - minX) / width * CanvasSize);
                int sy = (int)Math.Round((p.Item2 - minY) / height * CanvasSize);
                sx = Math.Clamp(sx, 0, CanvasSize);
                sy = Math.Clamp(sy, 0, CanvasSize);
                scaled.Add((sx, sy));
            }
            return scaled;
        }
    }

    class Program
    {
        static void Main()
        {
           
            string template = "{Timestamp:HH:mm:ss} | [{Level:u3}] | {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: template)
                .WriteTo.File("logs/triangle_log_.txt", outputTemplate: template, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Приложение запущено. Введите длины сторон (каждую с новой строки). exit - выход.");

            while (true)
            {
                Console.WriteLine("\n--- Новый запрос ---");
                Console.Write("Сторона A: ");
                string a = Console.ReadLine();
                if (IsExit(a)) break;

                Console.Write("Сторона B: ");
                string b = Console.ReadLine();
                if (IsExit(b)) break;

                Console.Write("Сторона C: ");
                string c = Console.ReadLine();
                if (IsExit(c)) break;

                ProcessRequest(a, b, c);
            }

            Log.Information("Приложение завершено.");
            Log.CloseAndFlush();
        }

        static bool IsExit(string input) => string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase);

        static void ProcessRequest(string a, string b, string c)
        {
            string parameters = $"({a}, {b}, {c})";
            var result = TriangleCalculator.Calculate(a, b, c);

            
            bool success = result.Type == "равносторонний" || result.Type == "равнобедренный" || result.Type == "разносторонний";

            if (success)
            {
                string coords = string.Join("; ", result.Points.ConvertAll(p => $"({p.X},{p.Y})"));
                Log.Information($"УСПЕХ | {parameters} | Тип: {result.Type} | Координаты: [{coords}]");
            }
            else
            {
                string reason = string.IsNullOrEmpty(result.Type) ? "нечисловые данные" : result.Type;
                Log.Warning($"НЕУСПЕХ | {parameters} | Результат: {reason}");
            }
        }
    }
}