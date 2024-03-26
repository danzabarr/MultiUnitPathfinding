using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IOnValidateListener<T>
{
	void OnScriptValidated(T script);
}

// Path: Assets/Scripts/OnValidateListener.cs