using System;
using UnityEngine;
using System.Text.RegularExpressions;

public class ScriptElement
{
    private readonly string source;
    private readonly string command;
    private readonly Regex pattern;
    private readonly Match match;

    /// <summary>
    /// Factory pattern for valid script elements.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pattern"></param>
    /// <param name="match"></param>
    private ScriptElement(string source, Regex pattern, Match match)
    {
        this.source = source;
        this.command = source.Split(' ')[0];
        this.pattern = pattern;
        this.match = match;
    }

    public string[] Variables => pattern.GetGroupNames();

    public string[] Values 
    {
        get
        {
            string[] values = new string[match.Groups.Count];
            for (int i = 0; i < match.Groups.Count; i++)
                values[i] = match.Groups[i].Value;
            return values;
        }
    }

    public string Command => command;

    public override string ToString()
    {
        string result = Command + "\n";
        for (int i = 1; i < Variables.Length; i++)
            result += $"{Variables[i]}: {Values[i]}\n";
        return result;
    }

    public static ScriptElement Parse(string source, ScriptElementPatterns patterns)
    {
        if (patterns == null)
            return null;

        if (string.IsNullOrEmpty(source))
            return null;

        foreach (Regex pattern in patterns.Patterns)
        {
            Match match = pattern.Match(source);
            if (match.Success)
                return new ScriptElement(source, pattern, match);
        }
        return null;
    }
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue")]
public class Script : ScriptableObject
{
    [TextArea(3, 10)]
    public string source;
    public ScriptElementPatterns patterns;

    private ScriptElement[] elements;
    public ScriptElement[] Elements => elements ??= Recompile();

    /// <summary>
    /// Tokenise the source code into script elements.
    /// </summary>
    /// <returns></returns>
    private ScriptElement[] Recompile()
    {
        string[] lines = source.Split('\n');
        ScriptElement[] elements = new ScriptElement[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            if (ScriptElement.Parse(lines[i], patterns) is ScriptElement element)
                elements[i] = element;
        }

        return elements;
    }
}

public class Dialogue : MonoBehaviour
{
    public ScriptElementPatterns patterns;

    [TextArea(3, 10)]
    public string input;

    public void OnValidate()
    {
        if (ScriptElement.Parse(input, patterns) is ScriptElement element)
        {
            Debug.Log("Matched command!");
            Debug.Log($"Command: {element}");
        }
        else
        {
            Debug.Log("Command did not match.");
        }
    }    

    public static bool ResolveObjectName(string name, out GameObject go)
    {
        return (go = GameObject.Find(name)) != null;
    }

    public static bool ResolveComponentRef<T>(string name, out T component) where T : Component
    {
        return (component = GameObject.Find(name).GetComponent<T>()) != null;
    }

    public static bool IsFlagged(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public static bool IsMapped(string key, string value)
    {
        return PlayerPrefs.GetString(key) == value;
    }
    
    public static bool ValidActor(string actor)
    {
        throw new NotImplementedException();
    }

    public static bool ValidObject(string obj)
    {
        throw new NotImplementedException();
    }

    public static bool ValidText(string text)
    {
        throw new NotImplementedException();
    }

    public static bool ValidKey(string key)
    {
        throw new NotImplementedException();
    }

    public static bool ValidValue(string value)
    {
        throw new NotImplementedException();
    }

    public static bool ValidTrigger(string trigger)
    {
        throw new NotImplementedException();
    }

    public static bool ValidFunction(string function)
    {
        throw new NotImplementedException();
    }

    public static bool ValidArguments(string arguments)
    {
        throw new NotImplementedException();
    }

    public static bool ValidProcess(string process)
    {
        throw new NotImplementedException();
    }

    public static bool ValidLabel(string label)
    {
        throw new NotImplementedException();
    }

    public static bool ValidFlag(string flag)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Performs validation on a variable and its value, without context of the command or the script.
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ValidateVariables(string variable, string value)
    {
        return variable switch
        {
            "actor"     => ValidActor(value),
            "object"    => ValidObject(value),
            "text"      => ValidText(value),
            "key"       => ValidKey(value),
            "value"     => ValidValue(value),
            "trigger"   => ValidTrigger(value),
            "function"  => ValidFunction(value),
            "arguments" => ValidArguments(value),
            "process"   => ValidProcess(value),
            "label"     => ValidLabel(value),
            "flag"      => ValidFlag(value),
            _           => false
        };
    }

    /// <summary>
    /// Resolve the variable within the context of the command and script, 
    /// and the current state of the dialogue and the game.
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="value"></param>
    public void ResolveVariable(string variable, string value)
    {

    }

    [Obsolete("Use ScriptElement.Parse instead.")]
    public static bool MatchCommand(string input, string template) 
    {
        string pattern = "^" + Regex.Escape(template).Replace("<", "(?<").Replace(">", ">.*?)") + "$";
        Regex regex = new Regex(pattern);

        Debug.Log($"Pattern: {pattern}");

        Match match = regex.Match(input);
        if (match.Success)
        {
            // Extract variables from capture groups
            GroupCollection groups = match.Groups;
            for (int i = 1; i < groups.Count; i++)
            {
                Debug.Log($"Variable {i}: {groups[i].Value}");
            }
            return true;
        }
        return false;
    }
}