-- triggerconditions.lua
TC = {}

-- arg order: trigger, options
function TC.PLAYER_DEPLOYED_MCV(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	return true -- return true if the condition is met
end

function TC.PLAYER_HAS_BARRACKS(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	return ((Engine.countActors("barr") + Engine.countActors("tent")) > 0)
end

function TC.PLAYER_HAS_PROC(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	return (Engine.countActors("proc") > 0)
end

function TC.ENEMY_HAS_ENGINEERS(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	local player = trigger.Data.Player
	
	if (player == nil) then -- loop through the enemies until 1 condition is 'met'
		-- Loop, set the player, and call this condition again
		for i, player in ipairs(Engine.getEnemies()) do
			trigger.Data.Player = player
			
			if (C.PLAYER_COUNT_ACTOR(player, "e6") > 0) then
				return true
			end
		end
		
		return false
	end
	
	if (C.PLAYER_COUNT_ACTOR(player, "e6") > 0) then
		return true
	end
	
	return false
end

function TC.ENEMY_HAS_CYARD(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	local player = trigger.Data.Player
	
	if (player == nil) then -- loop through the enemies until 1 condition is 'met'
		-- Loop, set the player, and call this condition again
		for i, player in ipairs(Engine.getEnemies()) do
			trigger.Data.Player = player
			
			if (C.PLAYER_COUNT_ACTOR(player, "fact") > 0) then
				return true
			end
		end
		
		return false
	end
	
	if (C.PLAYER_COUNT_ACTOR(player, "fact") > 0) then
		return true
	end
	
	return false
end

function TC.ENEMY_HAS_PROC(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	local player = trigger.Data.Player
	
	if (player == nil) then -- loop through the enemies until 1 condition is 'met'
		-- Loop, set the player, and call this condition again
		for i, player in ipairs(Engine.getEnemies()) do
			trigger.Data.Player = player
			
			if (C.PLAYER_COUNT_ACTOR(player, "proc") > 0) then
				return true
			end
		end
		
		return false
	end
	
	if (C.PLAYER_COUNT_ACTOR(player, "proc") > 0) then
		return true
	end
	
	return false
end

function TC.ENEMY_HAS_NO_MCV(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	local player = trigger.Data.Player
	
	if (player == nil) then -- loop through the enemies until 1 condition is 'met'
		-- Loop, set the player, and call this condition again
		for i, player in ipairs(Engine.getEnemies()) do
			trigger.Data.Player = player
			
			if (TC.ENEMY_HAS_NO_MCV(trigger, options)) then
				return true
			end
		end
		
		return false
	end
	
	if (C.PLAYER_COUNT_ACTOR(player, "mcv") == 0) then
		return true
	end
	
	return false
end

function TC.CORE_HAS_CYARD(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	return (Engine.getPlayer():countActors("fact") > 0)
end

function TC.CORE_NEEDS_POWER(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	if (Settings:getNumber(PowerSurplus, 50) < Engine.getPowerAvailable()) then
		return false
	end
	
	return true
end

function TC.PLAYER_IS_ALLIES(trigger, options)
	return (Engine.getRace() == "allies")
end

function TC.PLAYER_IS_SOVIET(trigger, options)
	return (Engine.getRace() == "soviet")
end
function TC.CORE_SUFFICIENT_POWER(trigger, options) -- options being [1] == 'a string' && [2] = yaml fields table
	return (not TC.CORE_NEEDS_POWER(trigger, options))
end
