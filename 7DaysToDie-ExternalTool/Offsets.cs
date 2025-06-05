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

	public const UIntPtr secondGameObjectPointer = 0x18;
	public const UIntPtr gameManager = 0x28;

	public class GameManager
	{
		public const UIntPtr mWorld = 0x58;
		public const UIntPtr myEntityPlayerLocal = 0x60;
	}

	public class World 
	{
		public const UIntPtr entityAlives = 0x48;
		public const UIntPtr worldTime = 0x1B8;
	}

	public class Inventory 
	{
		public const UIntPtr slots = 0x20;
		public const UIntPtr mHoldingItemIndex = 0xD4;
	}

	public class  ItemInventoryData 
	{
		public const UIntPtr itemStack = 0x18;
	}

	public class  ItemStack 
	{
		public const UIntPtr itemValue = 0x10;
	}

	public class  ItemValue 
	{
		public const UIntPtr itemType = 0x28;
		public const UIntPtr useTimes = 0x30;
		public const UIntPtr quality = 0x38;
	}

	public const UIntPtr actionData = 0x38;

	public class Entity
	{
		public const UIntPtr isFlyMode = 0x68;
		public const UIntPtr isGodMode = 0x70;
		public const UIntPtr belongsPlayerId = 0x270;
	}

	public class EntityAlive
	{
		public const UIntPtr entityName = 0x4C8;
		public const UIntPtr ExperienceValue = 0x7B4;
		public const UIntPtr inventory = 0x490;
		public const UIntPtr entityStats = 0x5F0;
		public const UIntPtr entityBuffs = 0x5F8;
		public const UIntPtr killedPlayers = 0x7D8;
		public const UIntPtr progression = 0x600;
	}

	public class Progression
	{
		//Static
		public const UIntPtr ExpMultiplier = 0x08; //System.Single
		public const UIntPtr MaxLevel = 0x0C;
		public const UIntPtr SkillPointsPerLevel = 0x10;
		public const UIntPtr SkillPointMultiplier = 0x14; //System.Single
		//Dynamic
		public const UIntPtr ExpToNextLevel = 0x44;
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
	public const UIntPtr activeBuffs = 0x18;

	public class BuffValue
	{
		public const UIntPtr buffName = 0x18;
		public const UIntPtr buffFlags = 0x2C;
		public const UIntPtr instigatorId = 0x28;
	}

	public class StringStruct
	{
		public const UIntPtr mLength = 0x10;
		public const UIntPtr mValue = 0x14;
	}

	public class StaticData
	{
		public const UIntPtr VTable = 0x0;
		public const UIntPtr MonoClass= 0x0;
		public const UIntPtr VTableSize = 0x5C;
		public const UIntPtr Fields = 0x48;
	}

	public class ListGeneric 
	{
		public const UIntPtr items = 0x10;
		public const UIntPtr size = 0x18;
		public const int arrayOffset = 0x20;
		public const int nextElement = 0x08;

		public static UIntPtr GetNextElementAddress(UIntPtr listBase, int item = 0)
		{
			UIntPtr nextElementAddress = listBase + arrayOffset;
			if (item > 0) 
			{
				nextElementAddress += (UIntPtr)(item * nextElement);
			}
			return nextElementAddress;
		}
	}

	[Flags]
	public enum BuffFlags : byte
	{
		None = 0,
		Started = 1,
		Finished = 2,
		Remove = 4,
		Update = 8,
		Invalid = 16,
		Paused = 32
	}
}