using UnityEngine;
using System.Collections.Generic;

public static class DataManager {
	private static bool _isLoaded = false;
	public static bool isLoaded { get { return _isLoaded; } }

	private static Dictionary<Card, CardData> _cards = new Dictionary<Card, CardData> ();
	public static Dictionary<Card, CardData> cards { get { return _cards; } }

	private static Dictionary<Command, CommandData> _commands = new Dictionary<Command, CommandData> ();
	public static Dictionary<Command, CommandData> commands { get { return _commands; } }

	private static Dictionary<Hero, HeroData> _heroes = new Dictionary<Hero, HeroData> ();
	public static Dictionary<Hero, HeroData> heroes { get { return _heroes; } }

	private static Dictionary<Region, RegionData> _regions = new Dictionary<Region, RegionData> ();
	public static Dictionary<Region, RegionData> regions { get { return _regions; } }

	private static GameRules _rules = null;
	public static GameRules rules { get { return _rules; } }

	public static void Load () {
		// Loading game rules
		_rules = (GameRules)Resources.Load ( "GameRules", typeof(GameRules) );
		Debug.Log ( string.Format ( "[DataManager] Loaded game rules." ) );

		// Loading card data
		foreach ( CardData cardData in Resources.LoadAll("CardData", typeof(CardData)) ) {
			_cards.Add ( cardData.cardId, cardData );
		}
		Debug.Log ( string.Format ( "[DataManager] Loaded {0} cards.", _cards.Count ) );

		// Loading command data
		foreach ( CommandData commandData in Resources.LoadAll("CommandData", typeof(CommandData)) ) {
			_commands.Add ( commandData.commandId, commandData );
		}
		Debug.Log ( string.Format ( "[DataManager] Loaded {0} commands.", _commands.Count ) );

		// Loading hero data
		foreach ( HeroData heroData in Resources.LoadAll("HeroData", typeof(HeroData)) ) {
			_heroes.Add ( heroData.heroId, heroData );
		}
		Debug.Log ( string.Format ( "[DataManager] Loaded {0} heroes.", _heroes.Count ) );

		// Loading region data
		foreach ( RegionData regionData in Resources.LoadAll("RegionData", typeof(RegionData)) ) {
			_regions.Add ( regionData.regionId, regionData );
		}
		Debug.Log ( string.Format ( "[DataManager] Loaded {0} regions.", _regions.Count ) );

		// Finished!
		_isLoaded = true;
	}
}
