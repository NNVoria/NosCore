﻿using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.Shop;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;

namespace NosCore.GameObject.Providers.UpgradeService
{
    public class UpgradeService: IUpgradeService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<UpgradePacketType,
            Func<ClientSession, InventoryItemInstance, InventoryItemInstance, InventoryItemInstance>> PacketsFunctions;

        public UpgradeService(ILogger logger)
        {
            _logger = logger;
            PacketsFunctions = new Dictionary<UpgradePacketType, Func<ClientSession, InventoryItemInstance, InventoryItemInstance, InventoryItemInstance>>
                {
                    { UpgradePacketType.SumResistance, new Func<ClientSession, InventoryItemInstance, InventoryItemInstance, InventoryItemInstance>(Sum) }
                };
        }

        public void HandlePacket(UpgradePacketType type, ClientSession clientSession, InventoryItemInstance item1, InventoryItemInstance item2)
        {
            PacketsFunctions[type].DynamicInvoke(clientSession, item1, item2);
        }

        #region Sum

        private InventoryItemInstance Sum(ClientSession clientSession, InventoryItemInstance item, InventoryItemInstance itemToSum)
        {
            if (clientSession.Character.Gold <
                UpgradeHelper.Instance.SumGoldPrice[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade])
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language),
                    SayColorType.Yellow));
                return null;
            }

            if (clientSession.Character.Inventory.CountItem(1027) <
                UpgradeHelper.Instance.SumSandAmount[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade])
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS, clientSession.Account.Language),
                    SayColorType.Yellow));
                return null;
            }

            var random = (short)RandomFactory.Instance.RandomNumber();
            if (random <=
                UpgradeHelper.Instance.SumSuccessPercent[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade])
            {
                HandleSuccessSum(clientSession, item, itemToSum);
            }
            else
            {
                HandleFailedSum(clientSession, item, itemToSum);
            }

            UpdateInv(clientSession, item, itemToSum);
            clientSession.SendPacket(new ShopEndPacket
            {
                Type = ShopEndPacketType.CloseSubWindow
            });

            return item;
        }

        private void HandleSuccessSum(ClientSession clientSession, InventoryItemInstance item,
            InventoryItemInstance itemToSum)
        {
            item.ItemInstance.Upgrade += (byte)(itemToSum.ItemInstance.Upgrade + 1);
            item.ItemInstance.Item.DarkResistance += itemToSum.ItemInstance.Item.DarkResistance;
            item.ItemInstance.Item.LightResistance += itemToSum.ItemInstance.Item.LightResistance;
            item.ItemInstance.Item.FireResistance += itemToSum.ItemInstance.Item.FireResistance;
            item.ItemInstance.Item.WaterResistance += itemToSum.ItemInstance.Item.WaterResistance;

            clientSession.SendPacket(new PdtiPacket
            {
                Unknow = 10,
                ItemVnum = item.ItemInstance.ItemVNum,
                RecipeAmount = 1,
                Unknow3 = 27,
                ItemUpgrade = item.ItemInstance.Upgrade,
                Unknow4 = 0
            });
            SendSumResult(clientSession, item, itemToSum, true);
        }

        private void HandleFailedSum(ClientSession clientSession, InventoryItemInstance item,
            InventoryItemInstance itemToSum)
        {
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, itemToSum.ItemInstanceId);
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, item.ItemInstanceId);
            SendSumResult(clientSession, item, itemToSum, false);
        }

        private void SendSumResult(ClientSession clientSession, InventoryItemInstance item,
            InventoryItemInstance itemToSum, bool success)
        {
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, itemToSum.ItemInstanceId);
            clientSession.SendPacket(new MsgPacket
            {
                Message = Language.Instance.GetMessageFromKey(
                    success ? LanguageKey.SUM_SUCCESS : LanguageKey.SUM_FAILED,
                    clientSession.Account.Language)
            });
            clientSession.SendPacket(clientSession.Character.GenerateSay(
                Language.Instance.GetMessageFromKey(
                    success ? LanguageKey.SUM_SUCCESS : LanguageKey.SUM_FAILED,
                    clientSession.Account.Language),
                success ? SayColorType.Green : SayColorType.Purple));
            clientSession.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.AfterSumming,
                Unknown = 1,
                EntityId = clientSession.Character.VisualId,
                Value = success ? (uint)1324 : 1332
            });
        }

        private void UpdateInv(ClientSession clientSession, InventoryItemInstance item, InventoryItemInstance itemToSum)
        {
            clientSession.Character.Gold -=
                UpgradeHelper.Instance.SumGoldPrice[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade];
            clientSession.SendPacket(clientSession.Character.GenerateGold());

            var invMainReload = new InvPacket
            {
                Type = PocketType.Main,
                IvnSubPackets = new List<IvnSubPacket>()
            };
            List<InventoryItemInstance> removedSand =
                clientSession.Character.Inventory.RemoveItemAmountFromInventoryByVNum(
                    (byte)UpgradeHelper.Instance.SumSandAmount[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade],
                    1027);
            foreach (InventoryItemInstance inventoryItemInstance in removedSand)
            {
                invMainReload.IvnSubPackets.Add(
                    inventoryItemInstance.ItemInstance.GenerateIvnSubPacket(PocketType.Main,
                        inventoryItemInstance.Slot));
            }

            itemToSum.ItemInstance = null;
            var invEquipReload = new InvPacket
            {
                Type = PocketType.Equipment,
                IvnSubPackets = new List<IvnSubPacket>
                {
                    item.ItemInstance.GenerateIvnSubPacket(PocketType.Equipment, item.Slot),
                    itemToSum.ItemInstance.GenerateIvnSubPacket(PocketType.Equipment, itemToSum.Slot)
                }
            };

            clientSession.SendPackets(new List<IPacket> { invEquipReload, invMainReload });
        }

        #endregion
    }
}
