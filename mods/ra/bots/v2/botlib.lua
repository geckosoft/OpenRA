-- botlib.lua
-- Core utilities - inclusion is *REQUIRED*

botlib = {}

botlib.Cheats = {}
botlib.Cheats.FastBuild = "DevFastBuild"
botlib.Cheats.GiveCash = "DevGiveCash"

botlib.BrainID = 1
botlib.Brains = {} -- Stores our generated brains -- TODO Clean this up once every while! (see if the Owner is destroyed / nil)

-- A 'cheat' so we can emulate some.table:function() calls from the core
function botlib.callObject(objPath, functionName, ...)
	local args = { ... }
	local parts = string.split(objPath, ".")	
	local tbl = _G
	
	for i,tblPath in pairs(parts) do
		if (tbl[tblPath] == nil) then
			Engine.debug("[botlib.callObject] Could not find object:" .. objPath)			
		else
			tbl = tbl[tblPath]
		end
	end

	if (tbl == nil) then
		Engine.debug("[botlib.callObject] Could not find object:" .. objPath)
		return false
	end	
	
	local f = tbl[functionName]
	
	if (f == nil) then
		Engine.debug("[botlib.callObject] Could not find function" .. objPath .. "."  .. functionName .. "()")
		return false
	end	
	
	f(tbl, unpack (args))
	
	return true
end

-- Clones table 't'
function botlib.clone(t)
	local new = {}             -- create a new table
	local i, v = next(t, nil)  -- i is an index of t, v = t[i]
	while i do
		if type(v)=="table" then v=botlib.clone(v) end
		new[i] = v
		i, v = next(t, i)        -- get next index
	end
	return new
end

-- Returns a string of the new 'brain' object
function botlib.setBrain(brainObj, owner)
	local newBrain = botlib.Clone(t)
	newBrain.Owner = owner;
	newBrain.ID = botlib.BrainID
	
	local newId = "Brain_" .. botlib.BrainID
	
	botlib.Brains[newId] = newBrain
	
	botlib.BrainID = botlib.BrainID + 1
	
	return "botlib.Brains." .. newId
end

function botlib.removeBrain(brainId)
	botlib.Brains[brainId] = nil -- Release the brain
end

-- Assigns a certain brain to given actor (self)
function botlib.assignBrain(self, brainObj)
	Engine.assignBrain(self, BotLib.SetBrain(brainObj, self))
end

function botlib.sortFieldDesc(tbl, field)
	local res = {}
	
	for index,value in ipairs(tbl) do
		if (tbl[index+1] ~= nil) then
			if (tbl[index][field] < tbl[index+1][field]) then
				local o = tbl[index+1]
				tbl[index+1] = tbl[index]
				tbl[index] = o
				return botlib.sortFieldDesc(tbl, field)
			end
		end
	end
	
	return tbl
end

-- Global utility functions
function string:split(sep)
        local sep, fields = sep or ":", {}
        local pattern = string.format("([^%s]+)", sep)
        self:gsub(pattern, function(c) fields[#fields+1] = c end)
        return fields
end

Yamlifier = {}
Yamlifier.Key = ""
Yamlifier.Value = ""

function Yamlifier:getKey()
	return self.Key
end

function Yamlifier:getValue()
	return self.Value
end

-- TODO add error handling
function Yamlifier.wrap(tbl)
	local newTbl = {}
	
	for k,v in ipairs(tbl) do
		newTbl[#newTbl + 1] = botlib.clone(Yamlifier)
		newTbl[#newTbl].Key = v[1]
		newTbl[#newTbl].Value = v[2]
	end
	
	return newTbl
end
