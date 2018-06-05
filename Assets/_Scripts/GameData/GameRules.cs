using UnityEngine;

[CreateAssetMenu(fileName="GameRules", menuName="Game Data/Game Rules")]
public class GameRules : ScriptableObject {
	[Header("Game Rules")]
	[Tooltip("Maximum number of siege engines.")]
	public int maxSiegeEngines = 12;
	[Tooltip("Maximum number of swords on the Round Table.")]
	public int maxSwords = 12;
	[Tooltip("Minimum number of black swords on the Round Table that causes a loss.")]
	public int minBlackSwords = 7;

	[Header("Player Rules")]
	[Tooltip("Maximum HP a player can have at any time.")]
	public int playerMaxHp = 6;
	public int playerStartingHp = 4;
	[Tooltip("Maximum number of cards a player can have at the end of his turn.")]
	public int playerMaxHand = 12;
	public int playerStartingHand = 6;
}
