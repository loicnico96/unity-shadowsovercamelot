using UnityEngine;

[CreateAssetMenu(fileName="CommandData", menuName="Game Data/Command Data")]
public class CommandData : ScriptableObject {
	public Command commandId;
	public string commandName;
	public string commandDescription;
	public bool isBadAction;
	public bool isGoodAction;
	[Tooltip("This action is only available after the Heroic Action has been used.")]
	public bool isBonusAction;
	[Tooltip("This action can always be used once per turn, regardless of other actions.")]
	public bool isHeroPower;
	[Tooltip("This action requires a specific hero.")]
	public Hero requiredHero;
	[Tooltip("This action requires to be in a specific region.")]
	public Region requiredRegion;
	[Tooltip("This actions replaces another action if it is available.")]
	public Command overridesAction;
}
