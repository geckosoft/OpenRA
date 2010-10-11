-- Include the botlib
dofile("mods/ra/bots/luabot/botlib.lua")

-- Global event handlers
dofile("mods/ra/bots/luabot/events.lua")

-- Braaaains
dofile("mods/ra/bots/luabot/brains.lua")

-- Data
dofile("mods/ra/bots/luabot/data.lua")

-- States
dofile("mods/ra/bots/luabot/states.lua")

-- Our bot utility (put utility methods in here please)
-- @todo Move to seperate file (bot.lua)
Bot = {}

function Bot:SelectNextBuilding(buildingType)
	local curBuildings = Engine.getBuildings(buildingType)
	local allBuildings = Engine.getBuildings()
	
	for bt, bf in pairs(Data.BuildingFractions) do
		repeat -- lua doesnt have continue, this is a hack for that :)
			if (Engine.getProductionType(bt) ~= buildingType) then
				break -- actually 'continues' the for loop
			end
			
			local cnt = 0;
			for key,actor in pairs(curBuildings) do
				if (actor:getName() == bt) then
					cnt = cnt + 1
				end
			end
			
			if ((cnt < bf * #allBuildings or (cnt == 0 and #allBuildings == 0)) and (Engine.canProduce(bt))) then
				return bt
			end
		until true
	end
	
	return nil
end


