using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSlot : MonoBehaviour {
	public Region regionId;
	public int slotId;

	private bool _isFree = true;
	public bool isFree {
		get {
			return _isFree;
		}
	}

	public void ToggleFree () {
		_isFree = !_isFree;
	}
}
