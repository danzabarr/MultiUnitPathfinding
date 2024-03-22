using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breeder : MonoBehaviour
{
    public List<Gene> genes = new List<Gene>();
    public Genome father, mother;

    [ContextMenu("Breed")]
    public Genome Breed()
    {
        int n = 10;
        
        for (int i = 0; i < n; i++)
        {
            Genome child = ScriptableObject.CreateInstance<Genome>();
            HashSet<string> keys = new HashSet<string>(father.Keys);
            keys.UnionWith(mother.Keys);
            foreach (string key in keys)
            {
                string paternalGrandfather = father.PaternalGene(key);
                string paternalGrandmother = father.MaternalGene(key);
                string maternalGrandfather = mother.PaternalGene(key);
                string maternalGrandmother = mother.MaternalGene(key);

                string paternal = Random.value > 0.5f ? paternalGrandfather : paternalGrandmother;
                string maternal = Random.value > 0.5f ? maternalGrandfather : maternalGrandmother;

                if (paternal != null) child.SetPaternalGene(key, paternal);
                if (maternal != null) child.SetMaternalGene(key, maternal);
            }

            child.Print();
        }
        return null;
    }
}
