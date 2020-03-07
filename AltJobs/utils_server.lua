ESX = nil
TriggerEvent( "esx:getSharedObject", function(obj) ESX = obj end)

local file_contents = {}

local function GetContentsOfFile(filename)

    if file_contents[filename] == nil then
        file_contents[filename] = LoadResourceFile(GetCurrentResourceName(), filename)
    end
    return file_contents[filename]

end

RegisterServerEvent("arp_alt_jobs:server:add_money")
AddEventHandler("arp_alt_jobs:server:add_money", function(amount) 

    if ESX == nil then
        Citizen.Trace("arp_alt_jobs:server:add_money has nil ESX object")
        return
    end

    local player = ESX.GetPlayerFromId(source)
    player.addMoney(amount)

end)

RegisterServerEvent("arp_alt_jobs:server:GetMorgueBlob")
AddEventHandler("arp_alt_jobs:server:GetMorgueBlob", function() 

    TriggerClientEvent("arp_alt_jobs:client:GiveMorgueBlob", source, GetContentsOfFile("organ_trafficking_config.json"))

end)