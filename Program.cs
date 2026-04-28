
//dotnet add package SixLabors.ImageSharp
namespace aaa;
using System.Numerics;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Text.Json;

#nullable disable
class Program
{
    static string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ567abcdefghijklmnopqrstuvwxyz890. 1234";
    //static int ExportWidth = 250;
    //static int ExportHeight = 250;
    //static string outputPath = "test1.png";
    //static Image<Rgba32> image = new Image<Rgba32>(ExportWidth, ExportHeight);
    //public static Camera cam = new Camera(new Vector3(0,0,40),MathF.PI,0,0);
    //static Particle[] particles;
    static void Main(string[] args)
    {
        //Console.WriteLine(args[2]);
        if(args.Length == 2 && args[1] == "g")
        {
            Console.WriteLine(allowedChars.Length);

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: app <input.txt>");
                return;
            }

            string inputPath = args[0];

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            string text = File.ReadAllText(inputPath);

            text = turnto64(text);

            //Console.WriteLine(text);

            
            char[] uniqueChars = text.Distinct().ToArray();
            Console.WriteLine(uniqueChars.Length);

            var invalidChars = uniqueChars
                .Where(c => !allowedChars.Contains(c));

            foreach (var c in invalidChars)
            {
                Console.WriteLine("char not allowed:");
                Console.WriteLine(c);
                return;
            }

            List<string> combos = new List<string>();
            foreach (char a in uniqueChars)
            {
                foreach (char b in uniqueChars)
                {
                    combos.Add($"{a}{b}");
                }
            }

            Dictionary<char, int> charIndex = new Dictionary<char, int>();
            for (int i = 0; i < uniqueChars.Length; i++)
            {
                charIndex[uniqueChars[i]] = i;
            }

            int[,] allfollowing = new int[combos.Count,combos.Count];
            

            /*for (int i = 0; i < allfollowing.GetLength(0); i++)
            {
                List<int> positions = new List<int>();
                int index = 0;
                while ((index = text.IndexOf(combos[i], index)) != -1)
                {
                    positions.Add(index);
                    index++;
                }

                for (int j = 0; j < positions.Count; j++)
                {
                    //Console.WriteLine(positions[j]);
                    //Console.WriteLine(text[positions[j]+2]);
                    //Console.WriteLine(text[positions[j]+3]);
                    //string testStr = ""+text[positions[j]+2]+text[positions[j]+3];
                    if (positions[j]+3 >= text.Length)
                    {
                        
                    } else
                    {
                        int pairIndex = charIndex[text[positions[j]+2]] * uniqueChars.Length + charIndex[text[positions[j]+3]];
                        allfollowing[i,pairIndex]+=1;
                    }
                }
            }*/
            for (int i = 0; i < combos.Count; i++)
            {
                char a = combos[i][0];
                char b = combos[i][1];

                for (int pos = 0; pos < text.Length - 3; pos++)
                {
                    if (text[pos] == a && text[pos+1] == b)
                    {
                        // found combo[i] at pos, now get the following pair
                        int pairIndex = charIndex[text[pos+2]] * uniqueChars.Length + charIndex[text[pos+3]];
                        allfollowing[i, pairIndex]++;
                    }
                }
            }

            Dictionary<string, int[]> sortedFollowing = new Dictionary<string, int[]>();

            for (int i = 0; i < combos.Count; i++)
            {
                // build a list of (comboIndex, count) for this row
                List<(int comboIndex, int count)> row = new List<(int, int)>();
                for (int j = 0; j < combos.Count; j++)
                {
                    row.Add((j, allfollowing[i, j]));
                }

                // sort by count descending
                row.Sort((a, b) => b.count.CompareTo(a.count));

                // extract just the indices in sorted order
                int[] sortedIndices = new int[row.Count];
                for (int j = 0; j < row.Count; j++)
                {
                    sortedIndices[j] = row[j].comboIndex;
                }

                sortedFollowing[combos[i]] = sortedIndices;
            }
            
            var result = new
            {
                FileName = Path.GetFileName(inputPath),
                combos = combos,
                sortedFollowing = sortedFollowing
            };

            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string outputPath = Path.ChangeExtension(inputPath, ".json");
            File.WriteAllText(outputPath, json);

            Console.WriteLine($"Output written to {outputPath}");

        } else if (args.Length == 3 && args[1] == "e")
        {
            string json = File.ReadAllText(args[0]);

            JsonDocument doc = JsonDocument.Parse(json);

            List<string> combos = doc.RootElement
                .GetProperty("combos")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();

            Dictionary<string, int[]> sortedFollowing = doc.RootElement
                .GetProperty("sortedFollowing")
                .EnumerateObject()
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.Value.EnumerateArray().Select(e => e.GetInt32()).ToArray()
                );

            Dictionary<string, int> comboIndex = new Dictionary<string, int>();
            for (int i = 0; i < combos.Count; i++)
            {
                comboIndex[combos[i]] = i;
            }
            string inputPath = args[2];

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            string encodeTxt = File.ReadAllText(inputPath);
            encodeTxt = turnto64(encodeTxt);
            if (encodeTxt.Length % 2 == 1)
            {
                encodeTxt+="1";
            }

            int[] outarr = new int[(encodeTxt.Length/2)];

            string beginnerStr = "" + encodeTxt[0] + encodeTxt[1];


            outarr[0] = comboIndex[beginnerStr];
            int outarrIndex = 1;

            string prev = beginnerStr;

            for (int i = 2; i < encodeTxt.Length; i+=2)
            {
                string current = ""+encodeTxt[i]+encodeTxt[i+1];
                int comboI = comboIndex[current];
                int index = Array.IndexOf(sortedFollowing[prev], comboI);
                outarr[outarrIndex] = index;
                prev = current;
                outarrIndex ++;
            }

            string outputPath = Path.ChangeExtension(inputPath, "_encoded.txt");
            File.WriteAllText(outputPath, string.Join("\n", outarr));
            Console.WriteLine($"Output written to {outputPath}");

        }
        else if (args.Length == 3 && args[1] == "d")
        {
            string json = File.ReadAllText(args[0]);

            JsonDocument doc = JsonDocument.Parse(json);

            List<string> combos = doc.RootElement
                .GetProperty("combos")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();

            Dictionary<string, int[]> sortedFollowing = doc.RootElement
                .GetProperty("sortedFollowing")
                .EnumerateObject()
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.Value.EnumerateArray().Select(e => e.GetInt32()).ToArray()
                );

            Dictionary<string, int> comboIndex = new Dictionary<string, int>();
            for (int i = 0; i < combos.Count; i++)
            {
                comboIndex[combos[i]] = i;
            }
            string inputPath = args[2];

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            int[] arr = File.ReadAllLines(inputPath).Select(int.Parse).ToArray();


            string outstring = "";
            outstring += combos[arr[0]];
            int prevComboIndex = arr[0];

            for (int i = 1; i < arr.Length; i++)
            {
                int nextComboIndex = sortedFollowing[combos[prevComboIndex]][arr[i]];
                outstring += combos[nextComboIndex];
                prevComboIndex = nextComboIndex;
            }

            if (outstring[outstring.Length-1] == 1)
            {
                outstring = outstring[..^1];
            }

            outstring = turnBack(outstring);

            string outputPath = Path.ChangeExtension(inputPath, "_decoded.txt");
            File.WriteAllText(outputPath, outstring);
            Console.WriteLine($"Output written to {outputPath}");
        }






        //Random rng = new Random();
        //float[,,] perlin3d = Helper.GeneratePerlinNoiseHeightMap(100,100,100,5,5,5,rng);
        //particles = new Particle[100*100*100];

        /*for (int x = 0; x < 100; x++)
        {
            for (int y = 0; y < 100; y++)
            {
                for (int z = 0; z < 100; z++)
                {
                    particles[(x*100*100)+(y*100)+z] = new Particle(new Vector3(((x+(float)rng.NextDouble())/5f)-5,((y+(float)rng.NextDouble())/5f)-5f,((z+(float)rng.NextDouble())/5f)-5f),perlin3d[x,y,z]);
                }
            }
        }*/
        /*for (int i = 0; i < particles.Length; i++)
        {
            particles[i] = new Particle(new Vector3((float)(rng.NextDouble()*10f)-5,(float)(rng.NextDouble()*10f)-5,(float)(rng.NextDouble()*10f)-5));
        }*/

        /*for (int y = 0; y < ExportHeight; y++)
        {
            for (int x = 0; x < ExportWidth; x++)
            {
                image[x, y] = new Rgba32(135,206,255,255);
            }
        }

        float focalLength = 300f;*/

        /*for (int i = 0; i < particles.Length; i++)
        {
            // Perspective (what to add)
            float z = (float)particles[i].curPos.Z + cameraDistance; // push world in front of camera
            float screenX = ExportWidth/2 + ((float)particles[i].curPos.X / z) * focalLength;
            float screenY = ExportHeight/2 + ((float)particles[i].curPos.Y / z) * focalLength;
            if (screenX >= 0 && screenX < ExportWidth && screenY >= 0 && screenY < ExportHeight)
            {
                image[(int)screenX, (int)screenY] = new Rgba32(
                    r: (byte)(255),
                    g: (byte)(255),
                    b: 128,
                    a: 255
                );
            }
        }*/

        /*for (int i = 0; i < particles.Length; i++)
        {
            // 1. Move into camera space
            Vector3 world = particles[i].curPos;
            Vector3 relative = world - cam.Position;

            // 2. Rotate by inverse camera rotation
            Vector3 camSpace = CamRotate(relative, -cam.Yaw, -cam.Pitch);

            float z = camSpace.Z;
            if (z <= 0) continue; // behind camera

            // 3. Perspective projection
            float screenX = ExportWidth / 2 + (camSpace.X / z) * focalLength;
            float screenY = ExportHeight / 2 + (camSpace.Y / z) * focalLength;

            if (screenX >= 0 && screenX < ExportWidth &&
                screenY >= 0 && screenY < ExportHeight)
            {
                //byte lightest = (byte)Math.Max(image[(int)screenX, (int)screenY].R,Math.Max(image[(int)screenX, (int)screenY].G,image[(int)screenX, (int)screenY].B));
                //image[(int)screenX, (int)screenY] = new Rgba32((byte)(lightest-(particles[i].density*10)), (byte)(lightest-(particles[i].density*10)), (byte)(lightest-(particles[i].density*10)), 255);
                image[(int)screenX, (int)screenY] = new Rgba32((byte)(particles[i].density*255),(byte)(particles[i].density*255),(byte)(particles[i].density*255), 255);
                //image[(int)screenX, (int)screenY] = new Rgba32((byte)(image[(int)screenX, (int)screenY].R),(byte)(image[(int)screenX, (int)screenY].G),(byte)(image[(int)screenX, (int)screenY].R), 255);
            }
        }*/


        /*for (int y = 0; y < ExportHeight; y++)
        {
            for (int x = 0; x < ExportWidth; x+=2)
            {
                image[x, y] = new Rgba32(
                    r: (byte)(x * 255 / ExportWidth),   // red gradient left→right
                    g: (byte)(y * 255 / ExportHeight),  // green gradient top→bottom
                    b: 128,
                    a: 255                         // fully opaque
                );
            }
        }*/

        //image.SaveAsPng(outputPath);
        //Console.WriteLine($"Saved to {outputPath}");

    }

    public static string turnto64(string str)
    {

        //note 11 er ikke gyldig på grunn av end-of-file spacing.
        string text = str;
        text = text
            .Replace("0", "1o")
            .Replace("1", "1e")
            .Replace("2", "1t")
            .Replace("3", "1r")
            .Replace("4", "1f")
            .Replace("5", "1m")
            .Replace("6", "1s")
            .Replace("7", "1y")
            .Replace("8", "1å")
            .Replace("9", "1n");

            //1o 1e 1t 1r 1f 1m 1s 1y 1å 1n

        text = text
            .Replace("\r\n", "1N")  // Windows newlines first (before \n)
            .Replace("\n", "1N")    // Unix newlines
            .Replace("\t", "1T")    // example: tabs
            .Replace("!", "1u")    // example: tabs
            .Replace("?", "1q")    // example: tabs
            .Replace(",", "1k")
            .Replace(":", "1K")
            .Replace(";", "1S")
            .Replace("-", "1b")
            .Replace("“", "1l")
            .Replace("”", "1R")
            .Replace("’", "1a")
            .Replace(")", "2P")
            .Replace("(", "2p")
            .Replace("{", "2c")
            .Replace("}", "2C")
            .Replace("[", "2q")
            .Replace("]", "2Q")

            .Replace("ß", "3S");

        text = text
            .Replace("Æ", "5")
            .Replace("Ø", "6")
            .Replace("Å", "7")
            .Replace("æ", "8")
            .Replace("ø", "9")
            .Replace("å", "0");
        return text;
    }
    public static string turnBack(string str)
    {
        string text = str;

        text = text
            .Replace("5", "Æ")
            .Replace("6", "Ø")
            .Replace("7", "Å")
            .Replace("8", "æ")
            .Replace("9", "ø")
            .Replace("0", "å");

        text = text
            .Replace("1N", "\r\n")  // Windows newlines first (before \n)
            .Replace("1N", "\n")    // Unix newlines
            .Replace("1T", "\t")    // example: tabs
            .Replace("1u", "!")    // example: tabs
            .Replace("1q", "?")    // example: tabs
            .Replace("1k", ",")
            .Replace("1K", ":")
            .Replace("1S", ";")
            .Replace("1b", "-")
            .Replace("1l", "“")
            .Replace("1R", "”")
            .Replace("1a", "’")
            .Replace("2P", ")")
            .Replace("2p", "(")
            .Replace("2c", "{")
            .Replace("2C", "}")
            .Replace("2q", "[")
            .Replace("2Q", "]")

            .Replace("3S", "ß");

        text = text
            .Replace("1o", "0")
            .Replace("1e", "1")
            .Replace("1t", "2")
            .Replace("1r", "3")
            .Replace("1f", "4")
            .Replace("1m", "5")
            .Replace("1s", "6")
            .Replace("1y", "7")
            .Replace("1å", "8")
            .Replace("1n", "9");
        return text;
    }
}