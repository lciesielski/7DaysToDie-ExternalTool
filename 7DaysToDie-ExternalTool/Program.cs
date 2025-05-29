using Reloaded.Memory;
using System.Diagnostics;

class Program
{
	const string targetProcessName = "7DaysToDie";
	const string TargetModuleName = "UnityPlayer.dll";

	class Offsets
	{
		public class GameObjectManager
		{
			public static UIntPtr lastTaggedObject = 0x0;
			public static UIntPtr taggedObject = 0x8;
			public static UIntPtr lastActiveObject = 0x20;
			public static UIntPtr activeObject = 0x28;
		};

		public class ObjectManagerNode
		{
			public static UIntPtr prevLink = 0x0;
			public static UIntPtr nextLink = 0x8;
			public static UIntPtr gameObject = 0x10;
		};

		public class GameObject
		{
			public const UIntPtr Components = 0x30;
			public const UIntPtr Name = 0x60;
		};

		public const UIntPtr GameObjectManagerFromUnityPlayer = 0x1CFD6C8;

		public const UIntPtr secondListObject = 0x18;
		public const UIntPtr gameManager = 0x28;

		public const UIntPtr mWorld = 0x58;

		public const UIntPtr myEntityPlayerLocal = 0x60;

		public class EntityPlayerLocal
		{
			public const UIntPtr isFlyMode = 0x68;
			public const UIntPtr isGodMode = 0x70;
			public const UIntPtr entityStats = 0x5F0;
		}

		public class EntityStats 
		{
			public const UIntPtr Health = 0x10;
			public const UIntPtr Stamina = 0x18;
			public const UIntPtr Temp = 0x20;
			public const UIntPtr Water = 0x28;
			public const UIntPtr Food = 0x30;
		}

		public class EntityStat
		{
			public const UIntPtr mBaseMax = 0x20;
			public const UIntPtr mOriginalBaseMax = 0x24;
			public const UIntPtr mValue = 0x2C;
			public const UIntPtr mOriginalValue = 0x30;
			public const UIntPtr mLastValue = 0x50;
		}

		public const UIntPtr internalValue = 0x28;
	}

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

		UIntPtr myEntityPlayerLocalAddress = gameManagerStruct + Offsets.myEntityPlayerLocal;
		externalMemory.Read(myEntityPlayerLocalAddress, out UIntPtr myEntityPlayerLocal);

		Console.WriteLine($"myEntityPlayerLocal {myEntityPlayerLocal:X2}");

		// Set God Mode & Fly Mode

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

		//externalMemory.Write(isFlyMode, true);
		//externalMemory.Write(isGodMode, true);

		externalMemory.Read(isFlyMode, out isFlyModeValue);
		externalMemory.Read(isGodMode, out isGodModeValue);

		Console.WriteLine($"isFlyMode after: {isFlyModeValue}");
		Console.WriteLine($"isGodMode after: {isFlyModeValue}");

		// Overwrite stats

		externalMemory.Read(myEntityPlayerLocal + Offsets.EntityPlayerLocal.entityStats, out UIntPtr statsAddress);

		externalMemory.Read(statsAddress + Offsets.EntityStats.Health, out UIntPtr healthAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Stamina, out UIntPtr staminaAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Water, out UIntPtr waterAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Temp, out UIntPtr tempAddress);
		externalMemory.Read(statsAddress + Offsets.EntityStats.Food, out UIntPtr foodAddress);

		for (;;)
		{
			WriteStat(healthAddress, externalMemory);
			WriteStat(staminaAddress, externalMemory);
			WriteStat(waterAddress, externalMemory);
			WriteStat(tempAddress, externalMemory);
			WriteStat(foodAddress, externalMemory);
			Thread.Sleep(500);
		}
#else
		Console.WriteLine("This tool is only supported on Windows.");
#endif
	}

	private static void WriteStat(UIntPtr statAddress, ExternalMemory externalMemory)
	{

		externalMemory.Write(statAddress + Offsets.EntityStat.mValue, (float)100);
		externalMemory.Write(statAddress + Offsets.EntityStat.mLastValue, (float)100);
		externalMemory.Write(statAddress + Offsets.EntityStat.mOriginalValue, (float)100);
		externalMemory.Write(statAddress + Offsets.EntityStat.mOriginalBaseMax, (float)100);
		externalMemory.Write(statAddress + Offsets.EntityStat.mBaseMax, (float)100);
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
		Console.WriteLine($"ActiveObjectNode {activeObjectNode:X2}");

		externalMemory.Read(activeObjectNode, out UIntPtr gameObject);
		gameObject += Offsets.ObjectManagerNode.gameObject;
		Console.WriteLine($"GameObject {gameObject:X2}");

		ReadObjectName(gameObject, externalMemory);

		return activeObjectNode;
	}

	private static UIntPtr GetLastActiveObjectNode(UIntPtr gameObjectManager, ExternalMemory externalMemory)
	{
		externalMemory.Read(gameObjectManager, out UIntPtr lastActiveObjectNode);
		lastActiveObjectNode += Offsets.GameObjectManager.lastActiveObject;
		Console.WriteLine($"LastActiveObjectNode {lastActiveObjectNode:X2}");

		externalMemory.Read(lastActiveObjectNode, out UIntPtr gameObject);
		gameObject += Offsets.ObjectManagerNode.gameObject;
		Console.WriteLine($"GameObject {gameObject:X2}");

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