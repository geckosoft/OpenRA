-- actions.lua
A = {} -- they're stored in here
Action = {}
Action.Fields = {}
Action.ID = ""
Action.UniqueID = 0
Action.Yaml = nil
Action.Max = 1
Action.TaskForce = ""
Action.Recruiter = true
Action.Trigger = nil
Action.Data = {}
Action.Data.Forces = {} -- Assigned (built / recruited) force
Action._Done = false
Action._Failed = false
Action.Scripts = {}
Action.CurrentScript = nil -- shall store a coroutine
Action.CurrentScriptID = 1
MAX_SCRIPTS = 100
ACTION_UNIQUE_ID = 0

Action.State = {} -- Scripts can use these
function Action:setFailed(val)
	if (val == nil) then
		self._Failed = true
	else
		self._Failed = val
	end	
end
function Action:setDone(val)
	if (val == nil) then
		self._Done = true
	else
		self._Done = val
	end	
end

function Action:getTrigger()
	return self.Trigger
end

function Action:isFailed()
	return self._Failed
end

function Action:isDone()
	return self._Done
end

function Action:getFields(entry)
	if (self.Fields[entry] == nil or self.Fields[entry][2] == nil) then
		return {}
	end
	
	return self.Fields[entry][2]
end

-- Go to next script, ...
function Action:run()
	if (#self.Scripts == 0) then
		self:setDone(true) -- No scripts to run :(
		return
	end
	if (self.CurrentScript ~= nil) then
		if (coroutine.status(self.CurrentScript) == "suspended") then
			local ign, res, data = coroutine.resume(self.CurrentScript, self, self.Scripts[self.CurrentScriptID][2])
			
			if (coroutine.status(self.CurrentScript) == "dead") then -- it be done
				if (data ~= nil) then
					-- self.Data = data -- todo is this needed? According to #lua I dont need it...
				end
				if (res ~= true) then -- failure!
					self:setDone() -- done ...
					self:setFailed() -- ... but we failed :(
				end
				
				return -- we ran once
			end
		else
			-- run next script
			self.CurrentScriptID  = self.CurrentScriptID  + 1
			local nextScript = self.Scripts[self.CurrentScriptID]
			if (nextScript == nil) then
				self:setDone() -- Done! :)
				return
			end
			nextScript = nextScript[1]
			
			self.CurrentScript = coroutine.create(nextScript)
			return self:run() -- will 'run' the script
		end
	else
		-- assign first script
		self.CurrentScriptID = 1
		self.CurrentScript = coroutine.create(self.Scripts[self.CurrentScriptID][1]) -- first :P
		ACTION_UNIQUE_ID = ACTION_UNIQUE_ID + 1
		self.UniqueID = ACTION_UNIQUE_ID
		return self:run() -- will 'run' the script
	end
end

-- Clean up (release brains, ...)
function Action:cleanup()

end

function Action.new(id)
	local a= botlib.clone(Action)
	a.ID = id
	
	return a
end

function Action:processYaml(y)
	self.Yaml = y
	if (y == nil) then
		return false
	end
	
	local fields = y:getFields()
	
	for key,entry in ipairs(fields) do 
		self.Fields[entry:getKey()] = { entry:getValue() , entry:getFields() }
	end
	
	self:processFields()
	
	return true
end

function Action:processFields()
	self.Max = self:getNumber("Max", self.Max)
	self.TaskForce = self:getString("TaskForce", "")
	self.Recruiter = self:getBool("Recruiter", self.Recruiter)
	
	-- Load the scripts
	for i=1,MAX_SCRIPTS do
		local entry = self:getString("Script" .. i, nil)
		if (entry == nil) then
			-- Engine.debug("Got all scripts")
			break -- got all scripts
		end	
		
		if (S[entry] ~= nil) then -- it exists! (S == the Scripts 'namespace')
			-- Engine.debug("[Action] Found script '"  .. entry .. "' for action '" .. self.ID .. "'");			
			self.Scripts[#self.Scripts + 1] = { S[entry], self:getFields("Script" .. i) } -- get the optional 'options'
		else
			Engine.debug("Could not find script '" .. entry .. "' for action '" .. self.ID .. "'")
		end
	end
end

function Action:getBool(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	if (self.Fields[entry][1] == "yes" or self.Fields[entry][1] == "true" or self.Fields[entry][1] == "1") then
		return true
	end
	
	return false
end

function Action:getString(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	return self.Fields[entry][1]
end

function Action:getNumber(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	local v = tonumber(self.Fields[entry][1])
	if (v == nil) then
		return defaultValue
	end
	
	return v
end


ActionManager = {}
A = {}
ActionManager.Yaml = nil
function ActionManager:load(path)
	local yaml = MiniYaml.open(path)
	if (not yaml:isLoaded()) then
		Engine.debug("Could not load yaml: " .. path)
		return false
	end
	
	Engine.debug("Loading " .. path)
	self:processYaml(yaml)
	ActionManager.Yaml = yaml
end



function ActionManager:getAction(id)
	return A[id]
end

function ActionManager:processYaml(y)
	self.Yaml = y
	local field = y:getFields()
	
	for key,entry in ipairs(field) do 
		if (entry:getKey() ~= nil) then
			A[entry:getKey()] = Action.new(entry:getKey())
			A[entry:getKey()]:processYaml(entry)
			Engine.debug("Action loaded: " .. entry:getKey())
		end
	end
	
	return true
end