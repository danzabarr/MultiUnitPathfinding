using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "New Element Patterns", menuName = "Element Patterns")]
public class ScriptElementPatterns : ScriptableObject
{
    [TextArea(3, 10)]
    public string source;
    private Regex[] patterns;

    public string[] preview;

    public static Regex CreatePattern(string source)
    {
        try
        {
            // ignore comments (any text after a // symbol)
            if (source.Contains("//"))
                source = source.Substring(0, source.IndexOf("//"));

            // trim whitespace
            source = source.Trim();

            if (!string.IsNullOrEmpty(source))
            {
                string pattern = "^" + Regex.Escape(source).Replace("<", "(?<").Replace(">", ">.*?)") + "$";
                return new Regex(pattern);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        Debug.LogError($"Failed to create pattern for source: {source}");
        return null;
    }

    public void Recompile()
    {
        string[] lines = source.Split('\n');

        List<Regex> regexes = new List<Regex>();

        foreach (string line in lines)
        {
            try
            {
                Regex regex = CreatePattern(line);
                if (regex != null)
                    regexes.Add(regex);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        patterns = regexes.ToArray();

        preview = new string[patterns.Length];
        for (int i = 0; i < patterns.Length; i++)
            preview[i] = patterns[i].ToString();
    }

    public Regex[] Patterns
    {   
        get 
        {
            if (patterns == null)
                Recompile();
            return patterns;
        }
    }

    public void OnValidate()
    {
        Recompile();
        foreach (Regex pattern in patterns)
            Debug.Log(pattern);
    }
}
