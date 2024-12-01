using BepInEx;
using ExamplePlugin;
using R2API;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PlsNoSlug
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class PlsNoSlug : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "ggeezzll";
        public const string PluginName = "PlsNoSlug";
        public const string PluginVersion = "1.0.0";

        private ItemDef grey;
        private ItemDef green;
        private ItemDef red;
        private ItemDef yellow;
        private ItemDef purple1;
        private ItemDef purple2;
        private ItemDef purple3;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);

            // Hook into stage transitions
            // Subscribe to the stage start event
            SceneDirector.onPostPopulateSceneServer += OnStageStart;

            // Hook into item drop logic
            On.RoR2.PickupDropTable.GenerateDrop += OverrideItemDrop;

            // Hook into interactable card selection to modify chest spawns
            SceneDirector.onPrePopulateSceneServer += IncreaseInteractableCredits;
            SceneDirector.onGenerateInteractableCardSelection += TripleChestSpawnChances;

            Log.Info($"{PluginName} initialized!");
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            SceneDirector.onPostPopulateSceneServer -= OnStageStart;
            SceneDirector.onGenerateInteractableCardSelection -= TripleChestSpawnChances;
        }

        private void OnStageStart(SceneDirector sceneDirector)
        {
            // Your logic when a stage starts
            grey = GetRandomItemOfTier(ItemTier.Tier1);
            green = GetRandomItemOfTier(ItemTier.Tier2);
            red = GetRandomItemOfTier(ItemTier.Tier3);
            yellow = GetRandomItemOfTier(ItemTier.Boss);
            purple1 = GetRandomItemOfTier(ItemTier.VoidTier1);
            purple2 = GetRandomItemOfTier(ItemTier.VoidTier2);
            purple3 = GetRandomItemOfTier(ItemTier.VoidTier3);

            Log.Info("Stage setup complete:");
            Log.Info($"  - Tier 1  -> {grey?.name ?? "None"}");
            Log.Info($"  - Tier 2  -> {green?.name ?? "None"}");
            Log.Info($"  - Tier 3  -> {red?.name ?? "None"}");
            Log.Info($"  - Boss    -> {yellow?.name ?? "None"}");
            Log.Info($"  - Void 1  -> {purple1?.name ?? "None"}");
            Log.Info($"  - Void 2  -> {purple2?.name ?? "None"}");
            Log.Info($"  - Void 3  -> {purple3?.name ?? "None"}");

            // Broadcast the item pool to all players in chat
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"Stage {Run.instance.stageClearCount + 1}: Item Pool:"
            });

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"  - Common (Tier 1): {GetDisplayName(grey)}"
            });

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"  - Uncommon (Tier 2): {GetDisplayName(green)}"
            });

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"  - Rare (Tier 3): {GetDisplayName(red)}"
            });

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"  - Boss: {GetDisplayName(yellow)}"
            });
        }

        string GetDisplayName(ItemDef item)
        {
            return item != null ? Language.GetString(item.nameToken) : "None";
        }

        private void TripleChestSpawnChances(SceneDirector sceneDirector, DirectorCardCategorySelection cardSelection)
        {
            foreach (var category in cardSelection.categories)
            {
                foreach (var card in category.cards)
                {
                    if (card.spawnCard && card.spawnCard.name.Contains("Chest"))
                    {
                        card.selectionWeight *= 5;
                        Log.Debug($"Triple spawn weight for chest: {card.spawnCard.name}");
                    }
                }
            }

            Log.Info("Chest spawn chances doubled.");
        }

        private ItemDef GetRandomItemOfTier(ItemTier tier)
        {
            var itemsOfTier = ItemCatalog.allItems
                .Where(itemIndex =>
                {
                    var itemDef = ItemCatalog.GetItemDef(itemIndex);
                    return itemDef != null && 
                    itemDef.tier == tier && 
                    !itemDef.hidden && 
                    Run.instance.availableItems.Contains(itemIndex) &&
                   !itemDef.tags.Contains(ItemTag.Scrap);
                })
                .ToList();

            if (itemsOfTier.Count > 0)
            {
                var randomItemIndex = itemsOfTier[Random.Range(0, itemsOfTier.Count)];
                return ItemCatalog.GetItemDef(randomItemIndex);
            }

            Log.Info($"No items found for tier {tier}");
            return null;
        }
        private void IncreaseInteractableCredits(SceneDirector sceneDirector)
        {
            sceneDirector.interactableCredit *= 2; 
            Log.Info($"Increased interactable credits for more chests.");
        }

        private PickupIndex OverrideItemDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
        {
            var originalDrop = orig(self, rng);

            // Check if it's an item drop
            var pickupDef = PickupCatalog.GetPickupDef(originalDrop);
            if (pickupDef != null && pickupDef.itemIndex != ItemIndex.None)
            {
                var itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);

                // Replace based on the tier
                if (itemDef.tier == ItemTier.Tier1 && grey != null)
                {
                    return PickupCatalog.FindPickupIndex(grey.itemIndex);
                }
                if (itemDef.tier == ItemTier.Tier2 && green != null)
                {
                    return PickupCatalog.FindPickupIndex(green.itemIndex);
                }
                if (itemDef.tier == ItemTier.Tier3 && red != null)
                {
                    return PickupCatalog.FindPickupIndex(red.itemIndex);
                }
                if (itemDef.tier == ItemTier.Boss && yellow != null)
                {
                    return PickupCatalog.FindPickupIndex(yellow.itemIndex);
                }
                if (itemDef.tier == ItemTier.VoidTier1 && purple1 != null)
                {
                    return PickupCatalog.FindPickupIndex(purple1.itemIndex);
                }
                if (itemDef.tier == ItemTier.VoidTier2 && purple2 != null)
                {
                    return PickupCatalog.FindPickupIndex(purple2.itemIndex);
                }
                if (itemDef.tier == ItemTier.VoidTier3 && purple3 != null)
                {
                    return PickupCatalog.FindPickupIndex(purple3.itemIndex);
                }
            }
            return originalDrop;
        }

        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
            }
        }
    }
}
