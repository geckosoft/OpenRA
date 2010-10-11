-- pure-lua bot utility library / 'constants'
BotLib = {}
BotLib.BrainID = 1

BotLib.Orders = {}
BotLib.Orders.CheatFastBuild = "DevFastBuild";
BotLib.Orders.CheatGiveCash = "DevGiveCash";

-- Clones table 't'
function BotLib.clone(t)
	local new = {}             -- create a new table
	local i, v = next(t, nil)  -- i is an index of t, v = t[i]
	while i do
		if type(v)=="table" then v=BotLib.clone(v) end
		new[i] = v
		i, v = next(t, i)        -- get next index
	end
	return new
end

-- Returns a string of the new 'brain' object
function BotLib.setBrain(brainObj, owner)
	local newBrain = BotLib.Clone(t)
	newBrain.Owner = owner;
	local newId = "Brain_" .. BotLib.BrainID
	
	Brains[newId] = newBrain
	
	BotLib.BrainID = BotLib.BrainID + 1
	
	return "Brains." .. newId
end

-- Assigns a certain brain to given actor (self)
function BotLib.assignBrain(self, brainObj)
	Engine.assignBrain(self, BotLib.SetBrain(brainObj, self))
end

function BotLib.sortFieldDesc(tbl, field)
	local res = {}
	
	for index,value in ipairs(tbl) do
		if (tbl[index+1] ~= nil) then
			if (tbl[index][field] < tbl[index+1][field]) then
				local o = tbl[index+1]
				tbl[index+1] = tbl[index]
				tbl[index] = o
				return BotLib.sortFieldDesc(tbl, field)
			end
		end
	end
	
	return tbl
end