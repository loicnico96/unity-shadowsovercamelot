using UnityEngine;

// Command IDs
public enum Command {
	None						= 0,
	// Bad actions
	BadActionDamage				= 1 << 0,	// Lose 1 HP
	BadActionEngine				= 1 << 1,	// Add 1 siege engine
	BadActionDraw				= 1 << 2,	// Draw 1 black card
	BadActionDrawArmor			= 1 << 3,	// Draw 2 black cards and choose one (only with Lancelot's Armor)
	BadActionPowerPerceval		= 1 << 4,	// Discard 2 cards (Perceval's hero power)
	// Good actions
	GoodActionTravel			= 1 << 8,	// Travel to another region
	GoodActionHomeDraw2			= 1 << 9,	// Draw 2 white cards (only at Round Table)
	GoodActionHomeDraw3			= 1 << 10,	// Draw 3 white cards (only at Round Table, Gauvain's hero power)
	GoodActionHomeFight			= 1 << 11,	// Fight a siege engine (only at Round Table)
	GoodActionSpecialCard		= 1 << 12,	// Use a special white card
	GoodActionPlayerHeal		= 1 << 13,	// Discard 3 cards to give 1 HP to a player
	GoodActionPlayerCharge		= 1 << 14,	// Accuse a player of being a traitor
	GoodActionBonusAction		= 1 << 15,	// Sacrifice 1 HP to perform a second action this turn
	GoodActionQuestBlackKnight	= 1 << 16,	// Set a card against the Black Knight
	GoodActionQuestPicts		= 1 << 17,	// Set a card against the Picts
	GoodActionQuestSaxons		= 1 << 18,	// Set a card against the Saxons
	GoodActionQuestGrail		= 1 << 19,	// Set a card for the Grail
	GoodActionQuestExcalibur	= 1 << 20,	// Discard 1 card to move excalibur
	GoodActionQuestLancelot		= 1 << 21,	// Set a card against Lancelot
	GoodActionQuestDragon		= 1 << 22,	// Set a card against the Dragon
	GoodActionPowerArthur		= 1 << 24,	// Exchange a card with another player (Arthur's hero power)
	GoodActionPowerBedivere		= 1 << 25,	// Discard 3 cards to get 1 Merlin from discard pile (Bedivere's hero power)
	GoodActionEndTurn			= 1 << 26	// End turn
}
