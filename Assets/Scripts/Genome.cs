using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Describes how genes are expressed from two parent alleles.
/// </summary>
public enum InheritanceMode
{
    Random,         // If two equally dominant alleles (dominance > 0) are present, one is chosen at random.
    Paternal,       // If two equally dominant alleles are present, the paternal allele is chosen.
    Maternal,       // If two equally dominant alleles are present, the maternal allele is chosen.
    Codominance,    // If two equally dominant alleles are present, both are expressed.
    Incomplete      // If two equally dominant alleles are present, a blend of the two is expressed.
}

[System.Serializable]
public class Allele
{
    public string key = "";
    public string descriptiveValue = "";
    public float continuousValue = 0;
    public int discreteValue = 0;
    public int dominance = 0;
}

/// <summary>
/// Describes a mutation from one state to another with a given probability.
/// Each mutation is considered independently of the others.
/// </summary>
[System.Serializable]
public class Mutation
{
    public string currentState;
    public string nextState;
    public float probability;
}

/// <summary>
/// Traits are the phenotypic expression of genes, and are dependent on the existence of certain combinations of alleles present in the genome. 
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Trait", menuName = "Genome/Trait")]
public class Trait : ScriptableObject
{
    public string descriptiveValue = "";
    public float continuousValue = 0;
    public int discreteValue = 0;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Gene", menuName = "Genome/Gene")]
public class Gene : ScriptableObject
{
    public InheritanceMode inheritanceMode = InheritanceMode.Random;
    public List<Allele> alleles = new List<Allele>();
    public List<Mutation> mutations = new List<Mutation>();

    public static List<Gene> genes;

    public static Gene GetGene(string key) 
    {
        if (genes == null) 
            genes = Resources.LoadAll<Gene>("").ToList();

        if (!genes.Exists(g => g.alleles.Exists(a => a.key == key)))
            genes = Resources.LoadAll<Gene>("").ToList();

        return genes.Find(g => g.alleles.Exists(a => a.key == key));
    }
}

[System.Serializable]
public struct KeyPair<TKey, TValue>
{
    public TKey key;
    public TValue value;

    public KeyPair(TKey key, TValue value)
    {
        this.key = key;
        this.value = value;
    }
}

[System.Serializable][CreateAssetMenu(fileName = "Genome", menuName = "Genome/Genome")]
public class Genome : ScriptableObject, ISerializationCallbackReceiver
{
    //public SerializableDictionary<string, string> paternalDict = new SerializableDictionary<string, string>();
    //public SerializableDictionary<string, string> maternalDict = new SerializableDictionary<string, string>();

    private Dictionary<string, string> paternalDict = new Dictionary<string, string>();
    private Dictionary<string, string> maternalDict = new Dictionary<string, string>();

    public List<KeyPair<string, string>> paternal = new List<KeyPair<string, string>>();
    public List<KeyPair<string, string>> maternal = new List<KeyPair<string, string>>();

    public IEnumerable<string> PaternalKeys => paternalDict.Keys;
    public IEnumerable<string> MaternalKeys => maternalDict.Keys;
    public IEnumerable<string> Keys => new HashSet<string>(paternalDict.Keys).Union<string>(maternalDict.Keys);

    public string PaternalGene(string key) => paternalDict.GetValueOrDefault(key, null);
    public string MaternalGene(string key) => maternalDict.GetValueOrDefault(key, null);

    public void SetPaternalGene(string key, string value) => paternalDict[key] = value;
    public void SetMaternalGene(string key, string value) => maternalDict[key] = value;

    public List<Trait> DetermineTraits()
    {
        List<Trait> traits = new List<Trait>();
        foreach (string key in Keys)
        {
            Gene gene = Gene.GetGene(key);
            string paternal = PaternalGene(key);
            string maternal = MaternalGene(key);
        }

        return traits;
    }

    public void Print()
    {
        string str = "Offspring: \n";
        foreach (string key in Keys)
        {
            string paternal = PaternalGene(key);
            string maternal = MaternalGene(key);
            str += $"{key}: {paternal} {maternal}\n";
        }

        Debug.Log(str);
    }

    public void OnBeforeSerialize()
    {
        paternal.Clear();
        maternal.Clear();

        foreach (KeyValuePair<string, string> pair in paternalDict)
            paternal.Add(new KeyPair<string, string>(pair.Key, pair.Value));

        foreach (KeyValuePair<string, string> pair in maternalDict)
            maternal.Add(new KeyPair<string, string>(pair.Key, pair.Value));
    }

    public void OnAfterDeserialize()
    {
        paternalDict.Clear();
        maternalDict.Clear();

        foreach (KeyPair<string, string> pair in paternal)
            // If the key already exists, add a blank key to the dictionary.
            if (paternalDict.ContainsKey(pair.key))
            {
                // If a blank key already exists, give up.
                if (!paternalDict.ContainsKey(""))
                    paternalDict.Add("", "");
            }
            else
                paternalDict.Add(pair.key, pair.value);

        foreach (KeyPair<string, string> pair in maternal)
            if (maternalDict.ContainsKey(pair.key))
            {
                if (!maternalDict.ContainsKey(""))
                    maternalDict.Add("", "");
            }
            else
                maternalDict.Add(pair.key, pair.value);
    }
}
