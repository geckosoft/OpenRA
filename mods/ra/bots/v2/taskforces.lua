-- taskforces.lua
-- Exposes the 'TF' (taskforces) namespace
TF = {}



TaskForce = {}
TaskForce.ID = ""
TaskForce.Actors = {}

function TaskForce.Load(path)
	local yaml = MiniYaml.open(path)
	
	if (not yaml:isLoaded()) then
		Engine.debug("Could not load yaml: " .. path)
		return false
	end
	Engine.debug("Loading " .. path)
	
	local fields = yaml:getFields()
	
	-- first loop will result in index, 'trigger id'
	for key,entry in ipairs(fields) do 
		local taskforce = botlib.clone(TaskForce)
		taskforce.ID = entry:getKey()
		local fields2 = entry:getFields()
		
		for key2,entry2 in ipairs(fields2) do 
			taskforce.Actors[#taskforce.Actors + 1] = {entry2:getKey(), tonumber(entry2:getValue())}
		end
		
		-- Store the trigger
		TF[taskforce.ID] = taskforce
		Engine.debug("TaskForce loaded: " .. taskforce.ID)
	end	
end
