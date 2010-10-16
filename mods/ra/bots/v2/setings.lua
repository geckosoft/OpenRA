-- settings.lua
Settings = {}
Settings.Fields = {}

function Settings.load(path)	
	local yaml = MiniYaml.open(path)
	
	if (not yaml:isLoaded()) then
		Engine.debug("Could not load yaml: " .. path)
		return false
	end
	
	Engine.debug("Loading " .. path)
	
	local fields = yaml:getFields()
	
	-- first loop will result in index, 'trigger id'
	for key,entry in ipairs(fields) do 
		Settings.Fields[entry:getKey()] = {entry:getValue(), entry}
	end	
	
end

function Settings:getBool(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	if (self.Fields[entry][1] == "yes" or self.Fields[entry][1] == "true" or self.Fields[entry][1] == "1") then
		return true
	end
	
	return false
end

function Settings:getString(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	return self.Fields[entry][1]
end

function Settings:getNumber(entry, defaultValue)
	if (self.Fields[entry] == nil or self.Fields[entry][1] == nil) then
		return defaultValue
	end
	
	local v = tonumber(self.Fields[entry][1])
	if (v == nil) then
		return defaultValue
	end
	
	return v
end