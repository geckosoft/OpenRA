-- scripts.lua
-- Exposes the 'S' (scripts) namespace

S = {}

-- Example 'debug' script (outputs a message)
-- format S.SCRIPT_NAME(actionEntry, tableOfYamlEntriesOptions)
function S.DEBUG(action, options)
	BEGIN_SCRIPT()
	Engine.debug("[Debug] " .. options[1]:getKey())	
	END_SCRIPT()
	
	return true
end

function S.DEPLOY_MCV(action, options)
	BEGIN_SCRIPT()
	
	Engine.debug("[DEPLOY_MCV] Deploying MCV ...")
	Engine.deployMcv()
	
	END_SCRIPT()
	return true, action.Data
end

function S.ASSIGN_BRAIN(action, options)

	BEGIN_SCRIPT()
	Engine.debug("[ASSIGN_BRAIN] --> " .. options[1]:getKey())	
	for i, actor in ipairs(action.Data.Forces) do
		Engine.assignBrain(actor, options[1]:getKey())
	end
	END_SCRIPT()
	
	return true, action.Data
end

function S.ATTACK_PLAYER(action, options)
	BEGIN_SCRIPT()
	local player = action.Data.Player -- Always store the target player in here
	Engine.debug("[ATTACK_PLAYER] Launching attack on " .. player:getName())
	
	for i, actor in ipairs(action.Data.Forces) do
		actor:attackMove(Engine.getStartLocation(player))
	end
	
	END_SCRIPT()
	
	return true, action.Data
end

function S_PRODUCE(actor)
	Engine.debug("Enqueing " ..actor)	
	Engine.queueProduction(actor)
end

function S_BUILD_ACTOR(actor)
	S_PRODUCE(actor)
	S_WAIT_FOR_ACTOR(actor)
end

function S_WAIT_FOR_ACTOR(actor)
	while (true) do
		local haveAll = false		
			
		local actors = Engine.getActors(actor)
		for ia, act in ipairs(actors) do
			if (act:getOption("IsNew", 1) == 1) then -- ALWAYS set IsNew to false on new actors please kthx :)
				haveAll = true
				act:setOption("IsNew", 0)
				act:setState("OwningAction", action.UniqueID)
				break
			end	
		end
		
		if (haveAll) then
			break;
		end
		
		-- dont have all yet :(
		coroutine.yield() -- return control
	end	
end

function S_WAIT_FOR_ACTORS(actorList)
	local have = 0
	while (true) do			
		for k,v in ipairs(actorList) do	
			local actor = v
			local actors = Engine.getActors(actor)
			for ia, act in ipairs(actors) do
				if (act:getOption("IsNew", 1) == 1) then -- ALWAYS set IsNew to false on new actors please kthx :)
					have = have + 1
					act:setOption("IsNew", 0)
					act:setState("OwningAction", action.UniqueID)
					break
				end	
			end
			
			if (have == #actors) then
				break;
			end
		end
		-- dont have all yet :(
		coroutine.yield() -- return control
	end	
end

function S.BUILD(action, options)
	BEGIN_SCRIPT()
	local actor = options[1]:getKey()
	
	S_BUILD_ACTOR(actor)	
	END_SCRIPT()
	
	return true
end

function S.CORE_BUILD_POWER(action, options)
	BEGIN_SCRIPT()
	local powerRequested = Settings:getNumber("PowerSurplus", 100)
	powerRequested = powerRequested - Engine.getPowerAvailable();
	
	if (powerRequested < 0) then
		END_SCRIPT()
		return true		
	end
	
	Engine.debug("[CORE_BUILD_POWER] Power requested: " ..powerRequested)
	
	local actor = "powr"
	
	-- find what to build
	if ((powerRequested > 100) and (Engine.canProduce("apwr"))) then
		actor = "apwr"
	end
	
	Engine.queueProduction(actor)
	Engine.debug("[CORE_BUILD_POWER] Building " .. actor)
	
	while (true) do
		local haveAll = false		
			
		local actors = Engine.getActors(actor)
		for ia, act in ipairs(actors) do
			if (act:getOption("IsNew", 1) == 1) then
				haveAll = true
				act:setOption("IsNew", 0)
				act:setState("OwningAction", action.UniqueID)
				break
			end	
		end
		
		if (haveAll) then
			break;
		end
		
		-- dont have all yet :(
		coroutine.yield() -- return control
	end	
	
	END_SCRIPT()
	return true, action.Data
end



function S.BUILD_POWER(action, options)
	BEGIN_SCRIPT()
	local powerRequested = options[1]:getKey()
	
	Engine.debug("[BUILD_POWER] Power requested: " ..powerRequested)
	
	local actor = "powr"
	
	-- find what to build
	if ((powerRequested > 100) and (Engine.canProduce("apwr"))) then
		actor = "apwr"
	end
	
	Engine.queueProduction(actor)
	Engine.debug("[BuildPower] Building " .. actor)
	
	while (true) do
		local haveAll = false		
			
		local actors = Engine.getActors(actor)
		for ia, act in ipairs(actors) do
			if (act:getOption("IsNew", 1) == 1) then
				haveAll = true
				act:setOption("IsNew", 0)
				act:setState("OwningAction", action.UniqueID)
				break
			end	
		end
		
		if (haveAll) then
			break;
		end
		
		-- dont have all yet :(
		coroutine.yield() -- return control
	end	
	
	END_SCRIPT()
	return true, action.Data
end

function S.BUILD_TASKFORCE(action, options)
	BEGIN_SCRIPT()
	
	local tf = action.TaskForce
	if (tf == "" or (TF[tf] == nil)) then
		Engine.debug("[BUILD_TASKFORCE] Action " .. action.ID .. " doesn't have a valid TaskForce assigned!")
		return false-- nothing to do? :s
	end
	
	Engine.debug("[BUILD_TASKFORCE] " ..  tf)
	
	tf = TF[tf] -- get from the TF namespace 
	-- Format: { { "actor", amount }, { "actor2", amount }, ... }
	
	for i, entry in ipairs(tf.Actors) do
		for a = 1, entry[2] do
			Engine.queueProduction(entry[1])
			Engine.debug("[BuildTaskForce] Enqueued " .. entry[1])
		end
	end
	
	while (true) do
		-- Engine.debug("[BuildTaskForce] Checking ...") -- Spam .. Dont use
		local haveAll = true
		for i, entry in ipairs(tf.Actors) do
			local amountNeeded = entry[2]
			
			local actors = Engine.getActors(entry[1])
			for ia, actor in ipairs(actors) do
				if (actor:getOption("IsNew", 1) == 1) then -- ALWAYS set IsNew to false on new actors please kthx :)
					amountNeeded = amountNeeded - 1
				end	
			end
			if (amountNeeded > 0) then
				haveAll = false;
				break
			end
		end
		if (haveAll) then
			break;
		end
		
		-- dont have all yet :(
		coroutine.yield() -- return control
	end	
	if (action.Data == nil) then
		action.Data = {}
	end
	--if (action.Data.Forces == nil) then
	--	action.Data.Forces = {}
	--end
	action.Data.Forces = {}
	-- We got all units! Change their IsNew option
	for i, entry in ipairs(tf.Actors) do
		local amountNeeded = entry[2]
		
		local actors = Engine.getActors(entry[1])
		for ia, actor in ipairs(actors) do
			if (amountNeeded <= 0) then
				break
			end
			if (actor:getOption("IsNew", 1) == 1) then -- ALWAYS set IsNew to false on new actors please kthx :)
				amountNeeded = amountNeeded - 1
				actor:setOption("IsNew", 0) -- not new anymore !! :)
				actor:setState("OwningAction", action.UniqueID)
				action.Data.Forces[#action.Data.Forces + 1] = actor
			end	
		end
	end
		
	END_SCRIPT()
	return true, action.Data
end

function S.CORE_EXPAND_BASE(action, options)
	BEGIN_SCRIPT()
	-- Engine.debug("[CORE_EXPAND_BASE] Expanding... ")
	
	-- Get settings
	local actor = ""
	local bestBuilder =  Bot.getBestEnemyBuilder();
	local maxBuildings = bestBuilder:countBuildings() + bestBuilder:countDefenses()
	local maxBaseSize = maxBuildings + Settings.BaseSizeAdd -- how many buildings should we MAX have

	local player = Engine.getPlayer()
	local curB = player:countBuildings() -- this method misses the cyard, but its forgiven :)
	local curD = player:countDefenses()
	local total = curB + curD-- + 1 -- hax (to prevent a 'div by 0')
	local defRatio = Settings.DefenseRatio + Settings.AdvDefenseRatio + Settings.AADefenseRatio;
	
	local maxB = maxBaseSize * Settings.BuildRatio;
	local maxD = maxBaseSize * defRatio;
	local actors = {}
	-- Settings.BuildRatio = 1 -- meh.. hack :(
	--Engine.debug("[CORE_EXPAND_BASE] " .. total .. " " .. curB .. " " .. Settings.BuildRatio .. maxB )
	--Engine.debug("[CORE_EXPAND_BASE] " .. (1 / total * curB));
	
	local defense = ""
	if (total > maxBaseSize) then
		return true -- done!
	end
	-- first see if we need to build a building
	if ((curB / total <= Settings.BuildRatio) and (curB < maxB)) then
		-- should build a building!
		-- but what?	
		-- Engine.debug("[CORE_EXPAND_BASE] Should be building...") -- IZ WORKING

		local totalBuildings = Bot.countActors(player, Settings.Buildings) -- expects tables as in tbl["actorHere"] = {}
		for k,v in pairs(Settings.Buildings) do
		
			local limit = Settings.BuildingLimits[k]
			if (limit == nil) then
				limit = 0
			end
			
			local cnt = player:countActors(k)
			if (limit == 0 or cnt < limit) then
				--Engine.debug("2 Checking " .. k)
				-- Engine.debug(k .. ": " .. cnt .. " < " ..  (v * totalBuildings))
				-- its a valid actor so far
				if (cnt <= v * totalBuildings and Engine.canProduce(k)) then
					-- found something !
					actors[#actors + 1] = k
					S_PRODUCE(k)
					total = total + 1
					break
				end
			end
		end
	end
	
	if (total <= maxBaseSize) then			
		if ((1 / total * curD < defRatio) and (curD < maxD)) then
			-- should build a building!
			-- but what?	
			-- Engine.debug("[CORE_EXPAND_BASE] Should be building defenses...")

			
			-- Get the totals (per type: Defense, AADefense and AdvDefense
			local totalDef = Bot.countActors(player, Settings.Defenses)
			local totalAdvDef = Bot.countActors(player, Settings.AdvDefenses)
			local totalAADef = Bot.countActors(player, Settings.AADefenses)		
			local totalAll = totalDef + totalAdvDef + totalAADef
			totalAll = totalAll + 1 -- hax
			
			-- first check 'Defense'
			if (totalDef < Settings.DefenseLimit and totalDef < totalAll * Settings.DefenseRatio+1) then
				for k,v in pairs(Settings.Defenses) do	
					limit = Settings.DefenseLimit * v				
					local cnt = player:countActors(k)
					
					if (cnt < limit) then
						if (cnt <= v * totalDef and Engine.canProduce(k)) then
							-- found something !
							actors[#actors + 1] = k
							defense = k
							S_PRODUCE(k)
							break
						end
					end
				end
			end
			
			-- then check 'anti air'
			if (defense == "" and totalAADef < Settings.AADefenseLimit and totalAADef < totalAll * Settings.AADefenseRatio+1) then
				for k,v in pairs(Settings.AADefenses) do	
					limit = Settings.AADefenseLimit * v				
					local cnt = player:countActors(k)
					
					if (cnt < limit) then
						if (cnt <= v * totalAADef and Engine.canProduce(k)) then
							-- found something !
							actors[#actors + 1] = k
							defense = k
							S_PRODUCE(k)
							break
						end
					end
				end
			end
			
			-- then check advanced
			if (defense == "" and totalAADef < Settings.AdvDefenseLimit and totalAdvDef < totalAll * Settings.AdvDefenseRatio+1) then
				for k,v in pairs(Settings.AdvDefenses) do	
					limit = Settings.AdvDefenseLimit * v				
					local cnt = player:countActors(k)
					
					if (cnt < limit) then
						if (cnt <= v * totalAdvDef and Engine.canProduce(k)) then
							-- found something !
							actors[#actors + 1] = k
							defense = k
							S_PRODUCE(k)
							break
						end
					end
				end
			end
		end
	end

	-- aaand again, if no defenses were built!
	if (curB + curD < maxBaseSize and defense == "") then
		-- should build a building!
		-- but what?	
		-- Engine.debug("[CORE_EXPAND_BASE] Should be building...") -- IZ WORKING

		local totalBuildings = Bot.countActors(player, Settings.Buildings) -- expects tables as in tbl["actorHere"] = {}
		for k,v in pairs(Settings.Buildings) do
		
			local limit = Settings.BuildingLimits[k]
			if (limit == nil) then
				limit = 0
			end
			
			local cnt = player:countActors(k)
			if (limit == 0 or cnt < limit) then
				--Engine.debug("2 Checking " .. k)
				-- Engine.debug(k .. ": " .. cnt .. " < " ..  (v * totalBuildings))
				-- its a valid actor so far
				if (cnt <= v * totalBuildings and Engine.canProduce(k)) then
					-- found something !
					actors[#actors + 1] = k
					S_PRODUCE(k)
					total = total + 1
					break
				end
			end
		end
	end
	if (#actors > 0) then
		S_WAIT_FOR_ACTORS(actors)
	end
	
	END_SCRIPT()
	
	return true
end

