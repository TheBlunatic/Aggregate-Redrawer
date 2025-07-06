using FastBitmapLib;
using LpSolveDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AO_Redrawer
{
    public class PriorityQueue<TKey, TValue> where TKey : IComparable<TKey>
    {
        // Classes
        protected class Node
        {
            public Node Parent;
            public Node Left;
            public Node Right;
            public bool MoveLeft;

            public TKey Key;
            public TValue Value;

            public Node(TKey Key, TValue Value, Node Parent)
            {
                Left = null;
                Right = null;
                MoveLeft = true;

                this.Parent = Parent;
                this.Key = Key;
                this.Value = Value;
            }

            public void SwapKeyValuePair(Node other)
            {
                TKey otherKey = other.Key;
                TValue otherValue = other.Value;
                other.Key = Key;
                other.Value = Value;
                Key = otherKey;
                Value = otherValue;
            }
        }

        // Fields
        protected Node Root;

        // Constructors
        public PriorityQueue()
        {
            Root = null;
        }

        // Methods
        public bool IsEmpty()
        {
            return Root == null;
        }
        public void Enqueue(TKey key, TValue value)
        {
            if (Root == null)
            {
                Root = new Node(key, value, null);
                return;
            }

            Node node = Root;

            while (true)
            {
                node.MoveLeft = !node.MoveLeft;
                if (!node.MoveLeft)
                {
                    if (node.Left == null)
                    {
                        node.Left = new Node(key, value, node);
                        node = node.Left;
                        break;
                    }
                    node = node.Left;
                }
                else
                {
                    if (node.Right == null)
                    {
                        node.Right = new Node(key, value, node);
                        node = node.Right;
                        break;
                    }
                    node = node.Right;
                }
            }

            while (node.Parent != null && node.Parent.Key.CompareTo(node.Key) > 0)
            {
                node.SwapKeyValuePair(node.Parent);
                node = node.Parent;
            }

        }
        public TValue Dequeue()
        {
            TValue returner = Root.Value;
            if (Root.Left == null)
            {
                Root = null;
                return returner;
            }
            Node node = Root;
            while (true)
            {
                node.MoveLeft = !node.MoveLeft;
                if (node.MoveLeft)
                {
                    if (node.Left == null)
                    {
                        node.SwapKeyValuePair(Root);
                        if (node.Parent.MoveLeft)
                        {
                            node.Parent.Left = null;
                        }
                        else
                        {
                            node.Parent.Right = null;
                        }
                        node = Root;
                        break;
                    }
                    node = node.Left;
                }
                else
                {
                    if (node.Right == null)
                    {
                        node.SwapKeyValuePair(Root);
                        if (node.Parent.MoveLeft)
                        {
                            node.Parent.Left = null;
                        }
                        else
                        {
                            node.Parent.Right = null;
                        }
                        node = Root;
                        break;
                    }
                    node = node.Right;
                }
            }

            while (node.Left != null && (node.Key.CompareTo(node.Left.Key) > 0 || node.Right != null && node.Key.CompareTo(node.Right.Key) > 0))
            {
                if (node.Right == null || node.Left.Key.CompareTo(node.Right.Key) < 0)
                {
                    node.SwapKeyValuePair(node.Left);
                    node = node.Left;
                }
                else
                {
                    node.SwapKeyValuePair(node.Right);
                    node = node.Right;
                }
            }

            return returner;
        }
        public TValue Peek()
        {
            return Root.Value;
        }
        public TKey PeekKey()
        {
            return Root.Key;
        }
    }
    internal class Program
    {
        public class Subimage
        {
            public Bitmap Bitmap { get; set; }
            public Color AverageColor { get; set; }
            public void SetAverageColor()
            {
                int r = 0;
                int g = 0;
                int b = 0;
                for (int x = 0; x < Bitmap.Width; x++)
                {
                    for (int y = 0; y < Bitmap.Height; y++)
                    {
                        Color color = Bitmap.GetPixel(x, y);
                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                }
                r /= Bitmap.Width * Bitmap.Height;
                g /= Bitmap.Width * Bitmap.Height;
                b /= Bitmap.Width * Bitmap.Height;
                AverageColor = ColorTranslator.FromHtml($"#{BitConverter.ToString(new byte[] { (byte)r, (byte)g, (byte)b }).Replace("-", "")}");
            }
            public int GetMatchScore(Color color)
            {
                int d(byte a, byte b) => Math.Abs(a - b);

                return d(color.R, AverageColor.R) + d(color.G, AverageColor.G) + d(color.B, AverageColor.B);
            } // lower better
        }
        static void Redraw(string mode)
        {
            Console.WriteLine("Reading design...");
            Color[,] designColorArray;
            Dictionary<Color, int> colorFrequency = new Dictionary<Color, int>();
            using (Bitmap design = new Bitmap("design\\image.png"))
            {
                designColorArray = new Color[design.Width, design.Height];
                for (int x = 0; x < design.Width; x++)
                {
                    for (int y = 0; y < design.Height; y++)
                    {
                        designColorArray[x,y] = design.GetPixel(x, y);
                        if (colorFrequency.ContainsKey(designColorArray[x, y]))
                        {
                            colorFrequency[designColorArray[x, y]]++;
                        }
                        else
                        {
                            colorFrequency.Add(designColorArray[x, y],1);
                        }
                    }
                }
            } // get design
            Subimage[,] subimages = new Subimage[designColorArray.GetLength(0), designColorArray.GetLength(1)];
            Rectangle subImageRect = new Rectangle(0,0,8,8);
            Console.WriteLine("Reading input...");
            using (Bitmap input = new Bitmap("input\\image.png"))
            {
                for (int x = 0; x < designColorArray.GetLength(0); x++)
                {
                    for (int y = 0; y < designColorArray.GetLength(1); y++)
                    {
                        Subimage subimage = new Subimage();
                        subimage.Bitmap = new Bitmap(subimages.GetLength(0), subimages.GetLength(1));
                        using (var fastBitmap = subimage.Bitmap.FastLock())
                        {
                            fastBitmap.CopyRegion(input, new Rectangle(x*8,y*8,8,8), subImageRect);
                        }
                        subimage.SetAverageColor();
                        subimages[x, y] = subimage;
                    }
                }
            }

            Console.WriteLine("Formulating LP...");
            List<string> lpfile = new List<string>() { "/*This is an LP formulation for image pixel assignment. I thought a bit of linear programming would be fun. Writing this before implementing, will probably regret this decision later :D*/"};

            string objectiveFunction = "min:";
            List<string> subjectTo = new List<string>();
            List<string> intStatements = new List<string>();
            List<string> variables = new List<string>();
            for (int i = 0; i < colorFrequency.Keys.Count; i++)
            {
                string longSubjectTo = "";
                for (int x = 0; x < subimages.GetLength(0); x++)
                {
                    for (int y = 0; y < subimages.GetLength(1); y++)
                    {
                        objectiveFunction += $"{subimages[x, y].GetMatchScore(colorFrequency.Keys.ToArray()[i])}x{i}x{x}x{y}+";
                        subjectTo.Add($"x{i}x{x}x{y}<1;");
                        intStatements.Add($"int x{i}x{x}x{y};");
                        longSubjectTo += $"x{i}x{x}x{y}+";
                        variables.Add($"x{i}x{x}x{y}");
                    }
                }
                longSubjectTo += $"0={colorFrequency[colorFrequency.Keys.ToArray()[i]]};";
                subjectTo.Add(longSubjectTo);
            }
            for (int x = 0; x < subimages.GetLength(0); x++)
            {
                for (int y = 0;y < subimages.GetLength(1); y++)
                {
                    string onlyOnce = "";
                    for (int i = 0; i < colorFrequency.Keys.Count; i++)
                    {
                        onlyOnce += $"x{i}x{x}x{y}+";
                    }
                    onlyOnce += "0=1;";
                    subjectTo.Add(onlyOnce);
                }
            }
            objectiveFunction += "0;";
            lpfile.Add("/*OBJECTIVE FUNCTION*/");
            lpfile.Add(objectiveFunction);

            lpfile.Add("/*CONSTRAINTS*/");
            foreach (string s in subjectTo)
            {
                lpfile.Add(s);
            }

            lpfile.Add("/*INT STATEMENTS*/");
            foreach (string s in intStatements)
            {
                lpfile.Add(s);
            }

            Console.WriteLine($"{variables.Count} variables created! ({subimages.GetLength(0)}*{subimages.GetLength(1)}*{colorFrequency.Keys.Count} colours)");
            Console.WriteLine("- High numbers mean that there are lots of colors. If the next steps are taking too long, try cutting down on the colours in your design image");
            Console.WriteLine("Moving LP to temp...");
            string path = "temp\\lp.txt";
            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (string s in lpfile)
                {
                    sw.WriteLine(s);
                }
                foreach (string s in intStatements)
                {
                    sw.WriteLine(s);
                }
            }
            Console.WriteLine("Solving LP...");
            double[] results = new double[colorFrequency.Keys.Count* subimages.GetLength(0)* subimages.GetLength(1)];
            using (LpSolve lp = LpSolve.read_LP(path, 0, null))
            {
                /*var result =*/
                lp.solve();
                //lp.print_solution(1);
                lp.get_variables(results);
            }

            Console.WriteLine("Comprehending LP solution...");

            Dictionary<Color, List<Subimage>> mapping = new Dictionary<Color, List<Subimage>>();
            for (int i = 0; i < variables.Count; i++)
            {
                string[] broken = variables[i].Split('x');
                if (results[i] == 1)
                {
                    if (mapping.ContainsKey(colorFrequency.Keys.ToArray()[int.Parse(broken[1])]))
                    {
                        mapping[colorFrequency.Keys.ToArray()[int.Parse(broken[1])]].Add(subimages[int.Parse(broken[2]), int.Parse(broken[3])]);
                    }
                    else
                    {
                        mapping.Add(colorFrequency.Keys.ToArray()[int.Parse(broken[1])], new List<Subimage>() { subimages[int.Parse(broken[2]), int.Parse(broken[3])] });
                    }
                }
            }

            Random rng = new Random(1);
            List< Subimage > used = new List< Subimage >();

            Console.WriteLine("Writing output...");

            if (mode != "1")
            {
                Console.WriteLine("Moving subimages to binary heap (gradient)...");
                if (mode == "3")
                {
                    Dictionary<Color, PriorityQueue<int, Subimage>> mapping2 = new Dictionary<Color, PriorityQueue<int, Subimage>>();
                    foreach (var c in mapping.Keys)
                    {
                        mapping2.Add(c, new PriorityQueue<int, Subimage>());
                        while (mapping[c].Count != 0)
                        {
                            mapping2[c].Enqueue(mapping[c][0].AverageColor.R + mapping[c][0].AverageColor.B + mapping[c][0].AverageColor.G, mapping[c][0]);
                            mapping[c].RemoveAt(0);
                        }
                    }
                    using (Bitmap newImage = new Bitmap("input\\image.png"))
                    {
                        using (var fastBitmap = newImage.FastLock())
                        {
                            for (int y = 0; y < designColorArray.GetLength(1); y++)
                            {
                                for (int x = 0; x < designColorArray.GetLength(0); x++)
                                {
                                    Subimage subimage = mapping2[designColorArray[x, y]].Dequeue();
                                    if (used.Contains(subimage))
                                    {
                                        for (int x2 = 0; x2 < subimage.Bitmap.Width; x2++)
                                        {
                                            for (int y2 = 0; y2 < subimage.Bitmap.Width; y2++)
                                            {
                                                subimage.Bitmap.SetPixel(x2, y2, Color.Yellow);
                                            }
                                        }
                                    }
                                    used.Add(subimage);
                                    mapping[designColorArray[x, y]].Remove(subimage);
                                    fastBitmap.CopyRegion(subimage.Bitmap, subImageRect, new Rectangle(x * 8, y * 8, 8, 8));
                                    subimage.Bitmap.Dispose();
                                }
                            }
                        }
                        Console.WriteLine("Saving...");
                        newImage.Save("output\\image.png", ImageFormat.Png);
                    }
                }
                else if (mode == "2")
                {
                    Dictionary<Color, PriorityQueue<int, Subimage>> mapping2 = new Dictionary<Color, PriorityQueue<int, Subimage>>();
                    foreach (var c in mapping.Keys)
                    {
                        mapping2.Add(c, new PriorityQueue<int, Subimage>());
                        while (mapping[c].Count != 0)
                        {
                            mapping2[c].Enqueue(mapping[c][0].AverageColor.R + mapping[c][0].AverageColor.B + mapping[c][0].AverageColor.G, mapping[c][0]);
                            mapping[c].RemoveAt(0);
                        }
                    }
                    using (Bitmap newImage = new Bitmap("input\\image.png"))
                    {
                        using (var fastBitmap = newImage.FastLock())
                        {
                            for (int x = 0; x < designColorArray.GetLength(0); x++)
                            {
                                for (int y = 0; y < designColorArray.GetLength(1); y++)
                                {
                                    Subimage subimage = mapping2[designColorArray[x, y]].Dequeue();
                                    if (used.Contains(subimage))
                                    {
                                        for (int x2 = 0; x2 < subimage.Bitmap.Width; x2++)
                                        {
                                            for (int y2 = 0; y2 < subimage.Bitmap.Width; y2++)
                                            {
                                                subimage.Bitmap.SetPixel(x2, y2, Color.Yellow);
                                            }
                                        }
                                    }
                                    used.Add(subimage);
                                    mapping[designColorArray[x, y]].Remove(subimage);
                                    fastBitmap.CopyRegion(subimage.Bitmap, subImageRect, new Rectangle(x * 8, y * 8, 8, 8));
                                    subimage.Bitmap.Dispose();
                                }
                            }
                        }
                        Console.WriteLine("Saving...");
                        newImage.Save("output\\image.png", ImageFormat.Png);
                    }
                }
            }
            else
            {
                using (Bitmap newImage = new Bitmap("input\\image.png"))
                {
                    using (var fastBitmap = newImage.FastLock())
                    {
                        for (int x = 0; x < designColorArray.GetLength(0); x++)
                        {
                            for (int y = 0; y < designColorArray.GetLength(1); y++)
                            {
                                Subimage subimage = mapping[designColorArray[x, y]][rng.Next(0, mapping[designColorArray[x, y]].Count)];
                                if (used.Contains(subimage))
                                {
                                    for (int x2 = 0; x2 < subimage.Bitmap.Width; x2++)
                                    {
                                        for (int y2 = 0; y2 < subimage.Bitmap.Width; y2++)
                                        {
                                            subimage.Bitmap.SetPixel(x2, y2, Color.Yellow);
                                        }
                                    }
                                }
                                used.Add(subimage);
                                mapping[designColorArray[x, y]].Remove(subimage);
                                fastBitmap.CopyRegion(subimage.Bitmap,subImageRect,new Rectangle(x*8,y*8,8,8));
                                subimage.Bitmap.Dispose();
                            }
                        }
                    }
                    Console.WriteLine("Saving...");
                    newImage.Save("output\\image.png", ImageFormat.Png);
                }
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Done! Press any key to continue.");
            Console.ReadKey();
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        static void Initialise()
        {
            if (!LpSolveDotNet.LpSolve.Init()) throw new System.Exception("Failed to initialise Lpsolve. Let me know if this error pops up for you, I'll try and sort dependencies out better.");
        }
        static void Menu()
        {
            Console.Clear();
            Console.WriteLine(
                "Welcome to the Aggregate Redrawer!\r\n" +
                "----------------------------------\r\n" +
                "|   1.     |   2.     |   3.     |\r\n" +
                "|  Input   |  Input   | Run and  |\r\n" +
                "|aggregate--> design --> collect |\r\n" +
                "|   map    |  image   |  output  |\r\n" +
                "| (input)  | (design) | (output) |\r\n" +
                "----------------------------------\r\n" +
                "\r\n" +
                "Once you have completed Steps 1+2,\r\n" +
                "press ENTER to continue to options.\r\n" +
                "\r\n" +
                "> Press enter to continue\n\n\nCreated by TheBlunatic\nMessage me if anything isn't working\nThis shouldn't break nearly as much\nas the Map Rewriter"
            );
            while (Console.ReadKey(true).Key != ConsoleKey.Enter);
            Console.Clear();
            Console.WriteLine(
                "What distribution of brightness would you like?\r\n" +
                "\r\n" +
                "1. Random distribution\r\n" +
                "2. Darkest on the left\r\n" +
                "3. Darkest on the top\r\n" +
                "\r\n" +
                "(Option 3 is recommended)\r\n" +
                "\r\n" +
                "Just enter the option number you wish to choose"
            );
            string[] answers = { "1", "2", "3" };
            string answer = $"{int.MinValue}";
            do { answer = Console.ReadLine(); } while (!answers.Contains(answer));
            Console.Clear();
            Redraw(answer);
        }
        static void Main(string[] args)
        {
            Initialise();
            while (true) Menu();
        }
    }
}
