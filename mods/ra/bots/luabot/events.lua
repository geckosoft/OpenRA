-- Create the 'Events' object
-- Most callbacks will be done on here!

Events = {}

-- Called when the bot is enabled
function Events.OnActivate(player)
	Engine.debug("LuaBot Up & Running!")	
end

function Events.OnFirstRun(self)
	-- Init the Data (see data.lua)
	Data_Init();
	
	-- Enable cheats
	Engine.getPlayer():order(BotLib.Orders.CheatFastBuild);
	
	Engine.deployMcv(); -- Unpack
	Engine.queueProduction("powr") -- Build a power fact
	Engine.queueProduction("proc") -- Build a ore ref
	Engine.queueProduction("tent") -- Build a ore ref
end

-- Called each tick before running other tick events (dont put heavy stuff in here kthx)
function Events.OnPreRun(self)

end

-- Called each tick after running the other tick events (dont put heavy stuff in here kthx)
-- Even better - dont use it at all, but use OnUpdate :)
function Events.OnRun(self)

end

-- Called (if you didnt change the option) around once every 2 seconds
function Events.OnCheck(self)
	--Engine.debug("Checking myself...")
	--for index,value in ipairs(Engine.getEnemies())	do
	--	Engine.debug("Enemy found! " ..value:getName())
	--end	
end

-- Called when ANY actor got created (includes the Player actor)
function Events.OnActorCreated(self)

end

-- Called when a unit (Actor with IMove) is created
function Events.OnBuildingFinished(item)
	local l = Engine.findBuildLocation(item, 15, Engine.getCYLocation());
	
	if (l == nil) then
		return false
	else
		Engine.placeBuilding(item, l);
		return true
	end	
end

-- Called when a unit (Actor with IMove) is created
function Events.OnUnitCreated(self)
	Engine.debug("Created a building!")
end

-- Called when a structure is created (placed)
function Events.OnStructureCreated(self)
	Engine.debug("Created a building!")
end

-- Called when a factory is created (Actor that can build)
function Events.OnFactoryCreated(self)
	Engine.debug("New factory created. Type: " .. self:getName() .. " || Setting Rally Point")
	Engine.assignBrain(self, "Dummy") -- Assign the dummy brain :p
	-- This will fetch (attempt to) and set a rally point
	local newRallyPoint = Engine.getRallyLocation(self:getLocation());
	self:order("SetRallyPoint", self, newRallyPoint)
end
