using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class RoomChat : NetworkBehaviour {
	public struct ChatMessage {
		public NetworkInstanceId senderId;
		public string message;
	}

	private Queue<ChatMessage> _chatMessages;
	private bool _chatIsShow;

	[SerializeField] private GameObject _chatWindow;
	[SerializeField] private Text _chatContent;
	[SerializeField] private Input _chatInput;

	void Awake () {
		_chatMessages = new Queue<ChatMessage> ();
		_chatIsShow = false;

	}

	[Command]
	public void CmdMsgFromPlayer ( NetworkInstanceId senderId, string message ) {
		// Here we could do some message validation
		RpcMsgFromPlayer ( senderId, message );
	}

	[ClientRpc]
	public void RpcMsgFromPlayer ( NetworkInstanceId senderId, string message ) {
		ChatMessage chatMessage = new ChatMessage () { senderId = senderId, message = message };
		_chatMessages.Enqueue ( chatMessage );
		_chatContent.text += FormatMessage ( chatMessage ) + "\n";
	}

	[ClientRpc]
	public void RpcMsgFromServer ( string message ) {
		ChatMessage chatMessage = new ChatMessage () { senderId = this.netId, message = message };
		_chatMessages.Enqueue ( chatMessage );
		_chatContent.text += FormatMessage ( chatMessage ) + "\n";
	}

	string FormatMessage ( ChatMessage message ) {
		// Message from server
		if ( message.senderId == this.netId ) {
			return message.message;
		
			// Message from a player
		} else if ( Player.players.ContainsKey ( message.senderId ) ) {
			Player sender = Player.players [ message.senderId ];
			HeroData heroData = DataManager.heroes [ sender.hero ];
			if ( string.IsNullOrEmpty ( heroData.heroName ) ) {
				return "<b>" + sender.playerName + ":</b> " + message.message;
			} else {
				return "<b><color=#" + ColorUtility.ToHtmlStringRGBA ( heroData.heroColor ) + ">" + heroData.heroName + " (" + sender.playerName + "):</color></b> " + message.message;
			}
		
			// Message from an unknown/invalid source (network problem?)
		} else if ( message.senderId == NetworkInstanceId.Invalid ) {
			Debug.LogWarning ( "[RoomChat] Message from invalid player ID." );
			return "";
		} else {
			Debug.LogWarning ( "[RoomChat] Message from unknown player ID : " + message.senderId.Value + "." );
			return "";
		}
	}
}
