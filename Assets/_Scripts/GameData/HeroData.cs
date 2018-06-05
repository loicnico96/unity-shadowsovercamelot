using UnityEngine;

[CreateAssetMenu(fileName="HeroData", menuName="Game Data/Hero Data")]
public class HeroData : ScriptableObject {
	public Hero heroId;
	public Color heroColor;
	public string heroName;
	public string heroDescription;
	public int homeSlotId;
}
