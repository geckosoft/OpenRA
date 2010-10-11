Brains = {}

-- A dummy (read: example brain
Brains.Dummy = {}

function Brains.Dummy.OnDamaged(self, attackInfo)
	Engine.debug("*drool*")
end


-- Construction Yard AI
Brains.CYard = {}

-- Note : custom event (as in : triggered from Lua code)
function Brains.CYard.Custom_OnStructureCreated(self)
	Engine.debug("Building built -> " .. self:getName())
	
	--if (Engine.countPendingQueue("Building") == 0) then
	--	local nb = Bot:SelectNextBuilding("Building")
	--	if (nb ~= nil) then
	--		Engine.debug("Queueing production: " .. nb)
	--		Engine.queueProduction(nb)
	--	end
	--end
end
