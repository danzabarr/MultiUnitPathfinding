using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

public interface IPath<Node> : IEnumerable<Node>
{
    Node Current();
    Node Next();
    Node Pop();
    int Count();
    float Cost();
}
