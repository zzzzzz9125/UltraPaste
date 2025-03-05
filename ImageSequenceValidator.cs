using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class ImageSequenceValidator
{
    public static string GetValidFirstFileName(IEnumerable<string> filePaths)
    {
        List<FileMeta> sequenceData = new List<FileMeta>();

        foreach (string path in filePaths)
        {
            FileMeta meta = ParseFileMeta(path);
            if (meta == null)
            {
                return null;
            }
            sequenceData.Add(meta);
        }

        if (sequenceData.Count < 2 || !IsValidConsistency(sequenceData) || !IsValidNumberSequence(sequenceData))
        {
            return null;
        }

        return FindFirstFileName(sequenceData);
    }

    private static FileMeta ParseFileMeta(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        Match match = Regex.Match(fileName, @"^(.*?)(\d+)$");
        if (!match.Success)
        {
            return null;
        }

        return new FileMeta(Path.Combine(Path.GetDirectoryName(filePath), match.Groups[1].Value), match.Groups[2].Value, int.Parse(match.Groups[2].Value), extension: extension);
    }

    private static bool IsValidConsistency(List<FileMeta> sequence)
    {
        FileMeta first = sequence[0];

        foreach (FileMeta meta in sequence)
        {
            if (meta.Prefix != first.Prefix || meta.Extension != first.Extension)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsValidNumberSequence(List<FileMeta> sequence)
    {
        List<int> numbers = new List<int>();
        foreach (FileMeta meta in sequence)
        {
            numbers.Add(meta.Number);
        }
        numbers.Sort();

        for (int i = 1; i < numbers.Count; i++)
        {
            if (numbers[i] != numbers[i - 1] + 1)
            {
                return false;
            }
        }
        return true;
    }

    private static string FindFirstFileName(List<FileMeta> sequence)
    {
        FileMeta minMeta = sequence[0];
        foreach (FileMeta meta in sequence)
        {
            if (meta.Number < minMeta.Number)
            {
                minMeta = meta;
            }
        }
        return string.Format("{0}{1}{2}", minMeta.Prefix, minMeta.NumberStr, minMeta.Extension);
    }

    private class FileMeta
    {
        public string Prefix { get; set; }
        public string NumberStr { get; set; }
        public int Number { get; set; }
        public string Extension { get; set; }

        public FileMeta(string prefix, string numberStr, int number, string extension)
        {
            Prefix = prefix;
            NumberStr = numberStr;
            Number = number;
            Extension = extension;
        }
    }
}