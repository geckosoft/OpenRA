-- settings.lua
Settings = {}
Settings.Fields = {}
Settings.Buildings = {}
Settings.BuildingLimits = {}
Settings.Defenses = {}
Settings.AdvDefenses = {}
Settings.AADefenses = {}
Settings.BaseSizeAdd = 4
Settings.DefenseRatio = 0.2
Settings.DefenseLimit = 40
Settings.AdvDefenseRatio = 0.2
Settings.AdvDefenseLimit = 40
Settings.AADefenseRatio = 0.2
Settings.AADefenseLimit = 40
Settings.BuildRatio = 1; -- auto calculated
Settings.PatrolScan = 15

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
		Settings.Fields[entry:getKey()] = {entry:getValue(), entry:getFields()}
		Engine.debug("Setting loaded: " .. entry:getKey())
	end	
	
	Settings:process()
end

function Settings:process()
	local Buildings = self:getFields("Buildings")
	local BuildingLimits = self:getFields("BuildingLimits")
	local Defenses = self:getFields("Defenses")
	local AdvDefenses = self:getFields("AdvDefenses")
	local AADefenses = self:getFields("AADefenses")
	
	for k, v in ipairs(Buildings) do
		Engine.debug("found " .. v:getKey())
		self.Buildings[v:getKey()] = tonumber(v:getValue())
	end
	
	for k, v in ipairs(BuildingLimits) do
		self.BuildingLimits[v:getKey()] = tonumber(v:getValue())
	end	
	
	for k, v in ipairs(Defenses) do
		self.Defenses[v:getKey()] = tonumber(v:getValue())
	end	
	
	for k, v in ipairs(AdvDefenses) do
		self.AdvDefenses[v:getKey()] = tonumber(v:getValue())
	end
	
	for k, v in ipairs(AADefenses) do
		self.AADefenses[v:getKey()] = tonumber(v:getValue())
	end
	
	self.BaseSizeAdd = self:getNumber("BaseSizeAdd", Settings.BaseSizeAdd)
	self.DefenseRatio =  self:getNumber("DefenseRatio", Settings.DefenseRatio)
	self.DefenseLimit = self:getNumber("DefenseLimit", Settings.DefenseLimit)
	self.AdvDefenseRatio = self:getNumber("AdvDefenseRatio", Settings.AdvDefenseRatio)
	self.AdvDefenseLimit = self:getNumber("AdvDefenseLimit", Settings.AdvDefenseLimit)
	self.AADefenseRatio = self:getNumber("AADefenseRatio", Settings.AADefenseRatio)
	self.AADefenseLimit = self:getNumber("AADefenseLimit", Settings.AADefenseLimit)	
	self.PatrolScan =  self:getNumber("PatrolScan", Settings.PatrolScan)	
	self.BuildRatio = 1 - self.DefenseRatio - self.AdvDefenseRatio - self.AADefenseRatio
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

function Settings:getFields(entry)
	if (self.Fields[entry] == nil or self.Fields[entry][2] == nil) then
		return {}
	end
	
	return self.Fields[entry][2]
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