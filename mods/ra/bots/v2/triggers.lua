-- triggers.lua
-- Exposes the 'T' (triggers) namespace

T = {}

Trigger = {}
Trigger.ID = ""

Trigger.Fields = {}
Trigger.Data = {} -- can store misc stuff (by means of a trigger condition, ...)
Trigger.MinWeight = 100
Trigger.MaxWeigh = 100
Trigger.Weight = 100
Trigger.Requirements = {}
Trigger.LoopEnemyPlayers = false
Trigger.MaxPerPlayer = 1
Trigger.Max = 0
Trigger.Delay = 10 -- before a possible retrigger
Trigger.DelayPerPlayer = 5
Trigger.Runs = 0
Trigger.Action = ""
Trigger.InitialDelay = 0
Trigger.Priority = 100 -- instead of 'alarm'
Trigger.LastRun = 0 -- in ticks
Trigger.RunsPerPlayer = {} -- Store players in here as in [player] = amountOfRuns
Trigger.LastRunPerPlayer = {} -- Store players in here as in [player] = 'ticks'
Trigger.Player = nil -- what player needs checking in the triggerconditions (nil == dont keep the player in mind)
Trigger.Yaml = nil

function Trigger.Load(path)
	local yaml = MiniYaml.open(path)
	
	if (not yaml:isLoaded()) then
		Engine.debug("Could not load yaml: " .. path)
		return false
	end
	Engine.debug("Loading " .. path)
	
	local fields = yaml:getFields()
	
	-- first loop will result in index, 'trigger id'
	for key,entry in ipairs(fields) do 
		local trigger = botlib.clone(Trigger)
		trigger.ID = entry:getKey()
		local fields2 = entry:getFields()
		
		for key2,entry2 in ipairs(fields2) do 
			trigger.Fields[entry2:getKey()] = { entry2:getValue() , entry2:getFields() }
		end
		trigger.Yaml = yaml
		trigger:loadFields()
		
		-- Store the trigger
		T[trigger.ID] = trigger
		Engine.debug("Trigger loaded: " .. trigger.ID)
	end	
end

function Trigger:loadFields()
	self.Max = self:getNumber("Max", Trigger.Max)
	self.MaxPerPlayer = self:getNumber("MaxPerPlayer", Trigger.MaxPerPlayer)
	self.LoopEnemyPlayers = self:getBool("LoopEnemyPlayers", false)
	self.Delay = self:getNumber("Delay", Trigger.Delay)
	self.DelayPerPlayer = self:getNumber("DelayPerPlayer", Trigger.DelayPerPlayer)
	self.Weight = self:getNumber("Weight", Trigger.Weight)
	self.MinWeight = self:getNumber("MinWeight", Trigger.MinWeight)
	self.MaxWeight = self:getNumber("MaxWeight", Trigger.MaxWeight)
	self.Priority = self:getNumber("MaxWeight", Trigger.Priority)
	self.InitialDelay = self:getNumber("InitialDelay", Trigger.InitialDelay)
	self.Action = self:getString("Action", Trigger.Action)
	self.Action = A[self.Action] -- find the Action?
	
	if (self.Fields["Requirements"] == nil) then
		self.Fields["Requirements"] = {}
		self.Fields["Requirements"][2] = {}
	end
	local events = self.Fields["Requirements"][2]
	
	for k, v in ipairs(events) do
		self.Requirements[#self.Requirements + 1] = { v:getKey(), v:getValue(), v:getFields() }
	end
end
function Trigger:getFields(entry)
	if (self.Fields[entry] == nil or self.Fields[entry][2] == nil) then
		return {}
	end
	
	return self.Fields[entry][2]:getFields()
end
function Trigger:getBool(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	if (self.Fields[entry][1] == "yes" or self.Fields[entry][1] == "true" or self.Fields[entry][1] == "1") then
		return true
	end
	
	return false
end

function Trigger:getString(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	return self.Fields[entry][1]
end

function Trigger:getNumber(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	local v = tonumber(self.Fields[entry][1])
	if (v == nil) then
		return defaultValue
	end
	
	return v
end

function Trigger:canRun() -- performs some basic checks
	if (self.Action ~= nil and TriggerManager:countUsedActions(self.Action.ID) >= self.Action.Max) then
		return false -- max amount of actions already triggered
	end
	
	-- Limit how many times it may run
	if (self.Max ~= 0 and self.Runs >= self.Max) then
		return false
	end
	
	if (Bot.TICKS - self.LastRun < self.Delay) then
		return false -- delay
	end
	
	if (Bot.TICKS < self.InitialDelay) then
		return false -- delay
	end
	
	return true
end

function Trigger:run()
	if (not self:canRun()) then -- the 'real' checking is already done in shouldRun
		return false -- cannot repeat this one!
	end	
		
	self.Runs = self.Runs  + 1
	
	if (self.Data.Player ~= nil) then
		if (self.RunsPerPlayer[self.Data.Player:getId()] == nil) then
			self.RunsPerPlayer[self.Data.Player:getId()] = 0
		end
		self.RunsPerPlayer[self.Data.Player:getId()] = self.RunsPerPlayer[self.Data.Player:getId()] + 1
		
		self.LastRunPerPlayer[self.Data.Player:getId()] = Bot.TICKS
	end
	
	self.LastRun = Bot.TICKS
	if (self.Action == nil) then
		Engine.debug("No action defined!")
		return false
	else
		TriggerManager.ActiveActions[#TriggerManager.ActiveActions + 1] = botlib.clone(self.Action)
		TriggerManager.ActiveActions[#TriggerManager.ActiveActions].Trigger = self -- assign the trigger (trigger can store stuff)
		TriggerManager.ActiveActions[#TriggerManager.ActiveActions].Data = botlib.clone(self.Data) -- assign 'current' data
		
		return true
	end
end

function Trigger:shouldRun() -- should we run or not? Also sets on WHAT we should run
	if (not self:canRun()) then
		return false
	end
	if (self.LoopEnemyPlayers) then		
		for i, player in ipairs(Engine.getEnemies()) do
			local ok = true
			self.Data.Player = player -- store the 'target'
			
			if (self.MaxPerPlayer > 0 and self.RunsPerPlayer[player:getId()] ~= nil and self.RunsPerPlayer[player:getId()] >= self.MaxPerPlayer) then
				ok = false -- not gud
			elseif (self.DelayPerPlayer > 0 and self.LastRunPerPlayer[player:getId()] ~= nil and (Bot.TICKS - self.LastRunPerPlayer[player:getId()] < self.DelayPerPlayer)) then
				ok = false -- not gud
			else			
				for k, v in ipairs(self.Requirements) do
					local key = v[1]
					local value = v[2]
					local fields = v[3]
					
					local f = TC[key]
					
					local res = f(self, { value, fields })
					if (not res) then
						ok = false
					end		
				end
			end
			
			if (ok) then
				return true
			end
		end
	else	
		self.Data.Player = nil -- no player stored
	
		for k, v in ipairs(self.Requirements) do
			local key = v[1]
			local value = v[2]
			local fields = v[3]
			
			local f = TC[key]
			
			local res = f(self, { value, fields })
			if (not res) then
				return false -- should not run
			end		
		end
		return true
	end
	
	return false -- oh oh
end	


TriggerManager = {}
TriggerManager.Triggers = {}
TriggerManager.ActiveActions = {}
TriggerManager.MaxActive = 100 -- How many 'actions' may be active at same time  (TODO implement me kthx)


function TriggerManager:countUsedActions(actionId)
	local count = 0
	
	for i, action in ipairs(self.ActiveActions) do	
		if (action.ID == actionId) then
			count = count + 1
		end
	end
	
	return count
end

function TriggerManager:run()	
	-- cleanup first
	for i, action in ipairs(self.ActiveActions) do	
		if (action:isDone()) then -- remove this action - it be done
			--Engine.debug("[TriggerManager] Cleaning up action " .. action.ID)
			action:cleanup() -- perform some cleanup
			table.remove(self.ActiveActions, i)
			return self:run()	
		end
	end
	
	for i, action in ipairs(self.ActiveActions) do	
		-- Engine.debug("[TriggerManager] Running action " .. action.ID) -- Spam .. dont..
		action:run() -- run the action (process scripts, ...)
	end
end

-- See if we need to trigger something (call this in Events.OnUpdate)
function TriggerManager:ping()
	local bestW = 0
	local bestP = 0
	local bestT = {}	
	
	-- todo Implement 'Priority'
	if (#self.ActiveActions < self.MaxActive) then	
		for i, trigger in pairs(T) do	
			if (trigger:shouldRun()) then		
				if (trigger.Weight > bestW) then
					if (#bestT == 0) then
						bestW = trigger.Weight
						bestT = {}
						bestT[#bestT + 1] = trigger
					end
				elseif (trigger.Weight == bestW) then
					--if ((not trigger:isImportant()) and (trigger:isAlarm() and bestT[1]:isAlarm() == false)) then
					--	bestT = {}			
					--	bestT[#bestT + 1] = trigger	
					--elseif ((not trigger:isImportant()) and (bestT[1]:isAlarm() and trigger:isAlarm() == false)) then
					--	-- do nothing (alarms have higher prio!)
					--else			
					bestT[#bestT + 1] = trigger				
					--end
				else
					-- do nothing
				end
			end
		end
	
		if (#bestT > 0) then
			--Engine.debug("[TriggerManager] " .. #bestT .. " possible trigger(s)")
			local t = bestT[math.random(1, #bestT)]
			
			t:run()
		end
	end -- else => limit reached
end



