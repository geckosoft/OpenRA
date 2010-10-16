-- conditions.lua
-- functions prefixed with C. returning either true / false
-- may accept options
-- for usage in trigger 'checks'

C = {}

function C.HAS_BUILDINGS()
	return (Engine.countBuildings() > 0)
end

function C.CAN_BUILD(actor)
	return Engine.canProduce(actor)
end

function C.PLAYER_HAS_ACTOR(player, actor)
	return (player:countActors(actor) > 0)
end

function C.PLAYER_COUNT_ACTOR(player, actor)
	return player:countActors(actor);
end