using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SerializableHashSet<T> : HashSet<T>, ISerializationCallbackReceiver
{
	[SerializeField]
	private List<T> list = new List<T>();

	public SerializableHashSet() { }

	public SerializableHashSet(ICollection<T> collection)
	{
		foreach (T item in collection)
			Add(item);
	}

	// save the HashSet to lists
	public void OnBeforeSerialize()
	{
		list.Clear();
		foreach (T item in this)
			list.Add(item);
	}

	// load HashSet from lists
	public void OnAfterDeserialize()
	{
		Clear();
		for (int i = 0; i < list.Count; i++)
			Add(list[i]);
	}
}
