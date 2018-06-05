using UnityEngine;

[CreateAssetMenu(fileName="RegionData", menuName="Game Data/Region Data")]
public class RegionData : ScriptableObject {
	public Region regionId;
	public string regionName;
	public bool isSoloQuest;
	public int numPlayerSlots;
}
