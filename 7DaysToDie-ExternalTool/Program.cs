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
		UIntPtr gameManagerAddress = GetGameManager(gameManagerGameObject, externalMemory);

		// Local Player

		UIntPtr myEntityPlayerLocalPointer = gameManagerAddress + Offsets.GameManager.myEntityPlayerLocal;
		externalMemory.Read(myEntityPlayerLocalPointer, out UIntPtr myEntityPlayerLocalAddress);
		Console.WriteLine($"myEntityPlayerLocalAddress {myEntityPlayerLocalAddress:X2}");

		// Mod Item Quality & Durability

		externalMemory.Read(myEntityPlayerLocalAddress + Offsets.EntityAlive.inventory, out UIntPtr inventoryAddress);
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
			externalMemory.Write(itemValue + Offsets.ItemValue.quality, (ushort) 6);
		}

		// Get player Id

		externalMemory.Read(myEntityPlayerLocalAddress + Offsets.Entity.belongsPlayerId, out int playerId);

		// Progression modifications

		externalMemory.Read(myEntityPlayerLocalAddress + Offsets.EntityAlive.progression, out UIntPtr progressionAddress);
		UIntPtr progressionStaticFieldsAddress = GetStaticFieldsAddress(progressionAddress, externalMemory);

		externalMemory.Write(progressionStaticFieldsAddress + Offsets.Progression.MaxLevel, 500);
		externalMemory.Write(progressionStaticFieldsAddress + Offsets.Progression.SkillPointsPerLevel, 10);
		externalMemory.Write(progressionAddress + Offsets.Progression.ExpToNextLevel, 1);

		// Entities list

		UIntPtr worldPointer = gameManagerAddress + Offsets.GameManager.mWorld;
		externalMemory.Read(worldPointer, out UIntPtr worldAddress);

		UIntPtr entityAlivesAddress = worldAddress + Offsets.World.entityAlives;
		externalMemory.Read(entityAlivesAddress, out UIntPtr entityAlivesListBase);

		externalMemory.Read(entityAlivesListBase + Offsets.ListGeneric.items, out UIntPtr entitiesAliveListItems);
		externalMemory.Read(entityAlivesListBase + Offsets.ListGeneric.size, out int entitiesAliveListSize);

		Console.WriteLine("-------------------------------------------------------------------");
		Console.WriteLine("Entity");

		for (int i = 0; i < entitiesAliveListSize; i++)
		{
			UIntPtr listItemAddress = Offsets.ListGeneric.GetNextElementAddress(entitiesAliveListItems, i);
			externalMemory.Read(listItemAddress, out UIntPtr entityAlive);

			externalMemory.Read(entityAlive + Offsets.EntityAlive.entityName, out UIntPtr entityNameAddress);
			externalMemory.Read(entityNameAddress + Offsets.StringStruct.mLength, out int entityNameLength);
			byte[] entityNameBuffer = new byte[entityNameLength * 2];
			externalMemory.ReadRaw(entityNameAddress + Offsets.StringStruct.mValue, entityNameBuffer);
			string entityNameValue = System.Text.Encoding.Unicode.GetString(entityNameBuffer);

			externalMemory.Write(entityAlive + Offsets.EntityAlive.ExperienceValue, 1000);
			externalMemory.Read(entityAlive + Offsets.EntityAlive.entityStats, out UIntPtr entityStats);
			externalMemory.Read(entityStats + Offsets.EntityStats.Health, out UIntPtr entityHealth);
			externalMemory.Read(entityHealth + Offsets.EntityStat.mValue, out float entityHealthValue);
			if (entityHealthValue > 0)
			{
				Console.WriteLine($"Entity is alive {entityNameValue}");
				WriteStat(entityHealth, externalMemory, 1.0f);
			}
			else
			{
				Console.WriteLine($"Entity is dead {entityNameValue}");
			}
		}

		Console.WriteLine("-------------------------------------------------------------------");

		// Set World Time
		// 0 = Day 1 00:00
		// 12000 = Day 1 12:00
		// 24000 = Day 2 0:00

		externalMemory.Read(worldAddress + Offsets.World.worldTime, out int worldTime);
		Console.WriteLine($"World Time before: {worldTime}");
		externalMemory.Write(worldAddress + Offsets.World.worldTime, 240006000);

		// Set God Mode & Fly Mode

		/*
		UIntPtr isFlyModeAddress = myEntityPlayerLocal + Offsets.EntityPlayerLocal.isFlyMode;
		UIntPtr isGodModeAddress = myEntityPlayerLocal + Offsets.EntityPlayerLocal.isGodMode;

		externalMemory.Read(isFlyModeAddress, out UIntPtr isFlyMode);
		externalMemory.Read(isGodModeAddress, out UIntPtr isGodMode);

		isFlyMode += Offsets.internalValue;
		isGodMode += Offsets.internalValue;

		externalMemory.Write(isFlyMode, true);
		externalMemory.Write(isGodMode, true);
		*/

		// Manage Buffs (eg. Cure Infection)

		externalMemory.Read(myEntityPlayerLocalAddress + Offsets.EntityAlive.entityBuffs, out UIntPtr buffsAddress);
		externalMemory.Read(buffsAddress + Offsets.activeBuffs, out UIntPtr buffValuesListBase);
		externalMemory.Read(buffValuesListBase + Offsets.ListGeneric.items, out UIntPtr buffValuesListItems);
		externalMemory.Read(buffValuesListBase + Offsets.ListGeneric.size, out int buffValuesListSize);

		for (int i = 0; i < buffValuesListSize; i++)
		{
			UIntPtr listItemAddress = Offsets.ListGeneric.GetNextElementAddress(buffValuesListItems, i);

			externalMemory.Read(listItemAddress, out UIntPtr buffValue);
			externalMemory.Read(buffValue + Offsets.BuffValue.buffName, out UIntPtr buffNameAddress);
			externalMemory.Read(buffNameAddress + Offsets.StringStruct.mLength, out int buffNameLength);

			byte[] buffNameBuffer = new byte[buffNameLength * 2];
			externalMemory.ReadRaw(buffNameAddress + Offsets.StringStruct.mValue, buffNameBuffer);

			string buffNameValue = System.Text.Encoding.Unicode.GetString(buffNameBuffer);
			externalMemory.Read(buffValue + Offsets.BuffValue.instigatorId, out int instigatorId);
			externalMemory.Read(buffValue + Offsets.BuffValue.buffFlags, out byte buffFlags);

			if (instigatorId != playerId)
			{ 
				Console.WriteLine($"Removing buff {buffNameValue} with instigatorId {instigatorId} (not equal to playerId {playerId})");
				externalMemory.Write(buffValue + Offsets.BuffValue.buffFlags, Offsets.BuffFlags.Finished);
			}
		}

		// Overwrite stats

		externalMemory.Read(myEntityPlayerLocalAddress + Offsets.EntityAlive.entityStats, out UIntPtr statsAddress);

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

	private static UIntPtr GetStaticFieldsAddress(UIntPtr progressionAddress, ExternalMemory externalMemory)
	{
		externalMemory.Read(progressionAddress + Offsets.StaticData.VTable, out UIntPtr vTableAddress);
		externalMemory.Read(vTableAddress + Offsets.StaticData.MonoClass, out UIntPtr monoClassAddress);
		externalMemory.Read(monoClassAddress + Offsets.StaticData.VTableSize, out int vTableSize);
		externalMemory.Read(vTableAddress + Offsets.StaticData.Fields + (UIntPtr)(vTableSize * UIntPtr.Size), out UIntPtr staticFieldsAddress);

		return staticFieldsAddress;
	}

	private static UIntPtr GetGameManager(UIntPtr gameManagerGameObject, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameManagerGameObject, out UIntPtr gameManagerGameObjecComponents);
		gameManagerGameObjecComponents += Offsets.GameObject.Components;

		externalMemory.Read(gameManagerGameObjecComponents, out UIntPtr secondComponent);
		secondComponent += Offsets.secondGameObjectPointer;

		externalMemory.Read(secondComponent, out UIntPtr gameManager);
		gameManager += Offsets.gameManager;

		externalMemory.Read(gameManager, out UIntPtr gameManagerAddress);
		Console.WriteLine($"GameManagerAddress: {gameManagerAddress:X2}");

		return gameManagerAddress;
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

		// Not found ? Inccrease loop counter
		for (int i = 0; i < 10; i++)
		{
			externalMemory.Read(objectNodeIter, out UIntPtr nextObjectNodeAddress);
			nextObjectNodeAddress += Offsets.ObjectManagerNode.nextLink;
			externalMemory.Read(nextObjectNodeAddress, out UIntPtr nextObjectNode);

			UIntPtr gameObject = nextObjectNode + Offsets.ObjectManagerNode.gameObject;
			string name = ReadObjectName(gameObject, externalMemory);

			if (name.Trim().Equals("gamemanager", StringComparison.CurrentCultureIgnoreCase)) 
			{
				Console.WriteLine($"Found GameManager GameObject: {gameObject:X2}");
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

		externalMemory.Read(activeObjectNode, out UIntPtr gameObject);
		gameObject += Offsets.ObjectManagerNode.gameObject;

		ReadObjectName(gameObject, externalMemory);

		return activeObjectNode;
	}

	private static UIntPtr GetLastActiveObjectNode(UIntPtr gameObjectManager, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameObjectManager, out UIntPtr lastActiveObjectNode);
		lastActiveObjectNode += Offsets.GameObjectManager.lastActiveObject;

		externalMemory.Read(lastActiveObjectNode, out UIntPtr gameObject);
		gameObject += Offsets.ObjectManagerNode.gameObject;

		ReadObjectName(gameObject, externalMemory);

		return lastActiveObjectNode;
	}

	private static string ReadObjectName(UIntPtr gameObject, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameObject, out UIntPtr gameObjectName);
		gameObjectName += Offsets.GameObject.Name;
		externalMemory.Read(gameObjectName, out UIntPtr gameObjectNameValueAddress);

		byte[] nameBuffer = new byte[128];
		externalMemory.ReadRaw(gameObjectNameValueAddress, nameBuffer);

		int actualLength = Array.IndexOf(nameBuffer, (byte)0);
		if (actualLength == -1)
		{
			actualLength = nameBuffer.Length;
		}

		string gameObjectNameValue = System.Text.Encoding.UTF8.GetString(nameBuffer, 0, actualLength);

		return gameObjectNameValue;
	}

	private static UIntPtr GetGameObjectManager(ProcessModule unityPlayerModule, ExternalMemory externalMemory)
	{
		UIntPtr unityPlayerBase = (UIntPtr)unityPlayerModule.BaseAddress;
		return unityPlayerBase + Offsets.GameObjectManagerFromUnityPlayer;
	}

	static void DumpBytes(byte[] bytes)
	{
		string hex = BitConverter.ToString(bytes).Replace('-', ' ');
		Console.WriteLine(hex);
	}
}