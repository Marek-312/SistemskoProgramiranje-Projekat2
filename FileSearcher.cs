using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using PalindromeServer;

namespace PalindromeServer
{
    public class SearchResult
    {
        public int PalindromeCount { get; set; }
        public List<string>? Palindromes { get; set; }
        public string FileName { get; set; }
    }
    public class FileSearcher
    {
        private readonly string _rootFolder;
        //private readonly Logger Logger;



        public FileSearcher(string rootFolder)
        {
            if (!Directory.Exists(rootFolder))
                throw new DirectoryNotFoundException($"Root folder ne postoji: {rootFolder}");

            _rootFolder = rootFolder;

        }

        // Pronalazi fajl rekurzivno kroz sve podfoldere
        private string FindFile(string fileName)
        {
            Logger.Log($"Pretraga fajla: {fileName}", Logger.Metode.Info, "SEARCHER");

            string[] found = Directory.GetFiles(_rootFolder, fileName, SearchOption.AllDirectories);

            if (found.Length == 0)
                return null;

            // Ako postoji vise fajlova sa istim imenom, uzima prvi
            return found[0];
        }

        // Proverava da li je rec palindrom
        private bool IsPalindrome(string word)
        {
            if (word.Length < 2)
                return false;

            string cleaned = word.ToLower();
            string reversed = new string(cleaned.Reverse().ToArray());

            return cleaned == reversed;
        }

        // Parsira tekst i vraca listu palindroma
        private List<string> FindPalindromes(string text)
        {
            char[] separators = { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '-', '"', '\'', '(', ')' };
            string[] words = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            List<string> palindromes = new List<string>();

            foreach (string word in words)
            {
                if (IsPalindrome(word) && !palindromes.Contains(word.ToLower()))
                {
                    palindromes.Add(word.ToLower());
                }
            }

            return palindromes;
        }

        // Glavna async metoda - trazi fajl i broji palindrome
        public async Task<SearchResult> SearchAsync(string fileName)
        {
            // Pretraga fajla je sinhronа (CPU operacija, ne I/O)
            string filePath = FindFile(fileName);

            if (filePath == null)
            {
                Logger.Log($"Fajl nije pronadjen: {fileName}", Logger.Metode.Warning, "SEARCHER");
                throw new FileNotFoundException($"Fajl '{fileName}' nije pronadjen ni u jednom podfolderu.");
            }

            Logger.Log($"Fajl pronadjen na putanji: {filePath}", Logger.Metode.Info, "SEARCHER");

            // Citanje fajla je async - oslobadja nit dok ceka na disk
            string content = await File.ReadAllTextAsync(filePath);

            Logger.Log($"Fajl uspesno procitan, velicina: {content.Length} karaktera", Logger.Metode.Info, "SEARCHER");

            // Trazenje palindroma je CPU operacija - radi se sinhrono
            List<string> palindromes = FindPalindromes(content);

            Logger.Log($"Pronadjeno {palindromes.Count} palindroma u fajlu: {fileName}", Logger.Metode.Info, "SEARCHER");

            return new SearchResult
            {
                FileName = fileName,
                PalindromeCount = palindromes.Count,
                Palindromes = palindromes
            };
        }
    }
}