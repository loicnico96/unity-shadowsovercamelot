using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class KnightsOfAvalon : NetworkBehaviour {
	private RoomChat _roomChat;

	// Lobby rules
	[Header("Lobby Rules")]
	public bool _useExtension = false;
	public int _minPlayers = 3;
	public int _maxPlayers = 7;

	// Turn management data
	[SyncVar] private int _turnCount;
	private NetworkInstanceId [] _turnOrder;
	[SyncVar] private int _turnPlayerIndex;
	// The following variables are only updated on the server
	private bool _turnIsFinished = false;
	private bool _turnUsedHeroPower = false;
	private bool _turnUsedBonusAction = false;
	private bool _turnMadeBadAction = false;
	private bool _turnMadeGoodAction = false;
	private Player _turnPlayer = null;

	// Draw and discard piles
	// Since we are not syncing the content of the draw piles, we need to sync the card count independently
	[SyncVar] private int _drawPileGoodCount;
	[SyncVar] private int _drawPileBadCount;
	// The cards themselves are only stored on the server
	private List<Card> _drawPileGood = new List<Card> ();
	private List<Card> _drawPileBad = new List<Card> ();
	// We also need to save the discard piles for the reshuffles
	private List<Card> _discardPileGood = new List<Card> ();
	private List<Card> _discardPileBad = new List<Card> ();

	// Game variables
	//[SyncVar] private bool _isStarted = false;
	[SyncVar] private bool _isVictory = false;
	[SyncVar] private bool _isDefeat = false;
	[SyncVar] private int _badSwords = 0;
	[SyncVar] private int _goodSwords = 0;
	[SyncVar] private int _engines = 0;

	// UI elements
	[Header("UI Manager")]
	[SerializeField] private Text _debugText;
	[SerializeField] private GameObject _buttonStartGame;
	[SerializeField] private GameObject _camera;

	// Buffering field positions
	private Dictionary<Region, List<PlayerSlot>> _playersSlots;

	void Awake () {
		_roomChat = GetComponent<RoomChat> ();
		Player.OnPlayerConnection += OnPlayerConnection;
		Player.OnPlayerDisconnection += OnPlayerDisconnection;
		RegisterPlayerSlots ();
		DataManager.Load ();
	}

	void OnDestroy () {
		Player.OnPlayerConnection -= OnPlayerConnection;
		Player.OnPlayerDisconnection -= OnPlayerDisconnection;
	}

	void RegisterPlayerSlots () {
		_playersSlots = new Dictionary<Region, List<PlayerSlot>> ();
		foreach ( GameObject obj in GameObject.FindGameObjectsWithTag("PlayerSlot") ) {
			PlayerSlot slot = obj.GetComponent<PlayerSlot> ();
			if ( slot != null ) {
				if ( !_playersSlots.ContainsKey ( slot.regionId ) ) {
					_playersSlots.Add ( slot.regionId, new List<PlayerSlot> () );
				}
				_playersSlots [ slot.regionId ].Add ( slot );
			}
		}
	}

	void OnPlayerConnection ( Player player ) {
		Debug.Log ( player.playerName + " joined the room." );
		// To be completed
		_buttonStartGame.SetActive ( isServer && Player.players.Count >= _minPlayers && Player.players.Count <= _maxPlayers );
	}

	void OnPlayerDisconnection ( Player player ) {
		Debug.Log ( player.playerName + " left the room." );
		// To be completed
		if ( _buttonStartGame != null ) {
			_buttonStartGame.SetActive ( isServer && Player.players.Count >= _minPlayers && Player.players.Count <= _maxPlayers );
		}
	}

	public void OnClickStartGame () {
		CmdStartGame ();
	}

	[ClientRpc]
	public void RpcUpdateDebugStatus () {
		_debugText.text = GetDebugStatus ();
	}

	public string GetDebugStatus () {
		string s = "";
		s += string.Format( "{0} engines, {1}/{2} swords\n", _engines, _goodSwords, _badSwords );
		s += string.Format ( "{0}/{1} cards in draw piles\n", _drawPileGoodCount, _drawPileBadCount );
		for (int i = 0 ; i < _turnOrder.Length ; i++) {
			Player player = Player.players [ _turnOrder [ i ] ];
			s += string.Format("{1} [{0}]\n", i, player.GetDebugStatus ());
		}
		s += string.Format ( "Currently turn {0}, player {1}\n", _turnCount, _turnPlayerIndex );
		return s;
	}

	[Command]
	void CmdStartGame () {
		StartCoroutine ( SrvStartGame () );
	}

	[Server]
	void SrvShuffleDrawPiles () {
		Debug.Log ( "[Server] Shuffling draw piles." );
		// Add the discarded cards back to the draw piles
		_drawPileBad.AddRange ( _discardPileBad );
		_drawPileGood.AddRange ( _discardPileGood );
		_discardPileBad.Clear ();
		_discardPileGood.Clear ();
		// Shuffle the draw piles
		Util.Shuffle ( _drawPileBad );
		Util.Shuffle ( _drawPileGood );
		// Communicate the new card count to clients
		_drawPileBadCount = _drawPileBad.Count;
		_drawPileGoodCount = _drawPileGood.Count;
	}

	[Server]
	IEnumerator SrvStartGame () {
		// Waiting for the data manager to be totally loaded
		while ( !DataManager.isLoaded ) yield return null;

		// We start by determining turn orders
		_turnOrder = Player.players.Keys.ToArray ();
		Util.Shuffle ( _turnOrder );

		// We setup the draw piles
		_drawPileBad.Clear ();
		_drawPileGood.Clear ();
		_discardPileBad.Clear ();
		_discardPileGood.Clear ();
		foreach ( CardData cardData in DataManager.cards.Values ) {
			for ( int i = 0 ; i < cardData.numCopies ; i++ ) {
				if ( cardData.isBlackCard ) {
					_drawPileBad.Add ( cardData.cardId );
				}
				if ( cardData.isWhiteCard ) {
					_drawPileGood.Add ( cardData.cardId );
				}
			}
		}

		// We shuffle the draw piles
		SrvShuffleDrawPiles ();

		// We are assigning a different hero to each player and spawning that player
		bool [] roleList = { false, false, false, false, false, false, false, true };
		Util.Shuffle ( roleList );
		Hero [] heroList = DataManager.heroes.Keys.ToArray ();
		Util.Shuffle ( heroList );
		for ( int i = 0 ; i < _turnOrder.Length ; i++ ) {
			Player player = Player.players [ _turnOrder [ i ] ];
			player.SrvInitPlayer ( heroList [ i ], roleList [ i ] );
			SrvMovePlayer ( player, Region.RegionRoundTable );
		}

		// We are reinitializing the game state
		_isVictory = _isDefeat = false;
		_goodSwords = _badSwords = _engines = 0;
		_turnPlayerIndex = 0;
		_turnCount = 1;

		// Finally, we can notify the clients that the game is ready to start
		// We are also communicating the game data that cannot be SyncVar (such as turn order)
		RpcStartGame ( _turnOrder );

		// Starting the game process
		yield return StartCoroutine ( SrvGameProcess () );
	}

	[Server]
	IEnumerator SrvRequestToPlayer ( Player player, MessageType messageType, int data = 0, int mask = 0, Message.Handler handler = null ) {
		player.connectionToClient.Send ( (short)messageType, new Message () { data = data, mask = mask } );
		if ( handler != null ) {
			bool pending = true;
			player.connectionToClient.RegisterHandler ( (short)messageType, ( NetworkMessage msg ) => {
				Message message = msg.ReadMessage<Message> ();
				if ( handler ( message ) ) {
					player.connectionToClient.UnregisterHandler ( (short)messageType );
					pending = false;
				}
			} );
			while ( pending ) yield return null;
			player.connectionToClient.Send ( (short)MessageType.StatusIdle, new Message () );
		}
	}

	[Server]
	bool SrvIsPlayerCanMakeAction ( Player player, Command command ) {
		switch ( command ) {
			case Command.BadActionDrawArmor:
				return false;
			case Command.GoodActionBonusAction:
				return !_turnUsedBonusAction;
			case Command.GoodActionHomeFight:
				return ( _engines > 0 );
			case Command.GoodActionPlayerCharge:
				return ( ( _engines >= 6 ) || ( _goodSwords + _badSwords ) >= 6 );
			case Command.GoodActionPlayerHeal:
				return true;
			default:
				return true;
		}
	}





	public int _questExcaliburLocation = 5; // position of Excalibur (0-10)
	public bool _questExcaliburCompleted = false;
	public Card [] _questionGrailCards = new Card [7] { Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None };
	public bool _questGrailCompleted = false;
	public int _questPictsWarriorCount = 0; // Number of Picts (0-4)
	public Card [] _questPictsCards = new Card [5] { Card.None, Card.None, Card.None, Card.None, Card.None };
	public int _questSaxonsWarriorCount = 0; // Number of Saxons (0-4)
	public Card [] _questSaxonsCards = new Card [5] { Card.None, Card.None, Card.None, Card.None, Card.None };
	public Card [] _questionBlackKnightCards = new Card [8] { Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None };
	public Card [] _questionLancelotCards = new Card [10] { Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None };
	public bool _questLancelotCompleted = false;
	public Card [] _questionDragonCards = new Card [14] { Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None, Card.None };
	public bool _questDragonCompleted = false;

	[Server]
	void SrvCheckQuestStatus () {
		// Excalibur
		if ( _questExcaliburLocation == 0 ) {
			// LOSE
		} else if ( _questExcaliburLocation == 10 ) {
			// WIN
		}


	}







	public int _excalibur = 5;
	public Card [] _grail = new Card[7];
	public int _picts = 0;
	public int _saxons = 0;

	[Server]
	IEnumerator SrvApplyCard ( Player player, Card card ) {
		switch ( card ) {

			case Card.BadCardWarPicts:
				_picts++;
				yield return null;
				break;

			case Card.BadCardWarSaxons:
				_saxons++;
				break;

			case Card.BadCardExcalibur:
				_excalibur--;
				break;

			case Card.BadCardGrail:
				for ( int i = 0 ; i < 7 ; i++ ) {
					if ( _grail [ i ] != Card.BadCardGrail ) {
						if ( _grail [ i ] == Card.GoodCardGrail ) {
							_grail [ i ] = Card.None;
						} else {
							_grail [ i ] = Card.BadCardGrail;
						}
						break;
					}
				}
				break;

			case Card.GoodCardGrail:
				for ( int i = 6 ; i >= 0 ; i-- ) {
					if ( _grail [ i ] != Card.GoodCardGrail ) {
						if ( _grail [ i ] == Card.BadCardGrail ) {
							_grail [ i ] = Card.None;
						} else {
							_grail [ i ] = Card.GoodCardGrail;
						}
						break;
					}
				}
				break;

			default:
				Debug.Log ( string.Format ( "[Server] SrvApplyCard : Unhandled card ({0}).", card ) );
				break;
		}
	}

	[Server]
	IEnumerator SrvPlayerPerformAction ( Player player, Command command ) {
		switch ( command ) {
			
			case Command.BadActionDamage:
				_turnMadeBadAction = true;
				_turnPlayer.SrvChangeHp ( -1 );
				break;

			case Command.BadActionEngine:
				_turnMadeBadAction = true;
				_engines++;
				break;

			case Command.BadActionDraw:
				_turnMadeBadAction = true;
				//
				break;

			case Command.BadActionDrawArmor:
				_turnMadeBadAction = true;
				//
				break;

			case Command.BadActionPowerPerceval:
				_turnMadeBadAction = true;
				break;

			case Command.GoodActionBonusAction:
				_turnPlayer.SrvChangeHp ( -1 );
				_turnUsedBonusAction = true;
				_turnMadeGoodAction = false;
				break;

			case Command.GoodActionEndTurn:
				_turnIsFinished = true;
				break;

			case Command.GoodActionHomeDraw2:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionHomeDraw3:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionHomeFight:
				_turnMadeGoodAction = true;
				//
				if ( Random.Range ( 0, 8 ) >= 5 ) {
					_turnPlayer.SrvChangeHp ( -1 );
				} else {
					_engines--;
				}
				break;

			case Command.GoodActionPlayerCharge:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionPlayerHeal:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionPowerArthur:
				_turnUsedHeroPower = true;
				//
				break;

			case Command.GoodActionPowerBedivere:
				_turnUsedHeroPower = true;
				//
				break;

			case Command.GoodActionQuestBlackKnight:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionQuestDragon:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionQuestExcalibur:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionQuestGrail:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionQuestLancelot:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionQuestPicts:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionQuestSaxons:
				_turnMadeGoodAction = true;
				//
				break;

			case Command.GoodActionSpecialCard:
				if ( player.hero == Hero.HeroGalahad && !_turnUsedHeroPower ) {
					_turnUsedHeroPower = true;
				} else {
					_turnMadeGoodAction = true;
				}
				//
				yield return null; // We need this somewhere to compile for now
				break;

			case Command.GoodActionTravel:
				if ( player.hero == Hero.HeroTristan && player.region == Region.RegionRoundTable && !_turnUsedHeroPower ) {
					_turnUsedHeroPower = true;
				} else {
					_turnMadeGoodAction = true;
				}
				SrvMovePlayer ( _turnPlayer, DataManager.regions.Keys.ToArray () [ Random.Range ( 0, DataManager.regions.Count ) ] );
				break;
			
			default:
				Debug.Log ( string.Format ( "[Server] SrvPlayerPerformAction : Unhandled command ({0}).", command ) );
				break;
		}
	}

	[Server]
	IEnumerator SrvPlayerRequestAction ( Player player, bool goodAction, bool badAction ) {
		Command command = Command.None;
		Command mask = Command.None;

		// Creating a list of available commands
		foreach ( CommandData commandData in DataManager.commands.Values ) {
			if ( ( ( commandData.isGoodAction && goodAction ) || ( commandData.isBadAction && badAction ) ) &&
			     ( ( commandData.isBonusAction == _turnMadeGoodAction ) || ( commandData.isHeroPower && !_turnUsedHeroPower ) ) &&
			     ( commandData.requiredHero == Hero.None || commandData.requiredHero == player.hero ) &&
			     ( commandData.requiredRegion == Region.None || commandData.requiredRegion == player.region ) &&
			     SrvIsPlayerCanMakeAction ( player, commandData.commandId ) ) {
				mask |= commandData.commandId;
				mask &= ~commandData.overridesAction;
			}
		}

		// Sending the request and waiting for an answer
		yield return StartCoroutine ( SrvRequestToPlayer ( _turnPlayer, MessageType.ChooseAction, (int)MessageReason.ChooseActionGood, (int)mask, ( Message message ) => {
			command = (Command)message.mask;
			return (command & mask) != Command.None;
		} ) );

		// Performing the corresponding action
		yield return StartCoroutine(SrvPlayerPerformAction(_turnPlayer, command));
	}

	[Server]
	IEnumerator SrvGameProcess () {
		while ( true ) {
			_turnPlayer = Player.players [ _turnOrder [ _turnPlayerIndex ] ];
			_turnIsFinished = false;
			_turnMadeBadAction = false;
			_turnMadeGoodAction = false;
			_turnUsedBonusAction = false;
			_turnUsedHeroPower = false;
			RpcUpdateDebugStatus ();

			// Phase 1 - Progression of Evil
			while ( _turnPlayer.isAlive && !_turnIsFinished && !_turnMadeBadAction ) {
				//yield return StartCoroutine ( SrvTurnPlayerBadAction () );
				yield return StartCoroutine ( SrvPlayerRequestAction ( _turnPlayer, false, true ) );
				RpcUpdateDebugStatus ();
				if ( SrvCheckGameState () ) {
					goto GameOver;
				}
			}
			
			// Phase 2 - Heroic actions
			while ( _turnPlayer.isAlive && !_turnIsFinished ) {
				//yield return StartCoroutine ( SrvTurnPlayerGoodAction () );
				yield return StartCoroutine ( SrvPlayerRequestAction ( _turnPlayer, true, false ) );
				RpcUpdateDebugStatus ();
				if ( SrvCheckGameState () ) {
					goto GameOver;
				}
			}

			// End Phase - Discard cards if more than 12
			yield return null; // Discard

			do { // Let's find the next player that can take a turn
				_turnPlayerIndex = ( _turnPlayerIndex + 1 ) % _turnOrder.Length;
				if ( _turnPlayerIndex == 0 ) _turnCount++;
			} while ( !SrvCanTakeTurn ( Player.players [ _turnOrder [ _turnPlayerIndex ] ] ) );
		}

		GameOver:
		if ( _isVictory ) {
			Debug.Log ( "Victory!" );
			// To be completed
		} else if ( _isDefeat ) {
			Debug.Log ( "Defeat!" );
			// To be completed
		}

		//_isStarted = false;
	}

	[ClientRpc]
	void RpcStartGame ( NetworkInstanceId[] turnOrder ) {
		Debug.Log ( "The game is starting!" );
		_turnOrder = turnOrder;
		//_isStarted = true;
		_buttonStartGame.SetActive ( false );
		//_camera.GetComponent<Animator> ().SetTrigger ( "RegionZoom0" );
	}

	[Server]
	bool SrvCheckGameState () {
		// First, we check if any player died
		//

		// Then we check for victory/lose conditions
		if ( _engines >= DataManager.rules.maxSiegeEngines ) {
			_isDefeat = true;
		} else if ( _badSwords >= DataManager.rules.minBlackSwords ) {
			_isDefeat = true;
		} else if ( _badSwords + _goodSwords >= DataManager.rules.maxSwords ) {
			if ( _goodSwords > _badSwords ) {
				_isVictory = true;
			} else {
				_isDefeat = true;
			}
		} else if ( SrvCountPlayers ( ( Player player ) => { return player.isAlive && !player.isRoleTraitor; } ) == 0 ) {
			_isDefeat = true;
		}

		return _isVictory || _isDefeat;
	}

	[Server]
	bool SrvCanTakeTurn ( Player player ) {
		return player.isAlive;
	}

	[Server]
	void SrvMovePlayer ( Player player, Region regionId ) {
		PlayerSlot slot = SrvFindSlotForPlayer ( player, regionId );
		if ( slot != null ) {
			player.SrvMoveTo (slot);
		} else {
			Debug.LogError ("[Server] MovePlayer : Trying to move player " + player.playerName + " to region " + regionId + " with no slots available.");
		}
	}
	
	[Server]
	PlayerSlot SrvFindSlotForPlayer ( Player player, Region regionId ) {
		if (_playersSlots.ContainsKey(regionId)) {
			List<PlayerSlot> regionSlots = _playersSlots [ regionId ];
			if ( regionSlots != null ) {
				if ( regionId == Region.RegionRoundTable ) { // Round Table, each player has their own seat
					foreach ( PlayerSlot slot in regionSlots ) {
						if ( slot.slotId == DataManager.heroes [ player.hero ].homeSlotId ) {
							return slot.isFree ? slot : null;
						}
					}
				} else { // Quest region, assign the first free slot we find
					foreach ( PlayerSlot slot in regionSlots ) {
						if ( slot.isFree ) {
							return slot;
						}
					}
				}
			}
		} else {
			Debug.LogError ( string.Format ( "[Server] FindSlotForPlayer : Couldn't find player slots for region {0}.", regionId ) );
		}
		return null;
	}

	[Server]
	bool SrvHasSlotForPlayer (Player player, Region regionId) {
		return SrvFindSlotForPlayer ( player, regionId ) != null;
	}

	[Server]
	int SrvCountPlayers (System.Func<Player, bool> condition = null) {
		int count = 0;
		foreach ( Player player in Player.players.Values ) {
			if ( condition == null || condition (player) ) {
				count++;
			}
		}
		return count;
	}

	[Server]
	List<Player> SrvListPlayers (System.Func<Player, bool> condition = null) {
		List<Player> players = new List<Player> ();
		foreach ( Player player in Player.players.Values ) {
			if ( condition == null || condition (player) ) {
				players.Add ( player );
			}
		}
		return players;
	}
}
