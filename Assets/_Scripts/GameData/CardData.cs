using UnityEngine;

[CreateAssetMenu(fileName="CardData", menuName="Game Data/Card Data")]
public class CardData : ScriptableObject {
	public Card cardId;
	public string cardName;
	public string cardDescription;
	public bool isWhiteCard;
	public bool isBlackCard;
	public bool isSpecialCard;
	public bool isFightCard;
	public int fightValue;
	[Tooltip("Secondary fight value (used for Dragon).")]
	public int fightValue2;
	[Tooltip("Total number of copies in game.")]
	public int numCopies;
}
