using UnityEngine;
using UnityEngine.Networking;

// Request message
public class Message : MessageBase {
	public delegate bool Handler (Message message);
	public MessageReason reason;
	public int data;
	public int mask;
}

// Message types
public enum MessageType : short {
	StatusIdle					= 1000,
	ChooseAction				= 1010,
	ChooseRegion				= 1020
}

// Message reasons
public enum MessageReason : short {
	ChooseActionBad				= 1011,
	ChooseActionGood			= 1012,
	ChooseRegionTravel			= 1021
}
