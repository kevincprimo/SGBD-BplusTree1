using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        string inputPath = "in.txt";
        string outputPath = "out.txt";
        string csvPath = "vinhos.csv";

        // Validar entrada
        string[] lines = File.ReadAllLines(inputPath);
        if (lines.Length == 0 || !lines[0].StartsWith("FLH/"))
        {
            Console.WriteLine("Arquivo de entrada inválido.");
            return;
        }

        // Inicializar árvore
        int order = int.Parse(lines[0].Split('/')[1]);
        BufferManager bufferManager = new BufferManager();
        BPlusTree tree = new BPlusTree(order, bufferManager);

        // Criar índice de chave para posição no vinhos.csv
        Dictionary<int, long> keyToOffset = new Dictionary<int, long>();
        using (FileStream fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read))
        using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
        {
            string? line;
            long position = 0;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (int.TryParse(parts[0], out int id)) // ID é a chave
                {
                    if (!keyToOffset.ContainsKey(id))
                        keyToOffset[id] = position;
                }
                position = fs.Position;
            }
        }

        // Processar comandos
        List<string> outputLines = new List<string> { lines[0] };

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("INC:"))
            {
                int key = int.Parse(line.Split(':')[1]);

                if (keyToOffset.TryGetValue(key, out long offset))
                {
                    tree.Insert(key, (int)offset); // insere chave + ponteiro real
                    outputLines.Add($"INC:{key}/1");
                }
                else
                {
                    outputLines.Add($"INC:{key}/0"); // chave não existe no CSV
                }
            }
            else if (line.StartsWith("INC:") || line.StartsWith("BUS=:")){
            string[] partes = line.Split(':');
            int valor = int.Parse(partes[1]);
    
            }

        }

        // Altura final da árvore
        outputLines.Add($"H/{tree.GetHeight()}");

        File.WriteAllLines(outputPath, outputLines);
        Console.WriteLine($"Execução concluída. Resultados gravados em {outputPath}");
    }
}