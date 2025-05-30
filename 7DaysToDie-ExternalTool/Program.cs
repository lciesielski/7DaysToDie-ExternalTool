using Reloaded.Memory;
using System.Diagnostics;

class Program
{
	const string targetProcessName = "7DaysToDie";
	const string TargetModuleName = "UnityPlayer.dll";

	static void Main(string[] args)
	{
#if WINDOWS
		Process targetProcess = Process.GetProcessesByName(targetProcessName)[0];
		ExternalMemory externalMemory = new ExternalMemory(targetProcess);

		UIntPtr baseAddress = (UIntPtr)targetProcess.MainModule.BaseAddress;
		Console.WriteLine($"baseAddress {baseAddress:X2}");

		ProcessModule unityPlayerModule = targetProcess.Modules.Cast<ProcessModule>()
			.FirstOrDefault(m => m.ModuleName.Equals(TargetModuleName, StringComparison.OrdinalIgnoreCase));

		UIntPtr gameObjectManager = GetGameObjectManager(unityPlayerModule, externalMemory);
		UIntPtr lastActiveObjectNode = GetLastActiveObjectNode(gameObjectManager, externalMemory);
		UIntPtr activeObjectNode = GetActiveObjectNode(gameObjectManager, externalMemory);
		UIntPtr gameManagerGameObject = GetGameManagerGameObject(activeObjectNode, externalMemory);

		externalMemory.Read(gameManagerGameObject, out UIntPtr gameManagerGameObjecComponents);
		gameManagerGameObjecComponents += Offsets.GameObject.Components;

		externalMemory.Read(gameManagerGameObjecComponents, out UIntPtr secondComponent);
		secondComponent += Offsets.secondListObject;

		externalMemory.Read(secondComponent, out UIntPtr gameManager);
		gameManager += Offsets.gameManager;

		externalMemory.Read(gameManager, out UIntPtr gameManagerStruct);
		Console.WriteLine($"GameManagerStruct: {gameManagerStruct:X2}");
		UIntPtr gamePaused = gameManagerStruct + 0x0230;
		externalMemory.Read(gamePaused, out bool gamePausedBytes);
		Console.WriteLine($"GamePaused: {gamePausedBytes}");

		// Local Player

		UIntPtr myEntityPlayerLocalAddress = gameManagerStruct + Offsets.GameManager.myEntityPlayerLocal;
		externalMemory.Read(myEntityPlayerLocalAddress, out UIntPtr myEntityPlayerLocal);
		Console.WriteLine($"myEntityPlayerLocal {myEntityPlayerLocal:X2}");

		// Mod Item Durability

		externalMemory.Read(myEntityPlayerLocal + Offsets.EntityAlive.inventory, out UIntPtr inventoryAddress);
		externalMemory.Read(inventoryAddress + Offsets.Inventory.mHoldingItemIndex, out int holdingItemIndex);
		externalMemory.Read(inventoryAddress + Offsets.Inventory.slots, out UIntPtr slotsArray);
		UIntPtr holdingItemInventoryDataAddress = Offsets.ListGeneric.GetNextElementAddress(slotsArray, holdingItemIndex);
		externalMemory.Read(holdingItemInventoryDataAddress, out UIntPtr holdingItemInventoryData);
		externalMemory.Read(holdingItemInventoryData + Offsets.ItemInventoryData.itemStack, out UIntPtr itemStack);
		externalMemory.Read(itemStack + Offsets.ItemStack.itemValue, out UIntPtr itemValue);
		externalMemory.Read(itemValue + Offsets.ItemValue.itemType, out int itemType);
		if (itemType != 0) 
		{
			externalMemory.Write(itemValue + Offsets.ItemValue.useTimes, (float) 0);
			externalMemory.Read(itemValue + Offsets.ItemValue.quality, out ushort quality);
			Console.WriteLine($"ItemType: {itemType}, Quality: {quality}");
			externalMemory.Write(itemValue + Offsets.ItemValue.quality, (ushort) 300);
		}

		// Get player Id

		externalMemory.Read(myEntityPlayerLocal + Offsets.Entity.belongsPlayerId, out int playerId);
		Console.WriteLine($"PlayerId: {playerId}");

		externalMemory.Read(myEntityPlayerLocal + Offsets.EntityAlive.killedPlayers, out int killedPlayers);
		Console.WriteLine($"killedPlayers: {killedPlayers}");

		// Entities list

		UIntPtr mWorldAddress = gameManagerStruct + Offsets.GameManager.mWorld;
		externalMemory.Read(mWorldAddress, out UIntPtr mWorld);
		UIntPtr entityAlivesAddress = mWorld + Offsets.World.entityAlives;
		externalMemory.Read(entityAlivesAddress, out UIntPtr entityAlivesListBase);

		externalMemory.Read(entityAlivesListBase + Offsets.ListGeneric.items, out UIntPtr entitiesAliveListItems);
		Console.WriteLine($"entitiesAliveListItems: {entitiesAliveListItems:X2}");

		externalMemory.Read(entityAlivesListBase + Offsets.ListGeneric.size, out int entitiesAliveListSize);
		Console.WriteLine($"entitiesAliveListSize: {entitiesAliveListSize:X2}");

		Console.WriteLine("-------------------------------------------------------------------");
		Console.WriteLine("Entity");

		for (int i = 0; i < entitiesAliveListSize; i++)
		{
			UIntPtr listItemAddress = Offsets.ListGeneric.GetNextElementAddress(entitiesAliveListItems, i);
			Console.WriteLine($"listItemAddress {i}: {listItemAddress:X2}");

			externalMemory.Read(listItemAddress, out UIntPtr entityAlive);
			Console.WriteLine($"entityAlive: {entityAlive:X2}");

			externalMemory.Read(entityAlive + Offsets.EntityAlive.entityName, out UIntPtr entityNameAddress);
			externalMemory.Read(entityNameAddress + Offsets.StringStruct.mLength, out int entityNameLength);
			byte[] entityNameBuffer = new byte[entityNameLength * 2];
			externalMemory.ReadRaw(entityNameAddress + Offsets.StringStruct.mValue, entityNameBuffer);
			string entityNameValue = System.Text.Encoding.Unicode.GetString(entityNameBuffer);

			//externalMemory.Read(entityAlive + Offsets.EntityAlive.ExperienceValue, out int entityXPValue);
			//Console.WriteLine($"entityXPValue: {entityXPValue}");

			externalMemory.Write(entityAlive + Offsets.EntityAlive.ExperienceValue, 1000);
			externalMemory.Read(entityAlive + Offsets.EntityAlive.entityStats, out UIntPtr entityStats);
			externalMemory.Read(entityStats + Offsets.EntityStats.Health, out UIntPtr entityHealth);
			externalMemory.Read(entityHealth + Offsets.EntityStat.mValue, out float entityHealthValue);
			if (entityHealthValue > 0)
			{
				Console.WriteLine($"Zombie is alive {entityNameValue}");
				WriteStat(entityHealth, externalMemory, 1.0f);
			}
			else
			{
				Console.WriteLine($"Zombie is dead {entityNameValue}");
			}
		}

		Console.WriteLine("-------------------------------------------------------------------");

		// Set God Mode & Fly Mode

		/*
		UIntPtr isFlyModeAddress = myEntityPlayerLocal + Offsets.EntityPlayerLocal.isFlyMode;
		UIntPtr isGodModeAddress = myEntityPlayerLocal + Offsets.EntityPlayerLocal.isGodMode;

		externalMemory.Read(isFlyModeAddress, out UIntPtr isFlyMode);
		externalMemory.Read(isGodModeAddress, out UIntPtr isGodMode);

		isFlyMode += Offsets.internalValue;
		isGodMode += Offsets.internalValue;

		externalMemory.Read(isFlyMode, out bool isFlyModeValue);
		externalMemory.Read(isGodMode, out bool isGodModeValue);

		
		Console.WriteLine($"isFlyMode before: {isFlyModeValue}");
		Console.WriteLine($"isGodMode before: {isFlyModeValue}");

		externalMemory.Write(isFlyMode, true);
		externalMemory.Write(isGodMode, true);

		externalMemory.Read(isFlyMode, out isFlyModeValue);
		externalMemory.Read(isGodMode, out isGodModeValue);

		Console.WriteLine($"isFlyMode after: {isFlyModeValue}");
		Console.WriteLine($"isGodMode after: {isFlyModeValue}");
		*/

		// Manage Buffs (eg. Cure Infection)

		externalMemory.Read(myEntityPlayerLocal + Offsets.EntityAlive.entityBuffs, out UIntPtr buffsAddress);
		Console.WriteLine($"buffsAddress: {buffsAddress:X2}");

		externalMemory.Read(buffsAddress + Offsets.activeBuffs, out UIntPtr buffValuesListBase);
		Console.WriteLine($"buffValuesListBase: {buffValuesListBase:X2}");

		externalMemory.Read(buffValuesListBase + Offsets.ListGeneric.items, out UIntPtr buffValuesListItems);
		Console.WriteLine($"buffValuesListItems: {buffValuesListItems:X2}");

		externalMemory.Read(buffValuesListBase + Offsets.ListGeneric.size, out int buffValuesListSize);
		Console.WriteLine($"buffValuesListSize: {buffValuesListSize:X2}");

		for (int i = 0; i < buffValuesListSize; i++)
		{
			UIntPtr listItemAddress = Offsets.ListGeneric.GetNextElementAddress(buffValuesListItems, i);
			Console.WriteLine($"listItemAddress {i}: {listItemAddress:X2}");

			externalMemory.Read(listItemAddress, out UIntPtr buffValue);
			Console.WriteLine($"buffValue: {buffValue:X2}");

			externalMemory.Read(buffValue + Offsets.BuffValue.buffName, out UIntPtr buffNameAddress);
			externalMemory.Read(buffNameAddress + Offsets.StringStruct.mLength, out int buffNameLength);
			byte[] buffNameBuffer = new byte[buffNameLength * 2];
			externalMemory.ReadRaw(buffNameAddress + Offsets.StringStruct.mValue, buffNameBuffer);
			string buffNameValue = System.Text.Encoding.Unicode.GetString(buffNameBuffer);
			Console.WriteLine($"buffNameValue: {buffNameValue}");

			externalMemory.Read(buffValue + Offsets.BuffValue.instigatorId, out int instigatorId);
			Console.WriteLine($"instigatorId: {instigatorId}");

			externalMemory.Read(buffValue + Offsets.BuffValue.buffFlags, out byte buffFlags);
			Console.WriteLine($"buffFlags: {(Offsets.BuffFlags)buffFlags}");

			if (instigatorId != playerId)
			{ 
				Console.WriteLine($"Removing buff {buffNameValue} with instigatorId {instigatorId} (not equal to playerId {playerId})");
				externalMemory.Write(buffValue + Offsets.BuffValue.buffFlags, Offsets.BuffFlags.Finished);
			}
		}

		// Overwrite stats

		externalMemory.Read(myEntityPlayerLocal + Offsets.EntityAlive.entityStats, out UIntPtr statsAddress);

		externalMemory.Read(statsAddress + Offsets.EntityStats.Health, out UIntPtr healthAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Stamina, out UIntPtr staminaAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Water, out UIntPtr waterAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Temp, out UIntPtr tempAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Food, out UIntPtr foodAddress);

		WriteStat(healthAddress, externalMemory);
		WriteStat(staminaAddress, externalMemory);
		WriteStat(waterAddress, externalMemory);
		WriteStat(tempAddress, externalMemory, 70);
		WriteStat(foodAddress, externalMemory);

		/*
		for (;;)
		{
			Console.WriteLine("Writing stats...");
			WriteStat(healthAddress, externalMemory);
			WriteStat(staminaAddress, externalMemory);
			WriteStat(waterAddress, externalMemory);
			WriteStat(tempAddress, externalMemory, 70);
			WriteStat(foodAddress, externalMemory);
			Thread.Sleep(500);
		}
		*/
#else
		Console.WriteLine("This tool is only supported on Windows.");
#endif
	}

	private static void WriteStat(UIntPtr statAddress, ExternalMemory externalMemory, float value = 100)
	{

		externalMemory.Write(statAddress + Offsets.EntityStat.mValue, value);
		externalMemory.Write(statAddress + Offsets.EntityStat.mLastValue, value);
		externalMemory.Write(statAddress + Offsets.EntityStat.mOriginalValue, value);
		externalMemory.Write(statAddress + Offsets.EntityStat.mOriginalBaseMax, value);
		externalMemory.Write(statAddress + Offsets.EntityStat.mBaseMax, value);
	}

	private static UIntPtr GetGameManagerGameObject(UIntPtr activeObjectNode, ExternalMemory externalMemory)
	{
		UIntPtr objectNodeIter = activeObjectNode;

		for (int i = 0; i < 10; i++)
		{
			externalMemory.Read(objectNodeIter, out UIntPtr nextObjectNodeAddress);
			nextObjectNodeAddress += Offsets.ObjectManagerNode.nextLink;

			externalMemory.Read(nextObjectNodeAddress, out UIntPtr nextObjectNode);
			//Console.WriteLine($"NextObjectNode {nextObjectNode:X2}");

			UIntPtr gameObject = nextObjectNode + Offsets.ObjectManagerNode.gameObject;
			string name = ReadObjectName(gameObject, externalMemory);

			if (name.Trim().Equals("gamemanager", StringComparison.CurrentCultureIgnoreCase)) 
			//if (name.Trim().Equals("input", StringComparison.CurrentCultureIgnoreCase)) 
			{
				Console.WriteLine($"Found GameManager: {gameObject:X2}");
				return gameObject;
			}

			objectNodeIter = nextObjectNodeAddress;
		}

		return UIntPtr.Zero;
	}

	private static UIntPtr GetActiveObjectNode(UIntPtr gameObjectManager, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameObjectManager, out UIntPtr activeObjectNode);
		activeObjectNode += Offsets.GameObjectManager.activeObject;
		//Console.WriteLine($"ActiveObjectNode {activeObjectNode:X2}");

		externalMemory.Read(activeObjectNode, out UIntPtr gameObject);
		gameObject += Offsets.ObjectManagerNode.gameObject;
		//Console.WriteLine($"GameObject {gameObject:X2}");

		ReadObjectName(gameObject, externalMemory);

		return activeObjectNode;
	}

	private static UIntPtr GetLastActiveObjectNode(UIntPtr gameObjectManager, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameObjectManager, out UIntPtr lastActiveObjectNode);
		lastActiveObjectNode += Offsets.GameObjectManager.lastActiveObject;
		//Console.WriteLine($"LastActiveObjectNode {lastActiveObjectNode:X2}");

		externalMemory.Read(lastActiveObjectNode, out UIntPtr gameObject);
		gameObject += Offsets.ObjectManagerNode.gameObject;
		//Console.WriteLine($"GameObject {gameObject:X2}");

		ReadObjectName(gameObject, externalMemory);

		return lastActiveObjectNode;
	}

	private static string ReadObjectName(UIntPtr gameObject, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameObject, out UIntPtr gameObjectName);
		//Console.WriteLine($"GameObjectName {gameObjectName:X2}");

		gameObjectName += Offsets.GameObject.Name;
		//Console.WriteLine($"GameObjectName {gameObjectName:X2}");

		externalMemory.Read(gameObjectName, out UIntPtr gameObjectNameValueAddress);
		//Console.WriteLine($"GameObjectValueAddress {gameObjectNameValueAddress:X2}");

		byte[] nameBuffer = new byte[128];
		externalMemory.ReadRaw(gameObjectNameValueAddress, nameBuffer);
		//DumpBytes(nameBuffer);

		int actualLength = Array.IndexOf(nameBuffer, (byte)0);
		if (actualLength == -1)
		{
			actualLength = nameBuffer.Length;
		}

		string gameObjectNameValue = System.Text.Encoding.UTF8.GetString(nameBuffer, 0, actualLength);
		//Console.WriteLine($"gameObjectNameValue {gameObjectNameValue:X2}");

		return gameObjectNameValue;
	}

	private static UIntPtr GetGameObjectManager(ProcessModule unityPlayerModule, ExternalMemory externalMemory)
	{
		UIntPtr unityPlayerBase = (UIntPtr)unityPlayerModule.BaseAddress;
		Console.WriteLine($"unityPlayerBase {unityPlayerBase:X2}");

		UIntPtr gameObjectManager = unityPlayerBase + Offsets.GameObjectManagerFromUnityPlayer;
		Console.WriteLine($"GameObjectManager {gameObjectManager:X2}");

		return gameObjectManager;
	}

	static void DumpBytes(byte[] bytes)
	{
		string hex = BitConverter.ToString(bytes).Replace('-', ' ');
		Console.WriteLine(hex);
	}
}