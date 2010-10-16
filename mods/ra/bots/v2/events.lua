-- events.lua
-- The bot engine calls these (if they exist!)

Events = {}

-- Called when the bot is enabled
function Events.OnActivate(player)
	Engine.debug("Activating GeckoAI (v2) ...")	
	
	ActionManager:load(Bot.PATH .. "actions.yaml") -- Load the actions
	Trigger.Load(Bot.PATH .. "triggers.yaml") -- Load the triggers
	TaskForce.Load(Bot.PATH .. "taskforces.yaml") -- Load the triggers
	Settings.load(Bot.PATH .. "settings.yaml")
	
	return true
end

-- Called on first run (first tick)
function Events.OnFirstRun(self)
	-- See if we want to use cheats
	if (Bot.USE_CHEATS) then
		Engine.getPlayer():order(botlib.Cheats.FastBuild);
		Engine.getPlayer():order(botlib.Cheats.GiveCash);
	end
	return true
end

-- Called each tick before running other tick events
function Events.OnPreRun(self)
	return true

end

-- Called each tick after running the other tick events
function Events.OnRun(self)
	Bot.TICKS = Bot.TICKS + 1
	
	TriggerManager:run()
	return true
end

-- Called every 'update' amount of ticks
function Events.OnUpdate(self)
	TriggerManager:ping() -- Ping the triggers (see if something is changed)
	return true
end

-- Called a once around every 2 seconds (use this to cleanup)
function Events.OnCheck(self)
	return true
	
end

-- Called when ANY actor got created (includes the Player actor!)
function Events.OnActorCreated(self)
	-- Set some basic values (stored in LuaTaskForce)
	self:setOption("CanRecruit", 1) -- Can be recruited?	
	self:setOption("IsNew", 1) -- Is newly created (nothing done with it yet)
	
	return true
end

-- Called when a building is created
function Events.OnBuildingFinished(item)	
	-- Default behaviour
	local l = Engine.findBuildLocation(item, 20, Engine.getCYLocation());
	
	if (l == nil) then
		return false
	else
		Engine.placeBuilding(item, l);
		return true
	end	
	
	return true
end

-- Called when a unit (Actor with IMove) is created
function Events.OnUnitCreated(self)
	return true
	
end

-- Called when a structure is created (placed)
function Events.OnStructureCreated(self)
	return true
	
end

-- Called when a factory is created (placed) - Actor that can build
function Events.OnFactoryCreated(actor)

	-- This will fetch (attempt to) and set a rally point
	local newRallyPoint = Engine.getRallyLocation(actor:getLocation());
	actor:order("SetRallyPoint", newRallyPoint)

	return true
	
end
