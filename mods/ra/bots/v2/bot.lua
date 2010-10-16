
-- Bot defines
Bot = {}
Bot.PATH = "mods/ra/bots/v2/"
Bot.USE_CHEATS = true
Bot.TICKS = 0 -- Increases at Events.OnTick

-- Some required mappings! (so coroutines can work!) - required
BEGIN_SCRIPT = Engine.beginCoroutine
END_SCRIPT = Engine.endCoroutine

-- Include the botlib - required
dofile(Bot.PATH .. "botlib.lua")

-- Include events handler
dofile(Bot.PATH .. "events.lua")

-- Include our scripts
dofile(Bot.PATH .. "scripts.lua")

-- Include our conditions
dofile(Bot.PATH .. "conditions.lua")

-- Include our actions (loaded later on from yaml)
dofile(Bot.PATH .. "actions.lua")

-- Include our triggers (loaded later on from yaml)
dofile(Bot.PATH .. "triggers.lua")

-- Include our task forces (loaded later on from yaml)
dofile(Bot.PATH .. "taskforces.lua")

-- Include our trigger condition methods
dofile(Bot.PATH .. "triggerconditions.lua")

-- Include our settings
dofile(Bot.PATH .. "settings.lua")

-- Bot utlity functions:

-- What player has the most buildings? (includes defenses)
function Bot.getBestEnemyBuilder()
	local best = nil
	
	for i, player in ipairs(Engine.getEnemies()) do
		if (best == nil or best:countBuildings()+best:countDefenses() < player:countBuildings()+player:countDefenses()) then
			best = player
		end 
	end
	
	return best
end

function Bot.countActors(player, tbl)
	local cnt = 0
	
	for k, v in pairs(tbl) do
		cnt = cnt + player:countActors(k)
	end	
	
	return cnt
end