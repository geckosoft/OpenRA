 
function Data_Init() 

	-- Just to keep some stuff seperate from the AI core
	Data = {}

	Data.BuildingFractions = {} -- How many '%' of all building should be these

	-- Buildings (0.05 => 5% of all buildings (defense / regular) should be this)
	Data.BuildingFractions["powr"] = 0.1
	Data.BuildingFractions["proc"] = 0.2
	Data.BuildingFractions["barr"] = 0.05
	Data.BuildingFractions["tent"] = 0.05
	Data.BuildingFractions["weap"] = 0.05
	Data.BuildingFractions["atek"] = 0.01
	Data.BuildingFractions["stek"] = 0.01
	Data.BuildingFractions["silo"] = 0.05
	Data.BuildingFractions["fix"] = 0.005
	Data.BuildingFractions["dome"] = 0.01	
	
	-- Defenses (0.05 => 5% of all buildings (defense / regular) should be this)
	Data.BuildingFractions["pbox"] = 0.01
	Data.BuildingFractions["hbox"] = 0.01
	Data.BuildingFractions["gun"] = 0.01
	Data.BuildingFractions["tsla"] = 0.01
	Data.BuildingFractions["ftur"] = 0.01
	Data.BuildingFractions["agun"] = 0.005
	Data.BuildingFractions["sam"] = 0.005
end
 