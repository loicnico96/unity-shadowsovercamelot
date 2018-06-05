using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util {

	public static void Log ( bool server, string message ) {
		Debug.Log ("[" + (server ? "Server" : "Client") + "] " + message);
	}

	public static void Shuffle<T> ( IList<T> list ) {
		for ( int i = list.Count - 1 ; i > 0 ; i-- ) {
			int r = Random.Range ( 0, i );
			T t = list [ i ];
			list [ i ] = list [ r ];
			list [ r ] = t;
		}
	}

	public static void Shuffle<T> ( T [] list ) {
		for ( int i = list.Length - 1 ; i > 0 ; i-- ) {
			int r = Random.Range ( 0, i );
			T t = list [ i ];
			list [ i ] = list [ r ];
			list [ r ] = t;
		}
	}
}

