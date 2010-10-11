-- Create the 'Events' object
-- Most callbacks will be done on here!

Events = {}
State = {} -- Store the state in here please :)

-- Called when the bot is enabled
function Events.OnActivate(player)
	Engine.debug("LuaBot Up & Running!")	
end

function Events.OnFirstRun(self)
	-- Init the Data (see data.lua)
	Data_Init();
	
	-- Enable cheats
	Engine.getPlayer():order(BotLib.Orders.CheatFastBuild);
	Engine.getPlayer():order(BotLib.Orders.CheatGiveCash);
	Engine.getPlayer():order(BotLib.Orders.CheatGiveCash);
	Engine.getPlayer():order(BotLib.Orders.CheatGiveCash);
	
	Engine.deployMcv(); -- Unpack
	--Engine.queueProduction("powr") -- Build a power fact
	--Engine.queueProduction("proc") -- Build a ore ref
	--Engine.queueProduction("tent") -- Build a ore ref
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
	-- See if we need to produce a building!
	if (Engine.countPendingQueue("Building") == 0) then
		local nb = Bot:SelectNextBuilding("Building")
		if (nb ~= nil) then
			Engine.debug("Queueing production: " .. nb)
			Engine.queueProduction(nb)
		end
	end
	
	--if (Engine.countPendingQueue("Defense") == 0) then
	--	local nb = Bot:SelectNextBuilding("Defense")
	--	if (nb ~= nil) then
	--		Engine.debug("Queueing production: " .. nb)
	--		Engine.queueProduction(nb)
	--	end
	--end
end

-- Called when ANY actor got created (includes the Player actor)
function Events.OnActorCreated(self)
	if (self:getName() == "fact") then
		-- BotLib.assignBrain(self, "Brains.CYard") -- @todo bot event should be called with :OnEvent instead of .OnEvent so we can access the .Owner ref!)
		Engine.assignBrain(self, "Brains.CYard")
		Brains.CYard.Owner = self -- Store a reference to the relevant construction yard
	end
end

-- Called when a building is created
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
	Engine.debug("Created a unit!")
end

-- Called when a structure is created (placed)
function Events.OnStructureCreated(self)
		Brains.CYard.Custom_OnStructureCreated(self)
end

-- Called when a factory is created (Actor that can build)
function Events.OnFactoryCreated(self)
	-- Engine.debug("New factory created. Type: " .. self:getName() .. " || Setting Rally Point")
	-- Engine.assignBrain(self, "Dummy") -- Assign the dummy brain :p
	
	-- This will fetch (attempt to) and set a rally point
	local newRallyPoint = Engine.getRallyLocation(self:getLocation());
	self:order("SetRallyPoint", self, newRallyPoint)
end
