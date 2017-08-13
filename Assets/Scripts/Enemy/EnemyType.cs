using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitType {
	BIO,
	MECH
}

public class EnemyType: MonoBehaviour {
	public HitType type;
}