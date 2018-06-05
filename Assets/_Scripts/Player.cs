using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Player : NetworkBehaviour {
	public static Dictionary<NetworkInstanceId, Player> players = new Dictionary<NetworkInstanceId, Player> ();
	public static Player localPlayer = null;

	public delegate void PlayerEvent ( Player player );
	public static event PlayerEvent OnPlayerConnection;
	public static event PlayerEvent OnPlayerDisconnection;

	[SerializeField] private GameObject _playerModel;

	private string _playerName = "PlayerDefault";
	public string playerName { get { return _playerName; } }

	private NetworkInstanceId _playerId = NetworkInstanceId.Invalid;
	public NetworkInstanceId playerId { get { return _playerId; } }

	SyncListInt

	[SyncVar(hook="OnChangedHp")] private int _hp = 0;
	public int hp { get { return _hp; } }

	[SyncVar(hook="OnChangedIsAlive")] private bool _isAlive = true;
	public bool isAlive { get { return _isAlive; } }
	public bool isDead { get { return !_isAlive; } }

	[SyncVar(hook="OnChangedIsRoleTraitor")] private bool _isRoleTraitor = false;
	public bool isRoleTraitor { get { return _isRoleTraitor; } }

	[SyncVar(hook="OnChangedIsRolePublic")] private bool _isRolePublic = false;
	public bool isRolePublic { get { return _isRolePublic; } }

	[SyncVar] private Region _region = Region.None;
	public Region region { get { return _region; } }

	[SyncVar] private Hero _hero = Hero.None;
	public Hero hero { get { return _hero; } }

	// Server-only variables
	private PlayerSlot _svSlot = null;

	void Start () {
		_playerId = this.netId;
		_playerName = "Player" + _playerId; // Will be better
		this.name = "Player" + _playerId;
		players.Add ( _playerId, this );
		if ( OnPlayerConnection != null ) {
			OnPlayerConnection ( this );
		}
	}

	void OnDestroy () {
		players.Remove ( _playerId );
		if ( localPlayer == this ) {
			localPlayer = null;
		}
		if ( _svSlot != null ) {
			_svSlot.ToggleFree ();
		}
		if ( OnPlayerDisconnection != null ) {
			OnPlayerDisconnection ( this );
		}
	}






	[ClientCallback]
	void Update () {
		if ( isLocalPlayer && _callbacks != null ) {
			for ( int i = 0 ; i <= 9 ; i++ ) {
				if ( Input.GetKeyDown ( KeyCode.Keypad0 + i ) && _callbacks.ContainsKey(i) ) {
					_callbacks [ i ] ();
				}
			}
		}
	}










	public GameObject _selectionPanel;
	public Text _selectionText;
	public delegate void SimpleCallback ();
	public Dictionary<int, SimpleCallback> _callbacks = null;

	public override void OnStartLocalPlayer () {
		localPlayer = this;
		connectionToServer.RegisterHandler ( (short)MessageType.StatusIdle, ( NetworkMessage msg ) => {
			OnReceivedMessageStatusIdle ( msg.ReadMessage<Message> () );
		} );
		connectionToServer.RegisterHandler ( (short)MessageType.ChooseAction, ( NetworkMessage msg ) => {
			OnReceivedMessageChooseAction ( msg.ReadMessage<Message> () );
		} );
	}

	[Client]
	void OnReceivedMessageStatusIdle ( Message message ) {
		HideSelectionPanel ();
	}

	[Client]
	void OnReceivedMessageChooseAction ( Message message ) {
		switch((MessageReason)message.data) {
			case MessageReason.ChooseActionGood:
				ShowSelectionPanel ("Choose an Evil Action", (Command)message.mask);
				break;
			case MessageReason.ChooseActionBad:
				ShowSelectionPanel ("Choose an Heroic Action", (Command)message.mask);
				break;
		}
	}

	[Client]
	void ShowSelectionPanel (string title, Command mask) {
		if ( _callbacks == null ) _callbacks = new Dictionary<int, SimpleCallback> ();
		_selectionPanel.SetActive ( true );
		string s = string.Format ("<b>{0}</b>\n", title);
		int i = 1;
		foreach (CommandData commandData in DataManager.commands.Values) {
			if ( ( mask & commandData.commandId ) != Command.None ) {
				s += string.Format ( "{1} [{0}]\n", i, commandData.commandName );
				_callbacks.Add ( i, () => {
					connectionToServer.Send ( (short)MessageType.ChooseAction, new Message () { mask = (int)commandData.commandId } );
				} );
				i++;
			}
		}
		_selectionText.text = s;
	}

	[Client]
	void HideSelectionPanel () {
		_selectionPanel.SetActive ( false );
		if ( _callbacks != null ) _callbacks.Clear ();
	}









	/**
	 * Server-only methods
	 **/

	[Server]
	public void SrvInitPlayer ( Hero hero, bool isTraitor ) {
		_hero = hero;
		_hp = DataManager.rules.playerStartingHp;
		_isAlive = true;
		_isRolePublic = false;
		_isRoleTraitor = isTraitor;
		this.RpcAssignHero ( hero );
	}

	[Server]
	public void SrvMoveTo ( PlayerSlot slot ) {
		if ( _svSlot != null ) _svSlot.ToggleFree ();
		RpcMoveTo ( slot.transform.position, slot.transform.rotation );
		_region = slot.regionId;
		_svSlot = slot;
		_svSlot.ToggleFree ();
	}

	[Server]
	public void SrvChangeHp ( int hpChange ) {
		_hp = Mathf.Clamp ( _hp + hpChange, 0, DataManager.rules.playerMaxHp );
		_isAlive = ( _hp > 0 );
	}



	/**
	 * Client-only methods
	 **/

	[ClientRpc]
	void RpcAssignHero ( Hero hero ) {
		_hero = hero;
		_playerModel.SetActive ( true );
		_playerModel.GetComponent<MeshRenderer> ().material.color = DataManager.heroes [ _hero ].heroColor;
		// To be completed
	}

	[ClientRpc]
	void RpcMoveTo ( Vector3 position, Quaternion rotation ) {
		gameObject.transform.position = position;
		gameObject.transform.rotation = rotation;
	}



	/**
	 * SyncVar callbacks
	 **/

	[Client]
	void OnChangedHp ( int hp ) {
		_hp = hp;
		// To be completed
	}

	[Client]
	void OnChangedIsAlive ( bool isAlive ) {
		_isAlive = isAlive;
		// To be completed
	}

	[Client]
	void OnChangedIsRoleTraitor ( bool isRoleTraitor ) {
		_isRoleTraitor = isRoleTraitor;
		// To be completed
	}

	[Client]
	void OnChangedIsRolePublic ( bool isRolePublic ) {
		_isRolePublic = isRolePublic;
		// To be completed
		
	}



	/**
	 * Utility methods
	 **/

	public string GetDebugStatus () {
		if ( _hero == Hero.None ) {
			return string.Format ( "{0} (ID {1}) - not initialized",
			                       _playerName,
			                       _playerId );
		} else {
			return string.Format ( "{0} (ID {1}) - {2}, {3}, {4}, {5} HP",
			                       _playerName,
			                       _playerId, 
			                       DataManager.heroes [ _hero ].heroName,
			                       ( isLocalPlayer || _isRolePublic ) ? ( _isRoleTraitor ? "Traitor" : "Loyal" ) : "???",
			                       ( isAlive ? "Alive" : "Dead" ),
			                       _hp );
		}
	}
}
